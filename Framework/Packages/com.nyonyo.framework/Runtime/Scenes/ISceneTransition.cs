using System.Threading;
using UnityEngine;

namespace Nyonyo.Framework.Scenes
{
    /// <summary>
    /// 씬 전환 연출입니다. 로딩 화면을 직접 만들려면 이 인터페이스를 구현하세요.
    /// </summary>
    public interface ISceneTransition
    {
        /// <summary>
        /// 씬을 내리기 전에 화면을 가립니다.
        /// </summary>
        Awaitable ShowAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 씬 로드 진행률(0~1)을 받습니다. 로딩 바가 없다면 비워 두세요.
        /// </summary>
        void SetProgress(float progress);

        /// <summary>
        /// 새 씬이 준비되면 화면을 다시 보여 줍니다.
        /// </summary>
        Awaitable HideAsync(CancellationToken cancellationToken);
    }
}
