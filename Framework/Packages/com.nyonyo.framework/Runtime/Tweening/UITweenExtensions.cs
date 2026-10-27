using UnityEngine;
using UnityEngine.UI;

namespace Nyonyo.Framework.Tweening
{
    /// <summary>
    /// UGUI 단축 트윈입니다(DOTween과 같은 이름). 대상은 각 컴포넌트입니다(예: <c>image.DOKill()</c>).
    /// </summary>
    /// <remarks>UI 연출은 일시정지 중에도 움직이도록 <c>SetUpdate(true)</c>를 붙이는 경우가 많습니다.</remarks>
    public static class UITweenExtensions
    {
        #region RectTransform

        public static Tweener<Vector2> DOAnchorPos(this RectTransform rectTransform, Vector2 endValue, float duration) =>
            Tween.Create(rectTransform, () => rectTransform.anchoredPosition, v => rectTransform.anchoredPosition = v, endValue, duration);

        public static Tweener<float> DOAnchorPosX(this RectTransform rectTransform, float endValue, float duration) =>
            Tween.Create(rectTransform, () => rectTransform.anchoredPosition.x, x =>
            {
                var position = rectTransform.anchoredPosition;
                position.x = x;
                rectTransform.anchoredPosition = position;
            }, endValue, duration);

        public static Tweener<float> DOAnchorPosY(this RectTransform rectTransform, float endValue, float duration) =>
            Tween.Create(rectTransform, () => rectTransform.anchoredPosition.y, y =>
            {
                var position = rectTransform.anchoredPosition;
                position.y = y;
                rectTransform.anchoredPosition = position;
            }, endValue, duration);

        public static Tweener<Vector2> DOSizeDelta(this RectTransform rectTransform, Vector2 endValue, float duration) =>
            Tween.Create(rectTransform, () => rectTransform.sizeDelta, v => rectTransform.sizeDelta = v, endValue, duration);

        /// <summary>anchoredPosition을 <paramref name="punch"/> 방향으로 튕겼다가 원래 자리로 돌아옵니다.</summary>
        public static Tweener<Vector2> DOPunchAnchorPos(this RectTransform rectTransform, Vector2 punch, float duration, int vibrato = 10,
            float elasticity = 1f) =>
            TweenShapes.Punch(rectTransform, () => rectTransform.anchoredPosition, v => rectTransform.anchoredPosition = v,
                punch, duration, vibrato, elasticity);

        /// <summary>anchoredPosition을 흔듭니다. 세기 단위는 UI 픽셀입니다.</summary>
        public static Tweener<Vector2> DOShakeAnchorPos(this RectTransform rectTransform, float duration, float strength = 100f, int vibrato = 10,
            float randomness = 90f, bool fadeOut = true) =>
            rectTransform.DOShakeAnchorPos(duration, new Vector2(strength, strength), vibrato, randomness, fadeOut);

        public static Tweener<Vector2> DOShakeAnchorPos(this RectTransform rectTransform, float duration, Vector2 strength, int vibrato = 10,
            float randomness = 90f, bool fadeOut = true) =>
            TweenShapes.Shake(rectTransform, () => rectTransform.anchoredPosition, v => rectTransform.anchoredPosition = v,
                strength, duration, vibrato, randomness, fadeOut);

        public static Tweener<Vector2> DOJumpAnchorPos(this RectTransform rectTransform, Vector2 endValue, float jumpPower, int numJumps,
            float duration) =>
            TweenShapes.Jump(rectTransform, () => rectTransform.anchoredPosition, v => rectTransform.anchoredPosition = v,
                endValue, jumpPower, numJumps, duration);

        #endregion

        #region 색과 투명도

        public static Tweener<float> DOFade(this CanvasGroup group, float endValue, float duration) =>
            Tween.Create(group, () => group.alpha, a => group.alpha = a, endValue, duration);

        /// <summary>Image, RawImage, Text 등 Graphic의 알파를 바꿉니다.</summary>
        public static Tweener<float> DOFade(this Graphic graphic, float endValue, float duration) =>
            Tween.Create(graphic, () => graphic.color.a, a =>
            {
                var color = graphic.color;
                color.a = a;
                graphic.color = color;
            }, endValue, duration);

        public static Tweener<Color> DOColor(this Graphic graphic, Color endValue, float duration) =>
            Tween.Create(graphic, () => graphic.color, c => graphic.color = c, endValue, duration);

        #endregion

        #region 값

        /// <summary>Image의 fillAmount(0~1)를 바꿉니다. 게이지 연출에 씁니다.</summary>
        public static Tweener<float> DOFillAmount(this Image image, float endValue, float duration) =>
            Tween.Create(image, () => image.fillAmount, v => image.fillAmount = v, Mathf.Clamp01(endValue), duration);

        /// <summary>Slider 값을 바꿉니다. onValueChanged도 호출됩니다.</summary>
        public static Tweener<float> DOValue(this Slider slider, float endValue, float duration) =>
            Tween.Create(slider, () => slider.value, v => slider.value = v, endValue, duration);

        public static Tweener<Vector2> DONormalizedPos(this ScrollRect scrollRect, Vector2 endValue, float duration) =>
            Tween.Create(scrollRect, () => scrollRect.normalizedPosition, v => scrollRect.normalizedPosition = v, endValue, duration);

        public static Tweener<float> DOHorizontalNormalizedPos(this ScrollRect scrollRect, float endValue, float duration) =>
            Tween.Create(scrollRect, () => scrollRect.horizontalNormalizedPosition, v => scrollRect.horizontalNormalizedPosition = v,
                endValue, duration);

        /// <summary>세로 스크롤 위치를 바꿉니다. 1이 맨 위, 0이 맨 아래입니다.</summary>
        public static Tweener<float> DOVerticalNormalizedPos(this ScrollRect scrollRect, float endValue, float duration) =>
            Tween.Create(scrollRect, () => scrollRect.verticalNormalizedPosition, v => scrollRect.verticalNormalizedPosition = v,
                endValue, duration);

        #endregion
    }
}
