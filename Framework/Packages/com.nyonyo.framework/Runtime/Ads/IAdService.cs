using System;
using UnityEngine;

namespace Nyonyo.Framework.Ads
{
    /// <summary>
    /// 광고 서비스입니다. 게임 코드는 이 인터페이스만 쓰고, 실제 광고 네트워크(AdMob 등)는 Installer에서 고릅니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 광고는 불러오기에 시간이 걸리고 실패할 수도 있습니다. 그래서 모든 Show는 "보여 줄 광고가 없으면 아무것도 하지 않음"으로 동작하고,
    /// 결과를 bool로 알려 줍니다. 광고가 없어도 게임이 막히지 않게 짜세요.
    /// </para>
    /// <para>광고 SDK 초기화와 동의(GDPR) 처리는 서비스가 알아서 합니다. 끝나기 전에는 광고가 준비되지 않은 것으로 취급합니다.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var ads = Services.Get&lt;IAdService&gt;();
    /// ads.ShowBanner();
    /// ads.ShowRewarded(onRewarded: () =&gt; coins += 100);
    /// if (await ads.ShowRewardedAsync()) coins += 100;
    /// ads.ShowInterstitial(onClosed: LoadNextStage);
    /// </code>
    /// </example>
    public interface IAdService
    {
        /// <summary>광고 SDK 초기화가 끝나면 한 번 호출합니다. 이미 끝났는지는 <see cref="IsInitialized"/>로 확인하세요.</summary>
        event Action Initialized;

        /// <summary>전체 화면 광고를 띄우기 직전에 호출합니다. 게임 소리를 끄는 등에 씁니다.</summary>
        event Action<AdFormat> FullScreenAdOpened;

        /// <summary>전체 화면 광고가 닫힌 뒤(띄우기에 실패한 경우 포함) 호출합니다.</summary>
        event Action<AdFormat> FullScreenAdClosed;

        /// <summary>광고 수익이 생겼을 때 호출합니다. 분석 도구에 광고 수익을 보낼 때 씁니다.</summary>
        event Action<AdRevenue> RevenuePaid;

        bool IsInitialized { get; }

        #region 동의 (GDPR)

        /// <summary>광고를 요청해도 되는 동의 상태면 true입니다. 동의가 필요 없는 지역이면 항상 true입니다.</summary>
        bool CanRequestAds { get; }

        /// <summary>
        /// 설정 화면에 "개인정보 설정" 버튼을 보여야 하면 true입니다. 동의를 받은 사용자가 다시 바꿀 수 있도록 Google 정책상 필요합니다.
        /// </summary>
        bool IsPrivacyOptionsRequired { get; }

        /// <summary>개인정보 설정(동의 변경) 화면을 띄우고, 닫힐 때까지 기다립니다.</summary>
        Awaitable ShowPrivacyOptionsAsync();

        #endregion

        #region 광고 제거

        /// <summary>
        /// 광고 제거 상품을 샀으면 true로 설정하세요. 배너, 전면, 앱 오프닝 광고가 나오지 않고 보상형 광고만 남습니다.
        /// SaveService를 넘겼으면 자동으로 저장하고 다음 실행 때 불러옵니다.
        /// </summary>
        bool AdsRemoved { get; set; }

        #endregion

        #region 배너

        /// <summary>배너가 실제로 화면에 보이면 true입니다. 불러오는 중이거나 광고가 없으면 false입니다.</summary>
        bool IsBannerVisible { get; }

        /// <summary>배너 높이(화면 픽셀)입니다. 보이지 않으면 0입니다. 배너에 가려지지 않게 UI를 올릴 때 씁니다.</summary>
        float BannerHeight { get; }

        /// <summary>배너를 보여 줍니다. 초기화 전에 부르면 초기화가 끝난 뒤 보여 줍니다.</summary>
        void ShowBanner(BannerPosition position = BannerPosition.Bottom);

        void HideBanner();

        #endregion

        #region 전체 화면 광고

        /// <summary>지금 전면 광고를 보여 줄 수 있으면 true입니다(불러옴, 대기 시간 지남, 광고 제거 안 함).</summary>
        bool IsInterstitialReady { get; }

        bool IsRewardedReady { get; }

        bool IsAppOpenReady { get; }

        /// <summary>전면 광고를 보여 주고 닫힐 때까지 기다립니다. 보여 줬으면 true, 보여 줄 수 없었으면 바로 false입니다.</summary>
        Awaitable<bool> ShowInterstitialAsync();

        /// <summary>보상형 광고를 보여 주고 닫힐 때까지 기다립니다. 보상을 받을 조건을 채웠으면 true입니다.</summary>
        Awaitable<bool> ShowRewardedAsync();

        /// <summary>앱 오프닝 광고를 보여 주고 닫힐 때까지 기다립니다. 보여 줬으면 true입니다.</summary>
        Awaitable<bool> ShowAppOpenAsync();

        /// <summary>
        /// 다음 한 번은 앱으로 돌아와도 앱 오프닝 광고를 띄우지 않습니다. 결제, 공유, 로그인처럼 잠깐 다른 화면에 다녀올 때 그 직전에 부르세요.
        /// </summary>
        void SkipNextAppOpen();

        #endregion
    }
}
