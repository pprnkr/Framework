using UnityEngine;

namespace Nyonyo.Framework.Tweening
{
    /// <summary>
    /// 트윈의 속도 변화 곡선입니다. In은 천천히 시작, Out은 천천히 끝, InOut은 둘 다입니다.
    /// </summary>
    public enum Ease
    {
        Linear,
        InSine,
        OutSine,
        InOutSine,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        OutBack,
        OutBounce,
    }

    /// <summary>
    /// <see cref="Ease"/> 곡선 계산입니다.
    /// </summary>
    public static class Easing
    {
        /// <summary>
        /// 진행도 <paramref name="t"/>(0~1)를 곡선에 따라 바꿉니다. 0이면 0, 1이면 1입니다(OutBack은 중간에 1을 넘습니다).
        /// </summary>
        public static float Evaluate(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case Ease.InSine: return 1f - Mathf.Cos(t * Mathf.PI / 2f);
                case Ease.OutSine: return Mathf.Sin(t * Mathf.PI / 2f);
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                case Ease.InCubic: return t * t * t;
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.InOutCubic: return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                case Ease.OutBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                }
                case Ease.OutBounce: return OutBounce(t);
                default: return t;
            }
        }

        static float OutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1)
                return n1 * t * t;
            if (t < 2f / d1)
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            if (t < 2.5f / d1)
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }
    }
}
