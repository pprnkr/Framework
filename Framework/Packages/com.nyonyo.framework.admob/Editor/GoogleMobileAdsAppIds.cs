using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Ads.AdMob.Editor
{
    /// <summary>
    /// AdMob 플러그인 설정 에셋의 앱 ID를 읽고 씁니다. 플러그인의 설정 클래스는 internal이라 직렬화 필드 이름으로 접근합니다.
    /// </summary>
    // 상태 없이 에셋만 다루는 에디터 함수 모음이라 static으로 둡니다. 인스펙터와 빌드 검사가 함께 씁니다.
    internal static class GoogleMobileAdsAppIds
    {
        public const string SettingsAssetPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        public const string SettingsMenuPath = "Assets/Google Mobile Ads/Settings...";

        private const string AndroidField = "adMobAndroidAppId";
        private const string IOSField = "adMobIOSAppId";

        public static string GetAndroid() => Read(AndroidField);

        public static string GetIOS() => Read(IOSField);

        /// <summary>비어 있는 앱 ID만 Google 테스트 앱 ID로 채웁니다. 설정 에셋이 없으면 플러그인 설정 창을 열어 만들게 합니다.</summary>
        /// <returns>설정 에셋이 있어서 값을 넣었으면 true입니다.</returns>
        public static bool FillEmptyWithTestIds()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SettingsAssetPath);
            if (asset == null)
            {
                EditorApplication.ExecuteMenuItem(SettingsMenuPath);
                asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SettingsAssetPath);
                if (asset == null)
                    return false;
            }

            var serialized = new SerializedObject(asset);
            FillIfEmpty(serialized, AndroidField, AdMobTestIds.AndroidAppId);
            FillIfEmpty(serialized, IOSField, AdMobTestIds.IOSAppId);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return true;
        }

        private static string Read(string field)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SettingsAssetPath);
            if (asset == null)
                return "";

            var property = new SerializedObject(asset).FindProperty(field);
            return property != null ? property.stringValue : "";
        }

        private static void FillIfEmpty(SerializedObject serialized, string field, string value)
        {
            var property = serialized.FindProperty(field);
            if (property != null && string.IsNullOrWhiteSpace(property.stringValue))
                property.stringValue = value;
        }
    }
}
