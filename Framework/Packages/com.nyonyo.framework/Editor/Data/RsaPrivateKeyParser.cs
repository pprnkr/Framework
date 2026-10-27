using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Nyonyo.Framework.Editor.Data
{
    /// <summary>
    /// PEM 형식의 RSA 개인 키(PKCS#8 "BEGIN PRIVATE KEY" 또는 PKCS#1 "BEGIN RSA PRIVATE KEY")를 <see cref="RSAParameters"/>로 읽습니다.
    /// </summary>
    /// <remarks>
    /// 구글 서비스 계정 키(JSON)의 private_key가 PKCS#8 PEM입니다. Unity의 Mono는 <c>ImportPkcs8PrivateKey</c>를 지원하지 않을 수 있어서
    /// DER(ASN.1)을 직접 읽고, 어느 플랫폼에서도 되는 <see cref="RSA.ImportParameters"/>로 넘길 수 있게 길이를 맞춥니다.
    /// </remarks>
    internal sealed class RsaPrivateKeyParser
    {
        // rsaEncryption OID 1.2.840.113549.1.1.1
        private static readonly byte[] s_rsaOid = { 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01 };

        private static readonly Regex s_pem = new(@"-----BEGIN (?<label>[A-Z ]+)-----(?<body>[\s\S]*?)-----END \k<label>-----");

        /// <exception cref="FormatException">PEM이나 키 형식이 맞지 않는 경우</exception>
        public RSAParameters Parse(string pem)
        {
            var match = s_pem.Match(pem ?? "");
            if (!match.Success)
                throw new FormatException("PEM 형식의 개인 키가 아닙니다(-----BEGIN PRIVATE KEY-----로 시작해야 합니다).");

            byte[] der;
            try
            {
                der = Convert.FromBase64String(Regex.Replace(match.Groups["body"].Value, @"\s", ""));
            }
            catch (FormatException)
            {
                throw new FormatException("개인 키의 Base64 내용이 손상되었습니다. 키 파일을 다시 받으세요.");
            }

            var label = match.Groups["label"].Value;
            if (label == "PRIVATE KEY")
                return ReadPkcs1(ReadPkcs8(der));
            if (label == "RSA PRIVATE KEY")
                return ReadPkcs1(der);
            throw new FormatException($"'{label}' 키는 지원하지 않습니다. 암호가 없는 RSA 개인 키(BEGIN PRIVATE KEY)를 쓰세요.");
        }

        // PrivateKeyInfo ::= SEQUENCE { version INTEGER, algorithm SEQUENCE { OID, NULL }, privateKey OCTET STRING, ... }
        private byte[] ReadPkcs8(byte[] der)
        {
            var reader = new DerReader(der);
            var info = reader.ReadSequence();
            info.ReadInteger();
            var algorithm = info.ReadSequence();
            var oid = algorithm.Read(0x06);
            if (!AreEqual(oid, s_rsaOid))
                throw new FormatException("RSA 키가 아닙니다.");
            return info.Read(0x04);
        }

        // RSAPrivateKey ::= SEQUENCE { version, n, e, d, p, q, dp, dq, qinv }
        private RSAParameters ReadPkcs1(byte[] der)
        {
            var key = new DerReader(der).ReadSequence();
            key.ReadInteger();
            var modulus = Trim(key.ReadInteger());
            var exponent = Trim(key.ReadInteger());
            var half = (modulus.Length + 1) / 2;

            // Windows의 CryptoAPI/CNG는 길이가 정확히 맞아야 받아 줍니다(D는 n과 같게, 나머지는 n의 절반).
            return new RSAParameters
            {
                Modulus = modulus,
                Exponent = exponent,
                D = Pad(key.ReadInteger(), modulus.Length),
                P = Pad(key.ReadInteger(), half),
                Q = Pad(key.ReadInteger(), half),
                DP = Pad(key.ReadInteger(), half),
                DQ = Pad(key.ReadInteger(), half),
                InverseQ = Pad(key.ReadInteger(), half),
            };
        }

        // DER 정수는 양수 표시용 0이 앞에 붙을 수 있어서 떼어 냅니다.
        private byte[] Trim(byte[] value)
        {
            var start = 0;
            while (start < value.Length - 1 && value[start] == 0)
                start++;
            if (start == 0)
                return value;

            var trimmed = new byte[value.Length - start];
            Buffer.BlockCopy(value, start, trimmed, 0, trimmed.Length);
            return trimmed;
        }

        private byte[] Pad(byte[] value, int length)
        {
            value = Trim(value);
            if (value.Length > length)
                throw new FormatException("RSA 키 값의 길이가 맞지 않습니다. 키 파일이 손상되었습니다.");
            if (value.Length == length)
                return value;

            var padded = new byte[length];
            Buffer.BlockCopy(value, 0, padded, length - value.Length, value.Length);
            return padded;
        }

        private bool AreEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }

        /// <summary>키를 읽는 데 필요한 만큼만 구현한 DER 읽기입니다.</summary>
        private sealed class DerReader
        {
            private readonly byte[] _data;
            private readonly int _end;
            private int _position;

            public DerReader(byte[] data) : this(data, 0, data.Length)
            {
            }

            private DerReader(byte[] data, int start, int end)
            {
                _data = data;
                _position = start;
                _end = end;
            }

            public DerReader ReadSequence()
            {
                var (start, length) = ReadHeader(0x30);
                return new DerReader(_data, start, start + length);
            }

            public byte[] ReadInteger() => Read(0x02);

            /// <summary><paramref name="tag"/> 값을 읽어 내용을 돌려줍니다.</summary>
            public byte[] Read(byte tag)
            {
                var (start, length) = ReadHeader(tag);
                var value = new byte[length];
                Buffer.BlockCopy(_data, start, value, 0, length);
                return value;
            }

            private (int start, int length) ReadHeader(byte tag)
            {
                if (_position >= _end || _data[_position] != tag)
                    throw new FormatException("개인 키 형식이 올바르지 않습니다. 구글 서비스 계정의 JSON 키를 그대로 쓰세요.");
                _position++;

                var length = (int)_data[_position++];
                if (length > 0x7F)
                {
                    var bytes = length & 0x7F;
                    if (bytes > 3)
                        throw new FormatException("개인 키 형식이 올바르지 않습니다.");
                    length = 0;
                    for (var i = 0; i < bytes; i++)
                        length = length << 8 | _data[_position++];
                }

                var start = _position;
                if (start + length > _end)
                    throw new FormatException("개인 키가 중간에 끊겼습니다. 키 파일이 손상되었습니다.");
                _position += length;
                return (start, length);
            }
        }
    }
}
