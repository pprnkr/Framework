using System.Reflection;
using Nyonyo.Framework.UI;
using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Editor.UI
{
    /// <summary>
    /// <see cref="UIBase"/>를 상속한 컴포넌트의 인스펙터에 Generate Script, Assign Object 버튼을 띄웁니다.
    /// 팝업이면 <see cref="PopupAttribute"/>가 없거나 프리팹 위치와 다를 때 경고합니다.
    /// </summary>
    [CustomEditor(typeof(UIBase), true)]
    sealed class UIBaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var uiBase = (UIBase)target;
            if (uiBase is PopupBase)
                DrawPopupPathCheck(uiBase);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Auto Mapper", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            using (new EditorGUILayout.HorizontalScope())
            {
                // 스크립트 임포트와 컴파일이 인스펙터 그리기 도중에 시작되지 않도록 다음 에디터 프레임으로 미룹니다.
                if (GUILayout.Button("Generate Script"))
                    EditorApplication.delayCall += () => UIAutoMapper.GenerateScript(uiBase);

                if (GUILayout.Button("Assign Object"))
                    EditorApplication.delayCall += () => UIAutoMapper.Assign(uiBase);
            }

            EditorGUILayout.HelpBox(
                "Generate Script: IMG_, TXT_ 같은 접두사 오브젝트로 #region Auto Generated UI 영역의 변수를 다시 만듭니다.\n" +
                "Assign Object: 그 오브젝트들을 같은 이름의 변수에 연결합니다. 규칙은 UIPrefixSettings에서 바꿀 수 있습니다.",
                MessageType.None);
        }

        static void DrawPopupPathCheck(UIBase popup)
        {
            var info = popup.GetType().GetCustomAttribute<PopupAttribute>();
            if (info == null || string.IsNullOrEmpty(info.Path))
            {
                EditorGUILayout.HelpBox(
                    "[Popup(Path = ..., LoadType = ...)] 어트리뷰트가 없어서 PopupManager가 이 팝업을 불러올 수 없습니다.",
                    MessageType.Warning);
                return;
            }

            var prefabPath = UIAutoMapper.GetPrefabPath(popup);
            if (prefabPath == null)
                return;

            var (expectedPath, expectedLoadType) = UIScriptGenerator.GetPopupLoadInfo(prefabPath);
            if (info.Path != expectedPath || info.LoadType != expectedLoadType)
            {
                EditorGUILayout.HelpBox(
                    $"[Popup] 값({info.LoadType}, {info.Path})이 프리팹 위치({expectedLoadType}, {expectedPath})와 다릅니다.\n" +
                    "프리팹을 옮겼다면 어트리뷰트를 고치세요. address를 일부러 다르게 정했다면 무시해도 됩니다.",
                    MessageType.Warning);
            }
        }
    }
}
