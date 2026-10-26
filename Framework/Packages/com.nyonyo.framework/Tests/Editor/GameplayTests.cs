using System.Collections.Generic;
using Nyonyo.Framework.Cameras;
using Nyonyo.Framework.Gameplay;
using Nyonyo.Framework.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace Nyonyo.Framework.Editor.Tests
{
    public class GameplayTests
    {
        readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _objects.Clear();
        }

        Health CreateHealth(string name = "Target")
        {
            var go = new GameObject(name);
            _objects.Add(go);
            return go.AddComponent<Health>();
        }

        [Test]
        public void Health_StartsFullAndTakesDamage()
        {
            var health = CreateHealth();
            DamageInfo? received = null;
            health.Damaged += info => received = info;

            var applied = health.TakeDamage(30f);

            Assert.AreEqual(30f, applied);
            Assert.AreEqual(70f, health.Current);
            Assert.AreEqual(0.7f, health.Normalized, 1e-5f);
            Assert.AreEqual(30f, received.Value.Amount);
        }

        [Test]
        public void Health_DiesOnceAndIgnoresFurtherDamage()
        {
            var health = CreateHealth();
            var deaths = 0;
            health.Died += () => deaths++;

            var applied = health.TakeDamage(150f);
            health.TakeDamage(10f);

            Assert.AreEqual(100f, applied, "남은 체력보다 많이 깎이지 않습니다.");
            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(1, deaths);
            Assert.AreEqual(0f, health.Heal(50f), "죽은 상태에서는 회복하지 않습니다.");
        }

        [Test]
        public void Health_HealIsClampedToMax()
        {
            var health = CreateHealth();
            health.TakeDamage(10f);

            Assert.AreEqual(10f, health.Heal(50f));
            Assert.AreEqual(100f, health.Current);
        }

        [Test]
        public void Health_InvincibleAfterDamage()
        {
            var health = CreateHealth();
            var now = 0d;
            health.Clock = () => now;
            SetInvincibleSeconds(health, 1f);

            health.TakeDamage(10f);
            Assert.AreEqual(0f, health.TakeDamage(10f));

            now = 1.5d;
            Assert.AreEqual(10f, health.TakeDamage(10f));
        }

        [Test]
        public void Health_ResetsWhenSpawnedFromPool()
        {
            var health = CreateHealth();
            health.TakeDamage(100f);

            ((IPoolable)health).OnSpawned();

            Assert.IsFalse(health.IsDead);
            Assert.AreEqual(100f, health.Current);
        }

        [Test]
        public void DamageDealer_HitsTargetsOnLayerOnly()
        {
            var target = CreateHealth();
            var dealerObject = new GameObject("Bullet");
            _objects.Add(dealerObject);
            var dealer = dealerObject.AddComponent<DamageDealer2D>();
            Health hit = null;
            dealer.Hit += h => hit = h;

            Assert.IsTrue(dealer.TryHit(target.gameObject));
            Assert.AreSame(target, hit);
            Assert.AreEqual(90f, target.Current);

            target.gameObject.layer = 5;
            var serialized = new UnityEditor.SerializedObject(dealer);
            serialized.FindProperty("_targetLayers").intValue = 1 << 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsFalse(dealer.TryHit(target.gameObject));
            Assert.AreEqual(90f, target.Current);
        }

        [Test]
        public void DamageDealer_DoesNotHitItsOwner()
        {
            var owner = CreateHealth("Player");
            var sword = new GameObject("Sword");
            sword.transform.SetParent(owner.transform);
            var dealer = sword.AddComponent<DamageDealer2D>();

            Assert.IsFalse(dealer.TryHit(owner.gameObject));
            Assert.AreEqual(100f, owner.Current);
        }

        [Test]
        public void CameraMath_ClampsToBounds()
        {
            var bounds = new Rect(0f, 0f, 20f, 10f);
            var half = new Vector2(4f, 3f);

            Assert.AreEqual(new Vector2(4f, 3f), CameraMath2D.ClampToBounds(new Vector2(-5f, -5f), half, bounds));
            Assert.AreEqual(new Vector2(16f, 7f), CameraMath2D.ClampToBounds(new Vector2(50f, 50f), half, bounds));
            Assert.AreEqual(new Vector2(10f, 5f), CameraMath2D.ClampToBounds(new Vector2(10f, 5f), half, bounds));
        }

        [Test]
        public void CameraMath_CentersWhenBoundsSmallerThanView()
        {
            var clamped = CameraMath2D.ClampToBounds(new Vector2(100f, 100f), new Vector2(10f, 10f), new Rect(0f, 0f, 4f, 4f));

            Assert.AreEqual(new Vector2(2f, 2f), clamped);
        }

        [Test]
        public void CameraMath_DeadZoneFollowsOnlyBeyondEdge()
        {
            var deadZone = new Vector2(2f, 2f);

            Assert.AreEqual(Vector2.zero, CameraMath2D.ApplyDeadZone(Vector2.zero, new Vector2(0.5f, -0.9f), deadZone));
            Assert.AreEqual(new Vector2(2f, 0f), CameraMath2D.ApplyDeadZone(Vector2.zero, new Vector2(3f, 0f), deadZone));
            Assert.AreEqual(new Vector2(0f, -1f), CameraMath2D.ApplyDeadZone(Vector2.zero, new Vector2(0f, -2f), deadZone));
        }

        static void SetInvincibleSeconds(Health health, float seconds)
        {
            var serialized = new UnityEditor.SerializedObject(health);
            serialized.FindProperty("_invincibleSeconds").floatValue = seconds;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
