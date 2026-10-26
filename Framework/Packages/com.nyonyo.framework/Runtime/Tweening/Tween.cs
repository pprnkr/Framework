using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Tweening
{
    /// <summary>
    /// 값을 시간에 따라 부드럽게 바꿉니다. 외부 트윈 라이브러리 없이 UI 연출 정도를 처리하는 가벼운 도구입니다.
    /// </summary>
    /// <remarks>
    /// 모든 함수는 <see cref="Awaitable"/>을 돌려주므로 await로 순서를 이어 붙일 수 있습니다.
    /// 대상 오브젝트가 파괴되면 조용히 멈춥니다. 일시정지 중에도 움직여야 하는 UI는 <c>unscaledTime: true</c>를 쓰세요.
    /// 많은 오브젝트를 동시에 움직이는 복잡한 연출에는 PrimeTween, DOTween 같은 전용 라이브러리를 권합니다.
    /// </remarks>
    /// <example>
    /// <code>
    /// await panel.FadeAsync(1f, 0.3f, unscaledTime: true);
    /// await icon.transform.ScaleAsync(Vector3.one * 1.2f, 0.15f, Ease.OutBack);
    /// await Tween.To(0f, 100f, 1f, v =&gt; label.text = $"{v:0}");
    /// </code>
    /// </example>
    public static class Tween
    {
        /// <summary>
        /// <paramref name="from"/>에서 <paramref name="to"/>까지 값을 바꾸며 <paramref name="setter"/>를 호출합니다. 마지막에는 정확히 <paramref name="to"/>를 넘깁니다.
        /// </summary>
        public static Awaitable To(float from, float to, float duration, Action<float> setter, Ease ease = Ease.OutQuad,
            bool unscaledTime = false, CancellationToken cancellationToken = default) =>
            Run(duration, ease, unscaledTime, cancellationToken, null, t => setter(Mathf.LerpUnclamped(from, to, t)));

        public static Awaitable FadeAsync(this CanvasGroup group, float to, float duration, Ease ease = Ease.OutQuad,
            bool unscaledTime = false, CancellationToken cancellationToken = default)
        {
            var from = group.alpha;
            return Run(duration, ease, unscaledTime, cancellationToken, group, t => group.alpha = Mathf.LerpUnclamped(from, to, t));
        }

        public static Awaitable MoveAsync(this Transform transform, Vector3 to, float duration, Ease ease = Ease.OutQuad,
            bool unscaledTime = false, CancellationToken cancellationToken = default)
        {
            var from = transform.position;
            return Run(duration, ease, unscaledTime, cancellationToken, transform, t => transform.position = Vector3.LerpUnclamped(from, to, t));
        }

        public static Awaitable LocalMoveAsync(this Transform transform, Vector3 to, float duration, Ease ease = Ease.OutQuad,
            bool unscaledTime = false, CancellationToken cancellationToken = default)
        {
            var from = transform.localPosition;
            return Run(duration, ease, unscaledTime, cancellationToken, transform, t => transform.localPosition = Vector3.LerpUnclamped(from, to, t));
        }

        public static Awaitable ScaleAsync(this Transform transform, Vector3 to, float duration, Ease ease = Ease.OutQuad,
            bool unscaledTime = false, CancellationToken cancellationToken = default)
        {
            var from = transform.localScale;
            return Run(duration, ease, unscaledTime, cancellationToken, transform, t => transform.localScale = Vector3.LerpUnclamped(from, to, t));
        }

        public static Awaitable AnchoredMoveAsync(this RectTransform rectTransform, Vector2 to, float duration, Ease ease = Ease.OutQuad,
            bool unscaledTime = false, CancellationToken cancellationToken = default)
        {
            var from = rectTransform.anchoredPosition;
            return Run(duration, ease, unscaledTime, cancellationToken, rectTransform,
                t => rectTransform.anchoredPosition = Vector2.LerpUnclamped(from, to, t));
        }

        public static Awaitable ColorAsync(this Graphic graphic, Color to, float duration, Ease ease = Ease.OutQuad,
            bool unscaledTime = false, CancellationToken cancellationToken = default)
        {
            var from = graphic.color;
            return Run(duration, ease, unscaledTime, cancellationToken, graphic, t => graphic.color = Color.LerpUnclamped(from, to, t));
        }

        public static Awaitable ColorAsync(this SpriteRenderer renderer, Color to, float duration, Ease ease = Ease.OutQuad,
            bool unscaledTime = false, CancellationToken cancellationToken = default)
        {
            var from = renderer.color;
            return Run(duration, ease, unscaledTime, cancellationToken, renderer, t => renderer.color = Color.LerpUnclamped(from, to, t));
        }

        // target이 있으면 파괴됐을 때 멈춥니다. 취소되면 OperationCanceledException이 그대로 전달됩니다.
        static async Awaitable Run(float duration, Ease ease, bool unscaledTime, CancellationToken cancellationToken,
            Object target, Action<float> apply)
        {
            var hasTarget = target != null;
            for (var elapsed = 0f; elapsed < duration; elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                if (hasTarget && target == null)
                    return;

                apply(Easing.Evaluate(ease, elapsed / duration));
                await Awaitable.NextFrameAsync(cancellationToken);
            }

            if (hasTarget && target == null)
                return;

            apply(1f);
        }
    }
}
