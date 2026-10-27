using System.Collections.Generic;

namespace Nyonyo.Framework.Editor.Data
{
    /// <summary>
    /// 생성 클래스의 속성 하나입니다. 배열이면 같은 변수명을 가진 여러 열이 하나로 묶입니다.
    /// </summary>
    internal sealed class SheetField
    {
        private readonly List<int> _columns = new();

        public SheetField(string name, string comment, SheetColumnType type, int column)
        {
            Name = name;
            Comment = comment;
            Type = type;
            _columns.Add(column);
        }

        public string Name { get; }

        /// <summary>시트 1행의 주석입니다. 생성 코드의 문서 주석이 됩니다.</summary>
        public string Comment { get; private set; }

        public SheetColumnType Type { get; }

        /// <summary>이 속성의 값이 들어 있는 열 번호(0부터)입니다. 배열이면 왼쪽부터 순서대로입니다.</summary>
        public IReadOnlyList<int> Columns => _columns;

        public void AddColumn(int column, string comment)
        {
            _columns.Add(column);
            if (string.IsNullOrEmpty(Comment))
                Comment = comment;
        }
    }
}
