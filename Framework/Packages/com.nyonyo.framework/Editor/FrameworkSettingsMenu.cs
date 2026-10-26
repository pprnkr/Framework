using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Editor
{
    static class FrameworkSettingsMenu
    {
        const string ResourcesFolder = "Assets/Resources";

        [MenuItem("Tools/Nyonyo Framework/Create Settings")]
        static void CreateSettings()
        {
            var path = $"{ResourcesFolder}/{Bootstrapper.SettingsResourcePath}.asset";
            var settings = AssetDatabase.LoadAssetAtPath<FrameworkSettings>(path);
            if (settings == null)
            {
                if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                    AssetDatabase.CreateFolder("Assets", "Resources");

                settings = ScriptableObject.CreateInstance<FrameworkSettings>();
                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.SaveAssets();
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
    }
}
