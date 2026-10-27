using System;
using Nyonyo.Framework.Save;
using UnityEngine;

namespace Nyonyo.Framework.Ads
{
    /// <summary>
    /// 광고 네트워크와 상관없는 공통 규칙(광고 제거, 전체 화면 광고 대기 시간, 한 번에 하나만, 배너 요청 기억, 이벤트)을 처리합니다.
    /// 광고 네트워크 모듈은 이 클래스를 상속해 불러오기와 띄우기만 구현합니다.
    /// </summary>
    public abstract class AdServiceBase : IAdService, IDisposable
    {
        public const string AdsRemovedSaveKey = "ads.removed";

        private readonly AdPolicy _policy;
        private readonly SaveService _save;
        private readonly Func<double> _clock;
        private bool _adsRemoved;
        private bool _bannerRequested;
        private BannerPosition _bannerPosition;
        private bool _isShowingFullScreen;
        private double _lastFullScreenClosedAt = double.NegativeInfinity;
        private bool _skipNextAppOpen;

        /// <param name="policy">null이면 기본 규칙을 씁니다.</param>
        /// <param name="save">넘기면 광고 제거 여부를 저장하고 불러옵니다.</param>
        /// <param name="clock">대기 시간 계산용 시계(초)입니다. null이면 <see cref="Time.realtimeSinceStartupAsDouble"/>입니다. 테스트에서 바꿉니다.</param>
        protected AdServiceBase(AdPolicy policy, SaveService save, Func<double> clock)
        {
            _policy = policy ?? new AdPolicy();
            _save = save;
            _clock = clock ?? GetRealtime;

            if (_save != null && _save.TryLoad(AdsRemovedSaveKey, out AdsRemovedData data, SaveScope.Global))
                _adsRemoved = data.removed;
        }

        public event Action Initialized;
        public event Action<AdFormat> FullScreenAdOpened;
        public event Action<AdFormat> FullScreenAdClosed;
        public event Action<AdRevenue> RevenuePaid;

        public bool IsInitialized { get; private set; }

        public AdPolicy Policy => _policy;

        /// <summary>전체 화면 광고가 떠 있는 동안 true입니다.</summary>
        public bool IsShowingFullScreen => _isShowingFullScreen;

        #region 동의

        public virtual bool CanRequestAds => true;

        public virtual bool IsPrivacyOptionsRequired => false;

        public virtual Awaitable ShowPrivacyOptionsAsync()
        {
            var source = new AwaitableCompletionSource();
            source.SetResult();
            return source.Awaitable;
        }

        #endregion

        #region 광고 제거

        public bool AdsRemoved
        {
            get => _adsRemoved;
            set
            {
                if (_adsRemoved == value)
                    return;

                _adsRemoved = value;
                _save?.Save(AdsRemovedSaveKey, new AdsRemovedData { removed = value }, SaveScope.Global);

                if (!_bannerRequested || !IsInitialized)
                    return;
                if (value)
                    HideBannerView();
                else
                    ShowBannerView(_bannerPosition);
            }
        }

        #endregion

        #region 배너

        public bool IsBannerVisible => ShouldShowBanner() && GetBannerHeight() > 0f;

        public float BannerHeight => IsBannerVisible ? GetBannerHeight() : 0f;

        /// <summary>마지막으로 요청한 배너 위치입니다.</summary>
        public BannerPosition BannerPosition => _bannerPosition;

        public void ShowBanner(BannerPosition position = BannerPosition.Bottom)
        {
            _bannerRequested = true;
            _bannerPosition = position;
            if (ShouldShowBanner())
                ShowBannerView(position);
        }

        public void HideBanner()
        {
            if (!_bannerRequested)
                return;

            var wasShown = ShouldShowBanner();
            _bannerRequested = false;
            if (wasShown)
                HideBannerView();
        }

        #endregion

        #region 전체 화면 광고

        public bool IsInterstitialReady => CanShow(AdFormat.Interstitial);

        public bool IsRewardedReady => CanShow(AdFormat.Rewarded);

        public bool IsAppOpenReady => CanShow(AdFormat.AppOpen);

        public Awaitable<bool> ShowInterstitialAsync() => ShowFullScreenAsync(AdFormat.Interstitial);

        public Awaitable<bool> ShowRewardedAsync() => ShowFullScreenAsync(AdFormat.Rewarded);

        public Awaitable<bool> ShowAppOpenAsync() => ShowFullScreenAsync(AdFormat.AppOpen);

        public void SkipNextAppOpen() => _skipNextAppOpen = true;

        #endregion

        public virtual void Dispose()
        {
        }

        #region 상속한 클래스가 구현하는 부분

        /// <summary><paramref name="format"/> 광고를 불러 두었으면 true입니다. 배너는 묻지 않습니다.</summary>
        protected abstract bool IsLoaded(AdFormat format);

        /// <summary>불러 둔 전체 화면 광고를 띄우고, 닫히거나 실패할 때 끝나는 Awaitable을 돌려줍니다. 끝난 뒤 다음 광고를 불러 두세요.</summary>
        protected abstract Awaitable<AdShowResult> PresentAsync(AdFormat format);

        protected abstract void ShowBannerView(BannerPosition position);

        protected abstract void HideBannerView();

        /// <summary>배너가 화면에 나와 있으면 그 높이(화면 픽셀), 아니면 0입니다.</summary>
        protected abstract float GetBannerHeight();

        /// <summary>광고 SDK 초기화가 끝났을 때 호출하세요. 기다리던 배너를 보여 주고 <see cref="Initialized"/>를 발행합니다.</summary>
        protected void MarkInitialized()
        {
            if (IsInitialized)
                return;

            IsInitialized = true;
            if (ShouldShowBanner())
                ShowBannerView(_bannerPosition);
            Invoke(Initialized);
        }

        /// <summary>앱이 백그라운드에서 돌아왔을 때 호출하세요. 규칙에 맞으면 앱 오프닝 광고를 띄웁니다.</summary>
        protected void OnAppForeground()
        {
            if (_skipNextAppOpen)
            {
                _skipNextAppOpen = false;
                return;
            }

            if (_policy.ShowAppOpenOnResume && CanShow(AdFormat.AppOpen))
                ShowFullScreenAsync(AdFormat.AppOpen).Forget();
        }

        protected void RaiseRevenuePaid(AdRevenue revenue)
        {
            try
            {
                RevenuePaid?.Invoke(revenue);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        #endregion

        private async Awaitable<bool> ShowFullScreenAsync(AdFormat format)
        {
            if (!CanShow(format))
                return false;

            _isShowingFullScreen = true;
            Invoke(FullScreenAdOpened, format);

            var result = AdShowResult.Failed;
            try
            {
                result = await PresentAsync(format);
            }
            finally
            {
                _isShowingFullScreen = false;
                if (result != AdShowResult.Failed)
                    _lastFullScreenClosedAt = _clock();
                Invoke(FullScreenAdClosed, format);
            }

            if (format == AdFormat.Rewarded)
                return result == AdShowResult.Rewarded;
            return result != AdShowResult.Failed;
        }

        private bool CanShow(AdFormat format)
        {
            if (!IsInitialized || _isShowingFullScreen)
                return false;

            if (format != AdFormat.Rewarded)
            {
                if (_adsRemoved)
                    return false;
                if (_clock() - _lastFullScreenClosedAt < _policy.FullScreenCooldownSeconds)
                    return false;
            }

            return IsLoaded(format);
        }

        private bool ShouldShowBanner() => _bannerRequested && IsInitialized && !_adsRemoved;

        private double GetRealtime() => Time.realtimeSinceStartupAsDouble;

        private void Invoke(Action callback)
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void Invoke(Action<AdFormat> callback, AdFormat format)
        {
            try
            {
                callback?.Invoke(format);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
