using System.Collections.Generic;
using GoogleMobileAds.Ump.Api;
using Nyonyo.Framework.Save;
using UnityEngine;

namespace Nyonyo.Framework.Ads.AdMob
{
    /// <summary>
    /// AdMob 광고 설정입니다. 광고 단위 ID, 테스트 광고, 동의(GDPR), 노출 규칙을 정합니다.
    /// </summary>
    /// <remarks>
    /// 앱 ID는 이 에셋이 아니라 AdMob 플러그인 설정(Assets > Google Mobile Ads > Settings...)에 넣습니다. 이 에셋의 인스펙터에서 상태를 확인할 수 있습니다.
    /// </remarks>
    public sealed class AdMobSettings : ScriptableObject
    {
        [Header("광고 단위 ID")]
        [Tooltip("켜져 있으면 아래 ID 대신 Google 테스트 광고 ID를 씁니다. 출시 빌드 전에 반드시 끄세요.")]
        [SerializeField] private bool _useTestAds = true;

        [SerializeField] private AdUnitIds _android = new();
        [SerializeField] private AdUnitIds _iOS = new();

        [Header("테스트")]
        [Tooltip("실제 광고 ID로 시험할 기기의 ID입니다. 기기에서 광고를 요청하면 로그(Logcat, Xcode)에 출력됩니다.")]
        [SerializeField] private List<string> _testDeviceIds = new();

        [Header("동의 (UMP, GDPR)")]
        [Tooltip("광고 전에 Google UMP로 사용자 동의를 받습니다. 유럽(EEA), 영국 등에 출시하면 켜야 합니다. " +
                 "AdMob 콘솔의 개인정보 보호 및 메시지에서 GDPR 메시지를 만들어 두어야 창이 뜹니다.")]
        [SerializeField] private bool _requestConsent = true;

        [Tooltip("동의 창을 시험할 때 지역을 흉내 냅니다. EEA로 두면 한국에서도 동의 창이 뜹니다(테스트 기기에서만). 출시 전 Disabled로 돌리세요.")]
        [SerializeField] private DebugGeography _consentDebugGeography = DebugGeography.Disabled;

        [Header("노출 규칙")]
        [SerializeField] private AdPolicy _policy = new();

        [Header("에디터")]
        [SerializeField] private AdMobEditorMode _editorMode = AdMobEditorMode.Fake;

        public bool UseTestAds => _useTestAds;

        public IReadOnlyList<string> TestDeviceIds => _testDeviceIds;

        public bool RequestConsent => _requestConsent;

        public DebugGeography ConsentDebugGeography => _consentDebugGeography;

        public AdPolicy Policy => _policy;

        public AdMobEditorMode EditorMode => _editorMode;

        /// <summary>
        /// 실행 환경에 맞는 광고 서비스를 만듭니다. Android, iOS는 AdMob, 에디터는 <see cref="EditorMode"/>, 그 밖의 플랫폼은 광고 없음입니다.
        /// </summary>
        /// <param name="parent">에디터용 가짜 광고 화면을 붙일 부모입니다. 보통 <see cref="Bootstrapper.Root"/>를 넘깁니다.</param>
        /// <param name="save">넘기면 광고 제거 여부를 저장합니다.</param>
        /// <example><c>services.Register&lt;IAdService&gt;(_adMobSettings.CreateService(Bootstrapper.Root, save));</c></example>
        public IAdService CreateService(Transform parent, SaveService save = null)
        {
            if (Application.isEditor)
            {
                if (_editorMode == AdMobEditorMode.Fake)
                    return new FakeAdService(parent, _policy, save);
                return new AdMobAdService(this, save);
            }

            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
                return new AdMobAdService(this, save);

            return new NullAdService(_policy, save);
        }

        /// <summary>현재 빌드 플랫폼의 광고 단위 ID입니다. <see cref="UseTestAds"/>가 켜져 있으면 Google 테스트 ID입니다.</summary>
        public string GetAdUnitId(AdFormat format)
        {
#if UNITY_IOS
            return GetAdUnitId(format, true);
#else
            return GetAdUnitId(format, false);
#endif
        }

        internal string GetAdUnitId(AdFormat format, bool iOS)
        {
            if (!_useTestAds)
                return (iOS ? _iOS : _android).Get(format);

            if (iOS)
                return new AdUnitIds(AdMobTestIds.IOSBanner, AdMobTestIds.IOSInterstitial, AdMobTestIds.IOSRewarded, AdMobTestIds.IOSAppOpen).Get(format);
            return new AdUnitIds(AdMobTestIds.AndroidBanner, AdMobTestIds.AndroidInterstitial, AdMobTestIds.AndroidRewarded, AdMobTestIds.AndroidAppOpen).Get(format);
        }
    }
}
