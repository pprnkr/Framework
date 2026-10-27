using System;

namespace Nyonyo.Framework.Tweening
{
    /// <summary>
    /// 값 하나를 시작 값에서 끝 값으로 바꾸는 트윈입니다. DOMove, DOFade 같은 단축 함수와 <see cref="Tween.To(Func{float}, Action{float}, float, float)"/>가 만듭니다.
    /// </summary>
    /// <remarks>시작 값은 지연이 끝나고 처음 움직일 때 읽습니다. 그래서 앞 트윈이 끝난 자리에서 이어집니다.</remarks>
    public sealed class Tweener<T> : Tween
    {
        /// <summary>Incremental 반복을 지원하지 않는 트윈(Punch, Shake)은 Restart처럼 동작합니다.</summary>
        internal bool SupportsIncremental = true;

        private readonly Func<T> _getter;
        private readonly Action<T> _setter;
        private readonly Func<T, T, float, T> _evaluate;
        private readonly Func<T, T, T> _add;
        private readonly T _endValue;
        private T _start;
        private T _end;
        private bool _relative;
        private bool _startCaptured;
        private bool _endResolved;

        /// <param name="evaluate">(시작, 끝, 진행도)로 값을 계산합니다. Incremental 반복이면 진행도가 1을 넘습니다.</param>
        /// <param name="add">상대값(SetRelative, From)에 씁니다. null이면 상대값을 쓸 수 없습니다.</param>
        internal Tweener(object target, Func<T> getter, Action<T> setter, T endValue, float duration,
            Func<T, T, float, T> evaluate, Func<T, T, T> add)
            : base(target, duration)
        {
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
            _evaluate = evaluate;
            _add = add;
            _endValue = endValue;
        }

        /// <summary>시작 값입니다. 트윈이 시작되기 전에는 기본값입니다.</summary>
        public T StartValue => _start;

        /// <summary>끝 값입니다. 상대값이면 시작할 때 정해집니다.</summary>
        public T EndValue => _endResolved ? _end : _endValue;

        /// <summary>
        /// 끝 값을 시작 값에 더한 상대값으로 씁니다. 예: <c>DOMoveX(2f, 1f).SetRelative()</c>는 지금 위치에서 2만큼 움직입니다.
        /// </summary>
        public Tweener<T> SetRelative(bool relative = true)
        {
            if (relative)
                RequireAdd(nameof(SetRelative));
            _relative = relative;
            return this;
        }

        /// <summary>
        /// 거꾸로 재생합니다. 지금 값을 끝 값으로 삼고, 대상을 바로 원래 끝 값으로 보낸 뒤 지금 값으로 돌아옵니다.
        /// </summary>
        /// <param name="isRelative">true면 원래 끝 값을 지금 값에 더한 위치에서 출발합니다.</param>
        public Tweener<T> From(bool isRelative = false)
        {
            var current = _getter();
            _end = current;
            _endResolved = true;
            return SetStart(isRelative ? Add(current, _endValue) : _endValue, true);
        }

        /// <summary>
        /// 시작 값을 직접 정합니다. 끝 값은 그대로입니다.
        /// </summary>
        /// <param name="setImmediately">true면 대상을 바로 시작 값으로 보냅니다(지연 중에 원래 값이 보이지 않게).</param>
        /// <param name="isRelative">true면 <paramref name="fromValue"/>를 지금 값에 더한 값에서 출발합니다.</param>
        public Tweener<T> From(T fromValue, bool setImmediately = true, bool isRelative = false) =>
            SetStart(isRelative ? Add(_getter(), fromValue) : fromValue, setImmediately);

        private protected override void OnStartup()
        {
            if (!_startCaptured)
            {
                _start = _getter();
                _startCaptured = true;
            }

            if (!_endResolved)
            {
                _end = _relative ? _add(_start, _endValue) : _endValue;
                _endResolved = true;
            }
        }

        private protected override void Apply(int loop, float progress)
        {
            if (LoopType == LoopType.Incremental && SupportsIncremental)
                progress += loop;
            _setter(_evaluate(_start, _end, progress));
        }

        private Tweener<T> SetStart(T start, bool setImmediately)
        {
            if (HasStarted)
                throw new InvalidOperationException("이미 시작된 트윈에는 From을 쓸 수 없습니다. 트윈을 만든 직후에 이어 붙이세요.");

            _start = start;
            _startCaptured = true;
            if (setImmediately)
                _setter(start);
            return this;
        }

        private T Add(T a, T b)
        {
            RequireAdd(nameof(From));
            return _add(a, b);
        }

        private void RequireAdd(string method)
        {
            if (_add == null)
                throw new NotSupportedException($"이 트윈은 상대값({method})을 지원하지 않습니다. 절대값으로 지정하세요.");
        }
    }
}
