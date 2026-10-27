using System;
using UnityEngine;

namespace Nyonyo.Framework.Tweening
{
    /// <summary>
    /// Transform 단축 트윈입니다(DOTween과 같은 이름). 대상은 Transform이므로 <c>transform.DOKill()</c>로 한꺼번에 멈출 수 있습니다.
    /// </summary>
    /// <remarks>
    /// X/Y/Z 축 함수는 그 축만 바꾸므로 다른 축을 바꾸는 트윈과 함께 쓸 수 있습니다.
    /// 카메라는 <c>CameraFollow2D</c>, <c>CameraShake</c>가 위치를 덮어쓰므로 카메라 흔들기에는 <c>CameraShake</c>를 쓰세요.
    /// </remarks>
    public static class TransformTweenExtensions
    {
        #region 이동

        public static Tweener<Vector3> DOMove(this Transform transform, Vector3 endValue, float duration) =>
            Tween.Create(transform, () => transform.position, v => transform.position = v, endValue, duration);

        public static Tweener<float> DOMoveX(this Transform transform, float endValue, float duration) =>
            Tween.Create(transform, () => transform.position.x, x =>
            {
                var position = transform.position;
                position.x = x;
                transform.position = position;
            }, endValue, duration);

        public static Tweener<float> DOMoveY(this Transform transform, float endValue, float duration) =>
            Tween.Create(transform, () => transform.position.y, y =>
            {
                var position = transform.position;
                position.y = y;
                transform.position = position;
            }, endValue, duration);

        public static Tweener<float> DOMoveZ(this Transform transform, float endValue, float duration) =>
            Tween.Create(transform, () => transform.position.z, z =>
            {
                var position = transform.position;
                position.z = z;
                transform.position = position;
            }, endValue, duration);

        public static Tweener<Vector3> DOLocalMove(this Transform transform, Vector3 endValue, float duration) =>
            Tween.Create(transform, () => transform.localPosition, v => transform.localPosition = v, endValue, duration);

        public static Tweener<float> DOLocalMoveX(this Transform transform, float endValue, float duration) =>
            Tween.Create(transform, () => transform.localPosition.x, x =>
            {
                var position = transform.localPosition;
                position.x = x;
                transform.localPosition = position;
            }, endValue, duration);

        public static Tweener<float> DOLocalMoveY(this Transform transform, float endValue, float duration) =>
            Tween.Create(transform, () => transform.localPosition.y, y =>
            {
                var position = transform.localPosition;
                position.y = y;
                transform.localPosition = position;
            }, endValue, duration);

        public static Tweener<float> DOLocalMoveZ(this Transform transform, float endValue, float duration) =>
            Tween.Create(transform, () => transform.localPosition.z, z =>
            {
                var position = transform.localPosition;
                position.z = z;
                transform.localPosition = position;
            }, endValue, duration);

        /// <summary>
        /// 월드 위치 <paramref name="endValue"/>로 이동하면서 Y축으로 <paramref name="numJumps"/>번 뜁니다. 기본 곡선은 Linear입니다.
        /// </summary>
        /// <param name="jumpPower">점프 높이(월드 단위)입니다.</param>
        public static Tweener<Vector3> DOJump(this Transform transform, Vector3 endValue, float jumpPower, int numJumps, float duration) =>
            TweenShapes.Jump(transform, () => transform.position, v => transform.position = v, endValue, jumpPower, numJumps, duration);

        public static Tweener<Vector3> DOLocalJump(this Transform transform, Vector3 endValue, float jumpPower, int numJumps, float duration) =>
            TweenShapes.Jump(transform, () => transform.localPosition, v => transform.localPosition = v, endValue, jumpPower, numJumps, duration);

        #endregion

        #region 회전

        /// <summary>월드 회전을 오일러 각 <paramref name="endValue"/>로 바꿉니다. 2D에서는 <c>new Vector3(0, 0, 각도)</c>로 씁니다.</summary>
        /// <param name="mode">한 바퀴 이상 돌리려면 <see cref="RotateMode.FastBeyond360"/>을 쓰세요.</param>
        public static Tweener<Vector3> DORotate(this Transform transform, Vector3 endValue, float duration, RotateMode mode = RotateMode.Fast) =>
            CreateRotation(transform, () => transform.eulerAngles, v => transform.eulerAngles = v, endValue, duration, mode);

        public static Tweener<Vector3> DOLocalRotate(this Transform transform, Vector3 endValue, float duration, RotateMode mode = RotateMode.Fast) =>
            CreateRotation(transform, () => transform.localEulerAngles, v => transform.localEulerAngles = v, endValue, duration, mode);

        #endregion

        #region 크기

        public static Tweener<Vector3> DOScale(this Transform transform, Vector3 endValue, float duration) =>
            Tween.Create(transform, () => transform.localScale, v => transform.localScale = v, endValue, duration);

        /// <summary>세 축을 모두 <paramref name="endValue"/>로 바꿉니다.</summary>
        public static Tweener<Vector3> DOScale(this Transform transform, float endValue, float duration) =>
            transform.DOScale(Vector3.one * endValue, duration);

        public static Tweener<float> DOScaleX(this Transform transform, float endValue, float duration) =>
            Tween.Create(transform, () => transform.localScale.x, x =>
            {
                var scale = transform.localScale;
                scale.x = x;
                transform.localScale = scale;
            }, endValue, duration);

        public static Tweener<float> DOScaleY(this Transform transform, float endValue, float duration) =>
            Tween.Create(transform, () => transform.localScale.y, y =>
            {
                var scale = transform.localScale;
                scale.y = y;
                transform.localScale = scale;
            }, endValue, duration);

        public static Tweener<float> DOScaleZ(this Transform transform, float endValue, float duration) =>
            Tween.Create(transform, () => transform.localScale.z, z =>
            {
                var scale = transform.localScale;
                scale.z = z;
                transform.localScale = scale;
            }, endValue, duration);

        #endregion

        #region Punch

        /// <summary>
        /// 로컬 위치를 <paramref name="punch"/> 방향으로 튕겼다가 진동하며 원래 자리로 돌아옵니다.
        /// </summary>
        /// <param name="vibrato">진동 횟수입니다.</param>
        /// <param name="elasticity">0~1. 반대 방향으로 얼마나 넘어가는지입니다. 0이면 원래 자리까지만 돌아옵니다.</param>
        public static Tweener<Vector3> DOPunchPosition(this Transform transform, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f) =>
            TweenShapes.Punch(transform, () => transform.localPosition, v => transform.localPosition = v, punch, duration, vibrato, elasticity);

        /// <example><c>button.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);</c></example>
        public static Tweener<Vector3> DOPunchScale(this Transform transform, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f) =>
            TweenShapes.Punch(transform, () => transform.localScale, v => transform.localScale = v, punch, duration, vibrato, elasticity);

        /// <param name="punch">오일러 각입니다. 2D에서는 <c>new Vector3(0, 0, 각도)</c>로 씁니다.</param>
        public static Tweener<Vector3> DOPunchRotation(this Transform transform, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f) =>
            TweenShapes.Punch(transform, () => transform.localEulerAngles, v => transform.localEulerAngles = v, punch, duration, vibrato, elasticity);

        #endregion

        #region Shake

        /// <summary>
        /// 로컬 위치를 무작위로 흔들다가 원래 자리로 돌아옵니다. float 세기는 X, Y축만 흔듭니다(2D 기준).
        /// </summary>
        /// <param name="vibrato">초당 흔들림 횟수입니다.</param>
        /// <param name="randomness">0~180. 흔들림마다 방향이 바뀌는 정도(도)입니다. 0이면 한 축으로 왕복합니다.</param>
        /// <param name="fadeOut">true면 끝으로 갈수록 약해집니다.</param>
        public static Tweener<Vector3> DOShakePosition(this Transform transform, float duration, float strength = 1f, int vibrato = 10,
            float randomness = 90f, bool fadeOut = true) =>
            transform.DOShakePosition(duration, new Vector3(strength, strength, 0f), vibrato, randomness, fadeOut);

        public static Tweener<Vector3> DOShakePosition(this Transform transform, float duration, Vector3 strength, int vibrato = 10,
            float randomness = 90f, bool fadeOut = true) =>
            TweenShapes.Shake(transform, () => transform.localPosition, v => transform.localPosition = v,
                strength, duration, vibrato, randomness, fadeOut);

        /// <summary>로컬 회전을 흔듭니다. float 세기(도)는 Z축만 흔듭니다(2D 기준).</summary>
        public static Tweener<Vector3> DOShakeRotation(this Transform transform, float duration, float strength = 90f, int vibrato = 10,
            float randomness = 90f, bool fadeOut = true) =>
            transform.DOShakeRotation(duration, new Vector3(0f, 0f, strength), vibrato, randomness, fadeOut);

        public static Tweener<Vector3> DOShakeRotation(this Transform transform, float duration, Vector3 strength, int vibrato = 10,
            float randomness = 90f, bool fadeOut = true) =>
            TweenShapes.Shake(transform, () => transform.localEulerAngles, v => transform.localEulerAngles = v,
                strength, duration, vibrato, randomness, fadeOut);

        /// <summary>크기를 흔듭니다. float 세기는 X, Y축만 흔듭니다(2D 기준).</summary>
        public static Tweener<Vector3> DOShakeScale(this Transform transform, float duration, float strength = 1f, int vibrato = 10,
            float randomness = 90f, bool fadeOut = true) =>
            transform.DOShakeScale(duration, new Vector3(strength, strength, 0f), vibrato, randomness, fadeOut);

        public static Tweener<Vector3> DOShakeScale(this Transform transform, float duration, Vector3 strength, int vibrato = 10,
            float randomness = 90f, bool fadeOut = true) =>
            TweenShapes.Shake(transform, () => transform.localScale, v => transform.localScale = v,
                strength, duration, vibrato, randomness, fadeOut);

        #endregion

        // Fast는 쿼터니언으로 가장 짧은 경로를 따라가고, FastBeyond360은 오일러 각을 그대로 보간합니다.
        private static Tweener<Vector3> CreateRotation(Transform transform, Func<Vector3> getter, Action<Vector3> setter,
            Vector3 endValue, float duration, RotateMode mode)
        {
            if (mode == RotateMode.FastBeyond360)
                return Tween.Create(transform, getter, setter, endValue, duration);

            return new Tweener<Vector3>(transform, getter, setter, endValue, duration,
                (start, end, t) => Quaternion.SlerpUnclamped(Quaternion.Euler(start), Quaternion.Euler(end), t).eulerAngles,
                (a, b) => a + b);
        }
    }
}
