using System;
using System.IO;
using System.Text;

namespace Nyonyo.Framework.Data
{
    /// <summary>
    /// <see cref="GameDataReader"/>가 읽는 형식으로 값을 씁니다. 에디터의 데이터 가져오기가 씁니다.
    /// </summary>
    internal sealed class GameDataWriter
    {
        private readonly MemoryStream _stream = new();

        public void WriteInt(int value)
        {
            _stream.WriteByte((byte)value);
            _stream.WriteByte((byte)(value >> 8));
            _stream.WriteByte((byte)(value >> 16));
            _stream.WriteByte((byte)(value >> 24));
        }

        public void WriteLong(long value)
        {
            WriteInt((int)value);
            WriteInt((int)(value >> 32));
        }

        public void WriteFloat(float value) => WriteInt(BitConverter.SingleToInt32Bits(value));

        public void WriteDouble(double value) => WriteLong(BitConverter.DoubleToInt64Bits(value));

        public void WriteBool(bool value) => _stream.WriteByte(value ? (byte)1 : (byte)0);

        public void WriteString(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            WriteInt(bytes.Length);
            _stream.Write(bytes, 0, bytes.Length);
        }

        public byte[] ToArray() => _stream.ToArray();
    }
}
