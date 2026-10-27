using System;
using System.IO;
using System.Reflection;
using System.Text;
using Nyonyo.Framework.UI;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Nyonyo.Framework.Editor.UI
{
    /// <summary>
    /// 접두사가 붙은 자식 오브젝트를 <see cref="UIBase"/> 스크립트의 필드로 만들고(Generate Script), 연결합니다(Assign Object).
    /// </summary>
    public static class UIAutoMapper
    {
        private const string PendingKey = "Nyonyo.Framework.UIAutoMapper.Pending";

        [Serializable]
        private class PendingMapping
        {
            public string PrefabPath;
            public string ScriptPath;
            public bool AddComponent;
        }

        /// <summary>
        /// <paramref name="target"/> 아래의 접두사 오브젝트를 같은 이름의 필드에 연결합니다.
        /// 설정에 따라 BTN_ 버튼의 OnClick에 <see cref="UIBase.OnButtonEvent"/>도 연결합니다(이미 있으면 다시 추가하지 않습니다).
        /// </summary>
        /// <returns>연결한 필드 수</returns>
        public static int Assign(UIBase target, UIPrefixSettings settings = null)
        {
            settings ??= UIPrefixSettings.GetOrCreate();
            var bindings = UIBindingCollector.Collect(target.gameObject, settings);
            var serialized = new SerializedObject(target);
            var assigned = 0;

            foreach (var binding in bindings)
            {
                var property = serialized.FindProperty(binding.FieldName);
                if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    Debug.LogWarning(
                        $"[Nyonyo Framework] {target.GetType().Name}에 {binding.FieldName} 필드가 없습니다. Generate Script를 먼저 실행하세요.",
                        target);
                    continue;
                }

                var field = FindField(target.GetType(), binding.FieldName);
                if (field != null && !field.FieldType.IsInstanceOfType(binding.Value))
                {
                    Debug.LogWarning(
                        $"[Nyonyo Framework] {target.GetType().Name}.{binding.FieldName}의 타입({field.FieldType.Name})이 " +
                        $"오브젝트({binding.FieldType.Name})와 달라서 건너뜁니다. Generate Script로 다시 만드세요.",
                        target);
                    continue;
                }

                property.objectReferenceValue = binding.Value;
                assigned++;
            }

            serialized.ApplyModifiedProperties();

            var boundButtons = 0;
            if (settings.BindButtonClick)
            {
                foreach (var binding in bindings)
                {
                    if (binding.Value is Button button && BindClick(target, button))
                        boundButtons++;
                }
            }

            MarkDirty(target);
            Debug.Log($"[Nyonyo Framework] {target.name}: 필드 {assigned}개, 버튼 클릭 {boundButtons}개를 연결했습니다.", target);
            return assigned;
        }

        /// <summary>
        /// <paramref name="target"/>의 스크립트에서 자동 생성 영역(#region Auto Generated UI)만 다시 씁니다.
        /// 스크립트가 바뀌면 컴파일이 끝난 뒤 자동으로 Assign합니다. 바뀌지 않았으면 바로 Assign합니다.
        /// </summary>
        public static void GenerateScript(UIBase target, UIPrefixSettings settings = null)
        {
            settings ??= UIPrefixSettings.GetOrCreate();
            var script = MonoScript.FromMonoBehaviour(target);
            var scriptPath = AssetDatabase.GetAssetPath(script);
            if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
            {
                Debug.LogError($"[Nyonyo Framework] {target.GetType().Name} 스크립트 파일을 찾을 수 없습니다. 수정할 수 없는 패키지 안의 스크립트일 수 있습니다.", target);
                return;
            }

            var bindings = UIBindingCollector.Collect(target.gameObject, settings);
            var source = File.ReadAllText(scriptPath);
            string updated;
            try
            {
                updated = UIScriptGenerator.UpdateScript(source, target.GetType().Name, bindings);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"[Nyonyo Framework] {e.Message}", script);
                return;
            }

            if (updated == source)
            {
                Assign(target, settings);
                return;
            }

            WriteScript(scriptPath, updated);
            var prefabPath = GetPrefabPath(target);
            if (prefabPath != null)
                SchedulePending(prefabPath, scriptPath, false);
            else
                Debug.Log("[Nyonyo Framework] 스크립트를 갱신했습니다. 컴파일이 끝나면 Assign Object를 눌러 주세요.", target);

            AssetDatabase.ImportAsset(scriptPath);
        }

        internal static void WriteScript(string scriptPath, string contents) =>
            File.WriteAllText(scriptPath, contents, new UTF8Encoding(false));

        /// <summary>
        /// 컴파일이 끝난 뒤 <paramref name="prefabPath"/> 프리팹에 스크립트를 붙이고(선택) Assign하도록 예약합니다.
        /// </summary>
        internal static void SchedulePending(string prefabPath, string scriptPath, bool addComponent)
        {
            var pending = new PendingMapping { PrefabPath = prefabPath, ScriptPath = scriptPath, AddComponent = addComponent };
            SessionState.SetString(PendingKey, JsonUtility.ToJson(pending));
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            var json = SessionState.GetString(PendingKey, "");
            if (string.IsNullOrEmpty(json))
                return;

            SessionState.EraseString(PendingKey);
            var pending = JsonUtility.FromJson<PendingMapping>(json);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(pending.ScriptPath);
            var type = script != null ? script.GetClass() : null;
            if (type == null || !typeof(UIBase).IsAssignableFrom(type))
            {
                Debug.LogError($"[Nyonyo Framework] {pending.ScriptPath}에서 UIBase 클래스를 찾지 못해 자동 연결을 건너뜁니다. 파일 이름과 클래스 이름이 같은지 확인하세요.");
                return;
            }

            // 스크립트 리로드 도중에는 프리팹을 수정하지 않고, 에디터가 안정된 다음 프레임에 처리합니다.
            EditorApplication.delayCall += () => ApplyToPrefab(pending.PrefabPath, type, pending.AddComponent);
        }

        private static void ApplyToPrefab(string prefabPath, Type componentType, bool addComponent)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == prefabPath)
            {
                var target = GetOrAddComponent(stage.prefabContentsRoot, componentType, addComponent);
                if (target != null)
                {
                    Assign(target);
                    EditorSceneManager.MarkSceneDirty(stage.scene);
                }

                return;
            }

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var target = GetOrAddComponent(root, componentType, addComponent);
                if (target != null)
                {
                    Assign(target);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static UIBase GetOrAddComponent(GameObject root, Type componentType, bool addComponent)
        {
            var component = root.GetComponent(componentType) as UIBase;
            if (component == null && addComponent)
                component = (UIBase)Undo.AddComponent(root, componentType);

            if (component == null)
                Debug.LogWarning($"[Nyonyo Framework] {root.name}에 {componentType.Name} 컴포넌트가 없어서 자동 연결을 건너뜁니다.", root);

            return component;
        }

        /// <summary>
        /// <paramref name="target"/>이 프리팹 모드나 프리팹 에셋에 있으면 그 프리팹 경로, 씬 오브젝트면 null입니다.
        /// </summary>
        internal static string GetPrefabPath(UIBase target)
        {
            var stage = PrefabStageUtility.GetPrefabStage(target.gameObject);
            if (stage != null)
                return stage.assetPath;

            return PrefabUtility.IsPartOfPrefabAsset(target) ? AssetDatabase.GetAssetPath(target) : null;
        }

        private static bool BindClick(UIBase target, Button button)
        {
            var onClick = button.onClick;
            for (var i = 0; i < onClick.GetPersistentEventCount(); i++)
            {
                if (onClick.GetPersistentTarget(i) == target && onClick.GetPersistentMethodName(i) == nameof(UIBase.OnButtonEvent))
                    return false;
            }

            Undo.RecordObject(button, "Bind Button Click");
            UnityEventTools.AddObjectPersistentListener<Button>(onClick, target.OnButtonEvent, button);
            EditorUtility.SetDirty(button);
            return true;
        }

        private static FieldInfo FindField(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var current = type; current != null; current = current.BaseType)
            {
                var field = current.GetField(name, flags);
                if (field != null)
                    return field;
            }

            return null;
        }

        private static void MarkDirty(UIBase target)
        {
            EditorUtility.SetDirty(target);
            var stage = PrefabStageUtility.GetPrefabStage(target.gameObject);
            if (stage != null)
                EditorSceneManager.MarkSceneDirty(stage.scene);
        }
    }
}
