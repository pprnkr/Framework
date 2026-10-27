namespace Nyonyo.Framework.Ads.AdMob
{
    /// <summary>
    /// 에디터 Play 모드에서 쓸 광고입니다.
    /// </summary>
    public enum AdMobEditorMode
    {
        /// <summary>프레임워크의 <see cref="FakeAdService"/>를 씁니다. 보상 성공과 실패, 광고 없음을 골라 확인할 수 있습니다.</summary>
        Fake,

        /// <summary>AdMob 플러그인의 에디터용 플레이스홀더 광고와 동의 창을 씁니다. 실제와 같은 흐름을 확인할 때 씁니다.</summary>
        AdMobPlaceholder,
    }
}
