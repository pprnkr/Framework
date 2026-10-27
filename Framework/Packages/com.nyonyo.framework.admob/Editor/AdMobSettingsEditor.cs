using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Ads.AdMob.Editor
{
    /// <summary>
    /// <see cref="AdMobSettings"/> 인스펙터입니다. 앱 ID 설정 상태와 테스트 광고 사용 여부를 함께 보여 줍니다.
    /// </summary>
    [CustomEditor(typeof(AdMobSettings))]
    internal sealed class AdMobSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var settings = (AdMobSettings)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("앱 ID (AdMob 플러그인 설정)", EditorStyles.boldLabel);

            var androidAppId = GoogleMobileAdsAppIds.GetAndroid();
            var iOSAppId = GoogleMobileAdsAppIds.GetIOS();
            DrawAppId("Android", androidAppId, AdMobTestIds.AndroidAppId);
            DrawAppId("iOS", iOSAppId, AdMobTestIds.IOSAppId);

            if (string.IsNullOrWhiteSpace(androidAppId) || string.IsNullOrWhiteSpace(iOSAppId))
            {
                EditorGUILayout.HelpBox(
                    "앱 ID가 비어 있습니다. Android는 앱 ID 없이 빌드하면 실행하자마자 종료됩니다. " +
                    "AdMob 콘솔의 앱 설정에서 앱 ID(ca-app-pub-...~...)를 복사해 넣으세요.", MessageType.Error);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("앱 ID 설정 열기"))
                    EditorApplication.ExecuteMenuItem(GoogleMobileAdsAppIds.SettingsMenuPath);
                if (GUILayout.Button("빈 앱 ID에 테스트 ID 넣기"))
                    GoogleMobileAdsAppIds.FillEmptyWithTestIds();
            }

            if (settings.UseTestAds)
                EditorGUILayout.HelpBox("테스트 광고를 쓰는 중입니다. 출시 빌드 전에 Use Test Ads를 끄고 실제 광고 단위 ID를 넣으세요.", MessageType.Warning);
            if (settings.ConsentDebugGeography != GoogleMobileAds.Ump.Api.DebugGeography.Disabled)
                EditorGUILayout.HelpBox("동의 창 지역 흉내가 켜져 있습니다. 출시 전에 Disabled로 돌리세요.", MessageType.Warning);
        }

        private void DrawAppId(string platform, string appId, string testAppId)
        {
            var label = string.IsNullOrWhiteSpace(appId) ? "(비어 있음)" : appId == testAppId ? $"{appId} (테스트)" : appId;
            EditorGUILayout.LabelField(platform, label);
        }
    }
}
