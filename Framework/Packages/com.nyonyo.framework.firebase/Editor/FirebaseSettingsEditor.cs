using UnityEditor;

namespace Nyonyo.Framework.Firebase.Editor
{
    /// <summary>
    /// <see cref="FirebaseSettings"/> 인스펙터입니다. Firebase SDK 설치 여부와 설정 파일 상태를 함께 보여 줍니다.
    /// </summary>
    [CustomEditor(typeof(FirebaseSettings))]
    internal sealed class FirebaseSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("설치 상태", EditorStyles.boldLabel);

#if NYONYO_FIREBASE_AUTH
            EditorGUILayout.LabelField("Firebase Auth", "설치됨");
#else
            EditorGUILayout.LabelField("Firebase Auth", "(없음)");
            EditorGUILayout.HelpBox(
                "Firebase Unity SDK(com.google.firebase.app, com.google.firebase.auth 14.0.0 이상)가 없습니다. " +
                "없으면 Android, iOS에서도 기기 안의 게스트 로그인만 됩니다. com.nyonyo.framework.firebase README의 설치 절차를 따르세요.",
                MessageType.Warning);
#endif

            DrawConfigFile("Android", FirebaseConfigFiles.FindAndroid(), FirebaseConfigFiles.AndroidFileName);
            DrawConfigFile("iOS", FirebaseConfigFiles.FindIOS(), FirebaseConfigFiles.IOSFileName);

            var settings = (FirebaseSettings)target;
            if (settings.GoogleSignIn)
                EditorGUILayout.HelpBox("구글 로그인: Firebase 콘솔에서 Google 로그인을 켜고, Android 앱에 SHA-1 지문(디버그 키, 출시 키, Play 앱 서명 키)을 등록한 뒤 google-services.json을 다시 받으세요.", MessageType.Info);
            if (settings.AppleSignIn)
                EditorGUILayout.HelpBox("애플 로그인: Firebase 콘솔에서 Apple 로그인을 켜고, Apple Developer의 App ID에 Sign in with Apple을 켜세요. iOS 빌드에는 권한이 자동으로 들어갑니다.", MessageType.Info);
        }

        private void DrawConfigFile(string platform, string path, string fileName)
        {
            EditorGUILayout.LabelField(platform, path ?? "(없음)");
            if (path == null)
                EditorGUILayout.HelpBox($"Firebase 콘솔의 프로젝트 설정에서 {platform} 앱의 {fileName}을 받아 Assets 아래에 넣으세요. 없으면 {platform}에서 Firebase가 시작되지 않습니다.", MessageType.Warning);
        }
    }
}
