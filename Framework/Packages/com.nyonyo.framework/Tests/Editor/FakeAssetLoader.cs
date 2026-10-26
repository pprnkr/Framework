using System;
using System.Collections.Generic;
using System.Threading;
using Nyonyo.Framework.Assets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Editor.Tests
{
    /// <summary>
    /// 테스트용 로더입니다. 불러오기와 반환을 기록하고, 모든 로드를 즉시 완료합니다.
    /// </summary>
    public sealed class FakeAssetLoader : IAssetLoader
    {
        public readonly List<string> Log = new();
        public readonly Dictionary<string, Object> Assets = new();

        /// <summary>
        /// <see cref="Assets"/>에 없는 경로를 불러올 때 에셋을 만듭니다. null을 반환하면 로드 실패로 처리합니다.
        /// </summary>
        public Func<string, Object> Factory;

        public T Load<T>(string path, LoadType loadType) where T : Object
        {
            Log.Add($"load:{path}:{loadType}");
            if (!Assets.TryGetValue(path, out var asset) && Factory != null)
            {
                asset = Factory(path);
                if (asset != null)
                    Assets[path] = asset;
            }

            if (asset == null)
                throw new InvalidOperationException($"{path} not found");

            return (T)asset;
        }

        public Awaitable<T> LoadAsync<T>(string path, LoadType loadType, CancellationToken cancellationToken = default)
            where T : Object
        {
            var asset = Load<T>(path, loadType);
            var source = new AwaitableCompletionSource<T>();
            source.SetResult(asset);
            return source.Awaitable;
        }

        public void Release(string path, LoadType loadType) => Log.Add($"release:{path}:{loadType}");
    }
}
