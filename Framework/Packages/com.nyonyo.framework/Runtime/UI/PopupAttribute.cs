using System;
using Nyonyo.Framework.Assets;
using UnityEngine.Scripting;

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
    /// <remarks>
    /// PopupManager가 이 어트리뷰트를 리플렉션으로 읽으므로, IL2CPP 빌드(Android, iOS, WebGL)의 코드 제거에서 어트리뷰트 정보가 지워지지 않도록
    /// <see cref="RequireAttributeUsagesAttribute"/>를 붙였습니다.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [RequireAttributeUsages]
    public sealed class PopupAttribute : Attribute
    {
        /// <summary>
        /// Addressables면 address(기본값은 에셋 경로), Resources면 Resources 폴더 기준 경로(확장자 제외)입니다.
        /// </summary>
        public string Path { get; set; }

        public LoadType LoadType { get; set; } = LoadType.Addressables;

        /// <summary>
        /// true면 게임 시작 시(부트스트랩 초기화 단계) 프리팹을 미리 불러 둡니다.
        /// WebGL은 Addressables를 동기로 불러올 수 없으므로, WebGL에서 <c>Open</c>으로 바로 열 Addressables 팝업은 true로 두세요.
        /// </summary>
        public bool Preload { get; set; }
    }
}
