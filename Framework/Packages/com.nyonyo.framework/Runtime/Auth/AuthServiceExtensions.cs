using System;
using UnityEngine;

namespace Nyonyo.Framework.Auth
{
    /// <summary>
    /// await 없이 콜백으로 로그인 작업을 하는 함수입니다. 버튼 OnClick처럼 async를 쓰기 어려운 곳에서 씁니다.
    /// </summary>
    // 확장 메서드는 C#에서 static 클래스에만 둘 수 있습니다.
    public static class AuthServiceExtensions
    {
        /// <summary>로그인하고, 끝나면 결과와 함께 <paramref name="onCompleted"/>를 호출합니다.</summary>
        public static void SignIn(this IAuthService auth, AuthProvider provider, Action<AuthResult> onCompleted = null) =>
            Run(auth.SignInAsync(provider), onCompleted);

        /// <summary>지금 계정에 구글이나 애플 계정을 연결하고, 끝나면 결과와 함께 <paramref name="onCompleted"/>를 호출합니다.</summary>
        public static void Link(this IAuthService auth, AuthProvider provider, Action<AuthResult> onCompleted = null) =>
            Run(auth.LinkAsync(provider), onCompleted);

        /// <summary>계정을 삭제하고, 끝나면 결과와 함께 <paramref name="onCompleted"/>를 호출합니다.</summary>
        public static void DeleteAccount(this IAuthService auth, Action<AuthResult> onCompleted = null) =>
            Run(auth.DeleteAccountAsync(), onCompleted);

        private static async void Run(Awaitable<AuthResult> operation, Action<AuthResult> onCompleted)
        {
            try
            {
                var result = await operation;
                onCompleted?.Invoke(result);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
