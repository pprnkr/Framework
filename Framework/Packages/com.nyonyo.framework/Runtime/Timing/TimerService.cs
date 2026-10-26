using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Timing
{
    /// <summary>
    /// 일정 시간 뒤 실행이나 반복 실행을 관리합니다. 코루틴 없이 지연 실행을 걸 때 씁니다.
    /// </summary>
    /// <remarks>
    /// <c>owner</c>를 넘기면 그 오브젝트가 파괴될 때 타이머도 자동으로 취소됩니다.
    /// 콜백에서 예외가 나도 다른 타이머는 계속 동작합니다.
    /// </remarks>
    /// <example>
    /// <code>
    /// TimerService.Instance.After(2f, Respawn, owner: this);
    /// var tick = TimerService.Instance.Every(0.5f, Regenerate, owner: this);
    /// tick.Cancel();
    /// </code>
    /// </example>
    public sealed class TimerService : IDisposable
    {
        readonly List<GameTimer> _timers = new();
        readonly List<GameTimer> _added = new();
        readonly TimerRunner _runner;

        /// <summary>
        /// 서비스로 등록된 TimerService입니다. <c>Services.Get&lt;TimerService&gt;()</c>와 같습니다.
        /// </summary>
        public static TimerService Instance => Services.Get<TimerService>();

        /// <param name="parent">매 프레임 타이머를 돌리는 컴포넌트를 붙일 부모입니다. 보통 <see cref="Bootstrapper.Root"/>를 넘깁니다.</param>
        public TimerService(Transform parent)
        {
            var go = new GameObject("[Timer]");
            if (parent != null)
                go.transform.SetParent(parent, false);
            else if (Application.isPlaying)
                Object.DontDestroyOnLoad(go);

            _runner = go.AddComponent<TimerRunner>();
            _runner.Service = this;
        }

        // 테스트용입니다. 시간은 Tick으로 직접 흘립니다.
        internal TimerService()
        {
        }

        /// <summary>현재 동작 중인 타이머 수입니다.</summary>
        public int ActiveCount => _timers.Count + _added.Count;

        /// <summary>
        /// <paramref name="seconds"/>초 뒤에 한 번 실행합니다.
        /// </summary>
        public GameTimer After(float seconds, Action callback, bool unscaledTime = false, Object owner = null) =>
            Add(seconds, callback, unscaledTime, 1, owner);

        /// <summary>
        /// <paramref name="interval"/>초마다 실행합니다.
        /// </summary>
        /// <param name="repeatCount">실행할 횟수입니다. 0 이하면 취소할 때까지 계속합니다.</param>
        public GameTimer Every(float interval, Action callback, bool unscaledTime = false, int repeatCount = -1, Object owner = null)
        {
            if (interval <= 0f)
                throw new ArgumentOutOfRangeException(nameof(interval), "반복 간격은 0보다 커야 합니다.");

            return Add(interval, callback, unscaledTime, repeatCount, owner);
        }

        public void CancelAll()
        {
            foreach (var timer in _timers)
                timer.Cancel();
            foreach (var timer in _added)
                timer.Cancel();
        }

        public void Dispose()
        {
            CancelAll();
            if (_runner != null)
                _runner.Service = null;
        }

        /// <summary>
        /// 시간을 흘려 타이머를 실행합니다. 매 프레임 자동으로 호출됩니다.
        /// </summary>
        internal void Tick(float deltaTime, float unscaledDeltaTime)
        {
            // 콜백 안에서 새로 만든 타이머는 다음 프레임부터 셉니다.
            _timers.AddRange(_added);
            _added.Clear();

            foreach (var timer in _timers)
            {
                if (!timer.IsActive)
                    continue;

                timer.Elapsed += timer.UnscaledTime ? unscaledDeltaTime : deltaTime;
                if (timer.Elapsed < timer.Interval)
                    continue;

                timer.Elapsed -= timer.Interval;
                if (timer.RemainingRepeats > 0 && --timer.RemainingRepeats == 0)
                    timer.Complete();

                try
                {
                    timer.Callback();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            _timers.RemoveAll(timer => !timer.IsActive);
        }

        GameTimer Add(float seconds, Action callback, bool unscaledTime, int repeatCount, Object owner)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            var timer = new GameTimer(Mathf.Max(0f, seconds), callback, unscaledTime, repeatCount, owner);
            _added.Add(timer);
            return timer;
        }
    }
}
