using NUnit.Framework;
using UnityEngine;

namespace Nyonyo.Framework.Editor.Tests
{
    public class UIBaseTests
    {
        GameObject _go;
        TestChildUI _ui;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("UI", typeof(RectTransform));
            _ui = _go.AddComponent<TestChildUI>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void Hide_DeactivatesAndCallsOnHideOnce()
        {
            _ui.Hide();
            _ui.Hide();

            Assert.IsFalse(_ui.IsVisible);
            CollectionAssert.AreEqual(new[] { "hide" }, _ui.Log);
        }

        [Test]
        public void Show_ActivatesAndCallsOnShowOnce()
        {
            _ui.Hide();

            _ui.Show();
            _ui.Show();

            Assert.IsTrue(_ui.IsVisible);
            CollectionAssert.AreEqual(new[] { "hide", "show" }, _ui.Log);
        }
    }
}
