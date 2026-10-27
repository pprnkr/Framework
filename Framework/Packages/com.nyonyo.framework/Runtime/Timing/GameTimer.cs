using System;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Timing
{
    /// <summary>
    /// <see cref="TimerService"/>가 돌려주는 타이머입니다. 취소하거나 남은 시간을 확인할 때 씁니다.
    /// </summary>
    public sealed class GameTimer
    {
        private readonly Object _owner;
        private readonly bool _hasOwner;

        internal GameTimer(float interval, Action callback, bool unscaledTime, int repeatCount, Object owner)
        {
            Interval = interval;
            Callback = callback;
            UnscaledTime = unscaledTime;
            RemainingRepeats = repeatCount;
            _owner = owner;
            _hasOwner = owner != null;
        }

        public float Interval { get; }

        /// <summary>true면 <c>Time.timeScale</c>의 영향을 받지 않습니다(일시정지 중에도 흐름).</summary>
        public bool UnscaledTime { get; }

        /// <summary>다음 호출까지 남은 시간(초)입니다.</summary>
        public float Remaining => Math.Max(0f, Interval - Elapsed);

        /// <summary>취소되거나 끝나지 않았으면 true입니다.</summary>
        public bool IsActive => !IsDone && !(_hasOwner && _owner == null);

        internal Action Callback { get; }

        internal float Elapsed { get; set; }

        /// <summary>남은 반복 횟수입니다. 0 이하면 무한 반복입니다.</summary>
        internal int RemainingRepeats { get; set; }

        internal bool IsDone { get; private set; }

        /// <summary>타이머를 취소합니다. 여러 번 불러도 됩니다.</summary>
        public void Cancel() => IsDone = true;

        internal void Complete() => IsDone = true;
    }
}
