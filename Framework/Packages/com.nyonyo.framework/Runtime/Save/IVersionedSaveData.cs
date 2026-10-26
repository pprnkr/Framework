namespace Nyonyo.Framework.Save
{
    /// <summary>
    /// 저장 데이터 구조가 바뀔 때 예전 데이터를 새 구조로 고칠 수 있게 합니다.
    /// 불러온 데이터의 <see cref="Version"/>이 <see cref="LatestVersion"/>보다 낮으면,
    /// <see cref="Migrate"/>를 호출한 뒤 최신 버전으로 다시 저장합니다.
    /// </summary>
    /// <example>
    /// <code>
    /// [Serializable]
    /// public class PlayerData : IVersionedSaveData
    /// {
    ///     public int version;          // 저장되는 버전 값
    ///     public int gold;
    ///     public string nickname = ""; // 2버전에서 추가
    ///
    ///     public int Version { get => version; set => version = value; }
    ///     public int LatestVersion => 2;
    ///
    ///     public void Migrate(int fromVersion)
    ///     {
    ///         if (fromVersion &lt; 2)
    ///             nickname = "Player";
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IVersionedSaveData
    {
        /// <summary>
        /// 저장된 데이터의 버전입니다. JsonUtility가 저장할 수 있도록 public 필드나 [SerializeField] 필드에 담으세요.
        /// </summary>
        int Version { get; set; }

        /// <summary>
        /// 지금 코드의 데이터 버전입니다. 구조를 바꿀 때마다 1씩 올리세요.
        /// </summary>
        int LatestVersion { get; }

        /// <summary>
        /// <paramref name="fromVersion"/> 데이터를 최신 구조로 고칩니다. 새 필드는 이미 필드 초기값으로 채워져 있습니다.
        /// </summary>
        void Migrate(int fromVersion);
    }
}
