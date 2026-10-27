using UnityEditor;

namespace Nyonyo.Framework.Ads.AdMob.Editor
{
    /// <summary>
    /// Tools > Nyonyo Framework > Settings > AdMob Settings 메뉴입니다. 프로젝트에 있으면 선택하고, 없으면 새로 만듭니다.
    /// </summary>
    internal static class AdMobSettingsMenu
    {
        // [MenuItem] 함수는 Unity가 static만 허용합니다.
        [MenuItem(Framework.Editor.SettingsMenu.MenuRoot + "AdMob Settings", priority = 60)]
        private static void Open() =>
            Framework.Editor.SettingsMenu.SelectOrCreate<AdMobSettings>($"{Framework.Editor.SettingsMenu.DefaultFolder}/AdMobSettings.asset");
    }
}
