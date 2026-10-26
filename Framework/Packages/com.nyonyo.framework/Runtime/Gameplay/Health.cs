using System;
using Nyonyo.Framework.Pooling;
using UnityEngine;

namespace Nyonyo.Framework.Gameplay
{
    /// <summary>
    /// 체력입니다. 피해, 회복, 사망, 피격 후 무적 시간을 처리합니다.
    /// </summary>
    /// <remarks>
    /// 죽어도 오브젝트를 파괴하지 않습니다. <see cref="Died"/>에서 원하는 처리를 하세요(파괴, 풀에 반환, 게임 오버 등).
    /// 풀링된 오브젝트라면 풀에서 꺼낼 때 체력이 자동으로 다시 채워집니다.
    /// </remarks>
    /// <example>
    /// <code>
    /// _health.Damaged += info =&gt; hpBar.fillAmount = _health.Normalized;
    /// _health.Died += () =&gt; PoolManager.Instance.Despawn(gameObject);
    /// </code>
    /// </example>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IPoolable
    {
        [Tooltip("최대 체력입니다.")]
        [Min(1f)]
        [SerializeField] float _max = 100f;

        [Tooltip("피해를 받은 뒤 이 시간(초) 동안 추가 피해를 받지 않습니다.")]
        [Min(0f)]
        [SerializeField] float _invincibleSeconds = 0f;

        float _current;
        double _invincibleUntil = double.MinValue;
        bool _initialized;

        public float Max => _max;

        public float Current
        {
            get
            {
                EnsureInitialized();
                return _current;
            }
        }

        /// <summary>체력 비율(0~1)입니다. 체력 바에 씁니다.</summary>
        public float Normalized => Current / _max;

        public bool IsDead => Current <= 0f;

        public bool IsInvincible => Clock() < _invincibleUntil;

        /// <summary>피해를 받은 뒤 호출됩니다.</summary>
        public event Action<DamageInfo> Damaged;

        /// <summary>회복한 뒤(실제 회복량) 호출됩니다.</summary>
        public event Action<float> Healed;

        /// <summary>체력이 0이 되는 순간 한 번 호출됩니다.</summary>
        public event Action Died;

        /// <summary>체력이 바뀔 때마다(현재, 최대) 호출됩니다.</summary>
        public event Action<float, float> Changed;

        internal Func<double> Clock { get; set; } = () => Time.timeAsDouble;

        /// <summary>
        /// 피해를 줍니다. 죽었거나 무적이거나 피해량이 0 이하면 무시합니다.
        /// </summary>
        /// <returns>실제로 깎인 체력입니다.</returns>
        public float TakeDamage(float amount, GameObject source = null)
        {
            if (amount <= 0f || IsDead || IsInvincible)
                return 0f;

            var applied = Mathf.Min(amount, _current);
            _current -= applied;
            if (_invincibleSeconds > 0f)
                _invincibleUntil = Clock() + _invincibleSeconds;

            Damaged?.Invoke(new DamageInfo(applied, source));
            Changed?.Invoke(_current, _max);
            if (_current <= 0f)
                Died?.Invoke();

            return applied;
        }

        /// <summary>
        /// 체력을 회복합니다. 죽은 상태에서는 회복하지 않습니다(<see cref="ResetHealth"/>를 쓰세요).
        /// </summary>
        /// <returns>실제로 회복한 체력입니다.</returns>
        public float Heal(float amount)
        {
            if (amount <= 0f || IsDead)
                return 0f;

            var applied = Mathf.Min(amount, _max - _current);
            if (applied <= 0f)
                return 0f;

            _current += applied;
            Healed?.Invoke(applied);
            Changed?.Invoke(_current, _max);
            return applied;
        }

        /// <summary>체력을 최대로 채우고 무적 시간을 없앱니다. 부활에도 씁니다.</summary>
        public void ResetHealth()
        {
            _initialized = true;
            _current = _max;
            _invincibleUntil = double.MinValue;
            Changed?.Invoke(_current, _max);
        }

        /// <summary>최대 체력을 바꿉니다. <paramref name="refill"/>이 true면 체력도 가득 채웁니다.</summary>
        public void SetMax(float max, bool refill = false)
        {
            EnsureInitialized();
            _max = Mathf.Max(1f, max);
            _current = refill ? _max : Mathf.Min(_current, _max);
            Changed?.Invoke(_current, _max);
        }

        void IPoolable.OnSpawned() => ResetHealth();

        void IPoolable.OnDespawned()
        {
        }

        void Awake() => EnsureInitialized();

        void EnsureInitialized()
        {
            if (_initialized)
                return;

            _initialized = true;
            _current = _max;
        }
    }
}
