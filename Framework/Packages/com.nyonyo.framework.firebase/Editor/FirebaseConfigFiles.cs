using System.IO;
using UnityEditor;

namespace Nyonyo.Framework.Firebase.Editor
{
    /// <summary>
    /// Firebase 콘솔에서 받은 설정 파일(google-services.json, GoogleService-Info.plist)을 Assets에서 찾습니다.
    /// </summary>
    // 상태 없이 에셋만 찾는 에디터 함수 모음이라 static으로 둡니다. 인스펙터와 빌드 검사가 함께 씁니다.
    internal static class FirebaseConfigFiles
    {
        public const string AndroidFileName = "google-services.json";
        public const string IOSFileName = "GoogleService-Info.plist";

        /// <summary>Android 설정 파일 경로입니다. 없으면 null입니다.</summary>
        public static string FindAndroid() => Find(AndroidFileName);

        /// <summary>iOS 설정 파일 경로입니다. 없으면 null입니다.</summary>
        public static string FindIOS() => Find(IOSFileName);

        private static string Find(string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            foreach (var guid in AssetDatabase.FindAssets(name, new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path) == fileName)
                    return path;
            }
            return null;
        }
    }
}
