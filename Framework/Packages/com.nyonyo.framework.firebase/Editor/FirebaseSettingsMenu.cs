using UnityEditor;

namespace Nyonyo.Framework.Firebase.Editor
{
    /// <summary>
    /// Tools > Nyonyo Framework > Settings > Firebase Settings 메뉴입니다. 프로젝트에 있으면 선택하고, 없으면 새로 만듭니다.
    /// </summary>
    internal static class FirebaseSettingsMenu
    {
        // [MenuItem] 함수는 Unity가 static만 허용합니다.
        [MenuItem(Framework.Editor.SettingsMenu.MenuRoot + "Firebase Settings", priority = 61)]
        private static void Open() =>
            Framework.Editor.SettingsMenu.SelectOrCreate<FirebaseSettings>($"{Framework.Editor.SettingsMenu.DefaultFolder}/FirebaseSettings.asset");
    }
}
