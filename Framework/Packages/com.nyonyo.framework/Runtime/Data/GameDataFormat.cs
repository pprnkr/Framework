namespace Nyonyo.Framework.Data
{
    /// <summary>
    /// 게임 데이터 파일(복호화한 내용)의 머리말 값입니다.
    /// </summary>
    /// <remarks>
    /// 파일 구조: [Magic 4][Version 4][SchemaHash 8][행 수 4][행...]. 행은 열 순서대로 값을 이어 씁니다.
    /// 숫자는 리틀 엔디언, 문자열은 [바이트 수 4][UTF-8], 배열은 [개수 4][값...]입니다.
    /// </remarks>
    // 상수만 모아 둔 클래스라 static입니다.
    internal static class GameDataFormat
    {
        /// <summary>"NYGD"</summary>
        public const int Magic = 0x4447594E;

        public const int Version = 1;
    }
}
