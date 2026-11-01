using Nyonyo.Framework.Auth;

namespace Nyonyo.Framework.Firebase
{
    /// <summary>
    /// 구글, 애플 로그인 창의 결과입니다. 성공하면 Firebase에 넘길 ID 토큰을 담습니다.
    /// </summary>
    internal sealed class ExternalSignInResult
    {
        public ExternalSignInResult(AuthResult result, string idToken = null, string rawNonce = null)
        {
            Result = result;
            IdToken = idToken;
            RawNonce = rawNonce;
        }

        public AuthResult Result { get; }

        public string IdToken { get; }

        /// <summary>애플 로그인에서 요청할 때 쓴 원래 nonce입니다. Firebase가 토큰의 nonce와 비교합니다.</summary>
        public string RawNonce { get; }
    }
}
