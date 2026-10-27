namespace Nyonyo.Framework.Tweening
{
    /// <summary>
    /// <see cref="TweenExtensions.SetLoops{T}"/>의 반복 방식입니다.
    /// </summary>
    public enum LoopType
    {
        /// <summary>매번 처음 값으로 돌아가서 다시 재생합니다.</summary>
        Restart,

        /// <summary>앞으로 갔다가 뒤로 돌아옵니다. 한 번 왕복이 반복 2회입니다.</summary>
        Yoyo,

        /// <summary>끝난 자리에서 같은 변화량만큼 이어서 재생합니다. 이동, 값 트윈에만 의미가 있습니다.</summary>
        Incremental,
    }
}
