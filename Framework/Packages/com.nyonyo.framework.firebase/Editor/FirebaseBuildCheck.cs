using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Nyonyo.Framework.Firebase.Editor
{
    /// <summary>
    /// Android, iOS 빌드 직전에 Firebase SDK와 설정 파일이 있는지 검사합니다.
    /// </summary>
    internal sealed class FirebaseBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var target = report.summary.platform;
            if (target != BuildTarget.Android && target != BuildTarget.iOS)
                return;

            // 게임이 FirebaseSettings를 쓰지 않으면 검사하지 않습니다.
            if (AssetDatabase.FindAssets("t:" + nameof(FirebaseSettings)).Length == 0)
                return;

#if !NYONYO_FIREBASE_AUTH
            Debug.LogWarning($"[Nyonyo Framework] Firebase Auth 패키지가 없어서 {target}에서 로그인이 기기 안의 게스트로만 동작합니다. com.nyonyo.framework.firebase README의 설치 절차를 따르세요.");
#endif

            if (target == BuildTarget.Android && FirebaseConfigFiles.FindAndroid() == null)
                Debug.LogError($"[Nyonyo Framework] {FirebaseConfigFiles.AndroidFileName}이 없습니다. Firebase 콘솔에서 Android 앱의 파일을 받아 Assets 아래에 넣으세요.");
            if (target == BuildTarget.iOS && FirebaseConfigFiles.FindIOS() == null)
                Debug.LogError($"[Nyonyo Framework] {FirebaseConfigFiles.IOSFileName}이 없습니다. Firebase 콘솔에서 iOS 앱의 파일을 받아 Assets 아래에 넣으세요.");
        }
    }
}
