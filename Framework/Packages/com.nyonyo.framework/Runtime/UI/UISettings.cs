using UnityEngine;

namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// <see cref="PopupManager"/> 설정입니다. 화면 방향, 해상도 기준, 뒤로가기 처리를 정합니다.
    /// 팝업 프리팹 경로는 각 팝업 클래스의 <see cref="PopupAttribute"/>로 정합니다.
    /// </summary>
    public sealed class UISettings : ScriptableObject
    {
        /// <summary>
        /// 기본 기준 해상도입니다. 프레임워크는 세로형 게임을 기본으로 봅니다.
        /// </summary>
        public static readonly Vector2 DefaultReferenceResolution = new(1080, 1920);

        [Header("Screen")]
        [Tooltip("게임 화면 방향입니다. 기준 해상도의 가로·세로 순서를 이 값에 맞춥니다.")]
        [SerializeField] private GameOrientation _orientation = GameOrientation.Portrait;

        [Header("Canvas")]
        [Tooltip("UI를 디자인한 기준 해상도입니다. 가로·세로 순서는 Orientation에 맞춰 자동으로 정렬됩니다(세로형이면 1080x1920, 가로형이면 1920x1080).")]
        [SerializeField] private Vector2 _referenceResolution = DefaultReferenceResolution;

        [Header("Input")]
        [Tooltip("뒤로가기 키(ESC, 안드로이드 뒤로가기)로 맨 위 팝업을 닫습니다.")]
        [SerializeField] private bool _handleBackKey = true;

        public GameOrientation Orientation => _orientation;

        /// <summary>
        /// <see cref="Orientation"/>에 맞춰 가로·세로 순서를 정렬한 기준 해상도입니다.
        /// </summary>
        public Vector2 ReferenceResolution => Orient(_referenceResolution, _orientation);

        public bool HandleBackKey => _handleBackKey;

        /// <summary>
        /// 해상도의 가로·세로 순서를 방향에 맞춥니다. 세로형이면 짧은 변이 x, 가로형이면 긴 변이 x가 됩니다.
        /// </summary>
        private Vector2 Orient(Vector2 resolution, GameOrientation orientation)
        {
            var shortSide = Mathf.Min(resolution.x, resolution.y);
            var longSide = Mathf.Max(resolution.x, resolution.y);
            return orientation == GameOrientation.Portrait
                ? new Vector2(shortSide, longSide)
                : new Vector2(longSide, shortSide);
        }
    }
}
