using System.Threading.Tasks;
using Nyonyo.Framework.Auth;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Scripting;

namespace Nyonyo.Framework.Firebase
{
    /// <summary>
    /// Android 구글 로그인(Credential Manager)으로 구글 ID 토큰을 받습니다.
    /// 실제 로그인 창은 <c>Plugins/Android/GoogleSignInBridge.java</c>가 띄웁니다.
    /// </summary>
    internal sealed class GoogleSignInAndroid
    {
        private const string BridgeClassName = "com.nyonyo.framework.firebase.GoogleSignInBridge";

        // GoogleSignInBridge.java의 결과 코드와 같아야 합니다.
        private const int ResultSuccess = 0;
        private const int ResultCancelled = 1;
        private const int ResultNotConfigured = 3;

        private readonly string _webClientId;

        /// <param name="webClientId">비어 있으면 google-services.json에서 만든 리소스(default_web_client_id)를 씁니다.</param>
        public GoogleSignInAndroid(string webClientId)
        {
            _webClientId = webClientId ?? "";
        }

        public async Awaitable<ExternalSignInResult> SignInAsync()
        {
            if (Application.platform != RuntimePlatform.Android)
                return new ExternalSignInResult(AuthResult.NotSupported);

            var completion = new TaskCompletionSource<ExternalSignInResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var bridge = new AndroidJavaClass(BridgeClassName))
                bridge.CallStatic("signIn", AndroidApplication.currentActivity, _webClientId, new Callback(completion));

            // 결과는 Android UI 스레드에서 옵니다. Task를 기다리면 Unity 메인 스레드에서 이어집니다.
            return await completion.Task;
        }

        /// <summary>GoogleSignInBridge.Callback(Java 인터페이스)을 C#에서 구현합니다.</summary>
        private sealed class Callback : AndroidJavaProxy
        {
            private readonly TaskCompletionSource<ExternalSignInResult> _completion;

            public Callback(TaskCompletionSource<ExternalSignInResult> completion)
                : base(BridgeClassName + "$Callback")
            {
                _completion = completion;
            }

            // Java가 이름으로 찾아 부르므로 Java 메서드 이름(소문자 시작)과 같아야 하고, 코드 제거에서 빠지지 않게 보존합니다.
            [Preserve]
            public void onCompleted(int result, string idToken, string message)
            {
                if (result == ResultSuccess)
                {
                    _completion.TrySetResult(new ExternalSignInResult(AuthResult.Success, idToken));
                    return;
                }
                if (result == ResultCancelled)
                {
                    _completion.TrySetResult(new ExternalSignInResult(AuthResult.Cancelled));
                    return;
                }

                if (result == ResultNotConfigured)
                    Debug.LogError("[Nyonyo Framework] 구글 로그인용 웹 클라이언트 ID가 없습니다. Firebase 콘솔에서 Google 로그인을 켠 뒤 google-services.json을 다시 받거나, FirebaseSettings의 Google Web Client Id를 채우세요.");
                else
                    Debug.LogWarning($"[Nyonyo Framework] 구글 로그인에 실패했습니다: {message}");
                _completion.TrySetResult(new ExternalSignInResult(AuthResult.Failed));
            }
        }
    }
}
