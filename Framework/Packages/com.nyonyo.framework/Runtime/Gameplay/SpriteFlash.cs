using System;
using UnityEngine;

namespace Nyonyo.Framework.Gameplay
{
    /// <summary>
    /// 피격 같은 순간에 스프라이트 색을 잠깐 바꿉니다. 같은 오브젝트에 <see cref="Health"/>가 있으면 피해를 받을 때 자동으로 깜빡입니다.
    /// </summary>
    /// <remarks>
    /// SpriteRenderer의 color에 곱해지는 색이라 흰색으로 만들 수는 없습니다. 흰색 번쩍임이 필요하면 셰이더를 쓰세요.
    /// 일시정지 중에도 원래 색으로 돌아오도록 실시간으로 잽니다.
    /// </remarks>
    public sealed class SpriteFlash : MonoBehaviour
    {
        [SerializeField] private Color _color = new(1f, 0.35f, 0.35f, 1f);

        [Min(0f)]
        [SerializeField] private float _duration = 0.1f;

        [Tooltip("같은 오브젝트의 Health가 피해를 받으면 자동으로 깜빡입니다.")]
        [SerializeField] private bool _flashOnDamage = true;

        private SpriteRenderer[] _renderers = Array.Empty<SpriteRenderer>();
        private Color[] _originalColors = Array.Empty<Color>();
        private Health _health;
        private int _flashId;
        private bool _flashing;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _originalColors = new Color[_renderers.Length];
            if (_flashOnDamage && TryGetComponent(out _health))
                _health.Damaged += OnDamaged;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.Damaged -= OnDamaged;
        }

        // 풀에 돌아가거나 꺼질 때 색이 바뀐 채로 남지 않게 합니다.
        private void OnDisable() => Restore();

        public void Flash() => Flash(_color, _duration);

        public void Flash(Color color, float duration) => FlashAsync(color, duration).Forget();

        private async Awaitable FlashAsync(Color color, float duration)
        {
            if (!_flashing)
            {
                for (var i = 0; i < _renderers.Length; i++)
                    _originalColors[i] = _renderers[i].color;
                _flashing = true;
            }

            var id = ++_flashId;
            foreach (var spriteRenderer in _renderers)
                spriteRenderer.color = color;

            var until = Time.realtimeSinceStartupAsDouble + duration;
            try
            {
                while (Time.realtimeSinceStartupAsDouble < until)
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (id == _flashId)
                Restore();
        }

        private void Restore()
        {
            if (!_flashing)
                return;

            _flashing = false;
            for (var i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].color = _originalColors[i];
            }
        }

        private void OnDamaged(DamageInfo info) => Flash();
    }
}
