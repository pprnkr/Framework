using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace Nyonyo.Framework.Editor.Data
{
    /// <summary>
    /// 구글 서비스 계정 JSON 키 파일입니다. Google Cloud Console > 서비스 계정 > 키 > 키 추가 > JSON으로 받은 파일을 읽습니다.
    /// </summary>
    internal sealed class ServiceAccountKey
    {
        public const string DefaultTokenUri = "https://oauth2.googleapis.com/token";

        /// <exception cref="FileNotFoundException">파일이 없는 경우</exception>
        /// <exception cref="InvalidDataException">서비스 계정 JSON 키가 아닌 경우</exception>
        public ServiceAccountKey(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException($"서비스 계정 키 파일이 없습니다: {path}. Game Data Settings 인스펙터에서 [키 파일 선택]으로 다시 고르세요.", path);

            KeyFile file;
            try
            {
                file = JsonUtility.FromJson<KeyFile>(File.ReadAllText(path));
            }
            catch (ArgumentException)
            {
                file = null;
            }

            if (file == null || file.type != "service_account" || string.IsNullOrEmpty(file.client_email) || string.IsNullOrEmpty(file.private_key))
                throw new InvalidDataException("서비스 계정 JSON 키 파일이 아닙니다. Google Cloud Console > 서비스 계정 > 키 > 키 추가 > JSON으로 받은 파일을 고르세요.");

            try
            {
                PrivateKey = new RsaPrivateKeyParser().Parse(file.private_key);
            }
            catch (FormatException e)
            {
                throw new InvalidDataException($"서비스 계정 키의 private_key를 읽지 못했습니다: {e.Message}", e);
            }

            ClientEmail = file.client_email;
            TokenUri = string.IsNullOrEmpty(file.token_uri) ? DefaultTokenUri : file.token_uri;
        }

        /// <summary>서비스 계정 이메일입니다. 이 주소를 스프레드시트 [공유]에 뷰어로 추가합니다.</summary>
        public string ClientEmail { get; }

        public string TokenUri { get; }

        public RSAParameters PrivateKey { get; }

        // JsonUtility가 JSON 키 이름 그대로 채우도록 필드 이름을 키 파일과 똑같이 둡니다.
        [Serializable]
        private sealed class KeyFile
        {
            public string type = "";
            public string client_email = "";
            public string private_key = "";
            public string token_uri = "";
        }
    }
}
