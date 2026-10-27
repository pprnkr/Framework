namespace Nyonyo.Framework.Ads
{
    /// <summary>
    /// 광고 형식입니다.
    /// </summary>
    public enum AdFormat
    {
        /// <summary>화면 위나 아래에 붙어 있는 띠 광고입니다.</summary>
        Banner,

        /// <summary>화면 전체를 덮는 광고입니다. 스테이지 사이처럼 자연스럽게 끊기는 곳에 씁니다.</summary>
        Interstitial,

        /// <summary>끝까지 보면 보상을 주는 전체 화면 광고입니다.</summary>
        Rewarded,

        /// <summary>앱을 열거나 다시 돌아왔을 때 보여 주는 전체 화면 광고입니다.</summary>
        AppOpen,
    }
}
