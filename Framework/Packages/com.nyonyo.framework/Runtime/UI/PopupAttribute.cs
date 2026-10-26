using System;
using Nyonyo.Framework.Assets;

namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// 팝업 프리팹을 어디서 어떻게 불러올지 정합니다. <see cref="PopupManager"/>는 이 값으로 프리팹을 불러옵니다.
    /// </summary>
    /// <example>
    /// <code>
    /// [Popup(Path = "Assets/Popups/PopupTest.prefab", LoadType = LoadType.Addressables)]
    /// public class PopupTest : PopupBase { }
    ///
    /// // 자주 쓰는 팝업은 게임 시작 때 미리 불러 둡니다(WebGL에서 Show로 바로 띄우려면 필요).
    /// [Popup(Path = "Assets/Popups/PopupConfirm.prefab", LoadType = LoadType.Addressables, Preload = true)]
    /// public class PopupConfirm : PopupBase { }
    ///
    /// // 게임 시작에 꼭 필요한 팝업은 Resources에 둡니다(경로는 Resources 폴더 기준, 확장자 제외).
    /// [Popup(Path = "Popups/Loading", LoadType = LoadType.Resources)]
    /// public class PopupLoading : PopupBase { }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class PopupAttribute : Attribute
    {
        /// <summary>
        /// Addressables면 address(기본값은 에셋 경로), Resources면 Resources 폴더 기준 경로(확장자 제외)입니다.
        /// </summary>
        public string Path { get; set; }

        public LoadType LoadType { get; set; } = LoadType.Addressables;

        /// <summary>
        /// true면 게임 시작 시(부트스트랩 초기화 단계) 프리팹을 미리 불러 둡니다.
        /// WebGL은 Addressables를 동기로 불러올 수 없으므로, WebGL에서 <c>Show</c>로 바로 띄울 Addressables 팝업은 true로 두세요.
        /// </summary>
        public bool Preload { get; set; }
    }
}
