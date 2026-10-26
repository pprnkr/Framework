using UnityEngine;

namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// <see cref="PopupManager"/> 설정입니다. 화면 해상도 기준과 뒤로가기 처리를 정합니다.
    /// 팝업 프리팹 경로는 각 팝업 클래스의 <see cref="PopupAttribute"/>로 정합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Nyonyo Framework/UI Settings", fileName = "UISettings")]
    public sealed class UISettings : ScriptableObject
    {
        [Header("Canvas")]
        [Tooltip("UI를 디자인한 기준 해상도입니다. 세로 게임이라면 1080x1920처럼 지정하세요.")]
        [SerializeField] Vector2 _referenceResolution = new(1920, 1080);

        [Tooltip("0이면 가로 폭, 1이면 세로 높이에 맞춰 UI 크기를 조절합니다.")]
        [Range(0f, 1f)]
        [SerializeField] float _matchWidthOrHeight = 0.5f;

        [Header("Input")]
        [Tooltip("뒤로가기 키(ESC, 안드로이드 뒤로가기)로 맨 위 팝업을 닫습니다.")]
        [SerializeField] bool _handleBackKey = true;

        public Vector2 ReferenceResolution => _referenceResolution;

        public float MatchWidthOrHeight => _matchWidthOrHeight;

        public bool HandleBackKey => _handleBackKey;
    }
}
