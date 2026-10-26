using System;
using System.Threading;
using UnityEngine;

namespace Nyonyo.Framework.Scenes
{
    /// <summary>
    /// 실제로 씬을 불러옵니다. Addressables처럼 다른 방식으로 씬을 불러오려면 이 인터페이스를 구현하세요.
    /// </summary>
    public interface ISceneLoader
    {
        Awaitable LoadAsync(string sceneName, Action<float> onProgress, CancellationToken cancellationToken);
    }
}
