using System;
using Nyonyo.Framework.Pooling;
using UnityEngine;

namespace Nyonyo.Framework.Gameplay
{
    /// <summary>
    /// 닿은 대상의 <see cref="Health"/>에 피해를 줍니다. 총알, 가시, 공격 판정 콜라이더에 붙이세요.
    /// </summary>
    /// <remarks>
    /// Trigger 콜라이더면 OnTriggerEnter2D로, 일반 콜라이더면 OnCollisionEnter2D로 판정합니다(둘 중 하나에 Rigidbody2D 필요).
    /// 대상의 Health는 닿은 콜라이더나 그 부모에서 찾습니다. 자기 자신의 Health에는 피해를 주지 않습니다.
    /// </remarks>
    public sealed class DamageDealer2D : MonoBehaviour
    {
        [Tooltip("한 번에 주는 피해량입니다.")]
        [SerializeField] float _damage = 10f;

        [Tooltip("피해를 줄 레이어입니다.")]
        [SerializeField] LayerMask _targetLayers = ~0;

        [Tooltip("피해를 준 뒤 이 오브젝트를 없앱니다(풀링된 오브젝트면 풀에 반환). 총알에 씁니다.")]
        [SerializeField] bool _removeOnHit = false;

        public float Damage
        {
            get => _damage;
            set => _damage = value;
        }

        /// <summary>피해를 준 뒤(대상) 호출됩니다.</summary>
        public event Action<Health> Hit;

        void OnTriggerEnter2D(Collider2D other) => TryHit(other.gameObject);

        void OnCollisionEnter2D(Collision2D collision) => TryHit(collision.gameObject);

        /// <summary>
        /// <paramref name="target"/>에 피해를 줍니다. 레이어가 맞지 않거나 Health가 없거나 피해가 무시되면 false입니다.
        /// </summary>
        public bool TryHit(GameObject target)
        {
            if (target == null || (_targetLayers.value & (1 << target.layer)) == 0)
                return false;

            var health = target.GetComponentInParent<Health>();
            if (health == null || health.transform.IsChildOf(transform) || transform.IsChildOf(health.transform))
                return false;

            if (health.TakeDamage(_damage, gameObject) <= 0f)
                return false;

            Hit?.Invoke(health);
            if (_removeOnHit)
                Remove();
            return true;
        }

        void Remove()
        {
            if (Services.TryGet(out PoolManager pools))
                pools.Despawn(gameObject);
            else if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }
    }
}
