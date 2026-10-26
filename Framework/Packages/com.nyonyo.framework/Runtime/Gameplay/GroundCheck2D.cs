using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nyonyo.Framework.Gameplay
{
    /// <summary>
    /// 발밑에 땅이 있는지 확인합니다. 플랫포머 캐릭터에 붙이세요.
    /// </summary>
    /// <remarks>
    /// FixedUpdate마다 발밑 상자 영역에 Ground Layers 콜라이더가 있는지 확인합니다. 자기 자신(자식 포함)의 콜라이더는 무시합니다.
    /// 씬 뷰에서 오브젝트를 선택하면 확인 영역이 초록(땅 위)/빨강(공중)으로 보입니다.
    /// </remarks>
    public sealed class GroundCheck2D : MonoBehaviour
    {
        [Tooltip("확인 영역의 중심(오브젝트 기준)입니다.")]
        [SerializeField] Vector2 _offset = new(0f, -0.5f);

        [Tooltip("확인 영역의 크기입니다.")]
        [SerializeField] Vector2 _size = new(0.8f, 0.1f);

        [Tooltip("땅으로 볼 레이어입니다.")]
        [SerializeField] LayerMask _groundLayers = 1;

        readonly List<Collider2D> _hits = new();
        ContactFilter2D _filter;

        public bool IsGrounded { get; private set; }

        /// <summary>공중에서 땅에 닿은 순간 호출됩니다.</summary>
        public event Action Landed;

        /// <summary>땅에서 떨어진 순간 호출됩니다.</summary>
        public event Action LeftGround;

        void Awake()
        {
            _filter = new ContactFilter2D { useTriggers = false };
            _filter.SetLayerMask(_groundLayers);
        }

        void FixedUpdate() => SetGrounded(Check());

        /// <summary>
        /// 지금 땅에 닿아 있는지 바로 확인합니다. 결과는 <see cref="IsGrounded"/>에 반영하지 않습니다.
        /// </summary>
        public bool Check()
        {
            _filter.SetLayerMask(_groundLayers);
            var count = Physics2D.OverlapBox((Vector2)transform.position + _offset, _size, 0f, _filter, _hits);
            for (var i = 0; i < count; i++)
            {
                if (!_hits[i].transform.IsChildOf(transform))
                    return true;
            }

            return false;
        }

        void SetGrounded(bool grounded)
        {
            if (grounded == IsGrounded)
                return;

            IsGrounded = grounded;
            if (grounded)
                Landed?.Invoke();
            else
                LeftGround?.Invoke();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube((Vector2)transform.position + _offset, _size);
        }
    }
}
