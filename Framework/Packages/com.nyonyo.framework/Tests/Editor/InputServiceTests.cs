#if NYONYO_INPUT_SYSTEM
using Nyonyo.Framework.Inputs;
using Nyonyo.Framework.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nyonyo.Framework.Editor.Tests
{
    public class InputServiceTests
    {
        private InputActionAsset _asset;

        [SetUp]
        public void SetUp()
        {
            _asset = ScriptableObject.CreateInstance<InputActionAsset>();
            _asset.AddActionMap("Player").AddAction("Jump", binding: "<Keyboard>/space");
            _asset.AddActionMap("UI").AddAction("Submit", binding: "<Keyboard>/enter");
        }

        [TearDown]
        public void TearDown()
        {
            _asset.Disable();
            Object.DestroyImmediate(_asset);
        }

        [Test]
        public void SwitchMap_EnablesOnlyThatMap()
        {
            var input = new InputService(_asset, initialMap: "Player");

            Assert.IsTrue(_asset.FindActionMap("Player").enabled);
            Assert.IsFalse(_asset.FindActionMap("UI").enabled);

            input.SwitchMap("UI");

            Assert.IsFalse(_asset.FindActionMap("Player").enabled);
            Assert.IsTrue(_asset.FindActionMap("UI").enabled);
            Assert.AreEqual("UI", input.CurrentMap.name);
        }

        [Test]
        public void PushAndPop_RestorePreviousMap()
        {
            var input = new InputService(_asset, initialMap: "Player");

            input.PushMap("UI");
            Assert.IsTrue(_asset.FindActionMap("UI").enabled);
            Assert.IsFalse(_asset.FindActionMap("Player").enabled);

            input.PopMap();
            Assert.IsTrue(_asset.FindActionMap("Player").enabled);
            Assert.IsFalse(_asset.FindActionMap("UI").enabled);
        }

        [Test]
        public void GetAction_FindsByNameOrPath()
        {
            var input = new InputService(_asset);

            Assert.AreEqual("Jump", input.GetAction("Jump").name);
            Assert.AreEqual("Jump", input.GetAction("Player/Jump").name);
        }

        [Test]
        public void BindingOverrides_AreSavedAndLoaded()
        {
            var save = new SaveService(new MemorySaveStorage());
            var input = new InputService(_asset, save);
            var jump = input.GetAction("Jump");

            jump.ApplyBindingOverride(0, "<Keyboard>/j");
            input.SaveBindings();
            jump.RemoveAllBindingOverrides();
            Assert.AreEqual("<Keyboard>/space", jump.bindings[0].effectivePath);

            new InputService(_asset, save);

            Assert.AreEqual("<Keyboard>/j", jump.bindings[0].effectivePath);
        }

        [Test]
        public void ResetBindings_RemovesOverridesAndSaves()
        {
            var save = new SaveService(new MemorySaveStorage());
            var input = new InputService(_asset, save);
            var jump = input.GetAction("Jump");
            jump.ApplyBindingOverride(0, "<Keyboard>/j");

            input.ResetBindings();

            Assert.AreEqual("<Keyboard>/space", jump.bindings[0].effectivePath);
            Assert.IsTrue(save.Exists(InputService.BindingsSaveKey, SaveScope.Global));
        }
    }
}
#endif
