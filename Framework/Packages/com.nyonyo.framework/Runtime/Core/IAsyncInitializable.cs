using System.Threading;
using UnityEngine;

namespace Nyonyo.Framework
{
    /// <summary>
    /// 등록된 서비스가 초기화 단계에서 세이브 로드처럼 시간이 걸리는 작업을 해야 할 때 구현합니다.
    /// 다음 서비스는 이 작업이 끝난 뒤에 초기화됩니다.
    /// </summary>
    public interface IAsyncInitializable
    {
        Awaitable InitializeAsync(CancellationToken cancellationToken);
    }
}
