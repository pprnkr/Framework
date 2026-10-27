using Nyonyo.Framework.Audio;
using Nyonyo.Framework.Editor.Data;
using Nyonyo.Framework.Editor.UI;
using Nyonyo.Framework.UI;
using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Editor
{
    /// <summary>
    /// Tools > Nyonyo Framework > Settings 메뉴입니다. 프레임워크의 설정 에셋은 모두 여기서 만들고 엽니다.
    /// 프로젝트에 이미 있으면 그 에셋을 선택하고, 없으면 기본 위치에 새로 만듭니다.
    /// </summary>
    /// <remarks>
    /// 선택 모듈(AdMob 등)도 <see cref="MenuRoot"/>와 <see cref="SelectOrCreate{T}"/>로 같은 메뉴에 항목을 추가합니다.
    /// </remarks>
    // [MenuItem] 함수는 Unity가 static만 허용하고, 다른 모듈의 메뉴에서도 부르는 헬퍼라 static입니다. 상태는 없습니다.
    public static class SettingsMenu
    {
        public const string MenuRoot = "Tools/Nyonyo Framework/Settings/";

        /// <summary>런타임 설정 에셋을 새로 만들 때 쓰는 기본 폴더입니다.</summary>
        public const string DefaultFolder = "Assets/Settings/Nyonyo Framework";

        [MenuItem(MenuRoot + "Framework Settings", priority = 0)]
        private static void OpenFrameworkSettings() =>
            SelectOrCreateAt<FrameworkSettings>($"Assets/Resources/{Bootstrapper.SettingsResourcePath}.asset");

        [MenuItem(MenuRoot + "UI Settings", priority = 20)]
        private static void OpenUISettings() => SelectOrCreate<UISettings>($"{DefaultFolder}/UISettings.asset");

        [MenuItem(MenuRoot + "Sound Settings", priority = 21)]
        private static void OpenSoundSettings() => SelectOrCreate<SoundSettings>($"{DefaultFolder}/SoundSettings.asset");

        [MenuItem(MenuRoot + "UI Prefix Settings", priority = 40)]
        private static void OpenUIPrefixSettings() => Select(UIPrefixSettings.GetOrCreate());

        [MenuItem(MenuRoot + "Game Data Settings", priority = 41)]
        private static void OpenGameDataSettings() => SelectOrCreate<GameDataSettings>("Assets/Editor/GameDataSettings.asset");

        /// <summary>
        /// 프로젝트(Assets 폴더)에서 <typeparamref name="T"/> 에셋을 찾아 선택합니다. 없으면 <paramref name="defaultPath"/>에 만들고 선택합니다.
        /// </summary>
        public static T SelectOrCreate<T>(string defaultPath) where T : ScriptableObject
        {
            var asset = Find<T>();
            if (asset == null)
                asset = Create<T>(defaultPath);

            Select(asset);
            return asset;
        }

        /// <summary>
        /// 정해진 위치에 있어야 하는 에셋(예: Resources에서 불러오는 설정)을 그 위치에서 찾아 선택하고, 없으면 그 위치에 만듭니다.
        /// </summary>
        public static T SelectOrCreateAt<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                asset = Create<T>(path);

            Select(asset);
            return asset;
        }

        private static T Find<T>() where T : ScriptableObject
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets" }))
            {
                var found = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (found != null)
                    return found;
            }

            return null;
        }

        private static T Create<T>(string path) where T : ScriptableObject
        {
            CreateFolders(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Nyonyo Framework] {typeof(T).Name}을(를) 만들었습니다: {path}", asset);
            return asset;
        }

        private static void CreateFolders(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
                return;

            var parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            CreateFolders(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        private static void Select(Object asset)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
