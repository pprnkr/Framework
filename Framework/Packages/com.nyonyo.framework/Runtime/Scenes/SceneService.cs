using System;
using System.Threading;
using Nyonyo.Framework.Events;
using UnityEngine;

namespace Nyonyo.Framework.Scenes
{
    /// <summary>
    /// 기본 <see cref="ISceneService"/> 구현입니다.
    /// <see cref="SceneLoadStarted"/> 발행 → 화면 가리기 → 씬 로드 → <see cref="SceneLoaded"/> 발행 → 화면 보이기 순서로 진행합니다.
    /// </summary>
    /// <remarks>
    /// <see cref="SceneLoaded"/>를 받는 시점에는 아직 <see cref="IsLoading"/>이 true입니다.
    /// 그래서 그 핸들러 안에서 바로 다른 씬을 불러오면 요청이 무시됩니다.
    /// </remarks>
    public sealed class SceneService : ISceneService, IDisposable
    {
        private readonly ISceneTransition _transition;
        private readonly IEventBus _eventBus;
        private readonly ISceneLoader _loader;
        private readonly Action<float> _reportProgress;
        private readonly CancellationTokenSource _disposeCts = new();

        /// <param name="transition">전환 연출입니다. null이면 연출 없이 바로 전환합니다.</param>
        /// <param name="eventBus">씬 전환 이벤트를 발행할 버스입니다. null이면 이벤트를 발행하지 않습니다.</param>
        /// <param name="loader">씬을 불러오는 방식입니다. null이면 <see cref="SceneManagerLoader"/>를 씁니다.</param>
        public SceneService(ISceneTransition transition = null, IEventBus eventBus = null, ISceneLoader loader = null)
        {
            _transition = transition;
            _eventBus = eventBus;
            _loader = loader ?? new SceneManagerLoader();
            _reportProgress = ReportProgress;
        }

        public bool IsLoading { get; private set; }

        public float Progress { get; private set; }

        /// <summary>
        /// 로드가 너무 빨리 끝나 화면이 번쩍이지 않도록, 화면을 가린 상태를 최소한 이 시간(초)만큼 유지합니다.
        /// </summary>
        public float MinimumLoadingSeconds { get; set; }

        public void Load(string sceneName) => LoadAsync(sceneName).Forget();

        public async Awaitable<bool> LoadAsync(string sceneName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sceneName))
                throw new ArgumentException("씬 이름이 비어 있습니다.", nameof(sceneName));
            if (_disposeCts.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(SceneService));
            if (IsLoading)
            {
                Debug.LogWarning($"[Nyonyo Framework] 이미 씬을 전환하는 중이라 {sceneName} 로드 요청을 무시합니다.");
                return false;
            }

            IsLoading = true;
            Progress = 0f;

            // 서비스가 정리되면(게임 종료, Play 모드 종료) 진행 중인 전환도 멈춥니다.
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);
            var token = linkedCts.Token;
            try
            {
                _eventBus?.Publish(new SceneLoadStarted(sceneName));
                if (_transition != null)
                    await _transition.ShowAsync(token);

                var startTime = Time.realtimeSinceStartup;
                await _loader.LoadAsync(sceneName, _reportProgress, token);
                ReportProgress(1f);

                while (Time.realtimeSinceStartup - startTime < MinimumLoadingSeconds)
                    await Awaitable.NextFrameAsync(token);

                _eventBus?.Publish(new SceneLoaded(sceneName));
                if (_transition != null)
                    await _transition.HideAsync(token);

                return true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void Dispose() => _disposeCts.Cancel();

        private void ReportProgress(float progress)
        {
            Progress = progress;
            _transition?.SetProgress(progress);
        }
    }
}
