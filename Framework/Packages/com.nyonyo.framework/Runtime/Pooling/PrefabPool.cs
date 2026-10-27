using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nyonyo.Framework.Pooling
{
    /// <summary>
    /// 프리팹 하나의 인스턴스를 재사용합니다. 보통은 <see cref="PoolManager"/>를 통해 씁니다.
    /// </summary>
    public sealed class PrefabPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _container;
        private readonly Stack<PooledObject> _inactive = new();

        /// <param name="prefab">재사용할 프리팹입니다.</param>
        /// <param name="container">쉬고 있는 인스턴스를 모아 둘 부모입니다.</param>
        /// <param name="maxInactive">쉬고 있는 인스턴스를 이보다 많이 보관하지 않습니다. 넘치면 Destroy합니다.</param>
        public PrefabPool(GameObject prefab, Transform container, int maxInactive)
        {
            _prefab = prefab;
            _container = container;
            MaxInactive = Mathf.Max(0, maxInactive);
        }

        public GameObject Prefab => _prefab;

        public int MaxInactive { get; set; }

        /// <summary>풀에서 꺼내서 사용 중인 인스턴스 수입니다.</summary>
        public int CountActive { get; private set; }

        /// <summary>풀에 보관 중인 인스턴스 수입니다.</summary>
        public int CountInactive => _inactive.Count;

        /// <summary>
        /// 인스턴스를 꺼냅니다. 보관 중인 게 없으면 새로 만듭니다.
        /// </summary>
        /// <param name="parent">null이면 현재 활성 씬의 루트에 둡니다. 씬이 바뀌면 함께 파괴됩니다.</param>
        public GameObject Get(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var pooled = PopInactive() ?? Create();
            var instance = pooled.gameObject;

            instance.transform.SetParent(parent, false);
            if (parent == null && SceneManager.GetActiveScene().IsValid())
                SceneManager.MoveGameObjectToScene(instance, SceneManager.GetActiveScene());
            instance.transform.SetPositionAndRotation(position, rotation);

            pooled.IsSpawned = true;
            CountActive++;
            instance.SetActive(true);
            pooled.NotifySpawned();
            return instance;
        }

        /// <summary>
        /// 인스턴스를 풀에 돌려놓습니다. 이미 돌려놓은 인스턴스면 경고를 남기고 무시합니다.
        /// </summary>
        public void Release(GameObject instance)
        {
            var pooled = instance != null ? instance.GetComponent<PooledObject>() : null;
            if (pooled == null || pooled.Pool != this)
            {
                Debug.LogWarning($"[Nyonyo Framework] {(instance != null ? instance.name : "null")}은(는) {_prefab.name} 풀의 인스턴스가 아닙니다.", instance);
                return;
            }

            if (!pooled.IsSpawned)
            {
                Debug.LogWarning($"[Nyonyo Framework] {instance.name}은(는) 이미 풀에 돌려놓은 인스턴스입니다.", instance);
                return;
            }

            pooled.NotifyDespawned();
            pooled.IsSpawned = false;
            CountActive--;

            if (_inactive.Count >= MaxInactive)
            {
                pooled.Pool = null;
                DestroyObject(instance);
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(_container, false);
            _inactive.Push(pooled);
        }

        /// <summary>
        /// 인스턴스를 미리 만들어 보관해 둡니다. 게임 도중 처음 생성할 때 생기는 끊김을 줄입니다.
        /// </summary>
        public void Prewarm(int count)
        {
            while (_inactive.Count < Mathf.Min(count, MaxInactive))
                _inactive.Push(Create());
        }

        /// <summary>
        /// 보관 중인 인스턴스를 모두 파괴합니다. 사용 중인 인스턴스는 그대로 둡니다.
        /// </summary>
        public void Clear()
        {
            while (_inactive.Count > 0)
            {
                var pooled = _inactive.Pop();
                if (pooled == null)
                    continue;

                pooled.Pool = null;
                DestroyObject(pooled.gameObject);
            }
        }

        internal void OnInstanceDestroyed(PooledObject pooled)
        {
            if (pooled.IsSpawned)
                CountActive--;
        }

        private PooledObject PopInactive()
        {
            // 풀을 거치지 않고 파괴된 인스턴스는 건너뜁니다.
            while (_inactive.Count > 0)
            {
                var pooled = _inactive.Pop();
                if (pooled != null)
                    return pooled;
            }

            return null;
        }

        private PooledObject Create()
        {
            var instance = Object.Instantiate(_prefab, _container, false);
            instance.name = _prefab.name;
            instance.SetActive(false);

            var pooled = instance.GetComponent<PooledObject>();
            if (pooled == null)
                pooled = instance.AddComponent<PooledObject>();
            pooled.Pool = this;
            return pooled;
        }

        private void DestroyObject(GameObject instance)
        {
            if (Application.isPlaying)
                Object.Destroy(instance);
            else
                Object.DestroyImmediate(instance);
        }
    }
}
