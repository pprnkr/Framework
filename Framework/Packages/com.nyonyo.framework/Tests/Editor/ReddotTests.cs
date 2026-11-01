using System.Collections.Generic;
using Nyonyo.Framework.Tests;
using Nyonyo.Framework.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Editor.Tests
{
    public class ReddotTests
    {
        private readonly List<GameObject> _objects = new();

        [SetUp]
        public void SetUp() => Reddot.Clear();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
            Reddot.Clear();
        }

        // EditMode에서는 Awake가 불리지 않으므로 등록을 직접 호출합니다.
        private TestReddotObject CreateReddot(TestReddotType type)
        {
            var go = new GameObject("Button");
            _objects.Add(go);
            var dot = new GameObject("Dot");
            dot.transform.SetParent(go.transform, false);

            var reddot = go.AddComponent<TestReddotObject>();
            var serialized = new SerializedObject(reddot);
            serialized.FindProperty("_type").intValue = (int)type;
            serialized.FindProperty("_target").objectReferenceValue = dot;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Reddot.Register(reddot);
            return reddot;
        }

        [Test]
        public void Register_WhenTypeIsInactive_HidesTarget()
        {
            var reddot = CreateReddot(TestReddotType.Mail);

            Assert.IsFalse(reddot.Target.activeSelf);
        }

        [Test]
        public void Register_WhenTypeWasActivatedBefore_ShowsTarget()
        {
            Reddot.Activate(TestReddotType.Mail, true);

            var reddot = CreateReddot(TestReddotType.Mail);

            Assert.IsTrue(reddot.Target.activeSelf);
        }

        [Test]
        public void Activate_TogglesAllReddotsOfThatTypeOnly()
        {
            var mailA = CreateReddot(TestReddotType.Mail);
            var mailB = CreateReddot(TestReddotType.Mail);
            var quest = CreateReddot(TestReddotType.Quest);

            Reddot.Activate(TestReddotType.Mail, true);

            Assert.IsTrue(mailA.Target.activeSelf);
            Assert.IsTrue(mailB.Target.activeSelf);
            Assert.IsFalse(quest.Target.activeSelf);
            Assert.IsTrue(Reddot.IsActive(TestReddotType.Mail));
            Assert.IsFalse(Reddot.IsActive(TestReddotType.Quest));

            Reddot.Activate(TestReddotType.Mail, false);

            Assert.IsFalse(mailA.Target.activeSelf);
            Assert.IsFalse(mailB.Target.activeSelf);
            Assert.IsFalse(Reddot.IsActive(TestReddotType.Mail));
        }

        [Test]
        public void Activate_AfterUnregister_DoesNotTouchTarget()
        {
            var reddot = CreateReddot(TestReddotType.Mail);
            Reddot.Unregister(reddot);

            Reddot.Activate(TestReddotType.Mail, true);

            Assert.IsFalse(reddot.Target.activeSelf);
        }

        [Test]
        public void Activate_SkipsDestroyedReddot()
        {
            var destroyed = CreateReddot(TestReddotType.Mail);
            var alive = CreateReddot(TestReddotType.Mail);
            Object.DestroyImmediate(destroyed.gameObject);

            Assert.DoesNotThrow(() => Reddot.Activate(TestReddotType.Mail, true));
            Assert.IsTrue(alive.Target.activeSelf);
        }

        [Test]
        public void Activate_WithDestroyedTarget_DoesNotTouchOwnObject()
        {
            var reddot = CreateReddot(TestReddotType.Mail);
            Object.DestroyImmediate(reddot.Target);

            Reddot.Activate(TestReddotType.Mail, false);

            Assert.IsTrue(reddot.gameObject.activeSelf);
        }

        [Test]
        public void DeactivateAll_HidesEveryReddotOfThatEnum()
        {
            var mail = CreateReddot(TestReddotType.Mail);
            var quest = CreateReddot(TestReddotType.Quest);
            Reddot.Activate(TestReddotType.Mail, true);
            Reddot.Activate(TestReddotType.Quest, true);

            Reddot.DeactivateAll<TestReddotType>();

            Assert.IsFalse(mail.Target.activeSelf);
            Assert.IsFalse(quest.Target.activeSelf);
            Assert.IsFalse(Reddot.IsActive(TestReddotType.Mail));
            Assert.IsFalse(Reddot.IsActive(TestReddotType.Quest));
        }
    }
}
