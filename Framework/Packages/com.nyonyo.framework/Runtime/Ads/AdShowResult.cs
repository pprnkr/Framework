namespace Nyonyo.Framework.Ads
{
    /// <summary>
    /// 전체 화면 광고를 띄운 결과입니다. <see cref="AdServiceBase"/>를 상속해 광고 네트워크를 붙일 때 씁니다.
    /// </summary>
    public enum AdShowResult
    {
        /// <summary>광고를 띄우지 못했습니다.</summary>
        Failed,

        /// <summary>광고를 보여 주고 닫혔습니다. 보상형이면 보상 조건을 채우지 못한 경우입니다.</summary>
        Closed,

        /// <summary>보상형 광고를 끝까지 보고 닫혔습니다.</summary>
        Rewarded,
    }
}
