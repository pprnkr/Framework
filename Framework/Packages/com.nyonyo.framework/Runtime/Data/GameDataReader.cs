using System;
using System.IO;
using System.Text;

namespace Nyonyo.Framework.Data
{
    /// <summary>
    /// 게임 데이터 바이너리를 읽습니다. 자동 생성된 데이터 클래스의 <c>Read</c>가 열 순서대로 호출합니다.
    /// </summary>
    /// <remarks>리플렉션 없이 정해진 순서로 값을 꺼내므로 IL2CPP(AOT)에서도 빠르고 안전합니다.</remarks>
    public sealed class GameDataReader
    {
        private readonly byte[] _buffer;
        private int _position;

        internal GameDataReader(byte[] buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        }

        internal bool IsAtEnd => _position == _buffer.Length;

        public int ReadInt()
        {
            Require(4);
            var value = _buffer[_position] | _buffer[_position + 1] << 8 | _buffer[_position + 2] << 16 | _buffer[_position + 3] << 24;
            _position += 4;
            return value;
        }

        public long ReadLong()
        {
            var low = (uint)ReadInt();
            var high = (uint)ReadInt();
            return (long)((ulong)high << 32 | low);
        }

        public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt());

        public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadLong());

        public bool ReadBool()
        {
            Require(1);
            return _buffer[_position++] != 0;
        }

        public string ReadString()
        {
            var length = ReadCount();
            if (length == 0)
                return string.Empty;

            var value = Encoding.UTF8.GetString(_buffer, _position, length);
            _position += length;
            return value;
        }

        /// <summary>배열 길이나 문자열 바이트 수를 읽습니다. 남은 데이터보다 크면 손상된 파일로 봅니다.</summary>
        public int ReadCount()
        {
            var count = ReadInt();
            if (count < 0 || count > _buffer.Length - _position)
                throw new InvalidDataException($"데이터 길이 값({count})이 잘못되었습니다. 파일이 손상되었거나 코드와 데이터가 맞지 않습니다.");
            return count;
        }

        public int[] ReadIntArray()
        {
            var values = new int[ReadCount()];
            for (var i = 0; i < values.Length; i++)
                values[i] = ReadInt();
            return values;
        }

        public long[] ReadLongArray()
        {
            var values = new long[ReadCount()];
            for (var i = 0; i < values.Length; i++)
                values[i] = ReadLong();
            return values;
        }

        public float[] ReadFloatArray()
        {
            var values = new float[ReadCount()];
            for (var i = 0; i < values.Length; i++)
                values[i] = ReadFloat();
            return values;
        }

        public double[] ReadDoubleArray()
        {
            var values = new double[ReadCount()];
            for (var i = 0; i < values.Length; i++)
                values[i] = ReadDouble();
            return values;
        }

        public bool[] ReadBoolArray()
        {
            var values = new bool[ReadCount()];
            for (var i = 0; i < values.Length; i++)
                values[i] = ReadBool();
            return values;
        }

        public string[] ReadStringArray()
        {
            var values = new string[ReadCount()];
            for (var i = 0; i < values.Length; i++)
                values[i] = ReadString();
            return values;
        }

        private void Require(int bytes)
        {
            if (_buffer.Length - _position < bytes)
                throw new InvalidDataException("데이터가 중간에 끊겼습니다. 파일이 손상되었거나 코드와 데이터가 맞지 않습니다. 데이터를 다시 가져오세요.");
        }
    }
}
