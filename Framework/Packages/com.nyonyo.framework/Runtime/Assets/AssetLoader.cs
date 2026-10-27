using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Assets
{
    /// <summary>
    /// 기본 <see cref="IAssetLoader"/> 구현입니다. 서비스로 등록해서 여러 매니저가 함께 쓰세요.
    /// </summary>
    /// <remarks>
    /// Addressables 핸들은 경로별로 보관합니다. 같은 경로를 여러 번 불러오면 Addressables가 같은 에셋을 공유하고,
    /// <see cref="Release"/>를 그만큼 호출하면 메모리에서 내려갑니다. <see cref="Dispose"/>는 남은 핸들을 모두 반환합니다.
    /// </remarks>
    public sealed class AssetLoader : IAssetLoader, IDisposable
    {
        private readonly Dictionary<string, List<AsyncOperationHandle>> _handles = new();

        /// <remarks>
        /// WebGL은 Addressables 동기 로드(WaitForCompletion)를 지원하지 않아서 <see cref="NotSupportedException"/>을 던집니다.
        /// 에디터의 빌드 대상이 WebGL이면 미리 경고합니다.
        /// </remarks>
        public T Load<T>(string path, LoadType loadType) where T : Object
        {
            ValidatePath(path);
            if (loadType == LoadType.Resources)
                return Validate(Resources.Load<T>(path), path, loadType);

#if UNITY_WEBGL && !UNITY_EDITOR
            throw new NotSupportedException(WebGLSyncLoadMessage(path));
#else
#if UNITY_WEBGL
            Debug.LogWarning($"[Nyonyo Framework] {WebGLSyncLoadMessage(path)} (에디터에서는 동작하지만 WebGL 빌드에서는 실패합니다)");
#endif
            var handle = StartAddressable<T>(path);
            handle.WaitForCompletion();
            return GetAddressableResult<T>(path, handle);
#endif
        }

        public async Awaitable<T> LoadAsync<T>(string path, LoadType loadType, CancellationToken cancellationToken = default)
            where T : Object
        {
            ValidatePath(path);
            if (loadType == LoadType.Resources)
            {
                var request = Resources.LoadAsync<T>(path);
                while (!request.isDone)
                    await Awaitable.NextFrameAsync(cancellationToken);

                return Validate(request.asset as T, path, loadType);
            }

            var handle = StartAddressable<T>(path);
            try
            {
                while (!handle.IsDone)
                    await Awaitable.NextFrameAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 기다리기를 그만둔 쪽이 핸들을 갖고 있지 않도록 바로 반환합니다.
                ReleaseHandle(path, handle);
                throw;
            }

            return GetAddressableResult<T>(path, handle);
        }

        public void Release(string path, LoadType loadType)
        {
            if (loadType == LoadType.Resources || path == null || !_handles.TryGetValue(path, out var handles))
                return;

            var last = handles.Count - 1;
            var handle = handles[last];
            handles.RemoveAt(last);
            if (handles.Count == 0)
                _handles.Remove(path);

            if (handle.IsValid())
                Addressables.Release(handle);
        }

        public void Dispose()
        {
            foreach (var handles in _handles.Values)
            {
                foreach (var handle in handles)
                {
                    if (handle.IsValid())
                        Addressables.Release(handle);
                }
            }

            _handles.Clear();
        }

        private AsyncOperationHandle StartAddressable<T>(string path) where T : Object
        {
            AsyncOperationHandle handle = Addressables.LoadAssetAsync<T>(path);
            if (!_handles.TryGetValue(path, out var handles))
            {
                handles = new List<AsyncOperationHandle>();
                _handles.Add(path, handles);
            }

            handles.Add(handle);
            return handle;
        }

        private T GetAddressableResult<T>(string path, AsyncOperationHandle handle) where T : Object
        {
            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result is T asset)
                return asset;

            var reason = handle.OperationException?.Message ?? $"{typeof(T).Name} 타입이 아닙니다";
            ReleaseHandle(path, handle);
            throw new InvalidOperationException(
                $"Addressables에서 {path}을(를) 불러오지 못했습니다({reason}). address가 맞는지, Addressables에 등록됐는지 확인하세요.");
        }

        private void ReleaseHandle(string path, AsyncOperationHandle handle)
        {
            if (_handles.TryGetValue(path, out var handles) && handles.Remove(handle) && handles.Count == 0)
                _handles.Remove(path);

            if (handle.IsValid())
                Addressables.Release(handle);
        }

        private T Validate<T>(T asset, string path, LoadType loadType) where T : Object
        {
            if (asset != null)
                return asset;

            throw new InvalidOperationException(
                $"{loadType}에서 {typeof(T).Name} {path}을(를) 찾지 못했습니다. Resources 폴더 기준 경로(확장자 제외)인지 확인하세요.");
        }

        private string WebGLSyncLoadMessage(string path) =>
            $"WebGL에서는 Addressables 에셋 {path}을(를) 동기로 불러올 수 없습니다. " +
            "LoadAsync를 쓰거나, 팝업이라면 [Popup(Preload = true)] 또는 PopupManager.PreloadAsync로 미리 불러 두세요.";

        private void ValidatePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("에셋 경로가 비어 있습니다.", nameof(path));
        }
    }
}
