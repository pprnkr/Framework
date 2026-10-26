using Nyonyo.Framework.UI;
using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Editor.UI
{
    /// <summary>
    /// <see cref="UIBase"/>를 상속한 컴포넌트의 인스펙터에 Generate Script, Assign Object 버튼을 띄웁니다.
    /// </summary>
    [CustomEditor(typeof(UIBase), true)]
    sealed class UIBaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Auto Mapper", EditorStyles.boldLabel);

            var uiBase = (UIBase)target;
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
    }
}
