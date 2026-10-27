using System;
using UnityEngine;

namespace Nyonyo.Framework.Ads
{
    /// <summary>
    /// await 없이 콜백으로 광고를 띄우는 함수입니다. 버튼 OnClick처럼 async를 쓰기 어려운 곳에서 씁니다.
    /// </summary>
    // 확장 메서드는 C#에서 static 클래스에만 둘 수 있습니다.
    public static class AdServiceExtensions
    {
        /// <summary>
        /// 보상형 광고를 띄웁니다. 보상 조건을 채우고 광고가 닫히면 <paramref name="onRewarded"/>를,
        /// 광고가 없거나 중간에 닫으면 <paramref name="onNotRewarded"/>를 호출합니다.
        /// </summary>
        public static void ShowRewarded(this IAdService ads, Action onRewarded, Action onNotRewarded = null)
        {
            if (onRewarded == null)
                throw new ArgumentNullException(nameof(onRewarded));

            Run(ads.ShowRewardedAsync(), rewarded => (rewarded ? onRewarded : onNotRewarded)?.Invoke());
        }

        /// <summary>
        /// 전면 광고를 띄우고, 닫히면 <paramref name="onClosed"/>를 호출합니다. 보여 줄 광고가 없어도 바로 호출하므로 다음 화면으로 넘어가는 코드를 넣으면 됩니다.
        /// </summary>
        public static void ShowInterstitial(this IAdService ads, Action onClosed = null) =>
            Run(ads.ShowInterstitialAsync(), _ => onClosed?.Invoke());

        private static async void Run(Awaitable<bool> show, Action<bool> onFinished)
        {
            try
            {
                onFinished(await show);
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
