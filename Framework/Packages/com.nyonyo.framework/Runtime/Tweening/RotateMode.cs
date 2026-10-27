namespace Nyonyo.Framework.Tweening
{
    /// <summary>
    /// <see cref="TransformTweenExtensions.DORotate"/>의 회전 방식입니다.
    /// </summary>
    public enum RotateMode
    {
        /// <summary>가장 짧은 경로로 회전합니다. 360도를 넘는 값은 같은 방향으로 취급합니다.</summary>
        Fast,

        /// <summary>오일러 각을 그대로 바꿉니다. 360을 넘기면 그만큼 더 돕니다(한 바퀴 돌리기 등).</summary>
        FastBeyond360,
    }
}
