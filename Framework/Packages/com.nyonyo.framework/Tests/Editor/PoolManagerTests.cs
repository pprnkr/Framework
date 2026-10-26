using System.Text.RegularExpressions;
using Nyonyo.Framework.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nyonyo.Framework.Editor.Tests
{
    public class PoolManagerTests
    {
        GameObject _parent;
        GameObject _prefab;
        PoolManager _pools;

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject("Pool Parent");
            _prefab = new GameObject("Bullet");
            _prefab.AddComponent<TestPoolable>();
            _pools = new PoolManager(_parent.transform, defaultMaxInactive: 2);
        }

        [TearDown]
        public void TearDown()
        {
            _pools.Dispose();
            Object.DestroyImmediate(_parent);
            Object.DestroyImmediate(_prefab);
            foreach (var leftover in Object.FindObjectsByType<PooledObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(leftover.gameObject);
        }

        [Test]
        public void Spawn_CreatesActiveInstanceAtPosition()
        {
            var instance = _pools.Spawn(_prefab, new Vector3(1, 2, 0), Quaternion.identity);

            Assert.IsTrue(instance.activeSelf);
            Assert.AreEqual(new Vector3(1, 2, 0), instance.transform.position);
            Assert.AreEqual("Bullet", instance.name);
            Assert.AreEqual(1, _pools.GetPool(_prefab).CountActive);
        }

        [Test]
        public void Despawn_ThenSpawn_ReusesSameInstance()
        {
            var first = _pools.Spawn(_prefab);
            _pools.Despawn(first);

            Assert.IsFalse(first.activeSelf);
            Assert.AreEqual(1, _pools.GetPool(_prefab).CountInactive);

            var second = _pools.Spawn(_prefab);

            Assert.AreSame(first, second);
            Assert.AreEqual(0, _pools.GetPool(_prefab).CountInactive);
        }

        [Test]
        public void SpawnAndDespawn_NotifyPoolables()
        {
            var instance = _pools.Spawn(_prefab);
            _pools.Despawn(instance);
            _pools.Spawn(_prefab);

            CollectionAssert.AreEqual(new[] { "spawned", "despawned", "spawned" }, instance.GetComponent<TestPoolable>().Log);
        }

        [Test]
        public void Despawn_Twice_WarnsAndIgnores()
        {
            var instance = _pools.Spawn(_prefab);
            _pools.Despawn(instance);

            LogAssert.Expect(LogType.Warning, new Regex("이미"));
            _pools.Despawn(instance);

            Assert.AreEqual(1, _pools.GetPool(_prefab).CountInactive);
        }

        [Test]
        public void Despawn_OverMaxInactive_DestroysInstance()
        {
            var a = _pools.Spawn(_prefab);
            var b = _pools.Spawn(_prefab);
            var c = _pools.Spawn(_prefab);

            _pools.Despawn(a);
            _pools.Despawn(b);
            _pools.Despawn(c);

            Assert.AreEqual(2, _pools.GetPool(_prefab).CountInactive);
            Assert.IsTrue(c == null, "보관 한도를 넘은 인스턴스는 파괴됩니다.");
        }

        [Test]
        public void Despawn_NonPooledObject_DestroysIt()
        {
            var plain = new GameObject("Plain");

            _pools.Despawn(plain);

            Assert.IsTrue(plain == null);
        }

        [Test]
        public void Prewarm_CreatesInactiveInstancesUpToMax()
        {
            _pools.Prewarm(_prefab, 5);

            Assert.AreEqual(2, _pools.GetPool(_prefab).CountInactive);
            Assert.AreEqual(0, _pools.GetPool(_prefab).CountActive);
        }

        [Test]
        public void SpawnComponent_ReturnsComponentOfInstance()
        {
            var poolable = _pools.Spawn(_prefab.GetComponent<TestPoolable>(), Vector3.zero, Quaternion.identity);

            Assert.AreNotSame(_prefab.GetComponent<TestPoolable>(), poolable);
            Assert.IsTrue(poolable.gameObject.activeSelf);
        }
    }
}
