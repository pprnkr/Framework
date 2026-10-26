using System;
using UnityEngine;

namespace Nyonyo.Framework.Data
{
    /// <summary>
    /// 모든 데이터 테이블의 기본 클래스입니다. 직접 상속하지 말고 <see cref="DataTable{TRow}"/>를 상속하세요.
    /// </summary>
    public abstract class DataTableBase : ScriptableObject
    {
        public abstract int Count { get; }

        public abstract Type RowType { get; }

        /// <summary>CSV로 모든 행을 바꿉니다. 에디터의 Import CSV 메뉴가 사용합니다.</summary>
        internal abstract void ImportCsv(string csv);
    }
}
