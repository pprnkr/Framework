using System.IO;

namespace Nyonyo.Framework.Data
{
    /// <summary>
    /// 시트 하나를 담는 데이터 테이블의 공통 부분입니다. 실제 테이블은 데이터 가져오기가 자동으로 만듭니다(<see cref="DataTable{TKey,TRow}"/>).
    /// </summary>
    public abstract class DataTableBase
    {
        /// <summary>시트 이름입니다. 데이터 파일 이름으로도 씁니다.</summary>
        public abstract string Name { get; }

        public abstract int Count { get; }

        public bool IsLoaded { get; private set; }

        /// <summary>코드를 만들 때의 열 구성으로 계산한 값입니다. 데이터 파일의 값과 다르면 불러오지 않습니다.</summary>
        protected abstract long SchemaHash { get; }

        /// <summary>
        /// 테이블을 다 불러온 뒤 호출됩니다. 게임 쪽 partial 클래스에서 재정의해 추가 색인 등을 만드세요.
        /// </summary>
        protected virtual void OnLoaded()
        {
        }

        /// <param name="payload">복호화한 데이터 파일 내용입니다.</param>
        /// <exception cref="InvalidDataException">형식이 맞지 않거나 코드와 데이터의 열 구성이 다른 경우</exception>
        internal void Load(byte[] payload)
        {
            var reader = new GameDataReader(payload);
            if (reader.ReadInt() != GameDataFormat.Magic)
                throw new InvalidDataException($"{Name}: 게임 데이터 파일이 아닙니다.");

            var version = reader.ReadInt();
            if (version != GameDataFormat.Version)
                throw new InvalidDataException($"{Name}: 데이터 형식 버전({version})이 프레임워크({GameDataFormat.Version})와 다릅니다. 데이터를 다시 가져오세요.");

            if (reader.ReadLong() != SchemaHash)
                throw new InvalidDataException($"{Name}: 데이터 파일과 코드의 열 구성이 다릅니다. 데이터를 다시 가져온 뒤 컴파일이 끝났는지 확인하세요.");

            var count = reader.ReadInt();
            if (count < 0)
                throw new InvalidDataException($"{Name}: 행 수({count})가 잘못되었습니다.");

            LoadRows(reader, count);
            if (!reader.IsAtEnd)
                throw new InvalidDataException($"{Name}: 데이터가 코드보다 깁니다. 코드와 데이터가 맞지 않습니다. 데이터를 다시 가져오세요.");

            IsLoaded = true;
            OnLoaded();
        }

        private protected abstract void LoadRows(GameDataReader reader, int count);
    }
}
