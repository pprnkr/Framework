using System;
using UnityEngine;

namespace Nyonyo.Framework.Ads.AdMob
{
    /// <summary>
    /// 한 플랫폼의 광고 단위 ID입니다. AdMob 콘솔의 "광고 단위"에서 복사합니다(ca-app-pub-.../... 형식). 쓰지 않는 형식은 비워 두세요.
    /// </summary>
    [Serializable]
    public sealed class AdUnitIds
    {
        [SerializeField] private string _banner = "";
        [SerializeField] private string _interstitial = "";
        [SerializeField] private string _rewarded = "";
        [SerializeField] private string _appOpen = "";

        public AdUnitIds()
        {
        }

        public AdUnitIds(string banner, string interstitial, string rewarded, string appOpen)
        {
            _banner = banner;
            _interstitial = interstitial;
            _rewarded = rewarded;
            _appOpen = appOpen;
        }

        public string Get(AdFormat format)
        {
            if (format == AdFormat.Banner)
                return _banner;
            if (format == AdFormat.Interstitial)
                return _interstitial;
            if (format == AdFormat.Rewarded)
                return _rewarded;
            return _appOpen;
        }
    }
}
