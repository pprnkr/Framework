using System.Collections.Generic;
using System.Linq;
using Nyonyo.Framework.Editor.Data;
using NUnit.Framework;

namespace Nyonyo.Framework.Editor.Tests
{
    public class SheetTableParserTests
    {
        private TestSheets _sheets;
        private SheetTableParser _parser;
        private List<string> _errors;

        [SetUp]
        public void SetUp()
        {
            _sheets = new TestSheets();
            _parser = _sheets.CreateParser();
            _errors = new List<string>();
        }

        [Test]
        public void Header_FirstColumnIsKey_ArraysMerge_UnderscoreColumnSkipped()
        {
            var table = _sheets.ParseItem();

            CollectionAssert.AreEqual(new[] { "Id", "Name", "Attack", "Grade", "Price" }, table.Fields.Select(f => f.Name).ToArray());
            Assert.AreEqual("Id", table.Key.Name);
            Assert.AreEqual("int[]", table.Fields[2].Type.TypeName);
            CollectionAssert.AreEqual(new[] { 2, 3 }, table.Fields[2].Columns.ToArray(), "같은 변수명의 열이 왼쪽부터 원소가 됩니다.");
            Assert.AreEqual("공격력", table.Fields[2].Comment);
            Assert.AreEqual("ItemData", table.RowClassName);
            Assert.AreEqual("ItemTable", table.TableClassName);
        }

        [Test]
        public void Rows_SkipUnderscoreKeysAndEmptyRows()
        {
            var table = _sheets.ParseItem();

            Assert.AreEqual(2, table.Rows.Count);
            var sword = table.Rows[0];
            Assert.AreEqual(1001, sword[0]);
            Assert.AreEqual("검", sword[1]);
            CollectionAssert.AreEqual(new object[] { 10, 20 }, (List<object>)sword[2]);
            Assert.AreEqual((int)TestItemGrade.Rare, sword[3]);
            Assert.AreEqual(1.5f, sword[4]);

            var shield = table.Rows[1];
            CollectionAssert.AreEqual(new object[] { 5 }, (List<object>)shield[2], "배열의 빈 칸은 건너뜁니다.");
            Assert.AreEqual((int)TestItemGrade.Common, shield[3], "enum 이름은 대소문자를 가리지 않습니다.");
            Assert.AreEqual(0f, shield[4], "단일 값의 빈 칸은 기본값입니다.");
        }

        [Test]
        public void DuplicateKey_IsErrorWithBothCells()
        {
            var grid = Grid(new[] { "int", "string" }, new[] { "Id", "Name" },
                new[] { "1", "a" }, new[] { "2", "b" }, new[] { "1", "c" });

            Assert.IsNull(_parser.Parse("Item", grid, _errors));

            Assert.AreEqual(1, _errors.Count);
            StringAssert.Contains("Item!A6", _errors[0]);
            StringAssert.Contains("Item!A4", _errors[0]);
        }

        [Test]
        public void StringKeys_AreSupported_AndUnderscoreKeySkipped()
        {
            var grid = Grid(new[] { "string", "int" }, new[] { "Code", "Value" },
                new[] { "potion", "1" }, new[] { "_draft", "2" }, new[] { "elixir", "3" });

            var table = _parser.Parse("Item", grid, _errors);

            CollectionAssert.IsEmpty(_errors);
            CollectionAssert.AreEqual(new[] { "potion", "elixir" }, table.Rows.Select(r => (string)r[0]).ToArray());
        }

        [Test]
        public void KeyColumn_MustBeIntLongOrString()
        {
            var grid = Grid(new[] { "float", "int" }, new[] { "Id", "Value" }, new[] { "1", "1" });

            Assert.IsNull(_parser.Parse("Item", grid, _errors));
            StringAssert.Contains("Item!A2", _errors[0]);
        }

        [Test]
        public void KeyColumn_CannotBeSkipped()
        {
            var grid = Grid(new[] { "int", "int" }, new[] { "_Id", "Value" }, new[] { "1", "1" });

            Assert.IsNull(_parser.Parse("Item", grid, _errors));
            StringAssert.Contains("유니크 키", _errors[0]);
        }

        [Test]
        public void InvalidValue_ReportsCell_AndCollectsAllErrors()
        {
            var grid = Grid(new[] { "int", "int", "bool" }, new[] { "Id", "Value", "Flag" },
                new[] { "1", "abc", "maybe" }, new[] { "2", "1.5", "o" });

            Assert.IsNull(_parser.Parse("Item", grid, _errors));

            Assert.AreEqual(3, _errors.Count);
            StringAssert.StartsWith("Item!B4", _errors[0]);
            StringAssert.StartsWith("Item!C4", _errors[1]);
            StringAssert.StartsWith("Item!B5", _errors[2], "1.5는 정수가 아닙니다.");
        }

        [Test]
        public void DuplicateName_WithoutArrayType_IsError()
        {
            var grid = Grid(new[] { "int", "int", "int" }, new[] { "Id", "Value", "Value" }, new[] { "1", "1", "2" });

            Assert.IsNull(_parser.Parse("Item", grid, _errors));
            StringAssert.Contains("배열", _errors[0]);
        }

        [Test]
        public void ArrayColumns_WithDifferentTypes_IsError()
        {
            var grid = Grid(new[] { "int", "int[]", "float[]" }, new[] { "Id", "Value", "Value" }, new[] { "1", "1", "2" });

            Assert.IsNull(_parser.Parse("Item", grid, _errors));
            StringAssert.StartsWith("Item!C2", _errors[0]);
        }

        [Test]
        public void UnknownType_IsError()
        {
            var grid = Grid(new[] { "int", "Vector9" }, new[] { "Id", "Value" }, new[] { "1", "1" });

            Assert.IsNull(_parser.Parse("Item", grid, _errors));
            StringAssert.StartsWith("Item!B2", _errors[0]);
        }

        [Test]
        public void EmptyKey_WithValues_IsError()
        {
            var grid = Grid(new[] { "int", "int" }, new[] { "Id", "Value" }, new[] { "", "5" });

            Assert.IsNull(_parser.Parse("Item", grid, _errors));
            StringAssert.StartsWith("Item!A4", _errors[0]);
        }

        [Test]
        public void InvalidNames_AreErrors()
        {
            var grid = Grid(new[] { "int", "int" }, new[] { "Id", "class" }, new[] { "1", "1" });

            Assert.IsNull(_parser.Parse("Item", grid, _errors));
            Assert.IsNull(_parser.Parse("아이템", grid, _errors), "시트 이름은 클래스 이름이 되므로 영문이어야 합니다.");
            Assert.AreEqual(2, _errors.Count);
        }

        [Test]
        public void Values_AcceptCommonSpreadsheetForms()
        {
            var parser = _sheets.CreateParser();
            var intType = new SheetColumnType(SheetValueKind.Int, false);
            var boolType = new SheetColumnType(SheetValueKind.Bool, false);
            var enumType = new SheetColumnType(SheetValueKind.Enum, false, typeof(TestItemGrade));

            Assert.IsTrue(parser.TryParseValue(intType, "3.0", out var three, out _));
            Assert.AreEqual(3, three);
            Assert.IsTrue(parser.TryParseValue(boolType, "TRUE", out var yes, out _));
            Assert.AreEqual(true, yes);
            Assert.IsTrue(parser.TryParseValue(boolType, "x", out var no, out _));
            Assert.AreEqual(false, no);
            Assert.IsFalse(parser.TryParseValue(enumType, "1", out _, out _), "enum은 숫자가 아닌 이름으로만 받습니다.");
            Assert.IsFalse(parser.TryParseValue(intType, "3000000000", out _, out _), "int 범위를 넘으면 에러입니다.");
        }

        [Test]
        public void SchemaHash_ChangesWhenColumnsChange()
        {
            var original = _sheets.ParseItem().SchemaHash;
            Assert.AreEqual(original, _sheets.ParseItem().SchemaHash, "같은 열 구성이면 같은 값입니다.");

            var grid = _sheets.ItemGrid();
            grid[1][6] = "double";
            var changed = _parser.Parse("Item", grid, _errors);

            Assert.AreNotEqual(original, changed.SchemaHash);
        }

        [Test]
        public void CellName_UsesSpreadsheetLetters()
        {
            Assert.AreEqual("Item!A1", _parser.CellName("Item", 0, 0));
            Assert.AreEqual("Item!Z3", _parser.CellName("Item", 2, 25));
            Assert.AreEqual("Item!AB10", _parser.CellName("Item", 9, 27));
        }

        // 1행 주석은 비워 두고, 2행 자료형, 3행 변수명, 4행부터 데이터인 시트를 만듭니다.
        private List<string[]> Grid(string[] types, string[] names, params string[][] rows)
        {
            var grid = new List<string[]> { new string[types.Length], types, names };
            grid.AddRange(rows);
            return grid;
        }
    }
}
