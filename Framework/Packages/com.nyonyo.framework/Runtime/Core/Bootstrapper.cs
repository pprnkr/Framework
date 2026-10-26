using System;
using System.Threading;
using UnityEngine;

namespace Nyonyo.Framework
{
    public enum BootstrapState
    {
        NotStarted,
        Initializing,
        Ready,
        Failed,
    }

    /// <summary>
    /// 첫 씬이 로드되기 전에 자동으로 실행되어 전역 서비스를 등록하고 초기화합니다.
    /// 어떤 씬에서 Play를 시작해도 똑같이 동작합니다.
    /// </summary>
    /// <remarks>
    /// 모든 서비스가 동기적으로 초기화되면 첫 씬의 <c>Awake</c>보다 먼저 준비가 끝납니다.
    /// 비동기 초기화가 있는 경우에는 <see cref="WaitUntilReadyAsync"/>로 준비될 때까지 기다리세요.
    /// </remarks>
    public static class Bootstrapper
    {
        public const string SettingsResourcePath = "NyonyoFrameworkSettings";

        static ServiceRegistry s_registry;

        public static BootstrapState State { get; private set; }

        public static bool IsReady => State == BootstrapState.Ready;

        /// <summary>
        /// 씬이 바뀌어도 유지되는 프레임워크 루트 오브젝트입니다. MonoBehaviour 서비스를 여기에 붙이세요.
        /// </summary>
        public static Transform Root { get; private set; }

        /// <summary>
        /// 서비스 초기화가 끝날 때까지 기다립니다. 초기화가 실패하면 예외를 던집니다.
        /// </summary>
        public static async Awaitable WaitUntilReadyAsync(CancellationToken cancellationToken = default)
        {
            while (State != BootstrapState.Ready)
            {
                if (State == BootstrapState.Failed)
                    throw new InvalidOperationException("서비스 초기화에 실패했습니다. 콘솔의 이전 에러를 확인하세요.");

                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        // Domain Reload를 끈 상태에서도 이전 Play의 정적 상태가 남지 않도록 초기화합니다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Application.quitting -= Shutdown;
            s_registry = null;
            Root = null;
            State = BootstrapState.NotStarted;
            Services.SetRegistry(null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoRun() => _ = RunAsync();

        static async Awaitable RunAsync()
        {
            State = BootstrapState.Initializing;

            var root = new GameObject("[Nyonyo Framework]");
            UnityEngine.Object.DontDestroyOnLoad(root);
            Root = root.transform;

            var registry = new ServiceRegistry();
            s_registry = registry;
            Services.SetRegistry(registry);
            Application.quitting += Shutdown;

            try
            {
                Install(registry);
                await registry.InitializeAsync(Application.exitCancellationToken);
                State = BootstrapState.Ready;
            }
            catch (OperationCanceledException)
            {
                // 초기화 도중 게임이 종료된 경우입니다.
            }
            catch (Exception e)
            {
                State = BootstrapState.Failed;
                Debug.LogException(e);
            }
        }

        static void Install(ServiceRegistry registry)
        {
            var settings = Resources.Load<FrameworkSettings>(SettingsResourcePath);
            if (settings == null)
            {
                Debug.LogWarning(
                    $"[Nyonyo Framework] Resources/{SettingsResourcePath} 설정 파일이 없어서 서비스 없이 시작합니다. " +
                    "메뉴 Tools > Nyonyo Framework > Create Settings로 만들 수 있습니다.");
                return;
            }

            foreach (var installer in settings.Installers)
            {
                if (installer == null)
                {
                    Debug.LogWarning("[Nyonyo Framework] FrameworkSettings에 비어 있는 Installer 항목이 있습니다.", settings);
                    continue;
                }

                installer.Install(registry);
            }
        }

        static void Shutdown()
        {
            Application.quitting -= Shutdown;
            s_registry?.Dispose();
            s_registry = null;
            Services.SetRegistry(null);
            State = BootstrapState.NotStarted;
        }
    }
}
