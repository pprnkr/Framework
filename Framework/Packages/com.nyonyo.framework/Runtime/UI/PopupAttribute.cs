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
    }
}
