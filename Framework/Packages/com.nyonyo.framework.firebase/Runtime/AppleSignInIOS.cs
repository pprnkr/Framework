using System.Security.Cryptography;
using System.Text;
using Nyonyo.Framework.Auth;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AOT;
#endif

namespace Nyonyo.Framework.Firebase
{
    /// <summary>
    /// iOS 애플 로그인(Sign in with Apple)으로 애플 ID 토큰을 받습니다.
    /// 실제 로그인 창은 <c>Plugins/iOS/NyonyoAppleSignIn.mm</c>이 띄우고, iOS가 아니면 <see cref="AuthResult.NotSupported"/>입니다.
    /// </summary>
    /// <remarks>
    /// 토큰을 가로채 다시 쓰는 공격을 막기 위해 요청마다 무작위 nonce를 만들고, 애플에는 그 SHA-256 해시를, Firebase에는 원래 값을 넘깁니다.
    /// </remarks>
    internal sealed class AppleSignInIOS
    {
        public Awaitable<ExternalSignInResult> SignInAsync()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return StartAsync();
#else
            var source = new AwaitableCompletionSource<ExternalSignInResult>();
            source.SetResult(new ExternalSignInResult(AuthResult.NotSupported));
            return source.Awaitable;
#endif
        }

        /// <summary>요청마다 새로 만드는 무작위 문자열(16진수 64자)입니다.</summary>
        internal string CreateRawNonce()
        {
            var bytes = new byte[32];
            using (var random = RandomNumberGenerator.Create())
                random.GetBytes(bytes);
            return ToHex(bytes);
        }

        /// <summary>애플에 넘기는 nonce입니다. 원래 값의 SHA-256을 소문자 16진수로 씁니다.</summary>
        internal string HashNonce(string rawNonce)
        {
            using (var sha = SHA256.Create())
                return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(rawNonce)));
        }

        private string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var value in bytes)
                builder.Append(value.ToString("x2"));
            return builder.ToString();
        }

#if UNITY_IOS && !UNITY_EDITOR
        // NyonyoAppleSignIn.mm의 결과 코드와 같아야 합니다.
        private const int ResultSuccess = 0;
        private const int ResultCancelled = 1;
        private const int ResultNotSupported = 3;

        private delegate void CompletedCallback(int result, string idToken, string message);

        // IL2CPP는 네이티브 콜백으로 static 메서드만 받을 수 있어서, 기다리는 요청을 static 필드에 둡니다.
        // 로그인은 AuthServiceBase가 한 번에 하나만 진행시키므로 요청 하나만 기억하면 됩니다.
        private static TaskCompletionSource<ExternalSignInResult> s_pending;
        private static string s_pendingRawNonce;

        [DllImport("__Internal")]
        private static extern void NyonyoAppleSignIn_Start(string hashedNonce, CompletedCallback callback);

        private async Awaitable<ExternalSignInResult> StartAsync()
        {
            var rawNonce = CreateRawNonce();
            var completion = new TaskCompletionSource<ExternalSignInResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            s_pending = completion;
            s_pendingRawNonce = rawNonce;
            NyonyoAppleSignIn_Start(HashNonce(rawNonce), OnCompleted);
            return await completion.Task;
        }

        [MonoPInvokeCallback(typeof(CompletedCallback))]
        private static void OnCompleted(int result, string idToken, string message)
        {
            var pending = s_pending;
            var rawNonce = s_pendingRawNonce;
            s_pending = null;
            s_pendingRawNonce = null;
            if (pending == null)
                return;

            if (result == ResultSuccess)
            {
                pending.TrySetResult(new ExternalSignInResult(AuthResult.Success, idToken, rawNonce));
            }
            else if (result == ResultCancelled)
            {
                pending.TrySetResult(new ExternalSignInResult(AuthResult.Cancelled));
            }
            else if (result == ResultNotSupported)
            {
                Debug.LogWarning($"[Nyonyo Framework] 이 기기에서는 애플 로그인을 쓸 수 없습니다: {message}");
                pending.TrySetResult(new ExternalSignInResult(AuthResult.NotSupported));
            }
            else
            {
                Debug.LogWarning($"[Nyonyo Framework] 애플 로그인에 실패했습니다: {message}");
                pending.TrySetResult(new ExternalSignInResult(AuthResult.Failed));
            }
        }
#endif
    }
}
