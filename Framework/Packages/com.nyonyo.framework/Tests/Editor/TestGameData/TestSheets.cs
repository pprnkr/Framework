using System;
using System.Collections.Generic;
using Nyonyo.Framework.Editor.Data;

namespace Nyonyo.Framework.Editor.Tests
{
    /// <summary>
    /// 게임 데이터 테스트에서 함께 쓰는 시트 예시와 파서입니다.
    /// </summary>
    internal sealed class TestSheets
    {
        /// <summary>
        /// <see cref="TestItemData"/>와 같은 열 구성의 Item 시트입니다.
        /// 배열 Attack은 int[]와 Array&lt;int&gt; 두 열, 메모 열(_Memo), 건너뛸 행(_1002), 빈 행이 들어 있습니다.
        /// </summary>
        public List<string[]> ItemGrid() => new()
        {
            new[] { "아이템 ID", "이름", "공격력", "공격력", "등급", "기획 메모", "가격" },
            new[] { "int", "string", "int[]", "Array<int>", "TestItemGrade", "string", "float" },
            new[] { "Id", "Name", "Attack", "Attack", "Grade", "_Memo", "Price" },
            new[] { "1001", "검", "10", "20", "Rare", "아무 값", "1.5" },
            new[] { "_1002", "숨긴 행", "abc", "", "Nope", "", "x" },
            new[] { "", "", "", "", "", "메모만 있는 행", "" },
            new[] { "1003", "방패", "5", "", "common", "", "" },
        };

        public SheetTableParser CreateParser() => new(FindEnum);

        public SheetTable ParseItem()
        {
            var errors = new List<string>();
            var table = CreateParser().Parse("Item", ItemGrid(), errors);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));
            return table;
        }

        private Type FindEnum(string name, out string error)
        {
            error = null;
            if (name == nameof(TestItemGrade) || name == typeof(TestItemGrade).FullName)
                return typeof(TestItemGrade);

            error = $"enum '{name}'도 찾지 못했습니다";
            return null;
        }
    }
}
