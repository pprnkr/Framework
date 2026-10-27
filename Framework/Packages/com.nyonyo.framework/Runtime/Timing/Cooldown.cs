using System;
using UnityEngine;

namespace Nyonyo.Framework.Timing
{
    /// <summary>
    /// 스킬, 공격 같은 재사용 대기시간입니다. 매 프레임 갱신할 필요 없이 시각을 비교합니다.
    /// </summary>
    /// <example>
    /// <code>
    /// readonly Cooldown _dash = new(1.5f);
    ///
    /// void OnDash()
    /// {
    ///     if (!_dash.TryUse())
    ///         return;
    ///     // 대시 실행, _dash.Progress로 UI 게이지 표시
    /// }
    /// </code>
    /// </example>
    public sealed class Cooldown
    {
        private readonly Func<double> _clock;
        private double _readyAt;

        /// <param name="duration">대기시간(초)입니다.</param>
        /// <param name="unscaledTime">true면 <c>Time.timeScale</c>의 영향을 받지 않습니다.</param>
        public Cooldown(float duration, bool unscaledTime = false)
            : this(duration, unscaledTime ? () => Time.unscaledTimeAsDouble : () => Time.timeAsDouble)
        {
        }

        internal Cooldown(float duration, Func<double> clock)
        {
            Duration = duration;
            _clock = clock;
            _readyAt = double.MinValue;
        }

        public float Duration { get; set; }

        public bool IsReady => _clock() >= _readyAt;

        /// <summary>다시 쓸 수 있을 때까지 남은 시간(초)입니다.</summary>
        public float Remaining => (float)Math.Max(0d, _readyAt - _clock());

        /// <summary>대기가 얼마나 진행됐는지(0~1)입니다. 쓸 수 있으면 1입니다.</summary>
        public float Progress => Duration <= 0f ? 1f : 1f - Mathf.Clamp01(Remaining / Duration);

        /// <summary>
        /// 쓸 수 있으면 대기를 시작하고 true를 돌려줍니다.
        /// </summary>
        public bool TryUse()
        {
            if (!IsReady)
                return false;

            Start();
            return true;
        }

        /// <summary>지금부터 대기를 시작합니다.</summary>
        public void Start() => _readyAt = _clock() + Duration;

        /// <summary>대기를 끝내서 바로 쓸 수 있게 합니다.</summary>
        public void Reset() => _readyAt = double.MinValue;
    }
}
