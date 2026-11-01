using System.Collections.Generic;
using System.Text;

namespace Nyonyo.Framework.Editor.Data
{
    /// <summary>
    /// 시트 하나를 해석한 결과입니다.
    /// </summary>
    /// <remarks>
    /// 일반 시트는 첫 번째 속성이 유니크 키이고, 행마다 값이 하나씩 있습니다.
    /// Config 시트(<see cref="IsConfig"/>)는 시트의 Key가 속성이 되고, <see cref="Rows"/>에 행이 하나만 있습니다.
    /// </remarks>
    internal sealed class SheetTable
    {
        public SheetTable(string name, IReadOnlyList<SheetField> fields, IReadOnlyList<object[]> rows, bool isConfig = false)
        {
            Name = name;
            Fields = fields;
            Rows = rows;
            IsConfig = isConfig;
            SchemaHash = ComputeSchemaHash();
        }

        /// <summary>시트 이름입니다. 클래스 이름(<c>{Name}Data</c>, <c>{Name}Table</c>)과 데이터 파일 이름에 씁니다.</summary>
        public string Name { get; }

        public string RowClassName => Name + "Data";

        public string TableClassName => Name + "Table";

        /// <summary>Type, Key, Value 열로 값을 아래로 적는 Config 시트인지 여부입니다.</summary>
        public bool IsConfig { get; }

        public IReadOnlyList<SheetField> Fields { get; }

        /// <summary>일반 시트의 유니크 키(첫 번째 속성)입니다. Config 시트에는 쓰지 않습니다.</summary>
        public SheetField Key => Fields[0];

        /// <summary>
        /// 행마다 속성 순서대로 담은 값입니다. 단일 값은 int, long, float, double, bool, string(enum은 int)이고, 배열은 <see cref="List{T}"/>입니다.
        /// </summary>
        public IReadOnlyList<object[]> Rows { get; }

        /// <summary>열 구성(이름과 자료형)의 FNV-1a 해시입니다. 생성 코드와 데이터 파일에 함께 들어가 서로 맞는지 확인합니다.</summary>
        public long SchemaHash { get; }

        private long ComputeSchemaHash()
        {
            var text = new StringBuilder(Name);
            if (IsConfig)
                text.Append("|config");
            foreach (var field in Fields)
                text.Append('|').Append(field.Name).Append(':').Append(field.Type.Describe());

            const ulong offset = 14695981039346656037;
            const ulong prime = 1099511628211;
            var hash = offset;
            foreach (var b in Encoding.UTF8.GetBytes(text.ToString()))
            {
                hash ^= b;
                hash *= prime;
            }
            return (long)hash;
        }
    }
}
