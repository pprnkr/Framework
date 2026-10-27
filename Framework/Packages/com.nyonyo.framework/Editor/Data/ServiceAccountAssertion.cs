using System;
using System.Security.Cryptography;
using System.Text;

namespace Nyonyo.Framework.Editor.Data
{
    /// <summary>
    /// 구글 서비스 계정으로 액세스 토큰을 받을 때 쓰는 서명된 JWT(RS256)를 만듭니다.
    /// </summary>
    /// <remarks>
    /// 규칙: https://developers.google.com/identity/protocols/oauth2/service-account (HTTP/REST)
    /// header {"alg":"RS256","typ":"JWT"}, claim {iss, scope, aud, iat, exp(최대 iat+1시간)}.
    /// </remarks>
    internal sealed class ServiceAccountAssertion
    {
        /// <summary>토큰 유효 시간입니다. 구글이 허용하는 최대값(1시간)입니다.</summary>
        public const int LifetimeSeconds = 3600;

        public string Create(string clientEmail, string scope, string audience, DateTimeOffset now, RSAParameters key)
        {
            var issuedAt = now.ToUnixTimeSeconds();
            var header = "{\"alg\":\"RS256\",\"typ\":\"JWT\"}";
            var claims = "{" +
                         $"\"iss\":{Quote(clientEmail)}," +
                         $"\"scope\":{Quote(scope)}," +
                         $"\"aud\":{Quote(audience)}," +
                         $"\"iat\":{issuedAt}," +
                         $"\"exp\":{issuedAt + LifetimeSeconds}" +
                         "}";

            var unsigned = Base64Url(Encoding.UTF8.GetBytes(header)) + "." + Base64Url(Encoding.UTF8.GetBytes(claims));
            using var rsa = RSA.Create();
            rsa.ImportParameters(key);
            var signature = rsa.SignData(Encoding.ASCII.GetBytes(unsigned), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return unsigned + "." + Base64Url(signature);
        }

        public string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private string Quote(string text)
        {
            var escaped = new StringBuilder("\"");
            foreach (var c in text ?? "")
            {
                if (c == '"' || c == '\\')
                    escaped.Append('\\').Append(c);
                else if (c < 0x20)
                    escaped.Append("\\u").Append(((int)c).ToString("x4"));
                else
                    escaped.Append(c);
            }
            return escaped.Append('"').ToString();
        }
    }
}
