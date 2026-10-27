namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// 게임 화면 방향입니다. <see cref="UISettings"/>에서 정하며, 기준 해상도의 가로·세로 순서를 결정합니다.
    /// </summary>
    public enum GameOrientation
    {
        /// <summary>세로형 게임입니다. 기준 해상도의 짧은 변이 가로가 됩니다(예: 1080x1920).</summary>
        Portrait = 0,

        /// <summary>가로형 게임입니다. 기준 해상도의 긴 변이 가로가 됩니다(예: 1920x1080).</summary>
        Landscape = 1,
    }
}
