namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// UI가 그려지는 층입니다. 값이 클수록 위에 그려지고, 같은 층에서는 나중에 띄운 것이 위에 그려집니다.
    /// </summary>
    public enum UILayer
    {
        /// <summary>메인 메뉴, HUD처럼 화면 전체를 차지하는 UI</summary>
        Screen = 0,

        /// <summary>일반 팝업</summary>
        Popup = 1,

        /// <summary>확인창, 네트워크 오류처럼 일반 팝업보다 우선하는 시스템 팝업</summary>
        System = 2,

        /// <summary>띠알람, 토스트 같은 알림. 팝업이 여러 개 떠 있어도 그 위에 보입니다.</summary>
        Notice = 3,

        /// <summary>로딩 인디케이터, 디버그 UI처럼 항상 맨 위에 있어야 하는 UI</summary>
        Top = 4,
    }
}
