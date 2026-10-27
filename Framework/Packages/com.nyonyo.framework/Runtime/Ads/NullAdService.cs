using Nyonyo.Framework.Save;
using UnityEngine;

namespace Nyonyo.Framework.Ads
{
    /// <summary>
    /// 광고가 없는 플랫폼(WebGL, PC)용입니다. 보상형 광고는 기본으로 광고를 본 것으로 보고 바로 보상을 주고,
    /// 전면, 앱 오프닝, 배너 광고는 나오지 않습니다(Show는 바로 false).
    /// </summary>
    /// <remarks>
    /// 보상형 동작은 <see cref="AdPolicy.RewardWithoutAdOnUnsupportedPlatform"/>(기본 true)로 정합니다.
    /// 켜져 있으면 <see cref="IAdService.IsRewardedReady"/>는 true이고, <c>ShowRewarded</c>는 바로 보상 콜백을 부릅니다.
    /// 끄면 보상형도 항상 준비되지 않은 상태가 됩니다.
    /// 광고 열림/닫힘 이벤트도 바로 이어서 발행되므로, 광고 시청 횟수를 분석에 남긴다면 이 플랫폼은 따로 구분하세요.
    /// 광고 제거 여부는 그대로 저장되므로, 플랫폼마다 구매 상태를 따로 관리할 필요가 없습니다.
    /// </remarks>
    public sealed class NullAdService : AdServiceBase
    {
        public NullAdService(AdPolicy policy = null, SaveService save = null)
            : base(policy, save, null)
        {
            MarkInitialized();
        }

        protected override bool IsLoaded(AdFormat format) =>
            format == AdFormat.Rewarded && Policy.RewardWithoutAdOnUnsupportedPlatform;

        protected override Awaitable<AdShowResult> PresentAsync(AdFormat format)
        {
            var result = IsLoaded(format) ? AdShowResult.Rewarded : AdShowResult.Failed;
            var source = new AwaitableCompletionSource<AdShowResult>();
            source.SetResult(result);
            return source.Awaitable;
        }

        protected override void ShowBannerView(BannerPosition position)
        {
        }

        protected override void HideBannerView()
        {
        }

        protected override float GetBannerHeight() => 0f;
    }
}
