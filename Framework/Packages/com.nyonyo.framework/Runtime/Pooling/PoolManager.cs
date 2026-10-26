using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Pooling
{
    /// <summary>
    /// 프리팹별 풀을 관리합니다. 총알, 이펙트처럼 자주 생기고 사라지는 오브젝트에 Instantiate/Destroy 대신 씁니다.
    /// </summary>
    /// <example>
    /// <code>
    /// var bullet = PoolManager.Instance.Spawn(bulletPrefab, muzzle.position, muzzle.rotation);
    /// PoolManager.Instance.Despawn(bullet);            // 바로 돌려놓기
    /// _ = PoolManager.Instance.DespawnAfter(effect, 1f); // 1초 뒤 돌려놓기
    /// </code>
    /// </example>
    public sealed class PoolManager : IDisposable
    {
        readonly Dictionary<GameObject, PrefabPool> _pools = new();
        readonly GameObject _root;
        readonly int _defaultMaxInactive;

        /// <summary>
        /// 서비스로 등록된 PoolManager입니다. <c>Services.Get&lt;PoolManager&gt;()</c>와 같습니다.
        /// </summary>
        public static PoolManager Instance => Services.Get<PoolManager>();

        /// <param name="parent">쉬고 있는 인스턴스를 모아 둘 부모입니다. 보통 <see cref="Bootstrapper.Root"/>를 넘깁니다.</param>
        /// <param name="defaultMaxInactive">풀마다 보관할 최대 인스턴스 수의 기본값입니다.</param>
        public PoolManager(Transform parent, int defaultMaxInactive = 64)
        {
            _defaultMaxInactive = defaultMaxInactive;
            _root = new GameObject("[Pool]");
            if (parent != null)
                _root.transform.SetParent(parent, false);
            else if (Application.isPlaying)
                Object.DontDestroyOnLoad(_root);
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null) =>
            GetPool(prefab).Get(position, rotation, parent);

        public GameObject Spawn(GameObject prefab, Transform parent = null) =>
            GetPool(prefab).Get(Vector3.zero, Quaternion.identity, parent);

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component =>
            Spawn(prefab.gameObject, position, rotation, parent).GetComponent<T>();

        /// <summary>
        /// 인스턴스를 풀에 돌려놓습니다. 풀에서 만든 오브젝트가 아니면 Destroy합니다.
        /// </summary>
        public void Despawn(GameObject instance)
        {
            if (instance == null)
                return;

            var pooled = instance.GetComponent<PooledObject>();
            if (pooled != null && pooled.Pool != null)
                pooled.Pool.Release(instance);
            else if (Application.isPlaying)
                Object.Destroy(instance);
            else
                Object.DestroyImmediate(instance);
        }

        /// <summary>
        /// <paramref name="seconds"/>초 뒤에 인스턴스를 풀에 돌려놓습니다. 그 전에 다른 곳에서 돌려놓았으면 아무 일도 하지 않습니다.
        /// </summary>
        public async Awaitable DespawnAfter(GameObject instance, float seconds, bool unscaledTime = false,
            CancellationToken cancellationToken = default)
        {
            var pooled = instance != null ? instance.GetComponent<PooledObject>() : null;
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                await Awaitable.NextFrameAsync(cancellationToken);
                if (instance == null || (pooled != null && !pooled.IsSpawned))
                    return;

                elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            }

            Despawn(instance);
        }

        public void Prewarm(GameObject prefab, int count) => GetPool(prefab).Prewarm(count);

        /// <summary>
        /// 프리팹의 풀을 가져옵니다. 없으면 만듭니다. 풀마다 보관 수(<see cref="PrefabPool.MaxInactive"/>)를 바꿀 때 씁니다.
        /// </summary>
        public PrefabPool GetPool(GameObject prefab)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            if (!_pools.TryGetValue(prefab, out var pool))
            {
                var container = new GameObject(prefab.name).transform;
                container.SetParent(_root.transform, false);
                pool = new PrefabPool(prefab, container, _defaultMaxInactive);
                _pools.Add(prefab, pool);
            }

            return pool;
        }

        /// <summary>
        /// 모든 풀의 보관 중인 인스턴스를 파괴합니다. 씬을 크게 바꿀 때 메모리를 비우는 데 씁니다.
        /// </summary>
        public void ClearAll()
        {
            foreach (var pool in _pools.Values)
                pool.Clear();
        }

        public void Dispose() => ClearAll();
    }
}
