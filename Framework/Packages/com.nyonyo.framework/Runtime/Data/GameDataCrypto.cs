using System;
using System.IO;
using System.Security.Cryptography;

namespace Nyonyo.Framework.Data
{
    /// <summary>
    /// 게임 데이터 파일을 AES-256(CBC)으로 암호화하고, HMAC-SHA256으로 변조를 검사합니다.
    /// </summary>
    /// <remarks>
    /// <para>파일 구조: [IV 16][암호문][HMAC 32]. HMAC은 IV와 암호문 전체에 대해 계산합니다.</para>
    /// <para>
    /// IV는 원문의 HMAC에서 만듭니다. 같은 데이터를 다시 가져오면 같은 파일이 나와서, 바뀌지 않은 테이블이 git 변경으로 잡히지 않습니다.
    /// </para>
    /// <para>키가 앱 안에 들어 있으므로 완벽한 보안은 아닙니다. 파일을 열어 보거나 고치는 가벼운 시도를 막는 용도입니다.</para>
    /// </remarks>
    internal sealed class GameDataCrypto
    {
        /// <summary>키 길이입니다. 앞 32바이트는 AES 키, 뒤 32바이트는 HMAC 키입니다.</summary>
        public const int KeySize = 64;

        private const int IvSize = 16;
        private const int MacSize = 32;
        private const int BlockSize = 16;

        private readonly byte[] _aesKey = new byte[32];
        private readonly byte[] _macKey = new byte[32];

        public GameDataCrypto(byte[] key)
        {
            if (key == null || key.Length != KeySize)
                throw new ArgumentException($"암호화 키는 {KeySize}바이트여야 합니다. 게임 데이터를 다시 가져와 코드를 새로 만드세요.", nameof(key));

            Buffer.BlockCopy(key, 0, _aesKey, 0, 32);
            Buffer.BlockCopy(key, 32, _macKey, 0, 32);
        }

        public byte[] Encrypt(byte[] plain)
        {
            var iv = new byte[IvSize];
            using (var hmac = new HMACSHA256(_macKey))
                Buffer.BlockCopy(hmac.ComputeHash(plain), 0, iv, 0, IvSize);

            byte[] cipher;
            using (var aes = CreateAes())
            using (var encryptor = aes.CreateEncryptor(_aesKey, iv))
                cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);

            var result = new byte[IvSize + cipher.Length + MacSize];
            Buffer.BlockCopy(iv, 0, result, 0, IvSize);
            Buffer.BlockCopy(cipher, 0, result, IvSize, cipher.Length);
            var mac = ComputeMac(result, IvSize + cipher.Length);
            Buffer.BlockCopy(mac, 0, result, IvSize + cipher.Length, MacSize);
            return result;
        }

        /// <exception cref="InvalidDataException">파일이 손상되었거나, 고쳐졌거나, 다른 키로 암호화된 경우</exception>
        public byte[] Decrypt(byte[] data)
        {
            if (data == null || data.Length < IvSize + BlockSize + MacSize)
                throw new InvalidDataException("게임 데이터 파일이 너무 짧습니다. 파일이 손상되었습니다. 데이터를 다시 가져오세요.");

            var signedLength = data.Length - MacSize;
            var expected = ComputeMac(data, signedLength);
            var difference = 0;
            for (var i = 0; i < MacSize; i++)
                difference |= expected[i] ^ data[signedLength + i];
            if (difference != 0)
                throw new InvalidDataException("게임 데이터 파일 검사에 실패했습니다. 파일이 고쳐졌거나, 다른 암호화 키로 만든 파일입니다. 데이터를 다시 가져오세요.");

            var iv = new byte[IvSize];
            Buffer.BlockCopy(data, 0, iv, 0, IvSize);
            using var aes = CreateAes();
            using var decryptor = aes.CreateDecryptor(_aesKey, iv);
            return decryptor.TransformFinalBlock(data, IvSize, signedLength - IvSize);
        }

        private Aes CreateAes()
        {
            var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }

        private byte[] ComputeMac(byte[] data, int length)
        {
            using var hmac = new HMACSHA256(_macKey);
            return hmac.ComputeHash(data, 0, length);
        }
    }
}
