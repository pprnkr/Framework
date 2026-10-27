using UnityEngine;

namespace Nyonyo.Framework.Tweening
{
    /// <summary>
    /// SpriteRenderer, Camera, AudioSource, Material, Rigidbody2D 단축 트윈과 대상별 Kill입니다(DOTween과 같은 이름).
    /// </summary>
    public static class ComponentTweenExtensions
    {
        #region Kill

        /// <summary>
        /// 이 컴포넌트를 대상으로 하는 트윈을 모두 Kill합니다. <c>transform.DOKill()</c>은 Transform 트윈만, <c>image.DOKill()</c>은 Image 트윈만 멈춥니다.
        /// </summary>
        /// <returns>Kill한 트윈 수입니다.</returns>
        public static int DOKill(this Component target, bool complete = false) => Tween.KillTarget(target, complete);

        /// <summary>이 컴포넌트를 대상으로 하는 트윈을 모두 끝 값으로 보내고 끝냅니다.</summary>
        public static int DOComplete(this Component target) => Tween.KillTarget(target, true);

        public static int DOKill(this Material target, bool complete = false) => Tween.KillTarget(target, complete);

        #endregion

        #region SpriteRenderer

        public static Tweener<float> DOFade(this SpriteRenderer renderer, float endValue, float duration) =>
            Tween.Create(renderer, () => renderer.color.a, a =>
            {
                var color = renderer.color;
                color.a = a;
                renderer.color = color;
            }, endValue, duration);

        public static Tweener<Color> DOColor(this SpriteRenderer renderer, Color endValue, float duration) =>
            Tween.Create(renderer, () => renderer.color, c => renderer.color = c, endValue, duration);

        #endregion

        #region Camera

        /// <summary>직교 카메라의 orthographicSize를 바꿉니다. 2D 줌 연출에 씁니다.</summary>
        public static Tweener<float> DOOrthoSize(this Camera camera, float endValue, float duration) =>
            Tween.Create(camera, () => camera.orthographicSize, v => camera.orthographicSize = v, endValue, duration);

        /// <summary>배경색(backgroundColor)을 바꿉니다.</summary>
        public static Tweener<Color> DOColor(this Camera camera, Color endValue, float duration) =>
            Tween.Create(camera, () => camera.backgroundColor, c => camera.backgroundColor = c, endValue, duration);

        #endregion

        #region AudioSource

        /// <summary>
        /// volume을 바꿉니다. SoundManager가 관리하는 AudioSource에는 쓰지 마세요(SoundManager 볼륨을 쓰세요).
        /// </summary>
        public static Tweener<float> DOFade(this AudioSource source, float endValue, float duration) =>
            Tween.Create(source, () => source.volume, v => source.volume = v, Mathf.Clamp01(endValue), duration);

        public static Tweener<float> DOPitch(this AudioSource source, float endValue, float duration) =>
            Tween.Create(source, () => source.pitch, v => source.pitch = v, endValue, duration);

        #endregion

        #region Material

        /// <param name="property">색 속성 이름입니다. URP Lit은 <c>_BaseColor</c>, Sprite 셰이더는 <c>_Color</c>입니다.</param>
        public static Tweener<Color> DOColor(this Material material, Color endValue, string property, float duration) =>
            DOColor(material, endValue, Shader.PropertyToID(property), duration);

        public static Tweener<Color> DOColor(this Material material, Color endValue, int propertyId, float duration) =>
            Tween.Create(material, () => material.GetColor(propertyId), c => material.SetColor(propertyId, c), endValue, duration);

        public static Tweener<float> DOFade(this Material material, float endValue, string property, float duration)
        {
            var id = Shader.PropertyToID(property);
            return Tween.Create(material, () => material.GetColor(id).a, a =>
            {
                var color = material.GetColor(id);
                color.a = a;
                material.SetColor(id, color);
            }, endValue, duration);
        }

        /// <summary>셰이더의 float 속성을 바꿉니다(디졸브, 외곽선 두께 등).</summary>
        public static Tweener<float> DOFloat(this Material material, float endValue, string property, float duration)
        {
            var id = Shader.PropertyToID(property);
            return Tween.Create(material, () => material.GetFloat(id), v => material.SetFloat(id, v), endValue, duration);
        }

        #endregion

        #region Rigidbody2D

        /// <summary>
        /// MovePosition으로 이동합니다. 물리와 함께 움직이는 키네마틱 바디에 씁니다.
        /// </summary>
        public static Tweener<Vector2> DOMove(this Rigidbody2D body, Vector2 endValue, float duration) =>
            Tween.Create(body, () => body.position, body.MovePosition, endValue, duration);

        public static Tweener<float> DOMoveX(this Rigidbody2D body, float endValue, float duration) =>
            Tween.Create(body, () => body.position.x, x => body.MovePosition(new Vector2(x, body.position.y)), endValue, duration);

        public static Tweener<float> DOMoveY(this Rigidbody2D body, float endValue, float duration) =>
            Tween.Create(body, () => body.position.y, y => body.MovePosition(new Vector2(body.position.x, y)), endValue, duration);

        /// <summary>MoveRotation으로 Z 회전(도)을 바꿉니다.</summary>
        public static Tweener<float> DORotate(this Rigidbody2D body, float endValue, float duration) =>
            Tween.Create(body, () => body.rotation, body.MoveRotation, endValue, duration);

        public static Tweener<Vector2> DOJump(this Rigidbody2D body, Vector2 endValue, float jumpPower, int numJumps, float duration) =>
            TweenShapes.Jump(body, () => body.position, body.MovePosition, endValue, jumpPower, numJumps, duration);

        #endregion
    }
}
