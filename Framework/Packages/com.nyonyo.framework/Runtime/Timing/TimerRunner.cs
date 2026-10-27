using UnityEngine;

namespace Nyonyo.Framework.Timing
{
    /// <summary>
    /// 매 프레임 <see cref="TimerService"/>의 시간을 흘립니다. TimerService가 직접 붙입니다.
    /// </summary>
    sealed class TimerRunner : MonoBehaviour
    {
        internal TimerService Service;

        private void Update() => Service?.Tick(Time.deltaTime, Time.unscaledDeltaTime);
    }
}
