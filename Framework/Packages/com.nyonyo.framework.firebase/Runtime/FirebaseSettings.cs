using Nyonyo.Framework.Auth;
using Nyonyo.Framework.Save;
using UnityEngine;

namespace Nyonyo.Framework.Firebase
{
    /// <summary>
    /// Firebase 모듈 설정입니다. 지금은 로그인(Auth) 설정만 있습니다.
    /// </summary>
    /// <remarks>
    /// Firebase 프로젝트 정보는 이 에셋이 아니라 Firebase 콘솔에서 받은 <c>google-services.json</c>(Android)과
    /// <c>GoogleService-Info.plist</c>(iOS)를 Assets 폴더에 넣어 정합니다. 이 에셋의 인스펙터에서 상태를 확인할 수 있습니다.
    /// </remarks>
    public sealed class FirebaseSettings : ScriptableObject
    {
        internal const string MissingAuthPackageMessage =
            "[Nyonyo Framework] Firebase Auth 패키지(com.google.firebase.auth 14.0.0 이상)가 없어서 로그인이 기기 안의 게스트로만 동작합니다. " +
            "com.nyonyo.framework.firebase README의 설치 절차대로 Firebase Unity SDK를 넣으세요.";

        [Header("로그인 (Auth)")]
        [Tooltip("시작할 때 로그인된 계정이 없으면 게스트(익명)로 로그인합니다. 끄면 게임이 직접 SignInAsync를 부릅니다.")]
        [SerializeField] private bool _signInAsGuestOnStart = true;

        [Tooltip("Android에서 구글 계정 로그인과 연결을 씁니다. Firebase 콘솔에서 Google 로그인을 켜고 앱의 SHA-1 지문을 등록해야 합니다.")]
        [SerializeField] private bool _googleSignIn = true;

        [Tooltip("구글 로그인에 쓰는 웹 클라이언트 ID입니다. 비워 두면 google-services.json의 값(default_web_client_id)을 씁니다.")]
        [SerializeField] private string _googleWebClientId = "";

        [Tooltip("iOS에서 애플 계정 로그인과 연결을 씁니다. 켜면 iOS 빌드에 Sign in with Apple 권한을 넣습니다. Firebase 콘솔에서 Apple 로그인을 켜야 합니다.")]
        [SerializeField] private bool _appleSignIn = true;

        [Header("에디터")]
        [SerializeField] private FirebaseAuthEditorMode _authEditorMode = FirebaseAuthEditorMode.Fake;

        public bool SignInAsGuestOnStart => _signInAsGuestOnStart;

        public bool GoogleSignIn => _googleSignIn;

        public string GoogleWebClientId => _googleWebClientId;

        public bool AppleSignIn => _appleSignIn;

        public FirebaseAuthEditorMode AuthEditorMode => _authEditorMode;

        /// <summary>
        /// 실행 환경에 맞는 로그인 서비스를 만듭니다. Android, iOS는 Firebase, 에디터는 <see cref="AuthEditorMode"/>, 그 밖의 플랫폼(WebGL, PC)은 기기 안의 게스트 로그인입니다.
        /// </summary>
        /// <param name="parent">에디터용 가짜 로그인 창을 붙일 부모입니다. 보통 <see cref="Bootstrapper.Root"/>를 넘깁니다.</param>
        /// <param name="save">에디터 가짜 로그인과 WebGL, PC 게스트 계정을 저장할 때 씁니다.</param>
        /// <example><c>services.Register&lt;IAuthService&gt;(_firebaseSettings.CreateAuthService(Bootstrapper.Root, save));</c></example>
        public IAuthService CreateAuthService(Transform parent, SaveService save = null)
        {
            if (Application.isEditor)
            {
                if (_authEditorMode == FirebaseAuthEditorMode.Firebase)
                {
#if NYONYO_FIREBASE_AUTH && !UNITY_WEBGL
                    return new FirebaseAuthService(this);
#else
                    Debug.LogWarning(MissingAuthPackageMessage + " 에디터에서는 가짜 로그인을 씁니다.");
#endif
                }
                return new FakeAuthService(parent, save, _signInAsGuestOnStart);
            }

            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
#if NYONYO_FIREBASE_AUTH && !UNITY_WEBGL
                return new FirebaseAuthService(this);
#else
                Debug.LogError(MissingAuthPackageMessage);
#endif
            }

            return new NullAuthService(save, _signInAsGuestOnStart);
        }
    }
}
