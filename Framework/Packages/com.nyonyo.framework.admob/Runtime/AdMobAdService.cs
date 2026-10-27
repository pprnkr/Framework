using System;
using System.Collections.Generic;
using System.Threading;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using Nyonyo.Framework.Save;
using UnityEngine;

namespace Nyonyo.Framework.Ads.AdMob
{
    /// <summary>
    /// Google AdMob으로 구현한 광고 서비스입니다. 보통 <see cref="AdMobSettings.CreateService"/>로 만듭니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 서비스가 초기화되면 동의(UMP) 확인 → 필요하면 동의 창 → AdMob SDK 초기화 → 전면, 보상형, 앱 오프닝 광고 미리 불러오기 순서로 진행합니다.
    /// 게임 시작을 막지 않고 뒤에서 진행하며, 끝나면 <see cref="IAdService.Initialized"/>가 발행됩니다.
    /// </para>
    /// <para>불러오기에 실패하면 2, 4, 8 ... 최대 64초 간격으로 다시 시도합니다. 광고를 보여 준 뒤에는 다음 광고를 바로 불러 둡니다.</para>
    /// <para>SDK 콜백은 다른 스레드에서 올 수 있어서 모두 메인 스레드로 옮겨 처리합니다.</para>
    /// </remarks>
    public sealed class AdMobAdService : AdServiceBase, IInitializable
    {
        private const float MaxRetrySeconds = 64f;

        // AdMob 정책상 앱 오프닝 광고는 불러온 뒤 4시간이 지나면 보여 주지 않습니다.
        private static readonly TimeSpan s_appOpenLifetime = TimeSpan.FromHours(4);

        private readonly AdMobSettings _settings;
        private readonly CancellationTokenSource _cts = new();
        private readonly Dictionary<AdFormat, int> _retryCounts = new();
        private InterstitialAd _interstitial;
        private RewardedAd _rewarded;
        private AppOpenAd _appOpen;
        private DateTime _appOpenExpiresAt;
        private BannerView _banner;
        private BannerPosition _bannerViewPosition;
        private bool _bannerLoaded;
        private bool _bannerHidden;
        private bool _sdkStarting;

        public AdMobAdService(AdMobSettings settings, SaveService save = null)
            : base(settings != null ? settings.Policy : null, save, null)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "AdMobSettings가 없습니다. Tools > Nyonyo Framework > Settings > AdMob Settings로 만들어 Installer에 연결하세요.");

            _settings = settings;
        }

        public override bool CanRequestAds => !_settings.RequestConsent || ConsentInformation.CanRequestAds();

        public override bool IsPrivacyOptionsRequired =>
            _settings.RequestConsent && ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public void Initialize() => RunAsync(StartAsync());

        public override async Awaitable ShowPrivacyOptionsAsync()
        {
            var source = new AwaitableCompletionSource();
            ConsentForm.ShowPrivacyOptionsForm(error => RunOnMainThread(() =>
            {
                if (error != null)
                    Debug.LogWarning($"[Nyonyo Framework] 개인정보 설정 창을 띄우지 못했습니다: {error.Message}");
                source.TrySetResult();
            }));
            await source.Awaitable;

            // 동의를 거부했다가 다시 허용한 경우에는 이제 광고를 시작할 수 있습니다.
            if (!IsInitialized && CanRequestAds)
                await StartSdkAsync();
        }

        public override void Dispose()
        {
            _cts.Cancel();
            AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;

            _interstitial?.Destroy();
            _interstitial = null;
            _rewarded?.Destroy();
            _rewarded = null;
            _appOpen?.Destroy();
            _appOpen = null;
            DestroyBanner();
        }

        #region 시작

        private async Awaitable StartAsync()
        {
            MobileAds.SetiOSAppPauseOnBackground(true);
            MobileAds.SetRequestConfiguration(new RequestConfiguration { TestDeviceIds = new List<string>(_settings.TestDeviceIds) });

            if (_settings.RequestConsent)
                await GatherConsentAsync();

            if (!CanRequestAds)
            {
                Debug.Log("[Nyonyo Framework] 광고 동의를 받지 못해 광고를 시작하지 않습니다. 개인정보 설정에서 동의하면 시작합니다.");
                return;
            }

            await StartSdkAsync();
        }

        private Awaitable GatherConsentAsync()
        {
            var source = new AwaitableCompletionSource();
            var parameters = new ConsentRequestParameters
            {
                ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = _settings.ConsentDebugGeography,
                    TestDeviceHashedIds = new List<string>(_settings.TestDeviceIds),
                },
            };

            ConsentInformation.Update(parameters, updateError => RunOnMainThread(() =>
            {
                // 동의 정보를 갱신하지 못해도 이전 실행에서 받은 동의가 있으면 광고를 쓸 수 있습니다.
                if (updateError != null)
                {
                    Debug.LogWarning($"[Nyonyo Framework] 광고 동의 정보를 갱신하지 못했습니다: {updateError.Message}");
                    source.TrySetResult();
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError => RunOnMainThread(() =>
                {
                    if (formError != null)
                        Debug.LogWarning($"[Nyonyo Framework] 광고 동의 창을 띄우지 못했습니다: {formError.Message}");
                    source.TrySetResult();
                }));
            }));

            return source.Awaitable;
        }

        private async Awaitable StartSdkAsync()
        {
            if (_sdkStarting || IsInitialized)
                return;

            _sdkStarting = true;
            var source = new AwaitableCompletionSource<bool>();
            MobileAds.Initialize(status => RunOnMainThread(() => source.TrySetResult(status != null)));
            var succeeded = await source.Awaitable;
            _sdkStarting = false;

            if (_cts.IsCancellationRequested)
                return;
            if (!succeeded)
            {
                Debug.LogError("[Nyonyo Framework] AdMob SDK를 초기화하지 못했습니다. 앱 ID 설정(Assets > Google Mobile Ads > Settings...)을 확인하세요.");
                return;
            }

            AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
            LoadInterstitial();
            LoadRewarded();
            LoadAppOpen();
            MarkInitialized();
        }

        private void OnAppStateChanged(AppState state)
        {
            if (state == AppState.Foreground)
                RunOnMainThread(OnAppForeground);
        }

        #endregion

        #region 불러오기

        private void LoadInterstitial()
        {
            var adUnitId = GetAdUnitIdOrNull(AdFormat.Interstitial);
            if (adUnitId == null)
                return;

            InterstitialAd.Load(adUnitId, new AdRequest(), (ad, error) => RunOnMainThread(() =>
            {
                if (!Accept(AdFormat.Interstitial, ad, error, LoadInterstitial, () => ad.Destroy()))
                    return;

                _interstitial = ad;
                ad.OnAdPaid += value => RunOnMainThread(() => RaiseRevenuePaid(ToRevenue(AdFormat.Interstitial, adUnitId, value)));
            }));
        }

        private void LoadRewarded()
        {
            var adUnitId = GetAdUnitIdOrNull(AdFormat.Rewarded);
            if (adUnitId == null)
                return;

            RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) => RunOnMainThread(() =>
            {
                if (!Accept(AdFormat.Rewarded, ad, error, LoadRewarded, () => ad.Destroy()))
                    return;

                _rewarded = ad;
                ad.OnAdPaid += value => RunOnMainThread(() => RaiseRevenuePaid(ToRevenue(AdFormat.Rewarded, adUnitId, value)));
            }));
        }

        private void LoadAppOpen()
        {
            var adUnitId = GetAdUnitIdOrNull(AdFormat.AppOpen);
            if (adUnitId == null)
                return;

            AppOpenAd.Load(adUnitId, new AdRequest(), (ad, error) => RunOnMainThread(() =>
            {
                if (!Accept(AdFormat.AppOpen, ad, error, LoadAppOpen, () => ad.Destroy()))
                    return;

                _appOpen = ad;
                _appOpenExpiresAt = DateTime.UtcNow + s_appOpenLifetime;
                ad.OnAdPaid += value => RunOnMainThread(() => RaiseRevenuePaid(ToRevenue(AdFormat.AppOpen, adUnitId, value)));
            }));
        }

        // 불러오기 결과를 확인합니다. 실패면 다시 시도를 예약하고 false입니다.
        private bool Accept(AdFormat format, object ad, LoadAdError error, Action reload, Action destroy)
        {
            if (_cts.IsCancellationRequested)
            {
                if (ad != null)
                    destroy();
                return false;
            }

            if (error != null || ad == null)
            {
                RetryLater(format, reload, error != null ? error.GetMessage() : "광고가 비어 있습니다.");
                return false;
            }

            _retryCounts[format] = 0;
            return true;
        }

        private void RetryLater(AdFormat format, Action reload, string reason)
        {
            _retryCounts.TryGetValue(format, out var attempt);
            attempt++;
            _retryCounts[format] = attempt;

            var delay = Mathf.Min(MaxRetrySeconds, Mathf.Pow(2f, attempt));
            Debug.LogWarning($"[Nyonyo Framework] AdMob {format} 광고를 불러오지 못했습니다: {reason} {delay}초 뒤 다시 시도합니다.");
            RunAsync(ReloadAfterAsync(delay, reload));
        }

        private async Awaitable ReloadAfterAsync(float seconds, Action reload)
        {
            // 일시정지(timeScale 0) 중에도 다시 시도하도록 실제 시간으로 기다립니다.
            var until = Time.realtimeSinceStartupAsDouble + seconds;
            while (Time.realtimeSinceStartupAsDouble < until)
                await Awaitable.NextFrameAsync(_cts.Token);

            reload();
        }

        private string GetAdUnitIdOrNull(AdFormat format)
        {
            if (_cts.IsCancellationRequested)
                return null;

            var adUnitId = _settings.GetAdUnitId(format);
            return string.IsNullOrWhiteSpace(adUnitId) ? null : adUnitId.Trim();
        }

        #endregion

        #region 전체 화면 광고

        protected override bool IsLoaded(AdFormat format)
        {
            if (format == AdFormat.Interstitial)
                return _interstitial != null && _interstitial.CanShowAd();
            if (format == AdFormat.Rewarded)
                return _rewarded != null && _rewarded.CanShowAd();
            if (format != AdFormat.AppOpen || _appOpen == null)
                return false;

            if (DateTime.UtcNow < _appOpenExpiresAt)
                return _appOpen.CanShowAd();

            // 너무 오래된 앱 오프닝 광고는 버리고 새로 불러옵니다.
            _appOpen.Destroy();
            _appOpen = null;
            LoadAppOpen();
            return false;
        }

        protected override Awaitable<AdShowResult> PresentAsync(AdFormat format)
        {
            var source = new AwaitableCompletionSource<AdShowResult>();

            if (format == AdFormat.Interstitial)
            {
                var ad = _interstitial;
                _interstitial = null;
                ad.OnAdFullScreenContentClosed += () => RunOnMainThread(() => Finish(source, AdShowResult.Closed, ad.Destroy, LoadInterstitial));
                ad.OnAdFullScreenContentFailed += error => RunOnMainThread(() => Fail(format, error, source, ad.Destroy, LoadInterstitial));
                ad.Show();
            }
            else if (format == AdFormat.Rewarded)
            {
                var ad = _rewarded;
                _rewarded = null;
                var earned = false;
                ad.OnAdFullScreenContentClosed += () => RunOnMainThread(() =>
                    Finish(source, earned ? AdShowResult.Rewarded : AdShowResult.Closed, ad.Destroy, LoadRewarded));
                ad.OnAdFullScreenContentFailed += error => RunOnMainThread(() => Fail(format, error, source, ad.Destroy, LoadRewarded));
                ad.Show(reward => RunOnMainThread(() => earned = true));
            }
            else
            {
                var ad = _appOpen;
                _appOpen = null;
                ad.OnAdFullScreenContentClosed += () => RunOnMainThread(() => Finish(source, AdShowResult.Closed, ad.Destroy, LoadAppOpen));
                ad.OnAdFullScreenContentFailed += error => RunOnMainThread(() => Fail(format, error, source, ad.Destroy, LoadAppOpen));
                ad.Show();
            }

            return source.Awaitable;
        }

        private void Fail(AdFormat format, AdError error, AwaitableCompletionSource<AdShowResult> source, Action destroy, Action reload)
        {
            Debug.LogWarning($"[Nyonyo Framework] AdMob {format} 광고를 띄우지 못했습니다: {error?.GetMessage()}");
            Finish(source, AdShowResult.Failed, destroy, reload);
        }

        // 보상 콜백이 닫힘 콜백보다 늦게 오는 기기가 있어서, 한 프레임 기다린 뒤 결과를 정합니다.
        // 보상형이 아닌 광고도 같은 순서로 처리해 흐름을 하나로 맞춥니다.
        private void Finish(AwaitableCompletionSource<AdShowResult> source, AdShowResult result, Action destroy, Action reload)
        {
            destroy();
            RunAsync(FinishNextFrameAsync(source, result, reload));
        }

        private async Awaitable FinishNextFrameAsync(AwaitableCompletionSource<AdShowResult> source, AdShowResult result, Action reload)
        {
            await Awaitable.NextFrameAsync(_cts.Token);
            source.TrySetResult(result);
            reload();
        }

        #endregion

        #region 배너

        protected override void ShowBannerView(BannerPosition position)
        {
            if (_banner != null && _bannerViewPosition == position)
            {
                _bannerHidden = false;
                _banner.Show();
                return;
            }

            var adUnitId = GetAdUnitIdOrNull(AdFormat.Banner);
            if (adUnitId == null)
            {
                Debug.LogWarning("[Nyonyo Framework] 배너 광고 단위 ID가 비어 있어 배너를 띄우지 않습니다. AdMobSettings를 확인하세요.");
                return;
            }

            DestroyBanner();
            var size = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            var banner = new BannerView(adUnitId, size, position == BannerPosition.Top ? AdPosition.Top : AdPosition.Bottom);
            _banner = banner;
            _bannerViewPosition = position;
            _bannerHidden = false;

            banner.OnBannerAdLoaded += () => RunOnMainThread(() =>
            {
                if (_banner != banner)
                    return;

                _bannerLoaded = true;
                _retryCounts[AdFormat.Banner] = 0;
                // 숨긴 뒤에 불러오기가 끝나도 보이지 않게 합니다.
                if (_bannerHidden)
                    banner.Hide();
            });
            banner.OnBannerAdLoadFailed += error => RunOnMainThread(() =>
            {
                if (_banner == banner)
                    RetryLater(AdFormat.Banner, () => ReloadBanner(banner), error?.GetMessage());
            });
            banner.OnAdPaid += value => RunOnMainThread(() => RaiseRevenuePaid(ToRevenue(AdFormat.Banner, adUnitId, value)));
            banner.LoadAd(new AdRequest());
        }

        protected override void HideBannerView()
        {
            _bannerHidden = true;
            _banner?.Hide();
        }

        protected override float GetBannerHeight() =>
            _banner != null && _bannerLoaded && !_bannerHidden ? _banner.GetHeightInPixels() : 0f;

        private void ReloadBanner(BannerView banner)
        {
            if (_banner == banner && !_cts.IsCancellationRequested)
                banner.LoadAd(new AdRequest());
        }

        private void DestroyBanner()
        {
            _banner?.Destroy();
            _banner = null;
            _bannerLoaded = false;
        }

        #endregion

        private AdRevenue ToRevenue(AdFormat format, string adUnitId, AdValue value) =>
            new(format, adUnitId, value.Value, value.CurrencyCode, value.Precision.ToString());

        private void RunOnMainThread(Action action) => MobileAdsEventExecutor.ExecuteInUpdate(action);

        // 기다리지 않고 실행합니다. 서비스가 정리되며 취소된 경우는 무시하고, 다른 예외는 로그로 남깁니다.
        private async void RunAsync(Awaitable work)
        {
            try
            {
                await work;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
