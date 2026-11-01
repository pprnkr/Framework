// iOS 빌드 지원 모듈이 있을 때만 Xcode 프로젝트 API를 쓸 수 있습니다.
#if UNITY_IOS
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;

namespace Nyonyo.Framework.Firebase.Editor
{
    /// <summary>
    /// iOS 빌드가 끝나면 Xcode 프로젝트에 Sign in with Apple 권한을 넣습니다. FirebaseSettings의 Apple Sign In이 켜져 있을 때만 넣습니다.
    /// </summary>
    internal sealed class AppleSignInCapability : IPostprocessBuildWithReport
    {
        private const string DefaultEntitlementsPath = "Unity-iPhone.entitlements";

        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS || !IsAppleSignInEnabled())
                return;

            var projectPath = PBXProject.GetPBXProjectPath(report.summary.outputPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            var target = project.GetUnityMainTargetGuid();

            // 다른 플러그인이 이미 권한 파일을 정했으면 그 파일에 함께 넣습니다.
            var entitlements = project.GetBuildPropertyForAnyConfig(target, "CODE_SIGN_ENTITLEMENTS");
            if (string.IsNullOrEmpty(entitlements))
                entitlements = DefaultEntitlementsPath;

            var capabilities = new ProjectCapabilityManager(projectPath, entitlements, null, target);
            capabilities.AddSignInWithApple();
            capabilities.WriteToFile();
        }

        private bool IsAppleSignInEnabled()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(FirebaseSettings)))
            {
                var settings = AssetDatabase.LoadAssetAtPath<FirebaseSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (settings != null && settings.AppleSignIn)
                    return true;
            }
            return false;
        }
    }
}
#endif
