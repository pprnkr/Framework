using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nyonyo.Framework.Editor.Scenes
{
    /// <summary>
    /// 메뉴 Tools > Nyonyo Framework > Scene Switcher.
    /// 씬 목록을 보여 주고 클릭 한 번으로 열거나 Play합니다. 바뀐 씬이 있으면 저장할지 먼저 묻습니다.
    /// </summary>
    sealed class SceneSwitcherWindow : EditorWindow
    {
        Vector2 _scroll;
        string _filter = "";
        List<string> _projectScenes;

        [MenuItem("Tools/Nyonyo Framework/Scene Switcher")]
        static void Open() => GetWindow<SceneSwitcherWindow>("Scene Switcher");

        void OnEnable() => Refresh();

        void OnProjectChange() => Refresh();

        void Refresh()
        {
            _projectScenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .ToList();
            Repaint();
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField);
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                    Refresh();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Scene List (Build Profiles)", EditorStyles.boldLabel);
            var buildScenes = EditorBuildSettings.scenes.Select(scene => scene.path).Where(File.Exists).ToList();
            if (buildScenes.Count == 0)
                EditorGUILayout.HelpBox("Scene List에 씬이 없습니다.", MessageType.None);
            foreach (var path in buildScenes)
                DrawScene(path);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("All Scenes (Assets)", EditorStyles.boldLabel);
            foreach (var path in _projectScenes)
                DrawScene(path);

            EditorGUILayout.EndScrollView();
        }

        void DrawScene(string path)
        {
            var sceneName = Path.GetFileNameWithoutExtension(path);
            if (_filter.Length > 0 && sceneName.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) < 0)
                return;

            var isActive = EditorSceneManager.GetActiveScene().path == path;
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    var style = isActive ? EditorStyles.boldLabel : EditorStyles.label;
                    if (GUILayout.Button(new GUIContent(sceneName, path), style))
                        OpenScene(path);
                    if (GUILayout.Button("Play", GUILayout.Width(44f)))
                    {
                        if (OpenScene(path))
                            EditorApplication.EnterPlaymode();
                    }
                }
            }
        }

        static bool OpenScene(string path)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            EditorSceneManager.OpenScene(path);
            return true;
        }
    }
}
