using Nyonyo.Framework.Data;

namespace Nyonyo.Framework.Editor.Tests
{
    /// <summary>
    /// 코드 생성기가 만드는 테이블 클래스와 같은 모양의 테스트용 클래스입니다. 열 구성 해시만 테스트에서 넘겨받습니다.
    /// </summary>
    public sealed class TestItemTable : DataTable<int, TestItemData>
    {
        private readonly long _schemaHash;

        public TestItemTable(long schemaHash)
        {
            _schemaHash = schemaHash;
        }

        public int LoadedCount { get; private set; }

        public override string Name => "Item";

        protected override long SchemaHash => _schemaHash;

        protected override TestItemData ReadRow(GameDataReader reader)
        {
            var row = new TestItemData();
            row.Read(reader);
            return row;
        }

        protected override int GetKey(TestItemData row) => row.Id;

        protected override void OnLoaded() => LoadedCount++;
    }
}
