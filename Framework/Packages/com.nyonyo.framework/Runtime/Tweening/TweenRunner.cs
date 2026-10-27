using UnityEngine;

namespace Nyonyo.Framework.Tweening
{
    /// <summary>
    /// 매 프레임 <see cref="TweenManager"/>의 시간을 흘립니다. 첫 트윈을 만들 때 자동으로 생깁니다.
    /// </summary>
    sealed class TweenRunner : MonoBehaviour
    {
        private void Update() => TweenManager.Update(Time.deltaTime, Time.unscaledDeltaTime);
    }
}
