using System.Threading;
using UnityEngine;

namespace Nyonyo.Framework.Assets
{
    /// <summary>
    /// Resources와 Addressables를 같은 방식으로 불러옵니다.
    /// </summary>
    /// <remarks>
    /// 불러온 횟수만큼 <see cref="Release"/>를 호출해야 Addressables 에셋이 메모리에서 내려갑니다.
    /// Resources 에셋은 <see cref="Release"/>가 아무 일도 하지 않습니다. 필요하면 <c>Resources.UnloadUnusedAssets</c>로 정리하세요.
    /// </remarks>
    public interface IAssetLoader
    {
        /// <summary>
        /// 에셋을 바로 불러옵니다. Addressables는 로드가 끝날 때까지 기다리므로(WaitForCompletion) WebGL에서는 쓰지 마세요.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">에셋을 찾지 못했거나 불러오지 못한 경우</exception>
        T Load<T>(string path, LoadType loadType) where T : Object;

        /// <summary>
        /// 에셋을 비동기로 불러옵니다.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">에셋을 찾지 못했거나 불러오지 못한 경우</exception>
        Awaitable<T> LoadAsync<T>(string path, LoadType loadType, CancellationToken cancellationToken = default) where T : Object;

        /// <summary>
        /// 불러온 에셋을 하나 반환합니다. Addressables는 불러온 횟수와 반환한 횟수가 같아지면 메모리에서 내려갑니다.
        /// </summary>
        void Release(string path, LoadType loadType);
    }
}
