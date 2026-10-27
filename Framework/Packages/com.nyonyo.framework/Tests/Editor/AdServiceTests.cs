using System.Collections.Generic;
using Nyonyo.Framework.Ads;
using Nyonyo.Framework.Save;
using NUnit.Framework;

namespace Nyonyo.Framework.Editor.Tests
{
    public class AdServiceTests
    {
        private double _now;
        private AdPolicy _policy;
        private SaveService _save;
        private FakeAdService _ads;
        private List<string> _log;

        [SetUp]
        public void SetUp()
        {
            _now = 0d;
            _policy = new AdPolicy { FullScreenCooldownSeconds = 30f, ShowAppOpenOnResume = true };
            _save = new SaveService(new MemorySaveStorage());
            _ads = CreateAds();
            _log = new List<string>();
        }

        [TearDown]
        public void TearDown() => _ads.Dispose();

        [Test]
        public void Fake_IsInitializedImmediately()
        {
            Assert.IsTrue(_ads.IsInitialized);
            Assert.IsTrue(_ads.CanRequestAds);
            Assert.IsTrue(_ads.IsInterstitialReady);
            Assert.IsTrue(_ads.IsRewardedReady);
        }

        [Test]
        public void Rewarded_ReturnsTrueOnlyWhenRewardEarned()
        {
            var earned = _ads.ShowRewardedAsync().GetAwaiter();
            Assert.IsFalse(earned.IsCompleted, "광고가 닫힐 때까지 기다립니다.");
            Assert.AreEqual(AdFormat.Rewarded, _ads.PresentingFormat);
            _ads.Respond(true);
            Assert.IsTrue(earned.IsCompleted);
            Assert.IsTrue(earned.GetResult());

            var skipped = _ads.ShowRewardedAsync().GetAwaiter();
            _ads.Respond(false);
            Assert.IsFalse(skipped.GetResult());
        }

        [Test]
        public void Interstitial_ReturnsTrueWhenShown()
        {
            var shown = _ads.ShowInterstitialAsync().GetAwaiter();
            _ads.Respond(false);

            Assert.IsTrue(shown.GetResult());
        }

        [Test]
        public void NoFill_ReturnsFalseImmediately()
        {
            _ads.SimulateNoFill = true;

            var shown = _ads.ShowInterstitialAsync().GetAwaiter();
            var rewarded = _ads.ShowRewardedAsync().GetAwaiter();

            Assert.IsTrue(shown.IsCompleted);
            Assert.IsFalse(shown.GetResult());
            Assert.IsFalse(rewarded.GetResult());
            Assert.IsFalse(_ads.IsPresenting);
        }

        [Test]
        public void OnlyOneFullScreenAdAtATime()
        {
            _ads.ShowRewardedAsync();

            var second = _ads.ShowInterstitialAsync().GetAwaiter();

            Assert.IsTrue(second.IsCompleted);
            Assert.IsFalse(second.GetResult());
            Assert.IsFalse(_ads.IsRewardedReady);
        }

        [Test]
        public void Cooldown_BlocksInterstitialAndAppOpen_ButNotRewarded()
        {
            _ads.ShowRewardedAsync();
            _ads.Respond(true);

            _now = 10d;
            Assert.IsFalse(_ads.IsInterstitialReady);
            Assert.IsFalse(_ads.IsAppOpenReady);
            Assert.IsTrue(_ads.IsRewardedReady);

            _now = 30d;
            Assert.IsTrue(_ads.IsInterstitialReady);
            Assert.IsTrue(_ads.IsAppOpenReady);
        }

        [Test]
        public void FailedShow_DoesNotStartCooldown()
        {
            var shown = _ads.ShowInterstitialAsync().GetAwaiter();

            // 정리되면서 떠 있던 광고가 실패로 끝난 상황입니다.
            _ads.Dispose();

            Assert.IsFalse(shown.GetResult());
            Assert.IsTrue(_ads.IsInterstitialReady);
        }

        [Test]
        public void Events_OpenedThenClosed()
        {
            _ads.FullScreenAdOpened += format => _log.Add($"opened {format}");
            _ads.FullScreenAdClosed += format => _log.Add($"closed {format}");

            _ads.ShowInterstitialAsync();
            _ads.Respond(false);

            CollectionAssert.AreEqual(new[] { "opened Interstitial", "closed Interstitial" }, _log);
        }

        [Test]
        public void AdsRemoved_BlocksAllButRewarded_AndIsSaved()
        {
            _ads.ShowBanner();
            Assert.IsTrue(_ads.IsBannerVisible);

            _ads.AdsRemoved = true;

            Assert.IsFalse(_ads.IsBannerVisible);
            Assert.AreEqual(0f, _ads.BannerHeight);
            Assert.IsFalse(_ads.IsInterstitialReady);
            Assert.IsFalse(_ads.IsAppOpenReady);
            Assert.IsTrue(_ads.IsRewardedReady);

            var reloaded = CreateAds();
            Assert.IsTrue(reloaded.AdsRemoved, "다음 실행 때 저장된 값을 불러옵니다.");
            reloaded.Dispose();
        }

        [Test]
        public void AdsRemovedOff_ShowsRequestedBannerAgain()
        {
            _ads.ShowBanner(BannerPosition.Top);
            _ads.AdsRemoved = true;

            _ads.AdsRemoved = false;

            Assert.IsTrue(_ads.IsBannerVisible);
            Assert.AreEqual(BannerPosition.Top, _ads.BannerPosition);
        }

        [Test]
        public void HideBanner_HidesIt()
        {
            _ads.ShowBanner();
            Assert.Greater(_ads.BannerHeight, 0f);

            _ads.HideBanner();

            Assert.IsFalse(_ads.IsBannerVisible);
            Assert.AreEqual(0f, _ads.BannerHeight);
        }

        [Test]
        public void AppForeground_ShowsAppOpen_UnlessSkipped()
        {
            _ads.SkipNextAppOpen();
            _ads.SimulateAppForeground();
            Assert.IsFalse(_ads.IsPresenting, "SkipNextAppOpen은 한 번 건너뜁니다.");

            _ads.SimulateAppForeground();
            Assert.IsTrue(_ads.IsPresenting);
            Assert.AreEqual(AdFormat.AppOpen, _ads.PresentingFormat);
        }

        [Test]
        public void AppForeground_RespectsPolicy()
        {
            _policy.ShowAppOpenOnResume = false;

            _ads.SimulateAppForeground();

            Assert.IsFalse(_ads.IsPresenting);
        }

        [Test]
        public void CallbackExtensions_CallMatchingCallback()
        {
            _ads.ShowRewarded(onRewarded: () => _log.Add("rewarded"), onNotRewarded: () => _log.Add("not rewarded"));
            _ads.Respond(false);

            _ads.SimulateNoFill = true;
            _ads.ShowInterstitial(onClosed: () => _log.Add("next stage"));

            CollectionAssert.AreEqual(new[] { "not rewarded", "next stage" }, _log);
        }

        [Test]
        public void NullAdService_RewardWithoutAdOff_NeverShows()
        {
            _policy.RewardWithoutAdOnUnsupportedPlatform = false;
            var ads = new NullAdService(_policy);

            ads.ShowBanner();

            Assert.IsTrue(ads.IsInitialized);
            Assert.IsFalse(ads.IsBannerVisible);
            Assert.IsFalse(ads.IsRewardedReady);
            Assert.IsFalse(ads.ShowRewardedAsync().GetAwaiter().GetResult());
        }

        [Test]
        public void NullAdService_ByDefault_GrantsRewardImmediately()
        {
            var ads = new NullAdService();

            ads.ShowRewarded(onRewarded: () => _log.Add("rewarded"), onNotRewarded: () => _log.Add("not rewarded"));

            Assert.IsTrue(ads.IsRewardedReady);
            CollectionAssert.AreEqual(new[] { "rewarded" }, _log);
        }

        [Test]
        public void NullAdService_ByDefault_DoesNotShowOtherFormats()
        {
            var ads = new NullAdService(_policy);

            ads.ShowBanner();

            Assert.IsFalse(ads.IsInterstitialReady);
            Assert.IsFalse(ads.IsAppOpenReady);
            Assert.IsFalse(ads.IsBannerVisible);
            Assert.IsFalse(ads.ShowInterstitialAsync().GetAwaiter().GetResult());
        }

        private FakeAdService CreateAds() => new(null, _policy, _save, () => _now);
    }
}
