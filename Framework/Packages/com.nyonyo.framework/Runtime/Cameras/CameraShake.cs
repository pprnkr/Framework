using UnityEngine;

namespace Nyonyo.Framework.Cameras
{
    /// <summary>
    /// 카메라를 흔듭니다. 카메라 오브젝트에 붙이고 <see cref="AddTrauma"/>로 흔들림 세기를 더하세요.
    /// </summary>
    /// <remarks>
    /// 세기(trauma, 0~1)는 시간이 지나면 줄어들고, 실제 흔들림은 세기의 제곱에 비례해서 약한 충격은 은은하게 보입니다.
    /// <see cref="CameraFollow2D"/>나 다른 스크립트가 카메라를 움직여도 흔들림이 위치에 쌓이지 않습니다.
    /// </remarks>
    /// <example>
    /// <code>
    /// CameraShake.ShakeMain(0.4f);           // 메인 카메라 흔들기
    /// GetComponent&lt;CameraShake&gt;().AddTrauma(1f); // 큰 폭발
    /// </code>
    /// </example>
    [DefaultExecutionOrder(100)]
    public sealed class CameraShake : MonoBehaviour
    {
        [Tooltip("세기가 1일 때 최대 흔들림 거리(월드 단위)입니다.")]
        [SerializeField] Vector2 _maxOffset = new(0.4f, 0.4f);

        [Tooltip("흔들림 속도입니다.")]
        [SerializeField] float _frequency = 25f;

        [Tooltip("초당 줄어드는 세기입니다.")]
        [SerializeField] float _decayPerSecond = 1.5f;

        [Tooltip("true면 일시정지(timeScale 0) 중에도 흔들림이 줄어듭니다.")]
        [SerializeField] bool _unscaledTime = true;

        float _trauma;
        float _seed;
        Vector3 _basePosition;
        Vector3 _lastWritten;
        bool _hasLast;

        /// <summary>현재 흔들림 세기(0~1)입니다.</summary>
        public float Trauma => _trauma;

        /// <summary>
        /// 흔들림 세기를 더합니다. 최대 1입니다.
        /// </summary>
        public void AddTrauma(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

        /// <summary>
        /// 메인 카메라에 붙은 <see cref="CameraShake"/>를 흔듭니다. 없으면 아무 일도 하지 않습니다.
        /// </summary>
        public static void ShakeMain(float trauma)
        {
            var main = Camera.main;
            if (main != null && main.TryGetComponent(out CameraShake shake))
                shake.AddTrauma(trauma);
        }

        void Awake() => _seed = Random.Range(0f, 100f);

        void OnDisable()
        {
            if (_hasLast && transform.localPosition == _lastWritten)
                transform.localPosition = _basePosition;
            _hasLast = false;
            _trauma = 0f;
        }

        void LateUpdate()
        {
            // 지난 프레임에 쓴 위치가 그대로면 흔들림을 걷어낸 원래 위치를, 다른 스크립트가 옮겼으면 그 위치를 기준으로 삼습니다.
            var current = transform.localPosition;
            _basePosition = _hasLast && current == _lastWritten ? _basePosition : current;

            var deltaTime = _unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _trauma = Mathf.Max(0f, _trauma - _decayPerSecond * deltaTime);

            var offset = Offset(_unscaledTime ? Time.unscaledTime : Time.time);
            _lastWritten = _basePosition + (Vector3)offset;
            _hasLast = true;
            transform.localPosition = _lastWritten;
        }

        Vector2 Offset(float time)
        {
            var shake = _trauma * _trauma;
            if (shake <= 0f)
                return Vector2.zero;

            var t = time * _frequency;
            return new Vector2(
                _maxOffset.x * shake * (Mathf.PerlinNoise(_seed, t) * 2f - 1f),
                _maxOffset.y * shake * (Mathf.PerlinNoise(_seed + 50f, t) * 2f - 1f));
        }
    }
}
