using UnityEngine;

namespace Nyonyo.Framework.Cameras
{
    /// <summary>
    /// 2D 직교 카메라가 대상을 부드럽게 따라가게 합니다. 카메라 오브젝트에 붙이세요.
    /// </summary>
    /// <remarks>
    /// 데드존 안에서는 카메라가 움직이지 않고, 영역(Bounds)을 정하면 그 밖을 비추지 않습니다.
    /// <see cref="CameraShake"/>와 함께 쓸 수 있습니다(흔들림은 따라가기 뒤에 더해집니다).
    /// 픽셀 아트라면 URP의 Pixel Perfect Camera 컴포넌트를 함께 쓰세요.
    /// </remarks>
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [Tooltip("따라갈 대상입니다.")]
        [SerializeField] private Transform _target;

        [Tooltip("대상 위치에서 카메라를 얼마나 떨어뜨릴지입니다.")]
        [SerializeField] private Vector2 _offset = Vector2.zero;

        [Tooltip("목표 위치까지 걸리는 대략적인 시간(초)입니다. 0이면 바로 붙어 다닙니다.")]
        [Min(0f)]
        [SerializeField] private float _smoothTime = 0.15f;

        [Tooltip("대상이 이 크기(카메라 중심 기준) 안에서 움직이면 카메라가 따라가지 않습니다.")]
        [SerializeField] private Vector2 _deadZone = Vector2.zero;

        [Tooltip("카메라가 비출 수 있는 영역입니다. 비워 두면 제한하지 않습니다.")]
        [SerializeField] private Collider2D _boundsCollider;

        [Tooltip("true면 일시정지(timeScale 0) 중에도 따라갑니다.")]
        [SerializeField] private bool _unscaledTime = false;

        private Camera _camera;
        private Vector2 _position;
        private Vector2 _velocity;
        private Rect? _bounds;
        private bool _hasPosition;

        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        private void Awake() => _camera = GetComponent<Camera>();

        /// <summary>
        /// 카메라가 비출 수 있는 영역을 정합니다. 인스펙터의 Bounds Collider보다 우선합니다.
        /// </summary>
        public void SetBounds(Rect bounds) => _bounds = bounds;

        public void ClearBounds()
        {
            _bounds = null;
            _boundsCollider = null;
        }

        /// <summary>
        /// 부드럽게 따라가지 않고 대상 위치로 바로 옮깁니다. 씬을 시작하거나 순간이동한 뒤에 씁니다.
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null)
                return;

            _position = Clamp((Vector2)_target.position + _offset);
            _velocity = Vector2.zero;
            _hasPosition = true;
            Apply();
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            if (!_hasPosition)
            {
                SnapToTarget();
                return;
            }

            var desired = CameraMath2D.ApplyDeadZone(_position, (Vector2)_target.position + _offset, _deadZone);
            var deltaTime = _unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _position = _smoothTime > 0f
                ? Vector2.SmoothDamp(_position, desired, ref _velocity, _smoothTime, Mathf.Infinity, deltaTime)
                : desired;
            _position = Clamp(_position);
            Apply();
        }

        private Vector2 Clamp(Vector2 position)
        {
            var bounds = _bounds ?? (_boundsCollider != null ? ToRect(_boundsCollider.bounds) : (Rect?)null);
            return bounds.HasValue && _camera != null && _camera.orthographic
                ? CameraMath2D.ClampToBounds(position, CameraMath2D.HalfExtents(_camera), bounds.Value)
                : position;
        }

        // 따라간 위치는 따로 기억해 두고 매번 덮어써서, 흔들림 오프셋이 쌓이지 않게 합니다.
        private void Apply() => transform.position = new Vector3(_position.x, _position.y, transform.position.z);

        private Rect ToRect(Bounds bounds) => new(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
    }
}
