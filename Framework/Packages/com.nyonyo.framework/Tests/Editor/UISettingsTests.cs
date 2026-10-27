using Nyonyo.Framework.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Nyonyo.Framework.Editor.Tests
{
    public class UISettingsTests
    {
        private UISettings _settings;
        private GameObject _parent;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<UISettings>();
            _parent = new GameObject("Test UI Parent");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_parent);
        }

        private void SetOrientation(GameOrientation orientation, Vector2 resolution)
        {
            var serializedObject = new SerializedObject(_settings);
            serializedObject.FindProperty("_orientation").enumValueIndex = (int)orientation;
            serializedObject.FindProperty("_referenceResolution").vector2Value = resolution;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void Default_IsPortrait1080x1920()
        {
            Assert.AreEqual(GameOrientation.Portrait, _settings.Orientation);
            Assert.AreEqual(new Vector2(1080, 1920), _settings.ReferenceResolution);
        }

        [Test]
        public void Portrait_WithLandscapeResolution_PutsShortSideOnX()
        {
            SetOrientation(GameOrientation.Portrait, new Vector2(1920, 1080));

            Assert.AreEqual(new Vector2(1080, 1920), _settings.ReferenceResolution);
        }

        [Test]
        public void Landscape_WithPortraitResolution_PutsLongSideOnX()
        {
            SetOrientation(GameOrientation.Landscape, new Vector2(1080, 1920));

            Assert.AreEqual(new Vector2(1920, 1080), _settings.ReferenceResolution);
        }

        [Test]
        public void PopupManager_UsesOrientedResolution()
        {
            SetOrientation(GameOrientation.Landscape, new Vector2(1080, 1920));
            var manager = new PopupManager(_settings, _parent.transform);

            var scaler = manager.GetLayerRoot(UILayer.Popup).GetComponent<CanvasScaler>();
            Assert.AreEqual(new Vector2(1920, 1080), scaler.referenceResolution);
            Assert.AreEqual(CanvasScaler.ScreenMatchMode.Expand, scaler.screenMatchMode);

            manager.Dispose();
        }

        [Test]
        public void PopupManager_WithoutSettings_UsesPortraitDefault()
        {
            var manager = new PopupManager(null, _parent.transform);

            var scaler = manager.GetLayerRoot(UILayer.Popup).GetComponent<CanvasScaler>();
            Assert.AreEqual(new Vector2(1080, 1920), scaler.referenceResolution);

            manager.Dispose();
        }
    }
}
