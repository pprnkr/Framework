using UnityEngine;

namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// RectTransform을 화면의 안전 영역(<see cref="Screen.safeArea"/>)에 맞춥니다. 노치, 펀치홀, 홈 인디케이터에 UI가 가리지 않게 합니다.
    /// </summary>
    /// <remarks>
    /// 화면 전체를 덮는 부모(층 Canvas나 화면 전체로 늘린 팝업 루트) 바로 아래, 버튼과 글자를 담는 오브젝트에 붙이세요.
    /// 배경이나 Dim처럼 화면 끝까지 채워야 하는 것은 이 오브젝트 밖에 둡니다.
    /// 안전 영역이 화면 전체인 PC와 WebGL에서는 화면 전체에 맞춰지므로, 같은 프리팹을 모든 플랫폼에서 그대로 씁니다.
    /// 화면 회전이나 창 크기 변경도 따라갑니다. Play 모드에서 Device Simulator로 기기별 노치를 확인할 수 있습니다.
    /// 편집 중인 프리팹 값을 바꾸지 않도록 Play 중에만 동작합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("위쪽 안전 영역을 적용합니다(노치, 상태 표시줄).")]
        [SerializeField] private bool _top = true;

        [Tooltip("아래쪽 안전 영역을 적용합니다(홈 인디케이터).")]
        [SerializeField] private bool _bottom = true;

        [Tooltip("왼쪽 안전 영역을 적용합니다(가로 화면의 노치).")]
        [SerializeField] private bool _left = true;

        [Tooltip("오른쪽 안전 영역을 적용합니다(가로 화면의 노치).")]
        [SerializeField] private bool _right = true;

        private Rect _appliedSafeArea;
        private Vector2Int _appliedScreenSize;

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.safeArea != _appliedSafeArea || ScreenSize() != _appliedScreenSize)
                Apply();
        }

        private void Apply()
        {
            var screenSize = ScreenSize();
            if (screenSize.x <= 0 || screenSize.y <= 0)
                return;

            var safeArea = Screen.safeArea;
            _appliedSafeArea = safeArea;
            _appliedScreenSize = screenSize;

            var min = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            var max = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);

            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(_left ? min.x : 0f, _bottom ? min.y : 0f);
            rect.anchorMax = new Vector2(_right ? max.x : 1f, _top ? max.y : 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private Vector2Int ScreenSize() => new(Screen.width, Screen.height);
    }
}
