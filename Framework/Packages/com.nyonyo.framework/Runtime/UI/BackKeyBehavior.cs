namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// 뒤로가기 키(ESC, 안드로이드 뒤로가기)를 눌렀을 때 팝업이 어떻게 반응할지 정합니다.
    /// </summary>
    public enum BackKeyBehavior
    {
        /// <summary>이 팝업을 닫습니다.</summary>
        Close,

        /// <summary>닫지 않고, 아래 팝업도 닫히지 않게 막습니다. 반드시 버튼으로 답해야 하는 팝업에 씁니다.</summary>
        Block,

        /// <summary>이 팝업은 무시하고 아래 팝업에 넘깁니다. 띠알람처럼 잠깐 떴다 사라지는 UI에 씁니다.</summary>
        Ignore,
    }
}
