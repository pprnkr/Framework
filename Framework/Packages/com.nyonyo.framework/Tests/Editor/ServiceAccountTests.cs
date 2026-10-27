using System;
using System.Security.Cryptography;
using System.Text;
using Nyonyo.Framework.Editor.Data;
using NUnit.Framework;

namespace Nyonyo.Framework.Editor.Tests
{
    public class ServiceAccountTests
    {
        // 테스트 전용으로 만든 1024비트 키입니다(실제 서비스 계정 키 아님). 구글 JSON 키의 private_key와 같은 PKCS#8 형식입니다.
        private const string Pkcs8Pem =
            "-----BEGIN PRIVATE KEY-----\n" +
            "MIICeAIBADANBgkqhkiG9w0BAQEFAASCAmIwggJeAgEAAoGBANSrl3NCgE4t5ptr\n" +
            "T2YXL59blePkD+ueYq5H8jRQEfY5u7Qv/YHx397NuwjhAiwv1OlIhPfI47w1aMqL\n" +
            "CbUh5q/XWo44a6Q1JUtzeTnKVsFH6eHnkP60FS0B5cs8QfM37fjn/WUvnvfcUp52\n" +
            "V4KFCEyxaiX9n/37a1zZXLX4/CztAgMBAAECgYEAhj5+joIm7bKpw+ecKgxuwkAU\n" +
            "wNLgfdHv5qWBrSXyAKR26Qn2E0W0FDd3XeZy8aOL2WoujR7hKpfoOxpDGXR54dTT\n" +
            "BVCg4Y1eoGqBV+zToTIobG7Mnyz+gzBCKxiraKd5AbRnv4DEmCldUj1LyZp9D3iu\n" +
            "4VvAsac9m4HC0i32gt0CQQDf+bREsyKEHyQhk88zPr85eET/PVbuYT1CWfNeg4t8\n" +
            "CO09L6IRwOk8ox0UzZEdCeX36WfypH0fy0FH/9kLscYXAkEA8xQUYIhxEYQCvZdO\n" +
            "XvUFDxu5HkG1r40PoZyXrt8D22UQ7kogjjBhGbhhuXkeLRvdE2DV8BaRbJUkzGCu\n" +
            "dgPLmwJBAN7tHN4cJki27jF1exAYbiB3csVRekr+AgkemrsB1z13bRovZPKivgMa\n" +
            "nwFrsfnjNj/Q0yORUPDqT2moX5HiObUCQQCqu9/ULgG9G8wQ/sW9TLxQ8FgGOIE0\n" +
            "3Q+tpB+mZF6BK2vI86gKN1D8j9WUQM5BQOD3S3z5NeDPM7K1GvNvURshAkAsin1v\n" +
            "BoWA8i5vgAhBNGBQ2JEEj5cZX6WQ/3WuVLjgs9QaGbUk92KVpW5F/s14rlpiWeSI\n" +
            "sCy2P7o1xl5EgP6H\n" +
            "-----END PRIVATE KEY-----\n";

        // 위와 같은 키의 PKCS#1 형식입니다.
        private const string Pkcs1Pem =
            "-----BEGIN RSA PRIVATE KEY-----\n" +
            "MIICXgIBAAKBgQDUq5dzQoBOLeaba09mFy+fW5Xj5A/rnmKuR/I0UBH2Obu0L/2B\n" +
            "8d/ezbsI4QIsL9TpSIT3yOO8NWjKiwm1Ieav11qOOGukNSVLc3k5ylbBR+nh55D+\n" +
            "tBUtAeXLPEHzN+345/1lL5733FKedleChQhMsWol/Z/9+2tc2Vy1+Pws7QIDAQAB\n" +
            "AoGBAIY+fo6CJu2yqcPnnCoMbsJAFMDS4H3R7+alga0l8gCkdukJ9hNFtBQ3d13m\n" +
            "cvGji9lqLo0e4SqX6DsaQxl0eeHU0wVQoOGNXqBqgVfs06EyKGxuzJ8s/oMwQisY\n" +
            "q2ineQG0Z7+AxJgpXVI9S8mafQ94ruFbwLGnPZuBwtIt9oLdAkEA3/m0RLMihB8k\n" +
            "IZPPMz6/OXhE/z1W7mE9QlnzXoOLfAjtPS+iEcDpPKMdFM2RHQnl9+ln8qR9H8tB\n" +
            "R//ZC7HGFwJBAPMUFGCIcRGEAr2XTl71BQ8buR5Bta+ND6Gcl67fA9tlEO5KII4w\n" +
            "YRm4Ybl5Hi0b3RNg1fAWkWyVJMxgrnYDy5sCQQDe7RzeHCZItu4xdXsQGG4gd3LF\n" +
            "UXpK/gIJHpq7Adc9d20aL2Tyor4DGp8Ba7H54zY/0NMjkVDw6k9pqF+R4jm1AkEA\n" +
            "qrvf1C4BvRvMEP7FvUy8UPBYBjiBNN0PraQfpmRegStryPOoCjdQ/I/VlEDOQUDg\n" +
            "90t8+TXgzzOytRrzb1EbIQJALIp9bwaFgPIub4AIQTRgUNiRBI+XGV+lkP91rlS4\n" +
            "4LPUGhm1JPdilaVuRf7NeK5aYlnkiLAstj+6NcZeRID+hw==\n" +
            "-----END RSA PRIVATE KEY-----\n";

        // .NET의 RSA.ExportPkcs8PrivateKeyPem으로 만든 원래 키가 "nyonyo"에 RS256으로 서명한 값입니다.
        private const string ExpectedSignature =
            "f+bGtpdwtoQSb0joZlvuWDXWO0aQRBOSaHiSCJ2NLrpXXJ+CZS6hX+bNIU/wvl0P7dkR2lS2sm/i2MgzeTtjPFJDixN9sfDSlHt/ajW635umRcGmJTFzYg+rhQj/NaUBzFkWKP2cCmHj30NNxCsY7AYDO6n9s/R0BIyqgqSpj6g=";

        [Test]
        public void Parse_Pkcs8_SignsExactlyLikeOriginalKey()
        {
            var key = new RsaPrivateKeyParser().Parse(Pkcs8Pem);

            Assert.AreEqual(ExpectedSignature, Sign(key, "nyonyo"));
        }

        [Test]
        public void Parse_Pkcs1_IsSameKey()
        {
            var parser = new RsaPrivateKeyParser();

            var pkcs1 = parser.Parse(Pkcs1Pem);
            var pkcs8 = parser.Parse(Pkcs8Pem);

            CollectionAssert.AreEqual(pkcs8.Modulus, pkcs1.Modulus);
            CollectionAssert.AreEqual(pkcs8.D, pkcs1.D);
            Assert.AreEqual(ExpectedSignature, Sign(pkcs1, "nyonyo"));
        }

        [Test]
        public void Parse_JsonEscapedNewlines_Works()
        {
            // JSON 키 파일을 읽으면 \n이 실제 줄바꿈이 되지만, 줄바꿈이 없어도 읽혀야 합니다.
            var key = new RsaPrivateKeyParser().Parse(Pkcs8Pem.Replace("\n", ""));

            Assert.AreEqual(ExpectedSignature, Sign(key, "nyonyo"));
        }

        [Test]
        public void Parse_NotAKey_Throws()
        {
            var parser = new RsaPrivateKeyParser();

            Assert.Throws<FormatException>(() => parser.Parse("not a key"));
            Assert.Throws<FormatException>(() => parser.Parse("-----BEGIN PRIVATE KEY-----\nAAAA\n-----END PRIVATE KEY-----"));
            Assert.Throws<FormatException>(() => parser.Parse("-----BEGIN ENCRYPTED PRIVATE KEY-----\nAAAA\n-----END ENCRYPTED PRIVATE KEY-----"));
        }

        [Test]
        public void Assertion_HasGoogleClaimsAndValidSignature()
        {
            var key = new RsaPrivateKeyParser().Parse(Pkcs8Pem);
            var now = new DateTimeOffset(2026, 10, 27, 0, 0, 0, TimeSpan.Zero);
            var assertion = new ServiceAccountAssertion();

            var jwt = assertion.Create("reader@game.iam.gserviceaccount.com", "https://www.googleapis.com/auth/drive.readonly",
                "https://oauth2.googleapis.com/token", now, key);

            var parts = jwt.Split('.');
            Assert.AreEqual(3, parts.Length);
            Assert.AreEqual("{\"alg\":\"RS256\",\"typ\":\"JWT\"}", DecodeBase64Url(parts[0]));
            var claims = DecodeBase64Url(parts[1]);
            StringAssert.Contains("\"iss\":\"reader@game.iam.gserviceaccount.com\"", claims);
            StringAssert.Contains("\"scope\":\"https://www.googleapis.com/auth/drive.readonly\"", claims);
            StringAssert.Contains("\"aud\":\"https://oauth2.googleapis.com/token\"", claims);
            StringAssert.Contains($"\"iat\":{now.ToUnixTimeSeconds()}", claims);
            StringAssert.Contains($"\"exp\":{now.ToUnixTimeSeconds() + 3600}", claims);
            StringAssert.DoesNotContain("=", jwt, "Base64Url은 = 패딩이 없습니다.");

            using var rsa = RSA.Create();
            rsa.ImportParameters(key);
            Assert.IsTrue(rsa.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), FromBase64Url(parts[2]),
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }

        private string Sign(RSAParameters key, string text)
        {
            using var rsa = RSA.Create();
            rsa.ImportParameters(key);
            return Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(text), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }

        private string DecodeBase64Url(string text) => Encoding.UTF8.GetString(FromBase64Url(text));

        private byte[] FromBase64Url(string text)
        {
            var base64 = text.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
        }
    }
}
