using Nyonyo.Framework;
using Nyonyo.Framework.Assets;
using Nyonyo.Framework.Audio;
using Nyonyo.Framework.Events;
using Nyonyo.Framework.Save;
using Nyonyo.Framework.Scenes;
using Nyonyo.Framework.UI;
using UnityEngine;

namespace Nyonyo.Dev
{
    /// <summary>
    /// 프레임워크 개발 프로젝트에서 기능을 직접 확인하기 위한 Installer입니다. 패키지에는 포함되지 않습니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Nyonyo Dev/Dev Installer", fileName = "DevInstaller")]
    public class DevInstaller : GameInstaller
    {
        [SerializeField] UISettings _uiSettings;
        [SerializeField] SoundSettings _soundSettings;

        public override void Install(ServiceRegistry services)
        {
            var events = new EventBus();
            services.Register<IEventBus>(events);

            var assets = new AssetLoader();
            services.Register<IAssetLoader>(assets);

            var save = new SaveService();
            services.Register(save);

            services.Register<ISceneService>(
                new SceneService(new FadeTransition(Bootstrapper.Root), events) { MinimumLoadingSeconds = 0.3f });
            services.Register(new PopupManager(_uiSettings, Bootstrapper.Root, events, assets));
            services.Register(new SoundManager(_soundSettings, Bootstrapper.Root, assets, save));

            Bootstrapper.Root.gameObject.AddComponent<DevSceneSwitcher>();
        }
    }
}
