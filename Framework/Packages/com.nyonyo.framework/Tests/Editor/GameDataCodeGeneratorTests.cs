using Nyonyo.Framework.Assets;
using Nyonyo.Framework.Editor.Data;
using NUnit.Framework;

namespace Nyonyo.Framework.Editor.Tests
{
    public class GameDataCodeGeneratorTests
    {
        private SheetTable _item;
        private GameDataCodeGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _item = new TestSheets().ParseItem();
            _generator = new GameDataCodeGenerator("Game.Data", "GameData", LoadType.Addressables, "GameData/{0}", new byte[64]);
        }

        [Test]
        public void Table_GeneratesPartialClassesWithReadInColumnOrder()
        {
            var code = _generator.GenerateTable(_item);

            StringAssert.StartsWith(GameDataCodeGenerator.Marker, code);
            StringAssert.Contains("namespace Game.Data", code);
            StringAssert.Contains("public sealed partial class ItemData", code);
            StringAssert.Contains("/// <summary>아이템 ID</summary>", code);
            StringAssert.Contains("public int[] Attack { get; private set; }", code);
            StringAssert.Contains("public global::Nyonyo.Framework.Editor.Tests.TestItemGrade Grade { get; private set; }", code);
            StringAssert.Contains("partial void OnLoaded();", code);
            StringAssert.DoesNotContain("Memo", code, "변수명이 _로 시작하는 열은 만들지 않습니다.");

            var read = code.IndexOf("Id = reader.ReadInt();");
            Assert.Less(read, code.IndexOf("Name = reader.ReadString();"));
            Assert.Less(code.IndexOf("Name = reader.ReadString();"), code.IndexOf("Attack = reader.ReadIntArray();"));
            Assert.Less(code.IndexOf("Attack = reader.ReadIntArray();"), code.IndexOf("Grade = (global::Nyonyo.Framework.Editor.Tests.TestItemGrade)reader.ReadInt();"));

            StringAssert.Contains("public sealed partial class ItemTable : global::Nyonyo.Framework.Data.DataTable<int, ItemData>", code);
            StringAssert.Contains($"protected override long SchemaHash => {_item.SchemaHash}L;", code);
            StringAssert.Contains("protected override int GetKey(ItemData row) => row.Id;", code);
        }

        [Test]
        public void Root_ListsTablesAndLoadSettings()
        {
            var code = _generator.GenerateRoot(new[] { _item });

            StringAssert.Contains("public sealed partial class GameData : global::Nyonyo.Framework.Data.GameDataBase", code);
            StringAssert.Contains("public ItemTable Item { get; } = new ItemTable();", code);
            StringAssert.Contains("_tables = new global::Nyonyo.Framework.Data.DataTableBase[] { Item };", code);
            StringAssert.Contains("public static GameData Instance => global::Nyonyo.Framework.Services.Get<GameData>();", code);
            StringAssert.Contains("LoadType => global::Nyonyo.Framework.Assets.LoadType.Addressables;", code);
            StringAssert.Contains("PathFormat => \"GameData/{0}\";", code);
        }
    }
}
