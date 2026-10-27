using Nyonyo.Framework.UI;
using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Editor.UI
{
    /// <summary>
    /// 방향에 맞춰 정렬된 실제 기준 해상도를 보여 주고, Player Settings와 방향이 다르면 경고와 적용 버튼을 표시합니다.
    /// </summary>
    [CustomEditor(typeof(UISettings))]
    sealed class UISettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var settings = (UISettings)target;
            var resolution = settings.ReferenceResolution;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox($"실제 기준 해상도: {resolution.x:0} x {resolution.y:0} ({settings.Orientation})", MessageType.Info);

            if (OrientationPlayerSettings.IsApplied(settings))
                return;

            EditorGUILayout.HelpBox("Player Settings의 화면 방향이나 기본 창 크기가 이 설정과 다릅니다.", MessageType.Warning);
            if (GUILayout.Button("Player Settings에 적용"))
                OrientationPlayerSettings.Apply(settings);
        }
    }
}
