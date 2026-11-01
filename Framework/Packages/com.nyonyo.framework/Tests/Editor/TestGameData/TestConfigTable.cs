using Nyonyo.Framework.Data;

namespace Nyonyo.Framework.Editor.Tests
{
    /// <summary>
    /// 코드 생성기가 Config 시트로 만드는 테이블 클래스와 같은 모양의 테스트용 클래스입니다. 열 구성 해시만 테스트에서 넘겨받습니다.
    /// 값: IntTest(int), StrTest(string), Rewards(int[]), Grade(enum), Rate(float)
    /// </summary>
    public sealed class TestConfigTable : ConfigTableBase
    {
        private readonly long _schemaHash;

        public TestConfigTable(long schemaHash)
        {
            _schemaHash = schemaHash;
        }

        public override string Name => "Config";

        public override int Count => 5;

        protected override long SchemaHash => _schemaHash;

        public int IntTest { get; private set; }

        public string StrTest { get; private set; }

        public int[] Rewards { get; private set; }

        public TestItemGrade Grade { get; private set; }

        public float Rate { get; private set; }

        protected override void ReadValues(GameDataReader reader)
        {
            IntTest = reader.ReadInt();
            StrTest = reader.ReadString();
            Rewards = reader.ReadIntArray();
            Grade = (TestItemGrade)reader.ReadInt();
            Rate = reader.ReadFloat();
        }
    }
}
