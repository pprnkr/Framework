using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Nyonyo.Framework.Assets;
using Nyonyo.Framework.Editor.Assets;
using Nyonyo.Framework.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nyonyo.Framework.Editor.UI
{
    /// <summary>
    /// 메뉴 Tools > Create UI Script > Popup / UI.
    /// 선택한 프리팹(또는 열려 있는 프리팹 모드)의 접두사 오브젝트로 새 스크립트를 만들고, 컴파일이 끝나면 프리팹에 붙여서 연결합니다.
    /// </summary>
    static class UIScriptMenu
    {
        const string MenuRoot = "Tools/Create UI Script/";
        const string LastFolderKey = "Nyonyo.Framework.UIScriptMenu.LastFolder";

        static readonly Regex s_identifier = new("^[A-Za-z_][A-Za-z0-9_]*$");

        [MenuItem(MenuRoot + "Popup")]
        static void CreatePopupScript() => CreateScript(typeof(PopupBase));

        [MenuItem(MenuRoot + "UI")]
        static void CreateUIScript() => CreateScript(typeof(UIBase));

        [MenuItem(MenuRoot + "Popup", true)]
        [MenuItem(MenuRoot + "UI", true)]
        static bool CanCreateScript() => TryGetTargetPrefab(out _, out _);

        static void CreateScript(Type baseType)
        {
            if (!TryGetTargetPrefab(out var prefabPath, out var root))
            {
                EditorUtility.DisplayDialog("Create UI Script", "Project 창에서 프리팹을 선택하거나, 프리팹 모드에서 실행해 주세요.", "확인");
                return;
            }

            var existing = root.GetComponent<UIBase>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Create UI Script",
                    $"{root.name}에는 이미 {existing.GetType().Name} 컴포넌트가 있습니다.\n인스펙터의 Generate Script 버튼으로 갱신해 주세요.", "확인");
                return;
            }

            var defaultFolder = EditorPrefs.GetString(LastFolderKey, Path.GetDirectoryName(prefabPath)?.Replace('\\', '/'));
            var scriptPath = EditorUtility.SaveFilePanelInProject(
                $"{baseType.Name} 스크립트 저장", root.name, "cs", "스크립트를 저장할 위치를 고르세요.", defaultFolder);
            if (string.IsNullOrEmpty(scriptPath))
                return;

            EditorPrefs.SetString(LastFolderKey, Path.GetDirectoryName(scriptPath)?.Replace('\\', '/'));

            var className = Path.GetFileNameWithoutExtension(scriptPath);
            if (!s_identifier.IsMatch(className))
            {
                EditorUtility.DisplayDialog("Create UI Script",
                    $"'{className}'은(는) 클래스 이름으로 쓸 수 없습니다.\n영문, 숫자, _만 쓰고 숫자로 시작하지 않게 지어 주세요.", "확인");
                return;
            }

            var settings = UIPrefixSettings.GetOrCreate();
            var scriptNamespace = settings.ScriptNamespace;
            var duplicate = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                .FirstOrDefault(t => t.Name == className && (t.Namespace ?? "") == scriptNamespace);
            if (duplicate != null)
            {
                EditorUtility.DisplayDialog("Create UI Script",
                    $"같은 이름의 클래스 {duplicate.FullName}이(가) 이미 있습니다. 다른 이름을 골라 주세요.", "확인");
                return;
            }

            var bindings = UIBindingCollector.Collect(root, settings);

            // 팝업은 PopupManager가 불러올 수 있도록 [Popup] 경로를 넣고, Addressables 방식이면 프리팹을 등록합니다.
            string popupPath = null;
            var popupLoadType = LoadType.Addressables;
            if (baseType == typeof(PopupBase))
            {
                (popupPath, popupLoadType) = UIScriptGenerator.GetPopupLoadInfo(prefabPath);
                if (popupLoadType == LoadType.Addressables)
                    AddressableEditorUtility.SetAddress(prefabPath, popupPath);
            }

            var code = UIScriptGenerator.CreateScript(className, baseType, scriptNamespace, bindings, popupPath, popupLoadType);
            UIAutoMapper.WriteScript(scriptPath, code);
            UIAutoMapper.SchedulePending(prefabPath, scriptPath, true);
            AssetDatabase.ImportAsset(scriptPath);

            Debug.Log($"[Nyonyo Framework] {scriptPath}을(를) 만들었습니다(필드 {bindings.Count}개). 컴파일이 끝나면 {root.name} 프리팹에 붙이고 연결합니다.");
        }

        static bool TryGetTargetPrefab(out string prefabPath, out GameObject root)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                prefabPath = stage.assetPath;
                root = stage.prefabContentsRoot;
                return true;
            }

            var selected = Selection.activeGameObject;
            if (selected != null && PrefabUtility.IsPartOfPrefabAsset(selected))
            {
                prefabPath = AssetDatabase.GetAssetPath(selected);
                root = selected.transform.root.gameObject;
                return true;
            }

            prefabPath = null;
            root = null;
            return false;
        }
    }
}
