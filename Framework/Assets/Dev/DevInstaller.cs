using Nyonyo.Framework;
using Nyonyo.Framework.Assets;
using Nyonyo.Framework.Audio;
using Nyonyo.Framework.Debugging;
using Nyonyo.Framework.Events;
using Nyonyo.Framework.Inputs;
using Nyonyo.Framework.Pooling;
using Nyonyo.Framework.Save;
using Nyonyo.Framework.Scenes;
using Nyonyo.Framework.Timing;
using Nyonyo.Framework.UI;
using UnityEngine;

namespace Nyonyo.Dev
{
    /// <summary>
    /// 프레임워크 개발 프로젝트에서 기능을 직접 확인하기 위한 Installer입니다. 패키지에는 포함되지 않습니다.
    /// 게임 Installer를 만들 때 등록 순서의 예시로도 쓸 수 있습니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Nyonyo Dev/Dev Installer", fileName = "DevInstaller")]
    public class DevInstaller : GameInstaller
    {
        [SerializeField] private UISettings _uiSettings;
        [SerializeField] private SoundSettings _soundSettings;

        public override void Install(ServiceRegistry services)
        {
            // 다른 서비스가 쓰는 기반 서비스를 먼저 등록합니다(정리는 역순이라 가장 나중에 정리됨).
            var events = new EventBus();
            services.Register<IEventBus>(events);

            var assets = new AssetLoader();
            services.Register<IAssetLoader>(assets);

            var save = new SaveService();
            services.Register(save);

            services.Register(new PauseService(events));
            services.Register(new TimerService(Bootstrapper.Root));
            services.Register(new PoolManager(Bootstrapper.Root));
            services.Register(new InputService(save: save, initialMap: "Player"));

            services.Register<ISceneService>(
                new SceneService(new FadeTransition(Bootstrapper.Root), events) { MinimumLoadingSeconds = 0.3f });
            services.Register(new PopupManager(_uiSettings, Bootstrapper.Root, events, assets));
            services.Register(new SoundManager(_soundSettings, Bootstrapper.Root, assets, save));
            services.Register(new DebugConsole(Bootstrapper.Root));

            Bootstrapper.Root.gameObject.AddComponent<DevSceneSwitcher>();
        }
    }
}
