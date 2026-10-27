using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nyonyo.Framework.Scenes
{
    /// <summary>
    /// Unity <see cref="SceneManager"/>로 빌드에 포함된 씬을 불러옵니다.
    /// </summary>
    public sealed class SceneManagerLoader : ISceneLoader
    {
        // Unity는 씬 활성화 직전까지의 진행률을 0~0.9로 보고합니다.
        private const float LoadedProgress = 0.9f;

        public async Awaitable LoadAsync(string sceneName, Action<float> onProgress, CancellationToken cancellationToken)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
                throw new ArgumentException(
                    $"{sceneName} 씬을 불러올 수 없습니다. Build Profiles의 Scene List에 추가했는지 확인하세요.",
                    nameof(sceneName));

            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!operation.isDone)
            {
                onProgress?.Invoke(Mathf.Clamp01(operation.progress / LoadedProgress));
                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }
    }
}
