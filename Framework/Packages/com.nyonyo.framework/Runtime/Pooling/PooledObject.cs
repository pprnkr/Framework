using UnityEngine;

namespace Nyonyo.Framework.Pooling
{
    /// <summary>
    /// 풀에서 만든 인스턴스에 자동으로 붙어서, 어느 풀에 속하는지 기억합니다. 직접 붙이지 마세요.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        IPoolable[] _poolables;

        internal PrefabPool Pool { get; set; }

        /// <summary>풀에서 꺼내져 사용 중이면 true입니다.</summary>
        public bool IsSpawned { get; internal set; }

        /// <summary>
        /// 이 오브젝트를 풀에 돌려놓습니다. <c>PoolManager.Despawn(gameObject)</c>와 같습니다.
        /// </summary>
        public void Despawn()
        {
            if (Pool != null)
                Pool.Release(gameObject);
            else
                Destroy(gameObject);
        }

        internal void NotifySpawned()
        {
            _poolables ??= GetComponentsInChildren<IPoolable>(true);
            foreach (var poolable in _poolables)
                poolable.OnSpawned();
        }

        internal void NotifyDespawned()
        {
            _poolables ??= GetComponentsInChildren<IPoolable>(true);
            foreach (var poolable in _poolables)
                poolable.OnDespawned();
        }

        // 씬과 함께 파괴되는 등 풀을 거치지 않고 사라지면 풀의 개수 기록을 맞춥니다.
        void OnDestroy() => Pool?.OnInstanceDestroyed(this);
    }
}
