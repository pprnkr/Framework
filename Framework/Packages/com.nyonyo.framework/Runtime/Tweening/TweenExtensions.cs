using System;
using System.Threading;
using UnityEngine;

namespace Nyonyo.Framework.Tweening
{
    /// <summary>
    /// 트윈 설정과 콜백을 이어 붙이는 함수입니다. 모두 같은 트윈을 돌려주므로 체이닝할 수 있습니다.
    /// </summary>
    /// <remarks>설정은 트윈을 만든 직후, 시작하기 전에 하세요. 콜백은 같은 종류를 다시 정하면 바뀝니다(누적되지 않음).</remarks>
    public static class TweenExtensions
    {
        public static T SetEase<T>(this T tween, Ease ease) where T : Tween
        {
            tween.Ease = ease;
            tween.EaseCurve = null;
            return tween;
        }

        /// <summary>곡선을 직접 정합니다. 가로축 0~1이 진행도, 세로축이 0(시작)~1(끝)입니다.</summary>
        public static T SetEase<T>(this T tween, AnimationCurve curve) where T : Tween
        {
            tween.EaseCurve = curve ?? throw new ArgumentNullException(nameof(curve));
            return tween;
        }

        /// <summary>시작하기 전에 <paramref name="seconds"/>초 기다립니다. 반복할 때는 처음 한 번만 기다립니다.</summary>
        public static T SetDelay<T>(this T tween, float seconds) where T : Tween
        {
            tween.Delay = Mathf.Max(0f, seconds);
            return tween;
        }

        /// <param name="loops">재생 횟수입니다. -1이면 Kill할 때까지 반복합니다(0은 1로 취급).</param>
        public static T SetLoops<T>(this T tween, int loops, LoopType loopType = LoopType.Restart) where T : Tween
        {
            tween.Loops = loops < 0 ? -1 : Mathf.Max(1, loops);
            tween.LoopType = loopType;
            return tween;
        }

        /// <summary>
        /// true면 <see cref="Time.timeScale"/>을 무시하고 실제 시간으로 움직입니다. 일시정지 중에도 움직여야 하는 UI에 씁니다.
        /// </summary>
        public static T SetUpdate<T>(this T tween, bool unscaledTime) where T : Tween
        {
            tween.UnscaledTime = unscaledTime;
            return tween;
        }

        /// <summary>
        /// false면 끝나도 없애지 않고 멈춰 둡니다. <see cref="Tween.Restart"/>로 다시 재생할 수 있고, 다 쓰면 직접 Kill하세요.
        /// </summary>
        public static T SetAutoKill<T>(this T tween, bool autoKill = true) where T : Tween
        {
            tween.AutoKill = autoKill;
            return tween;
        }

        /// <summary><see cref="Tween.KillTarget"/>로 한꺼번에 없앨 때 쓸 식별자입니다.</summary>
        public static T SetId<T>(this T tween, object id) where T : Tween
        {
            tween.Id = id;
            return tween;
        }

        /// <summary>
        /// <paramref name="gameObject"/>가 파괴되면 트윈도 Kill합니다. 단축 함수는 대상 컴포넌트에 자동으로 연결되므로 <c>Tween.To</c>에 주로 씁니다.
        /// </summary>
        public static T SetLink<T>(this T tween, GameObject gameObject) where T : Tween
        {
            tween.Link = gameObject;
            return tween;
        }

        /// <summary>
        /// 토큰이 취소되면 트윈을 Kill하고, await 중인 코드에는 <see cref="OperationCanceledException"/>을 던집니다.
        /// </summary>
        /// <example><c>transform.DOMoveX(5f, 1f).SetCancellation(destroyCancellationToken);</c></example>
        public static T SetCancellation<T>(this T tween, CancellationToken cancellationToken) where T : Tween
        {
            tween.CancellationToken = cancellationToken;
            return tween;
        }

        /// <summary>지연이 끝나고 처음 움직이기 직전에 한 번 호출합니다.</summary>
        public static T OnStart<T>(this T tween, Action action) where T : Tween
        {
            tween.OnStartCallback = action;
            return tween;
        }

        /// <summary>값이 바뀔 때마다 호출합니다.</summary>
        public static T OnUpdate<T>(this T tween, Action action) where T : Tween
        {
            tween.OnUpdateCallback = action;
            return tween;
        }

        /// <summary>반복 한 번이 끝날 때마다 호출합니다.</summary>
        public static T OnStepComplete<T>(this T tween, Action action) where T : Tween
        {
            tween.OnStepCompleteCallback = action;
            return tween;
        }

        /// <summary>끝까지 재생했을 때 호출합니다. Kill되면 호출하지 않습니다.</summary>
        public static T OnComplete<T>(this T tween, Action action) where T : Tween
        {
            tween.OnCompleteCallback = action;
            return tween;
        }

        /// <summary>트윈이 없어질 때(끝난 뒤 자동 Kill 포함) 호출합니다.</summary>
        public static T OnKill<T>(this T tween, Action action) where T : Tween
        {
            tween.OnKillCallback = action;
            return tween;
        }

        public static T Play<T>(this T tween) where T : Tween
        {
            tween.SetPlaying(true);
            return tween;
        }

        /// <summary>일시정지합니다. 만들자마자 멈춰 두려면 <c>.SetAutoKill(false).Pause()</c>처럼 씁니다.</summary>
        public static T Pause<T>(this T tween) where T : Tween
        {
            tween.SetPlaying(false);
            return tween;
        }
    }
}
