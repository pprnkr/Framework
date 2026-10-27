using Nyonyo.Framework.Audio;
using Nyonyo.Framework.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Nyonyo.Framework.Editor.Tests
{
    public class UIViewTests
    {
        private GameObject _go;
        private TestChildUI _ui;

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

        private Button CreateButton(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_go.transform, false);
            return go.GetComponent<Button>();
        }

        [Test]
        public void OnButtonEvent_PlaysClickSoundAndCallsOnButtonClick()
        {
            var loader = new FakeAssetLoader { Factory = path => AudioClip.Create(path, 4410, 1, 44100, false) };
            var sound = new SoundManager(null, _go.transform, loader);
            var registry = new ServiceRegistry();
            registry.Register(sound);
            Services.SetRegistry(registry);

            try
            {
                _ui.OnButtonEvent(CreateButton("BTN_Ok"));

                Assert.AreEqual("SFX/Click", sound.GetSfxSource(0).clip.name);
                CollectionAssert.AreEqual(new[] { "click:BTN_Ok" }, _ui.Log);
            }
            finally
            {
                Services.SetRegistry(null);
                sound.Dispose();
                foreach (var clip in loader.Assets.Values)
                    Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void OnButtonEvent_WithoutServices_StillCallsOnButtonClick()
        {
            Services.SetRegistry(null);

            _ui.OnButtonEvent(CreateButton("BTN_Ok"));

            CollectionAssert.AreEqual(new[] { "click:BTN_Ok" }, _ui.Log);
        }
    }
}
