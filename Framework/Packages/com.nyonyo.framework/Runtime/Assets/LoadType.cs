namespace Nyonyo.Framework.Assets
{
    /// <summary>
    /// 에셋을 불러오는 방식입니다.
    /// </summary>
    public enum LoadType
    {
        /// <summary>
        /// Resources 폴더에서 불러옵니다. 경로는 Resources 폴더 기준이고 확장자를 뺍니다(예: Popups/Loading).
        /// 게임 시작에 꼭 필요한 최소한의 에셋에만 씁니다.
        /// </summary>
        Resources,

        /// <summary>
        /// Addressables로 불러옵니다. 경로는 Addressables address입니다(기본값은 에셋 경로, 예: Assets/Popups/PopupTest.prefab).
        /// </summary>
        Addressables,
    }
}
