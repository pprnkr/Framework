using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace Nyonyo.Framework.Editor.UI
{
    /// <summary>
    /// 접두사 규칙 중 타입을 찾을 수 없거나 접두사가 겹치는 항목을 인스펙터에 표시합니다.
    /// </summary>
    [CustomEditor(typeof(UIPrefixSettings))]
    sealed class UIPrefixSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var settings = (UIPrefixSettings)target;
            var problems = new StringBuilder();
            var prefixes = new HashSet<string>();
            foreach (var rule in settings.Rules)
            {
                if (string.IsNullOrEmpty(rule.Prefix))
                    problems.AppendLine("접두사가 비어 있는 규칙이 있습니다.");
                else if (!prefixes.Add(rule.Prefix))
                    problems.AppendLine($"{rule.Prefix}: 같은 접두사가 두 번 있습니다.");

                if (rule.ResolveType() == null)
                    problems.AppendLine($"{rule.Prefix}: {rule.TypeName} 타입을 찾을 수 없습니다.");
            }

            EditorGUILayout.Space();
            if (problems.Length > 0)
                EditorGUILayout.HelpBox(problems.ToString().TrimEnd(), MessageType.Warning);
            else
                EditorGUILayout.HelpBox("모든 규칙의 타입을 찾았습니다.", MessageType.Info);
        }
    }
}
