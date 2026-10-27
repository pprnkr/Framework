using UnityEngine;

namespace Nyonyo.Framework.Tweening
{
    /// <summary>
    /// 트윈의 속도 변화 곡선입니다. In은 천천히 시작, Out은 천천히 끝, InOut은 둘 다입니다.
    /// </summary>
    /// <remarks>곡선 모양은 https://easings.net 에서 볼 수 있습니다.</remarks>
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
        InQuart,
        OutQuart,
        InOutQuart,
        InQuint,
        OutQuint,
        InOutQuint,
        InExpo,
        OutExpo,
        InOutExpo,
        InCirc,
        OutCirc,
        InOutCirc,
        InElastic,
        OutElastic,
        InOutElastic,
        InBack,
        OutBack,
        InOutBack,
        InBounce,
        OutBounce,
        InOutBounce,
    }

    /// <summary>
    /// <see cref="Ease"/> 곡선 계산입니다.
    /// </summary>
    public static class Easing
    {
        private const float BackOvershoot = 1.70158f;

        /// <summary>
        /// 진행도 <paramref name="t"/>(0~1)를 곡선에 따라 바꿉니다. 0이면 0, 1이면 1입니다(Back과 Elastic은 중간에 0~1을 벗어납니다).
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
                case Ease.InQuart: return t * t * t * t;
                case Ease.OutQuart: return 1f - Mathf.Pow(1f - t, 4f);
                case Ease.InOutQuart: return t < 0.5f ? 8f * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 4f) / 2f;
                case Ease.InQuint: return t * t * t * t * t;
                case Ease.OutQuint: return 1f - Mathf.Pow(1f - t, 5f);
                case Ease.InOutQuint: return t < 0.5f ? 16f * t * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 5f) / 2f;
                case Ease.InExpo: return t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
                case Ease.OutExpo: return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                case Ease.InOutExpo:
                    if (t <= 0f || t >= 1f)
                        return t;
                    return t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) / 2f : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;
                case Ease.InCirc: return 1f - Mathf.Sqrt(1f - t * t);
                case Ease.OutCirc: return Mathf.Sqrt(1f - (t - 1f) * (t - 1f));
                case Ease.InOutCirc:
                    return t < 0.5f
                        ? (1f - Mathf.Sqrt(1f - 4f * t * t)) / 2f
                        : (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) / 2f;
                case Ease.InElastic:
                    if (t <= 0f || t >= 1f)
                        return t;
                    return -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((10f * t - 10.75f) * (2f * Mathf.PI / 3f));
                case Ease.OutElastic:
                    if (t <= 0f || t >= 1f)
                        return t;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((10f * t - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
                case Ease.InOutElastic:
                {
                    if (t <= 0f || t >= 1f)
                        return t;
                    const float c5 = 2f * Mathf.PI / 4.5f;
                    return t < 0.5f
                        ? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) / 2f
                        : Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * c5) / 2f + 1f;
                }
                case Ease.InBack: return (BackOvershoot + 1f) * t * t * t - BackOvershoot * t * t;
                case Ease.OutBack:
                {
                    const float c3 = BackOvershoot + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + BackOvershoot * Mathf.Pow(t - 1f, 2f);
                }
                case Ease.InOutBack:
                {
                    const float c2 = BackOvershoot * 1.525f;
                    return t < 0.5f
                        ? Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2) / 2f
                        : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
                }
                case Ease.InBounce: return 1f - OutBounce(1f - t);
                case Ease.OutBounce: return OutBounce(t);
                case Ease.InOutBounce: return t < 0.5f ? (1f - OutBounce(1f - 2f * t)) / 2f : (1f + OutBounce(2f * t - 1f)) / 2f;
                default: return t;
            }
        }

        private static float OutBounce(float t)
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
