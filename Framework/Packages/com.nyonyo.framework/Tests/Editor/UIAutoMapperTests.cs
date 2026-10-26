using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nyonyo.Framework.Editor.UI;
using Nyonyo.Framework.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Nyonyo.Framework.Editor.Tests
{
    public class UIAutoMapperTests
    {
        GameObject _root;
        UIPrefixSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TestUI", typeof(RectTransform));

            // TMP 기본 폰트 의존을 피하려고 TMP 규칙은 빼고 테스트합니다.
            _settings = ScriptableObject.CreateInstance<UIPrefixSettings>();
            _settings.SetRules(
                new UIPrefixSettings.Rule("IMG_", "UnityEngine.UI.Image"),
                new UIPrefixSettings.Rule("BTN_", "UnityEngine.UI.Button"),
                new UIPrefixSettings.Rule("RT_", "UnityEngine.RectTransform"),
                new UIPrefixSettings.Rule("GO_", "UnityEngine.GameObject"),
                new UIPrefixSettings.Rule("UI_", "Nyonyo.Framework.UI.UIBase"));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_settings);
        }

        GameObject Add(string name, Transform parent = null, params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            foreach (var component in components)
                go.AddComponent(component);
            go.transform.SetParent(parent != null ? parent : _root.transform, false);
            return go;
        }

        void BuildMappedHierarchy()
        {
            var panel = Add("RT_Panel", null, typeof(Image));
            Add("IMG_Icon", panel.transform, typeof(Image));
            Add("BTN_Ok", panel.transform, typeof(Image), typeof(Button));
            Add("GO_Root");
            Add("Plain", null, typeof(Image));
            var child = Add("UI_Child", null, typeof(TestChildUI));
            Add("IMG_Inner", child.transform, typeof(Image));
        }

        [Test]
        public void Collect_MapsPrefixedObjectsToConcreteTypes()
        {
            BuildMappedHierarchy();

            var bindings = UIBindingCollector.Collect(_root, _settings);

            CollectionAssert.AreEqual(
                new[] { "RT_Panel", "IMG_Icon", "BTN_Ok", "GO_Root", "UI_Child" },
                bindings.Select(b => b.FieldName));
            CollectionAssert.AreEqual(
                new[] { typeof(RectTransform), typeof(Image), typeof(Button), typeof(GameObject), typeof(TestChildUI) },
                bindings.Select(b => b.FieldType));
        }

        [Test]
        public void Collect_DoesNotDescendIntoNestedUI()
        {
            BuildMappedHierarchy();

            var bindings = UIBindingCollector.Collect(_root, _settings);

            Assert.IsFalse(bindings.Any(b => b.FieldName == "IMG_Inner"), "자식 UI 아래 오브젝트는 자식 UI가 관리합니다.");
        }

        [Test]
        public void Collect_LongestPrefixWins()
        {
            _settings.SetRules(
                new UIPrefixSettings.Rule("IMG_", "UnityEngine.UI.Image"),
                new UIPrefixSettings.Rule("IMG_RT_", "UnityEngine.RectTransform"));
            Add("IMG_RT_Frame", null, typeof(Image));

            var binding = UIBindingCollector.Collect(_root, _settings).Single();

            Assert.AreEqual(typeof(RectTransform), binding.FieldType);
        }

        [Test]
        public void Collect_MissingComponent_WarnsAndSkips()
        {
            Add("IMG_NoImage");

            LogAssert.Expect(LogType.Warning, new Regex("IMG_NoImage"));
            var bindings = UIBindingCollector.Collect(_root, _settings);

            Assert.AreEqual(0, bindings.Count);
        }

        [Test]
        public void Collect_DuplicateName_WarnsAndKeepsFirst()
        {
            var first = Add("IMG_Same", null, typeof(Image));
            Add("IMG_Same", null, typeof(Image));

            LogAssert.Expect(LogType.Warning, new Regex("IMG_Same"));
            var bindings = UIBindingCollector.Collect(_root, _settings);

            Assert.AreEqual(1, bindings.Count);
            Assert.AreSame(first.GetComponent<Image>(), bindings[0].Value);
        }

        [Test]
        public void ToFieldName_ReplacesInvalidCharacters()
        {
            Assert.AreEqual("IMG_My_Icon_1_", UIBindingCollector.ToFieldName("IMG_My Icon(1)"));
        }

        [Test]
        public void CreateScript_PopupHasRegionFieldsUsingsAndButtonCases()
        {
            var bindings = new List<UIBinding>
            {
                new("IMG_A", typeof(Image), null),
                new("BTN_Ok", typeof(Button), null),
            };

            var code = UIScriptGenerator.CreateScript("Test", typeof(PopupBase), "", bindings);

            StringAssert.Contains("using Nyonyo.Framework.UI;", code);
            StringAssert.Contains("using UnityEngine.UI;", code);
            StringAssert.Contains("public class Test : PopupBase", code);
            StringAssert.Contains("#region Auto Generated UI", code);
            StringAssert.Contains("[SerializeField] private Image IMG_A;", code);
            StringAssert.Contains("protected override void OnOpen()", code);
            StringAssert.Contains("public override void OnButtonEvent(Button button)", code);
            StringAssert.Contains("case \"BTN_Ok\":", code);
            StringAssert.DoesNotContain("namespace", code);
        }

        [Test]
        public void CreateScript_UIBaseWithNamespace_WrapsClassAndSkipsOnOpen()
        {
            var code = UIScriptGenerator.CreateScript("Inventory", typeof(UIBase), "MyGame.UI", new List<UIBinding>());

            StringAssert.Contains("namespace MyGame.UI", code);
            StringAssert.Contains("    public class Inventory : UIBase", code);
            StringAssert.DoesNotContain("OnOpen", code);
        }

        [Test]
        public void UpdateScript_ReplacesOnlyRegion()
        {
            const string source =
                "using UnityEngine;\n" +
                "using UnityEngine.UI;\n" +
                "\n" +
                "public class Test : PopupBase\n" +
                "{\n" +
                "    #region Auto Generated UI\n" +
                "    [SerializeField] private Image IMG_Old;\n" +
                "    #endregion\n" +
                "\n" +
                "    void MyCode() { }\n" +
                "}\n";
            var bindings = new List<UIBinding> { new("IMG_New", typeof(Image), null) };

            var updated = UIScriptGenerator.UpdateScript(source, "Test", bindings);

            StringAssert.Contains("    [SerializeField] private Image IMG_New;", updated);
            StringAssert.DoesNotContain("IMG_Old", updated);
            StringAssert.Contains("    void MyCode() { }", updated);
            StringAssert.EndsWith("}\n", updated);
        }

        [Test]
        public void UpdateScript_InsertsRegionAndUsingWhenMissing()
        {
            const string source =
                "using UnityEngine;\r\n" +
                "\r\n" +
                "public class Test : UIBase\r\n" +
                "{\r\n" +
                "    void MyCode() { }\r\n" +
                "}\r\n";
            var bindings = new List<UIBinding> { new("IMG_A", typeof(Image), null) };

            var updated = UIScriptGenerator.UpdateScript(source, "Test", bindings);

            StringAssert.Contains("using UnityEngine.UI;\r\n", updated);
            StringAssert.Contains(
                "{\r\n" +
                "    #region Auto Generated UI\r\n" +
                "    #pragma warning disable CS0169, CS0649\r\n" +
                "    [SerializeField] private Image IMG_A;\r\n" +
                "    #pragma warning restore CS0169, CS0649\r\n" +
                "    #endregion\r\n", updated);
            StringAssert.Contains("    void MyCode() { }", updated);
        }

        [Test]
        public void UpdateScript_SameBindings_ReturnsSameText()
        {
            var bindings = new List<UIBinding> { new("IMG_A", typeof(Image), null) };
            var source = UIScriptGenerator.CreateScript("Test", typeof(UIBase), "", bindings);

            Assert.AreEqual(source, UIScriptGenerator.UpdateScript(source, "Test", bindings));
        }

        [Test]
        public void Assign_SetsFieldsAndBindsButtonClickOnce()
        {
            BuildMappedHierarchy();
            var ui = _root.AddComponent<TestMappedUI>();

            UIAutoMapper.Assign(ui, _settings);
            UIAutoMapper.Assign(ui, _settings);

            Assert.AreSame(_root.transform.Find("RT_Panel/IMG_Icon").GetComponent<Image>(), ui.Icon);
            Assert.AreSame(_root.transform.Find("RT_Panel/BTN_Ok").GetComponent<Button>(), ui.Ok);
            Assert.AreSame(_root.transform.Find("RT_Panel"), ui.Panel);
            Assert.AreSame(_root.transform.Find("GO_Root").gameObject, ui.Root);
            Assert.AreSame(_root.transform.Find("UI_Child").GetComponent<TestChildUI>(), ui.Child);

            var onClick = ui.Ok.onClick;
            Assert.AreEqual(1, onClick.GetPersistentEventCount());
            Assert.AreSame(ui, onClick.GetPersistentTarget(0));
            Assert.AreEqual(nameof(UIBase.OnButtonEvent), onClick.GetPersistentMethodName(0));
        }

        [Test]
        public void Assign_FieldMissing_Warns()
        {
            Add("IMG_Unknown", null, typeof(Image));
            var ui = _root.AddComponent<TestMappedUI>();

            LogAssert.Expect(LogType.Warning, new Regex("IMG_Unknown"));
            UIAutoMapper.Assign(ui, _settings);
        }
    }
}
