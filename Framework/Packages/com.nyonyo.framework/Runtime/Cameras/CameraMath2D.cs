using UnityEngine;

namespace Nyonyo.Framework.Cameras
{
    /// <summary>
    /// 2D 카메라 위치 계산입니다. <see cref="CameraFollow2D"/>가 쓰며, 직접 카메라를 움직일 때도 쓸 수 있습니다.
    /// </summary>
    public static class CameraMath2D
    {
        /// <summary>
        /// 직교 카메라가 화면에 보여 주는 크기의 절반(가로, 세로)입니다.
        /// </summary>
        public static Vector2 HalfExtents(Camera camera) =>
            new(camera.orthographicSize * camera.aspect, camera.orthographicSize);

        /// <summary>
        /// 화면이 <paramref name="bounds"/> 밖을 보여 주지 않도록 카메라 중심을 제한합니다.
        /// 영역이 화면보다 작으면 영역 가운데에 맞춥니다.
        /// </summary>
        public static Vector2 ClampToBounds(Vector2 center, Vector2 halfExtents, Rect bounds)
        {
            return new Vector2(
                ClampAxis(center.x, halfExtents.x, bounds.xMin, bounds.xMax),
                ClampAxis(center.y, halfExtents.y, bounds.yMin, bounds.yMax));
        }

        /// <summary>
        /// 대상이 <paramref name="deadZone"/>(카메라 중심 기준 가로세로 크기) 안에서 움직일 때는 카메라를 그대로 두고,
        /// 밖으로 나간 만큼만 따라가도록 새 카메라 중심을 계산합니다.
        /// </summary>
        public static Vector2 ApplyDeadZone(Vector2 cameraCenter, Vector2 target, Vector2 deadZone)
        {
            var half = deadZone * 0.5f;
            var delta = target - cameraCenter;
            return new Vector2(
                cameraCenter.x + Mathf.Sign(delta.x) * Mathf.Max(0f, Mathf.Abs(delta.x) - half.x),
                cameraCenter.y + Mathf.Sign(delta.y) * Mathf.Max(0f, Mathf.Abs(delta.y) - half.y));
        }

        private static float ClampAxis(float value, float halfExtent, float min, float max)
        {
            if (max - min <= halfExtent * 2f)
                return (min + max) * 0.5f;

            return Mathf.Clamp(value, min + halfExtent, max - halfExtent);
        }
    }
}
