using System;
using System.Collections.Generic;
using System.IO;

namespace Nyonyo.Framework.Data
{
    /// <summary>
    /// 첫 번째 열(유니크 키)로 행을 찾는 데이터 테이블입니다. 데이터 가져오기가 시트마다 상속 클래스를 만듭니다.
    /// </summary>
    /// <example>
    /// <code>
    /// var sword = GameData.Instance.Item.Get(1001);
    /// if (GameData.Instance.Item.TryGet(id, out var item)) { ... }
    /// foreach (var monster in GameData.Instance.Monster.All) { ... }
    /// </code>
    /// </example>
    public abstract class DataTable<TKey, TRow> : DataTableBase where TRow : class
    {
        private TRow[] _rows = Array.Empty<TRow>();
        private Dictionary<TKey, TRow> _byKey = new();

        public override int Count => _rows.Length;

        /// <summary>시트 순서대로 담긴 모든 행입니다.</summary>
        public IReadOnlyList<TRow> All => _rows;

        /// <exception cref="KeyNotFoundException">키에 맞는 행이 없는 경우</exception>
        public TRow Get(TKey key)
        {
            if (_byKey.TryGetValue(key, out var row))
                return row;

            if (!IsLoaded)
                throw new InvalidOperationException($"{Name} 테이블을 아직 불러오지 않았습니다. GameData를 서비스로 등록했는지 확인하세요.");
            throw new KeyNotFoundException($"{Name} 테이블에 키 '{key}'가 없습니다.");
        }

        public bool TryGet(TKey key, out TRow row) => _byKey.TryGetValue(key, out row);

        public bool Contains(TKey key) => _byKey.ContainsKey(key);

        /// <summary>행 하나를 읽습니다. 자동 생성 코드가 구현합니다.</summary>
        protected abstract TRow ReadRow(GameDataReader reader);

        /// <summary>행의 키(첫 번째 열)입니다. 자동 생성 코드가 구현합니다.</summary>
        protected abstract TKey GetKey(TRow row);

        private protected sealed override void LoadRows(GameDataReader reader, int count)
        {
            var rows = new TRow[count];
            var byKey = new Dictionary<TKey, TRow>(count);
            for (var i = 0; i < count; i++)
            {
                var row = ReadRow(reader);
                if (!byKey.TryAdd(GetKey(row), row))
                    throw new InvalidDataException($"{Name}: 키 '{GetKey(row)}'가 두 번 있습니다. 데이터를 다시 가져오세요.");
                rows[i] = row;
            }

            _rows = rows;
            _byKey = byKey;
        }
    }
}
