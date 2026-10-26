using System;
using System.Collections.Generic;
using Nyonyo.Framework.Events;
using UnityEngine;

namespace Nyonyo.Framework.Timing
{
    /// <summary>
    /// 일시정지가 바뀔 때 발행됩니다.
    /// </summary>
    public readonly struct PauseChanged : IEvent
    {
        public readonly bool IsPaused;

        public PauseChanged(bool isPaused) => IsPaused = isPaused;
    }

    /// <summary>
    /// 일시정지와 타임스케일을 한곳에서 관리합니다. <c>Time.timeScale</c>을 직접 바꾸지 말고 이 서비스를 쓰세요.
    /// </summary>
    /// <remarks>
    /// 여러 시스템이 동시에 일시정지를 요청해도 충돌하지 않습니다. 요청한 쪽이 모두 Resume해야 다시 흐릅니다.
    /// 예를 들어 설정 팝업과 튜토리얼이 동시에 멈춰도, 팝업만 닫았을 때 게임이 풀리지 않습니다.
    /// 일시정지 중에는 <c>Time.timeScale</c>이 0이 되므로, UI 연출은 unscaled time을 쓰세요.
    /// </remarks>
    /// <example>
    /// <code>
    /// PauseService.Instance.Pause(this);   // 설정 팝업 OnOpen
    /// PauseService.Instance.Resume(this);  // 설정 팝업 Close
    /// PauseService.Instance.TimeScale = 0.3f; // 슬로 모션
    /// </code>
    /// </example>
    public sealed class PauseService : IDisposable
    {
        readonly HashSet<object> _requests = new();
        readonly IEventBus _eventBus;
        float _timeScale = 1f;
        bool _pauseAudio;
        bool _wasPaused;

        /// <summary>
        /// 서비스로 등록된 PauseService입니다. <c>Services.Get&lt;PauseService&gt;()</c>와 같습니다.
        /// </summary>
        public static PauseService Instance => Services.Get<PauseService>();

        /// <param name="eventBus">넘기면 일시정지가 바뀔 때 <see cref="PauseChanged"/>를 발행합니다.</param>
        public PauseService(IEventBus eventBus = null)
        {
            _eventBus = eventBus;
            Apply();
        }

        public bool IsPaused => _requests.Count > 0;

        /// <summary>
        /// 일시정지가 아닐 때의 타임스케일입니다. 슬로 모션이나 빨리 감기에 씁니다.
        /// </summary>
        public float TimeScale
        {
            get => _timeScale;
            set
            {
                _timeScale = Mathf.Max(0f, value);
                Apply();
            }
        }

        /// <summary>
        /// true면 일시정지 중에 모든 소리(<c>AudioListener.pause</c>)도 멈춥니다. 기본값은 false라 BGM은 계속 나옵니다.
        /// </summary>
        public bool PauseAudio
        {
            get => _pauseAudio;
            set
            {
                _pauseAudio = value;
                Apply();
            }
        }

        /// <summary>
        /// <paramref name="requester"/> 이름으로 일시정지를 요청합니다. 같은 요청자가 여러 번 불러도 한 번으로 셉니다.
        /// </summary>
        public void Pause(object requester)
        {
            if (requester == null)
                throw new ArgumentNullException(nameof(requester));

            _requests.Add(requester);
            Apply();
        }

        /// <summary>
        /// <paramref name="requester"/>의 일시정지 요청을 거둡니다. 다른 요청이 남아 있으면 계속 멈춰 있습니다.
        /// </summary>
        public void Resume(object requester)
        {
            if (requester != null && _requests.Remove(requester))
                Apply();
        }

        /// <summary>모든 일시정지 요청을 거둡니다. 씬을 바꿀 때 씁니다.</summary>
        public void ResumeAll()
        {
            _requests.Clear();
            Apply();
        }

        /// <summary>
        /// 타임스케일을 1로, 소리 일시정지를 해제한 상태로 돌려놓습니다.
        /// </summary>
        public void Dispose()
        {
            _requests.Clear();
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        void Apply()
        {
            var paused = IsPaused;
            Time.timeScale = paused ? 0f : _timeScale;
            AudioListener.pause = paused && _pauseAudio;

            if (paused == _wasPaused)
                return;

            _wasPaused = paused;
            _eventBus?.Publish(new PauseChanged(paused));
        }
    }
}
