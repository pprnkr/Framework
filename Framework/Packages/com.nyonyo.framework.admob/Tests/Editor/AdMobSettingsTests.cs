using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Ads.AdMob.Editor.Tests
{
    public class AdMobSettingsTests
    {
        private AdMobSettings _settings;

        [SetUp]
        public void SetUp() => _settings = ScriptableObject.CreateInstance<AdMobSettings>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_settings);

        [Test]
        public void TestAds_UseGoogleTestIds()
        {
            Assert.IsTrue(_settings.UseTestAds, "새 설정은 테스트 광고가 기본입니다.");

            Assert.AreEqual(AdMobTestIds.AndroidRewarded, _settings.GetAdUnitId(AdFormat.Rewarded, false));
            Assert.AreEqual(AdMobTestIds.IOSBanner, _settings.GetAdUnitId(AdFormat.Banner, true));
            Assert.AreEqual(AdMobTestIds.AndroidAppOpen, _settings.GetAdUnitId(AdFormat.AppOpen, false));
        }

        [Test]
        public void RealAds_UsePlatformIds()
        {
            var serialized = new SerializedObject(_settings);
            serialized.FindProperty("_useTestAds").boolValue = false;
            serialized.FindProperty("_android._interstitial").stringValue = "android-interstitial";
            serialized.FindProperty("_iOS._interstitial").stringValue = "ios-interstitial";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual("android-interstitial", _settings.GetAdUnitId(AdFormat.Interstitial, false));
            Assert.AreEqual("ios-interstitial", _settings.GetAdUnitId(AdFormat.Interstitial, true));
            Assert.AreEqual("", _settings.GetAdUnitId(AdFormat.Banner, false), "비워 둔 형식은 빈 문자열입니다.");
        }

        [Test]
        public void CreateService_InEditor_UsesFakeByDefault()
        {
            var ads = _settings.CreateService(null);

            Assert.IsInstanceOf<FakeAdService>(ads);
            ((FakeAdService)ads).Dispose();
        }
    }
}
