using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Nyonyo.Framework.Ads.AdMob.Editor
{
    /// <summary>
    /// Android, iOS 빌드 직전에 AdMob 설정을 검사합니다. 출시 빌드에 테스트 광고나 테스트 앱 ID가 남아 있으면 경고합니다.
    /// </summary>
    internal sealed class AdMobBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var target = report.summary.platform;
            if (target != BuildTarget.Android && target != BuildTarget.iOS)
                return;

            var isRelease = (report.summary.options & BuildOptions.Development) == 0;
            var appId = target == BuildTarget.Android ? GoogleMobileAdsAppIds.GetAndroid() : GoogleMobileAdsAppIds.GetIOS();
            var testAppId = target == BuildTarget.Android ? AdMobTestIds.AndroidAppId : AdMobTestIds.IOSAppId;

            if (string.IsNullOrWhiteSpace(appId))
                Debug.LogError($"[Nyonyo Framework] {target} AdMob 앱 ID가 비어 있습니다. Assets > Google Mobile Ads > Settings...에서 넣으세요.");
            else if (isRelease && appId == testAppId)
                Debug.LogWarning($"[Nyonyo Framework] 출시 빌드에 {target} 테스트 앱 ID가 들어 있습니다. 실제 앱 ID로 바꾸세요.");

            if (!isRelease)
                return;

            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(AdMobSettings)))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var settings = AssetDatabase.LoadAssetAtPath<AdMobSettings>(path);
                if (settings != null && settings.UseTestAds)
                    Debug.LogWarning($"[Nyonyo Framework] 출시 빌드인데 {path}의 Use Test Ads가 켜져 있습니다. 실제 광고 단위 ID를 쓰려면 끄세요.");
            }
        }
    }
}
