using System;
using UnityEngine;

namespace Nyonyo.Framework.Ads
{
    /// <summary>
    /// 광고 네트워크와 상관없이 지키는 노출 규칙입니다. 광고 설정 에셋(AdMobSettings 등)에 들어 있습니다.
    /// </summary>
    [Serializable]
    public sealed class AdPolicy
    {
        [Tooltip("전면, 앱 오프닝 광고는 마지막 전체 화면 광고(보상형 포함)가 닫힌 뒤 이 시간(초)이 지나야 다시 나옵니다.")]
        [Min(0f)]
        [SerializeField] private float _fullScreenCooldownSeconds = 10f;

        [Tooltip("앱이 백그라운드에서 돌아올 때 앱 오프닝 광고를 자동으로 보여 줍니다. 기본값은 꺼짐입니다.")]
        [SerializeField] private bool _showAppOpenOnResume = false;

        [Tooltip("광고가 없는 플랫폼(WebGL, PC)에서 보상형 광고를 요청하면 광고를 본 것으로 보고 바로 보상을 줍니다(기본값). " +
                 "끄면 그 플랫폼에서는 보상형 광고가 항상 준비되지 않은 상태라 보상을 받을 수 없습니다.")]
        [SerializeField] private bool _rewardWithoutAdOnUnsupportedPlatform = true;

        public float FullScreenCooldownSeconds
        {
            get => _fullScreenCooldownSeconds;
            set => _fullScreenCooldownSeconds = Mathf.Max(0f, value);
        }

        public bool ShowAppOpenOnResume
        {
            get => _showAppOpenOnResume;
            set => _showAppOpenOnResume = value;
        }

        /// <summary>
        /// 광고가 없는 플랫폼(WebGL, PC, <see cref="NullAdService"/>)에서 보상형 광고를 광고 없이 바로 보상으로 처리할지 여부입니다.
        /// 기본값은 true라서, 게임에서 따로 설정하지 않아도 웹과 PC 이용자는 광고를 본 것처럼 보상을 받습니다.
        /// 그 플랫폼에서 보상형 보상을 막으려면 false로 끄고 보상 버튼을 숨기세요.
        /// 전면, 앱 오프닝, 배너 광고에는 영향이 없습니다.
        /// </summary>
        public bool RewardWithoutAdOnUnsupportedPlatform
        {
            get => _rewardWithoutAdOnUnsupportedPlatform;
            set => _rewardWithoutAdOnUnsupportedPlatform = value;
        }
    }
}
