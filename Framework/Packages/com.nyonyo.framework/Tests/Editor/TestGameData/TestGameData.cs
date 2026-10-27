using System.Collections.Generic;
using Nyonyo.Framework.Assets;
using Nyonyo.Framework.Data;

namespace Nyonyo.Framework.Editor.Tests
{
    /// <summary>
    /// 코드 생성기가 만드는 서비스 클래스와 같은 모양의 테스트용 클래스입니다.
    /// </summary>
    public sealed class TestGameData : GameDataBase
    {
        private readonly byte[] _key;
        private readonly DataTableBase[] _tables;

        public TestGameData(IAssetLoader loader, byte[] key, long itemSchemaHash) : base(loader)
        {
            _key = key;
            Item = new TestItemTable(itemSchemaHash);
            _tables = new DataTableBase[] { Item };
        }

        public TestItemTable Item { get; }

        public override IReadOnlyList<DataTableBase> Tables => _tables;

        protected override LoadType LoadType => LoadType.Resources;

        protected override string PathFormat => "GameData/{0}";

        protected override byte[] EncryptionKey => _key;
    }
}
