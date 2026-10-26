using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nyonyo.Framework.Data
{
    /// <summary>
    /// 밸런스 데이터(아이템, 몬스터, 스테이지 등)를 담는 ScriptableObject 테이블입니다.
    /// </summary>
    /// <remarks>
    /// 행은 인스펙터에서 직접 편집하거나, 에셋 우클릭(⋮) 메뉴의 <b>Import CSV...</b>로 엑셀/구글 시트에서 내보낸 CSV를 가져옵니다.
    /// 테이블 에셋은 다른 에셋처럼 Addressables나 인스펙터 참조로 불러오세요.
    /// </remarks>
    /// <example>
    /// <code>
    /// [Serializable]
    /// public class ItemRow : IDataRow
    /// {
    ///     public string id;
    ///     public string name;
    ///     public int price;
    ///     public ItemGrade grade;
    ///     public string Id =&gt; id;
    /// }
    ///
    /// [CreateAssetMenu(menuName = "MyGame/Item Table")]
    /// public class ItemTable : DataTable&lt;ItemRow&gt; { }
    ///
    /// var potion = itemTable.Get("potion_small");
    /// </code>
    /// </example>
    public abstract class DataTable<TRow> : DataTableBase where TRow : IDataRow, new()
    {
        [SerializeField] List<TRow> _rows = new();

        [NonSerialized] Dictionary<string, TRow> _index;

        public IReadOnlyList<TRow> Rows => _rows;

        public override int Count => _rows.Count;

        public override Type RowType => typeof(TRow);

        /// <exception cref="KeyNotFoundException">id에 맞는 행이 없는 경우</exception>
        public TRow Get(string id)
        {
            if (TryGet(id, out var row))
                return row;

            throw new KeyNotFoundException($"{name} 테이블에 id '{id}' 행이 없습니다.");
        }

        public bool TryGet(string id, out TRow row)
        {
            EnsureIndex();
            if (id != null)
                return _index.TryGetValue(id, out row);

            row = default;
            return false;
        }

        public bool Contains(string id) => TryGet(id, out _);

        internal override void ImportCsv(string csv)
        {
            _rows = CsvRowMapper.Map<TRow>(csv);
            _index = null;
        }

        protected virtual void OnValidate() => _index = null;

        void EnsureIndex()
        {
            if (_index != null)
                return;

            _index = new Dictionary<string, TRow>();
            foreach (var row in _rows)
            {
                var id = row?.Id;
                if (string.IsNullOrEmpty(id))
                    continue;

                if (!_index.TryAdd(id, row))
                    Debug.LogWarning($"[Nyonyo Framework] {name} 테이블에 id '{id}'가 두 번 있습니다. 첫 번째 행만 씁니다.", this);
            }
        }
    }
}
