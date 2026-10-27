using Nyonyo.Framework.Data;

namespace Nyonyo.Framework.Editor.Tests
{
    /// <summary>
    /// 코드 생성기가 만드는 행 클래스와 같은 모양으로 직접 쓴 테스트용 클래스입니다.
    /// 열 구성: Id(int), Name(string), Attack(int[]), Grade(enum), Price(float)
    /// </summary>
    public sealed partial class TestItemData
    {
        public int Id { get; private set; }

        public string Name { get; private set; }

        public int[] Attack { get; private set; }

        public TestItemGrade Grade { get; private set; }

        public float Price { get; private set; }

        /// <summary>게임 쪽 partial 구현을 흉내 냅니다. 로드가 끝나면 true입니다.</summary>
        public bool LoadedHookCalled { get; private set; }

        partial void OnLoaded();

        internal void Read(GameDataReader reader)
        {
            Id = reader.ReadInt();
            Name = reader.ReadString();
            Attack = reader.ReadIntArray();
            Grade = (TestItemGrade)reader.ReadInt();
            Price = reader.ReadFloat();
            OnLoaded();
        }
    }

    // 게임 쪽에서 덧붙이는 partial 클래스를 흉내 냅니다.
    public sealed partial class TestItemData
    {
        partial void OnLoaded() => LoadedHookCalled = true;
    }
}
