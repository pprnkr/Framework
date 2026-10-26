using System.IO;
using System.Linq;
using Nyonyo.Framework;
using Nyonyo.Framework.Editor.UI;
using Nyonyo.Framework.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Nyonyo.Dev.Editor
{
    /// <summary>
    /// 개발용 샘플(팝업 프리팹, UISettings, DevInstaller, 테스트 씬)을 한 번에 준비합니다. 여러 번 실행해도 됩니다.
    /// </summary>
    static class DevSampleSetup
    {
        const string DevFolder = "Assets/Dev";
        const string PrefabFolder = DevFolder + "/Prefabs";
        const string UISettingsPath = DevFolder + "/DevUISettings.asset";
        const string InstallerPath = DevFolder + "/DevInstaller.asset";
        const string FrameworkSettingsPath = "Assets/Resources/NyonyoFrameworkSettings.asset";
        const string SceneA = "Assets/Scenes/SampleScene.unity";
        const string SceneB = "Assets/Scenes/SampleScene2.unity";

        [MenuItem("Tools/Nyonyo Framework/Dev/Setup Samples")]
        static void Setup()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder(DevFolder, "Prefabs");

            var popupTest = CreatePopupTestPrefab();
            var popupNotice = CreatePopupNoticePrefab();
            var uiSettings = CreateUISettings(popupTest, popupNotice);
            var installer = CreateInstaller(uiSettings);
            AddInstallerToFrameworkSettings(installer);
            SetupScenes();

            AssetDatabase.SaveAssets();
            Debug.Log("[Dev] 샘플 준비 완료. Play를 누르고 왼쪽 위 버튼으로 확인하세요.");
        }

        static PopupTest CreatePopupTestPrefab()
        {
            var root = CreateUI("PopupTest", null);
            Stretch(root);
            var popup = root.gameObject.AddComponent<PopupTest>();

            var dim = CreateUI("Dim", root, typeof(Image));
            Stretch(dim);
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

            // 접두사 이름(RT_, BTN_)을 붙여 두면 UIAutoMapper.Assign이 필드와 버튼 클릭을 자동으로 연결합니다.
            // 샘플 텍스트는 TMP 기본 리소스 없이도 보이도록 기존 Text를 쓰므로 TXT_ 접두사를 붙이지 않습니다.
            var panel = CreateUI("RT_Panel", root, typeof(Image));
            panel.sizeDelta = new Vector2(760f, 420f);
            panel.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f, 1f);

            CreateText(panel, "Title", "PopupTest", 44, new Vector2(0f, 150f), new Vector2(700f, 70f));
            CreateText(panel, "Body", "Open Another: stacks on the same layer.\nShow Notice: band alarm on the Notice layer.",
                26, new Vector2(0f, 40f), new Vector2(700f, 120f));

            CreateButton(panel, "BTN_Stack", "Open Another", new Vector2(-240f, -130f));
            CreateButton(panel, "BTN_Notice", "Show Notice", new Vector2(0f, -130f));
            CreateButton(panel, "BTN_Ok", "OK", new Vector2(240f, -130f));
            CreateButton(panel, "BTN_Close", "X", new Vector2(340f, 170f), new Vector2(60f, 60f));

            UIAutoMapper.Assign(popup);

            return SavePrefab<PopupTest>(root.gameObject, $"{PrefabFolder}/PopupTest.prefab");
        }

        static PopupNotice CreatePopupNoticePrefab()
        {
            var root = CreateUI("PopupNotice", null);
            Stretch(root);
            var popup = root.gameObject.AddComponent<PopupNotice>();

            var band = CreateUI("Band", root, typeof(Image));
            band.anchorMin = new Vector2(0f, 1f);
            band.anchorMax = new Vector2(1f, 1f);
            band.pivot = new Vector2(0.5f, 1f);
            band.sizeDelta = new Vector2(0f, 110f);
            band.anchoredPosition = new Vector2(0f, -60f);
            var bandImage = band.GetComponent<Image>();
            bandImage.color = new Color(0.85f, 0.25f, 0.2f, 0.92f);
            bandImage.raycastTarget = false;

            var message = CreateText(band, "Message", "Notice", 34, Vector2.zero, Vector2.zero);
            Stretch(message.rectTransform);
            message.color = Color.white;

            var serialized = new SerializedObject(popup);
            serialized.FindProperty("_layer").enumValueIndex = (int)UILayer.Notice;
            serialized.FindProperty("_backKey").enumValueIndex = (int)BackKeyBehavior.Ignore;
            serialized.FindProperty("_closeOnSceneChange").boolValue = false;
            serialized.FindProperty("_message").objectReferenceValue = message;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab<PopupNotice>(root.gameObject, $"{PrefabFolder}/PopupNotice.prefab");
        }

        static UISettings CreateUISettings(params PopupBase[] prefabs)
        {
            var settings = AssetDatabase.LoadAssetAtPath<UISettings>(UISettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<UISettings>();
                AssetDatabase.CreateAsset(settings, UISettingsPath);
            }

            var serialized = new SerializedObject(settings);
            var list = serialized.FindProperty("_popupPrefabs");
            list.arraySize = prefabs.Length;
            for (var i = 0; i < prefabs.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        static DevInstaller CreateInstaller(UISettings uiSettings)
        {
            // 이미 만들어 둔 DevInstaller가 있으면 그걸 씁니다.
            var guid = AssetDatabase.FindAssets("t:DevInstaller").FirstOrDefault();
            var installer = guid != null
                ? AssetDatabase.LoadAssetAtPath<DevInstaller>(AssetDatabase.GUIDToAssetPath(guid))
                : null;
            if (installer == null)
            {
                installer = ScriptableObject.CreateInstance<DevInstaller>();
                AssetDatabase.CreateAsset(installer, InstallerPath);
            }

            SetReference(installer, "_uiSettings", uiSettings);
            return installer;
        }

        static void AddInstallerToFrameworkSettings(DevInstaller installer)
        {
            var settings = AssetDatabase.LoadAssetAtPath<FrameworkSettings>(FrameworkSettingsPath);
            if (settings == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                settings = ScriptableObject.CreateInstance<FrameworkSettings>();
                AssetDatabase.CreateAsset(settings, FrameworkSettingsPath);
            }

            var serialized = new SerializedObject(settings);
            var list = serialized.FindProperty("_installers");
            for (var i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == installer)
                    return;
            }

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = installer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetupScenes()
        {
            if (!File.Exists(SceneB))
                AssetDatabase.CopyAsset(SceneA, SceneB);

            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (var path in new[] { SceneA, SceneB })
            {
                if (scenes.All(s => s.path != path))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static RectTransform CreateUI(string name, Transform parent, params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            foreach (var component in components)
                go.AddComponent(component);
            if (parent != null)
                go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Text CreateText(Transform parent, string name, string value, int fontSize, Vector2 position, Vector2 size)
        {
            var rect = CreateUI(name, parent, typeof(Text));
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = rect.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            text.raycastTarget = false;
            return text;
        }

        static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2? size = null)
        {
            var rect = CreateUI(name, parent, typeof(Image), typeof(Button));
            rect.anchoredPosition = position;
            rect.sizeDelta = size ?? new Vector2(220f, 70f);
            rect.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.85f, 1f);

            var text = CreateText(rect, "Label", label, 28, Vector2.zero, Vector2.zero);
            Stretch(text.rectTransform);
            text.color = Color.white;
            return rect.GetComponent<Button>();
        }

        static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static T SavePrefab<T>(GameObject root, string path) where T : Component
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<T>();
        }
    }
}
