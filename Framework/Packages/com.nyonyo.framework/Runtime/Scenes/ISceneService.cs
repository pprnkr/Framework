using System.Threading;
using UnityEngine;

namespace Nyonyo.Framework.Scenes
{
    /// <summary>
    /// 전환 연출과 함께 씬을 바꿉니다.
    /// </summary>
    public interface ISceneService
    {
        /// <summary>
        /// 씬을 전환하는 중인지 여부입니다. 전환 연출이 끝날 때까지 true입니다.
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// 현재 씬 로드 진행률(0~1)입니다.
        /// </summary>
        float Progress { get; }

        /// <summary>
        /// 현재 씬을 내리고 <paramref name="sceneName"/> 씬을 불러옵니다.
        /// 이미 전환 중이면 요청을 무시하고 false를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 취소하면 기다리는 것만 멈추고, Unity의 씬 로드 자체는 끝까지 진행됩니다.
        /// </remarks>
        Awaitable<bool> LoadAsync(string sceneName, CancellationToken cancellationToken = default);
    }
}
