using System;
using System.Collections.Generic;
using Nyonyo.Framework.Events;
using Nyonyo.Framework.Scenes;
using Nyonyo.Framework.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Editor.Tests
{
    public class PopupManagerTests
    {
        GameObject _parent;
        EventBus _eventBus;
        PopupManager _manager;
        TestPopupA _prefabA;
        TestPopupB _prefabB;
        readonly List<Object> _cleanup = new();

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject("Test UI Parent");
            _eventBus = new EventBus();
            _manager = new PopupManager(null, _parent.transform, _eventBus);

            // Edit 모드에서는 씬에 있는 오브젝트를 프리팹 대신 원본으로 씁니다.
            _prefabA = CreateTemplate<TestPopupA>("TestPopupA");
            _prefabB = CreateTemplate<TestPopupB>("TestPopupB");
            _manager.RegisterPrefab(_prefabA);
            _manager.RegisterPrefab(_prefabB);
        }

        [TearDown]
        public void TearDown()
        {
            _manager.Dispose();
            _eventBus.Dispose();
            Services.SetRegistry(null);
            Object.DestroyImmediate(_parent);
            foreach (var obj in _cleanup)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            _cleanup.Clear();
        }

        T CreateTemplate<T>(string name) where T : PopupBase
        {
            var go = new GameObject(name, typeof(RectTransform));
            _cleanup.Add(go);
            return go.AddComponent<T>();
        }

        static void SetSerialized(Object target, string propertyName, Action<SerializedProperty> set)
        {
            var serializedObject = new SerializedObject(target);
            set(serializedObject.FindProperty(propertyName));
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void Instance_ReturnsRegisteredService()
        {
            var registry = new ServiceRegistry();
            registry.Register(_manager);
            Services.SetRegistry(registry);

            Assert.AreSame(_manager, PopupManager.Instance);
        }

        [Test]
        public void LayerCanvases_HigherLayerIsDrawnAbove()
        {
            var layers = (UILayer[])Enum.GetValues(typeof(UILayer));
            for (var i = 1; i < layers.Length; i++)
            {
                var lower = _manager.GetLayerRoot(layers[i - 1]).GetComponent<Canvas>().sortingOrder;
                var upper = _manager.GetLayerRoot(layers[i]).GetComponent<Canvas>().sortingOrder;
                Assert.Greater(upper, lower, $"{layers[i]} 층이 {layers[i - 1]} 층보다 위에 그려져야 합니다.");
            }
        }

        [Test]
        public void Show_PlacesPopupOnPrefabLayer_AndCallsOnOpen()
        {
            var popup = _manager.Show<TestPopupA>();

            Assert.AreEqual(_manager.GetLayerRoot(UILayer.Popup), popup.transform.parent);
            Assert.AreEqual(UILayer.Popup, popup.Layer);
            CollectionAssert.AreEqual(new[] { "open" }, popup.Log);
        }

        [Test]
        public void Show_WithLayer_UsesGivenLayer()
        {
            var popup = _manager.Show<TestPopupA>(UILayer.Notice);

            Assert.AreEqual(_manager.GetLayerRoot(UILayer.Notice), popup.transform.parent);
            Assert.AreEqual(UILayer.Notice, popup.Layer);
        }

        [Test]
        public void Show_SameLayer_LaterPopupIsDrawnOnTop()
        {
            var first = _manager.Show<TestPopupA>();
            var second = _manager.Show<TestPopupB>();
            var third = _manager.Show<TestPopupA>();

            Assert.Greater(second.transform.GetSiblingIndex(), first.transform.GetSiblingIndex());
            Assert.Greater(third.transform.GetSiblingIndex(), second.transform.GetSiblingIndex());
        }

        [Test]
        public void Show_Unregistered_Throws()
        {
            var manager = new PopupManager(null, _parent.transform);

            var e = Assert.Throws<InvalidOperationException>(() => manager.Show<TestPopupA>());
            StringAssert.Contains(nameof(TestPopupA), e.Message);
        }

        [Test]
        public void Close_DestroysPopupAndRemovesFromOpenList()
        {
            var popup = _manager.Show<TestPopupA>();

            popup.Close();

            Assert.IsTrue(popup == null, "닫힌 팝업은 Destroy되어야 합니다.");
            Assert.IsFalse(_manager.IsOpen<TestPopupA>());
            Assert.AreEqual(0, _manager.GetOpenPopups(UILayer.Popup).Count);
        }

        [Test]
        public void Close_RaisesClosedEventOnce()
        {
            var popup = _manager.Show<TestPopupA>();
            var count = 0;
            popup.Closed += _ => count++;

            popup.Close();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Find_ReturnsTopmostInstance()
        {
            _manager.Show<TestPopupA>(UILayer.System);
            _manager.Show<TestPopupA>(UILayer.Popup);
            var top = _manager.Show<TestPopupA>(UILayer.Notice);

            Assert.AreSame(top, _manager.Find<TestPopupA>());
            Assert.IsNull(_manager.Find<TestPopupB>());
        }

        [Test]
        public void HandleBack_SkipsHiddenPopups()
        {
            var lower = _manager.Show<TestPopupA>(UILayer.Popup);
            var hidden = _manager.Show<TestPopupB>(UILayer.System);
            hidden.Hide();

            Assert.IsTrue(_manager.HandleBack());

            Assert.IsTrue(lower == null);
            Assert.IsTrue(hidden != null);
        }

        [Test]
        public void HandleBack_ClosesTopmostPopupAcrossLayers()
        {
            var lower = _manager.Show<TestPopupA>(UILayer.Popup);
            var upper = _manager.Show<TestPopupB>(UILayer.System);

            Assert.IsTrue(_manager.HandleBack());
            Assert.IsTrue(upper == null);
            Assert.IsTrue(lower != null);

            Assert.IsTrue(_manager.HandleBack());
            Assert.IsTrue(lower == null);
        }

        [Test]
        public void HandleBack_SkipsIgnorePopups()
        {
            SetSerialized(_prefabB, "_backKey", p => p.enumValueIndex = (int)BackKeyBehavior.Ignore);
            var popup = _manager.Show<TestPopupA>(UILayer.Popup);
            var notice = _manager.Show<TestPopupB>(UILayer.Notice);

            Assert.IsTrue(_manager.HandleBack());

            Assert.IsTrue(popup == null);
            Assert.IsTrue(notice != null);
        }

        [Test]
        public void HandleBack_BlockPopupStopsBackKey()
        {
            SetSerialized(_prefabB, "_backKey", p => p.enumValueIndex = (int)BackKeyBehavior.Block);
            var popup = _manager.Show<TestPopupA>(UILayer.Popup);
            var blocker = _manager.Show<TestPopupB>(UILayer.System);

            Assert.IsTrue(_manager.HandleBack());

            Assert.IsTrue(blocker != null);
            Assert.IsTrue(popup != null);
        }

        [Test]
        public void HandleBack_NothingOpen_ReturnsFalseAndRaisesUnhandled()
        {
            var unhandled = false;
            _manager.BackKeyUnhandled += () => unhandled = true;

            Assert.IsFalse(_manager.HandleBack());
            Assert.IsTrue(unhandled);
        }

        [Test]
        public void CloseAll_WithLayer_ClosesOnlyThatLayer()
        {
            var popup = _manager.Show<TestPopupA>(UILayer.Popup);
            var system = _manager.Show<TestPopupB>(UILayer.System);

            _manager.CloseAll(UILayer.Popup);

            Assert.IsTrue(popup == null);
            Assert.IsTrue(system != null);
        }

        [Test]
        public void SceneLoaded_ClosesOnlyPopupsMarkedCloseOnSceneChange()
        {
            SetSerialized(_prefabB, "_closeOnSceneChange", p => p.boolValue = false);
            var closes = _manager.Show<TestPopupA>();
            var stays = _manager.Show<TestPopupB>();

            _eventBus.Publish(new SceneLoaded("Next"));

            Assert.IsTrue(closes == null);
            Assert.IsTrue(stays != null);
        }

        [Test]
        public void Show_LoadsPrefabFromPopupAttributeOnce_AndReleasesOnDispose()
        {
            var loader = new FakeAssetLoader();
            loader.Assets["Popups/TestPopupB"] = _prefabB.gameObject;
            var manager = new PopupManager(null, _parent.transform, null, loader);

            var first = manager.Show<TestPopupB>();
            var second = manager.Show<TestPopupB>();

            Assert.IsNotNull(first);
            Assert.AreNotSame(first, second);
            CollectionAssert.AreEqual(new[] { "load:Popups/TestPopupB:Resources" }, loader.Log);

            manager.Dispose();
            CollectionAssert.Contains(loader.Log, "release:Popups/TestPopupB:Resources");
        }

        [Test]
        public void ShowAsync_LoadsPrefabFromPopupAttribute()
        {
            var loader = new FakeAssetLoader();
            loader.Assets["Popups/TestPopupB"] = _prefabB.gameObject;
            var manager = new PopupManager(null, _parent.transform, null, loader);

            var awaiter = manager.ShowAsync<TestPopupB>(UILayer.System).GetAwaiter();
            Assert.IsTrue(awaiter.IsCompleted);
            var popup = awaiter.GetResult();

            Assert.AreEqual(manager.GetLayerRoot(UILayer.System), popup.transform.parent);
            manager.Dispose();
        }

        [Test]
        public void Show_WithoutAttributeOrRegistration_Throws()
        {
            var manager = new PopupManager(null, _parent.transform, null, new FakeAssetLoader());

            var e = Assert.Throws<InvalidOperationException>(() => manager.Show<TestPopupA>());
            StringAssert.Contains("Popup", e.Message);
            manager.Dispose();
        }

        [Test]
        public void Show_LoadedPrefabWithoutComponent_ThrowsAndReleases()
        {
            var loader = new FakeAssetLoader();
            var wrongPrefab = new GameObject("Wrong", typeof(RectTransform));
            _cleanup.Add(wrongPrefab);
            loader.Assets["Popups/TestPopupB"] = wrongPrefab;
            var manager = new PopupManager(null, _parent.transform, null, loader);

            Assert.Throws<InvalidOperationException>(() => manager.Show<TestPopupB>());
            CollectionAssert.Contains(loader.Log, "release:Popups/TestPopupB:Resources");
            manager.Dispose();
        }

        [Test]
        public void DestroyedWithoutClose_IsRemovedFromOpenList()
        {
            var popup = _manager.Show<TestPopupA>();

            Object.DestroyImmediate(popup.gameObject);

            Assert.AreEqual(0, _manager.GetOpenPopups(UILayer.Popup).Count);
        }
    }
}
