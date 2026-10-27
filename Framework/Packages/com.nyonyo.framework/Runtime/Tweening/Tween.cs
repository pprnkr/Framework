using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Tweening
{
    /// <summary>
    /// 트윈 하나(<see cref="Tweener{T}"/>) 또는 여러 트윈을 묶은 <see cref="Sequence"/>의 공통 핸들입니다. DOTween과 비슷하게 씁니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 트윈은 만든 다음 프레임부터 움직이므로, 만든 직후에 <c>SetEase</c>, <c>SetDelay</c>, <c>OnComplete</c> 같은 설정을 이어 붙이세요.
    /// 끝나면 자동으로 사라집니다(<see cref="TweenExtensions.SetAutoKill{T}"/>로 끌 수 있음).
    /// </para>
    /// <para>
    /// 대상 오브젝트가 파괴되면 조용히 멈춥니다. 일시정지(timeScale 0) 중에도 움직여야 하는 UI는 <c>SetUpdate(true)</c>를 쓰세요.
    /// 트윈은 await할 수 있고, 끝나거나 Kill되면 이어서 진행합니다. 무한 반복 트윈은 Kill하기 전까지 끝나지 않습니다.
    /// </para>
    /// <para>Play 모드에서만 움직입니다. 메인 스레드에서만 쓰세요.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// transform.DOMoveX(5f, 0.5f).SetEase(Ease.OutBack).SetLoops(-1, LoopType.Yoyo);
    /// await panel.DOFade(1f, 0.3f).SetUpdate(true);
    /// await Tween.To(() =&gt; score, v =&gt; label.text = v.ToString(), 100, 1f);
    /// </code>
    /// </example>
    public abstract class Tween
    {
        internal float Delay;
        internal int Loops = 1;
        internal LoopType LoopType;
        internal Ease Ease = Ease.OutQuad;
        internal AnimationCurve EaseCurve;
        internal bool UnscaledTime;
        internal bool AutoKill = true;
        internal object Id;
        internal Object Link;
        internal CancellationToken CancellationToken;
        internal Action OnStartCallback;
        internal Action OnUpdateCallback;
        internal Action OnStepCompleteCallback;
        internal Action OnCompleteCallback;
        internal Action OnKillCallback;

        /// <summary>이 트윈을 담고 있는 시퀀스입니다. 시퀀스 안의 트윈은 시퀀스가 움직입니다.</summary>
        internal Sequence Parent;

        /// <summary>한 번 재생하는 시간입니다. 시퀀스는 트윈을 넣을 때마다 늘어납니다.</summary>
        private protected float _duration;

        private readonly Object _unityTarget;
        private List<AwaitableCompletionSource> _waiters;
        private float _time;
        private int _loopIndex;
        private bool _started;
        private bool _isComplete;
        private bool _isActive = true;
        private bool _isPlaying = true;
        private bool _applied;
        private int _appliedLoop;
        private float _appliedProgress;

        private protected Tween(object target, float duration)
        {
            Target = target;
            _unityTarget = target as Object;
            _duration = Mathf.Max(0f, duration);
            TweenManager.Add(this);
        }

        /// <summary>트윈 대상입니다. 단축 함수(DOMove 등)는 그 컴포넌트, <c>Tween.To</c>와 시퀀스는 null입니다.</summary>
        public object Target { get; }

        /// <summary><see cref="TweenExtensions.SetId{T}"/>로 정한 식별자입니다.</summary>
        public object TweenId => Id;

        /// <summary>한 번 재생하는 시간(지연 제외)입니다.</summary>
        public float Duration => _duration;

        /// <summary>지연과 반복을 모두 포함한 전체 시간입니다. 무한 반복이면 <see cref="float.PositiveInfinity"/>입니다.</summary>
        public float FullDuration => Loops < 0 ? float.PositiveInfinity : Delay + _duration * Loops;

        /// <summary>지연을 포함해 지금까지 흐른 시간입니다.</summary>
        public float Elapsed => _time;

        /// <summary>끝까지 재생한 반복 횟수입니다.</summary>
        public int CompletedLoops => _isComplete ? Mathf.Max(1, Loops) : _loopIndex;

        /// <summary>Kill되지 않았으면 true입니다. 자동 Kill이 켜져 있으면 끝난 뒤 false가 됩니다.</summary>
        public bool IsActive => _isActive;

        /// <summary>일시정지 상태가 아니면 true입니다.</summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>끝까지 재생했으면 true입니다.</summary>
        public bool IsComplete => _isComplete;

        internal bool HasStarted => _started;

        #region 전역 제어
        // 아래 static 함수들은 트윈을 어디서든 만들고 멈추는 진입점이라 static입니다(전역 TweenManager를 감쌉니다).

        /// <summary>움직이고 있거나 대기 중인 트윈 수입니다(시퀀스 안의 트윈은 시퀀스 하나로 셉니다).</summary>
        public static int ActiveCount => TweenManager.ActiveCount;

        /// <summary>
        /// 대상 또는 <see cref="TweenExtensions.SetId{T}"/>의 식별자가 <paramref name="targetOrId"/>인 트윈을 모두 Kill합니다.
        /// </summary>
        /// <param name="complete">true면 끝 값으로 보내고 OnComplete를 호출한 뒤 Kill합니다.</param>
        /// <returns>Kill한 트윈 수입니다.</returns>
        public static int KillTarget(object targetOrId, bool complete = false) => TweenManager.Kill(targetOrId, complete);

        /// <summary>모든 트윈을 Kill합니다.</summary>
        public static int KillAll(bool complete = false) => TweenManager.Kill(null, complete);

        /// <summary>대상 또는 식별자가 <paramref name="targetOrId"/>인 트윈이 동작 중이면 true입니다.</summary>
        public static bool IsTweening(object targetOrId) => TweenManager.IsTweening(targetOrId);

        #endregion

        #region 범용 트윈

        /// <summary>
        /// <paramref name="getter"/>로 읽은 현재 값에서 <paramref name="endValue"/>까지 바꾸며 <paramref name="setter"/>를 호출합니다.
        /// </summary>
        public static Tweener<float> To(Func<float> getter, Action<float> setter, float endValue, float duration) =>
            Create(null, getter, setter, endValue, duration);

        /// <summary>정수 값 트윈입니다. 점수 카운터처럼 쓸 때 편합니다(반올림).</summary>
        public static Tweener<int> To(Func<int> getter, Action<int> setter, int endValue, float duration) =>
            new(null, getter, setter, endValue, duration, (a, b, t) => Mathf.RoundToInt(Mathf.LerpUnclamped(a, b, t)), (a, b) => a + b);

        public static Tweener<Vector2> To(Func<Vector2> getter, Action<Vector2> setter, Vector2 endValue, float duration) =>
            Create(null, getter, setter, endValue, duration);

        public static Tweener<Vector3> To(Func<Vector3> getter, Action<Vector3> setter, Vector3 endValue, float duration) =>
            Create(null, getter, setter, endValue, duration);

        public static Tweener<Color> To(Func<Color> getter, Action<Color> setter, Color endValue, float duration) =>
            Create(null, getter, setter, endValue, duration);

        /// <summary>
        /// <paramref name="from"/>에서 <paramref name="to"/>까지 값을 바꾸며 <paramref name="setter"/>를 호출합니다. 마지막에는 정확히 <paramref name="to"/>를 넘깁니다.
        /// </summary>
        public static Tweener<float> To(float from, float to, float duration, Action<float> setter) =>
            Create(null, () => from, setter, to, duration);

        /// <summary>
        /// 직접 정한 보간 함수로 임의 타입을 트윈합니다. <paramref name="lerp"/>는 (시작, 끝, 진행도)를 받으며, 진행도는 0~1을 벗어날 수 있습니다.
        /// </summary>
        public static Tweener<T> To<T>(Func<T> getter, Action<T> setter, T endValue, float duration, Func<T, T, float, T> lerp) =>
            new(null, getter, setter, endValue, duration, lerp ?? throw new ArgumentNullException(nameof(lerp)), null);

        /// <summary>
        /// <paramref name="delay"/>초 뒤에 <paramref name="callback"/>을 한 번 호출합니다. 반환된 시퀀스를 Kill하면 취소됩니다.
        /// </summary>
        public static Sequence DelayedCall(float delay, Action callback, bool unscaledTime = false)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            return Sequence.Create().AppendInterval(delay).AppendCallback(callback).SetUpdate(unscaledTime);
        }

        internal static Tweener<float> Create(object target, Func<float> getter, Action<float> setter, float endValue, float duration) =>
            new(target, getter, setter, endValue, duration, Mathf.LerpUnclamped, (a, b) => a + b);

        internal static Tweener<Vector2> Create(object target, Func<Vector2> getter, Action<Vector2> setter, Vector2 endValue, float duration) =>
            new(target, getter, setter, endValue, duration, Vector2.LerpUnclamped, (a, b) => a + b);

        internal static Tweener<Vector3> Create(object target, Func<Vector3> getter, Action<Vector3> setter, Vector3 endValue, float duration) =>
            new(target, getter, setter, endValue, duration, Vector3.LerpUnclamped, (a, b) => a + b);

        internal static Tweener<Color> Create(object target, Func<Color> getter, Action<Color> setter, Color endValue, float duration) =>
            new(target, getter, setter, endValue, duration, Color.LerpUnclamped, (a, b) => a + b);

        internal static Tweener<Quaternion> Create(object target, Func<Quaternion> getter, Action<Quaternion> setter, Quaternion endValue, float duration) =>
            new(target, getter, setter, endValue, duration, Quaternion.SlerpUnclamped, (a, b) => a * b);

        #endregion

        #region 개별 제어

        /// <summary>
        /// 트윈을 멈추고 없앱니다. 이미 바뀐 값은 그대로 둡니다. await 중인 코드는 이어서 진행합니다.
        /// </summary>
        /// <param name="complete">true면 먼저 끝 값으로 보내고 OnComplete를 호출합니다.</param>
        public void Kill(bool complete = false)
        {
            if (!CanControl())
                return;

            if (complete)
                Complete();
            KillInternal(false);
        }

        /// <summary>
        /// 바로 끝 값으로 보내고 OnComplete를 호출합니다. 무한 반복이면 이번 반복의 끝으로 보냅니다.
        /// </summary>
        public void Complete()
        {
            if (!CanControl() || _isComplete)
                return;

            CompleteInternal();
            OnRootComplete();
        }

        /// <summary>
        /// 처음 값으로 되돌리고 일시정지합니다. 끝났어도 자동 Kill을 끈 트윈이면 다시 쓸 수 있습니다.
        /// </summary>
        public void Rewind()
        {
            if (!CanControl())
                return;

            _isPlaying = false;
            RewindInternal();
        }

        /// <summary>처음 값으로 되돌려 다시 재생합니다.</summary>
        public void Restart()
        {
            if (!CanControl())
                return;

            RewindInternal();
            _isPlaying = true;
        }

        public void TogglePause()
        {
            if (CanControl())
                _isPlaying = !_isPlaying;
        }

        internal void SetPlaying(bool playing)
        {
            if (CanControl())
                _isPlaying = playing;
        }

        /// <summary>
        /// 끝나거나 Kill될 때까지 기다립니다. <see cref="TweenExtensions.SetCancellation{T}"/>의 토큰으로 취소되면 <see cref="OperationCanceledException"/>을 던집니다.
        /// </summary>
        public Awaitable WaitForCompletionAsync()
        {
            var source = new AwaitableCompletionSource();
            if (!_isActive || _isComplete)
                source.SetResult();
            else
                (_waiters ??= new List<AwaitableCompletionSource>()).Add(source);
            return source.Awaitable;
        }

        /// <summary><c>await tween</c>을 쓸 수 있게 합니다. <see cref="WaitForCompletionAsync"/>와 같습니다.</summary>
        public Awaitable.Awaiter GetAwaiter() => WaitForCompletionAsync().GetAwaiter();

        #endregion

        #region 진행

        /// <summary>
        /// 지연을 포함한 경과 시간 <paramref name="time"/>의 상태로 이동합니다. 루트 트윈은 매니저가, 시퀀스 안의 트윈은 시퀀스가 호출합니다.
        /// </summary>
        internal void Goto(float time)
        {
            if (!_isActive)
                return;

            if (IsDestroyed(_unityTarget) || IsDestroyed(Link))
            {
                KillInternal(false);
                return;
            }

            _time = Mathf.Max(0f, time);
            var t = _time - Delay;
            if (t < 0f)
            {
                // 시퀀스가 되감을 때입니다. 시작했던 트윈은 처음 값으로 돌려 둡니다.
                if (_started)
                {
                    _isComplete = false;
                    _loopIndex = 0;
                    ApplyAt(0, 0f);
                }
                return;
            }

            EnsureStarted();

            var loops = _duration <= 0f ? Mathf.Max(1, Loops) : Loops;
            int loop;
            float raw;
            bool finished;
            if (loops >= 0 && (_duration <= 0f || t >= _duration * loops))
            {
                finished = true;
                loop = loops - 1;
                raw = 1f;
            }
            else
            {
                finished = false;
                loop = (int)(t / _duration);
                raw = (t - loop * _duration) / _duration;
            }

            var previousLoop = _loopIndex;
            _loopIndex = loop;
            ApplyAt(loop, raw);

            for (var i = previousLoop; i < loop; i++)
                Invoke(OnStepCompleteCallback);

            if (finished && !_isComplete)
            {
                _isComplete = true;
                Invoke(OnStepCompleteCallback);
                Invoke(OnCompleteCallback);
                ResolveWaiters(false);
            }
            else if (!finished)
            {
                _isComplete = false;
            }
        }

        /// <summary>처음 시작할 때(지연이 끝난 직후) 한 번 호출됩니다. 시작 값을 읽어 둡니다.</summary>
        private protected abstract void OnStartup();

        /// <summary><paramref name="progress"/>는 곡선을 적용한 이번 반복의 진행도입니다.</summary>
        private protected abstract void Apply(int loop, float progress);

        private protected virtual void OnKilled()
        {
        }

        internal void KillInternal(bool canceled)
        {
            if (!_isActive)
                return;

            _isActive = false;
            _isPlaying = false;
            OnKilled();
            Invoke(OnKillCallback);
            ResolveWaiters(canceled);
        }

        /// <summary>루트 트윈이 끝났을 때 매니저가 호출합니다. 자동 Kill이면 없앱니다.</summary>
        internal void OnRootComplete()
        {
            if (_isActive && _isComplete && AutoKill)
                KillInternal(false);
        }

        private void CompleteInternal()
        {
            if (Loops >= 0)
            {
                Goto(FullDuration);
                return;
            }

            EnsureStarted();
            ApplyAt(_loopIndex, 1f);
            _isComplete = true;
            Invoke(OnStepCompleteCallback);
            Invoke(OnCompleteCallback);
            ResolveWaiters(false);
        }

        private void RewindInternal()
        {
            _time = 0f;
            _loopIndex = 0;
            _isComplete = false;
            if (_started)
                ApplyAt(0, 0f);
        }

        private void EnsureStarted()
        {
            if (_started)
                return;

            _started = true;
            OnStartup();
            Invoke(OnStartCallback);
        }

        private void ApplyAt(int loop, float raw)
        {
            if (LoopType == LoopType.Yoyo && loop % 2 == 1)
                raw = 1f - raw;
            if (_applied && loop == _appliedLoop && raw == _appliedProgress)
                return;

            _applied = true;
            _appliedLoop = loop;
            _appliedProgress = raw;
            Apply(loop, EaseCurve != null ? EaseCurve.Evaluate(raw) : Easing.Evaluate(Ease, raw));
            Invoke(OnUpdateCallback);
        }

        private void ResolveWaiters(bool canceled)
        {
            if (_waiters == null || _waiters.Count == 0)
                return;

            // 이어지는 코드가 바로 실행되면서 새로 기다릴 수 있으므로 목록을 먼저 비웁니다.
            var waiters = _waiters.ToArray();
            _waiters.Clear();
            foreach (var waiter in waiters)
            {
                if (canceled)
                    waiter.TrySetCanceled();
                else
                    waiter.TrySetResult();
            }
        }

        private bool CanControl()
        {
            if (!_isActive)
                return false;
            if (Parent == null)
                return true;

            Debug.LogWarning("[Nyonyo Framework] 시퀀스 안의 트윈은 직접 제어할 수 없습니다. 시퀀스를 제어하세요.");
            return false;
        }

        internal void Invoke(Action callback)
        {
            if (callback == null)
                return;

            try
            {
                callback();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private bool IsDestroyed(Object obj) => !ReferenceEquals(obj, null) && obj == null;

        #endregion
    }
}
