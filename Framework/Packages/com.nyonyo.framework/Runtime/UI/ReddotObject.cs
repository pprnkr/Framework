using System;
using UnityEngine;

namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// 레드닷 하나입니다. 인스펙터에서 정한 종류가 <see cref="Reddot.Activate{TType}"/>로 켜지면 대상 오브젝트를 켜고, 꺼지면 끕니다.
    /// </summary>
    /// <remarks>
    /// Unity는 제네릭 컴포넌트를 붙일 수 없으므로, 게임에서 레드닷 enum마다 한 줄짜리 클래스를 만들어 씁니다.
    /// <code>
    /// public enum ReddotType { Mail, Quest, Shop }
    /// public class ReddotObject : Nyonyo.Framework.UI.ReddotObject&lt;ReddotType&gt; { }
    /// </code>
    /// 레드닷 모양(이미지, 숫자, 애니메이션)은 게임마다 다르므로, 이 컴포넌트는 대상 오브젝트를 켜고 끄기만 합니다.
    /// Awake에서 등록하므로 이 컴포넌트가 붙은 오브젝트는 처음에 켜져 있어야 합니다. 꺼진 채로 시작하는 오브젝트는 처음 켜질 때 등록됩니다.
    /// 보통 버튼처럼 늘 켜져 있는 오브젝트에 붙이고, 레드닷 이미지를 대상으로 지정합니다.
    /// 컴포넌트를 붙이면 대상은 이 오브젝트 자신으로 채워집니다. 그대로 쓸 때도 프리팹에서는 켜 두세요(Awake에서 상태에 맞춰 꺼집니다).
    /// </remarks>
    public abstract class ReddotObject<TType> : MonoBehaviour where TType : struct, Enum
    {
        [Tooltip("이 레드닷의 종류입니다.")]
        [SerializeField] private TType _type = default;

        [Tooltip("켜고 끌 오브젝트입니다. 레드닷 이미지를 넣으세요.")]
        [SerializeField] private GameObject _target = null;

        /// <summary>이 레드닷의 종류입니다.</summary>
        public TType Type => _type;

        /// <summary>켜고 끌 오브젝트입니다.</summary>
        public GameObject Target => _target;

        private void Reset() => _target = gameObject;

        private void Awake()
        {
            if (_target == null)
                Debug.LogWarning($"[Nyonyo Framework] '{name}' 레드닷({_type})의 Target이 비어 있어 켜고 끌 수 없습니다. 인스펙터에서 레드닷 이미지를 Target에 넣으세요.", this);

            Reddot.Register(this);
        }

        private void OnDestroy() => Reddot.Unregister(this);

        internal void Apply(bool active)
        {
            if (_target != null && _target.activeSelf != active)
                _target.SetActive(active);
        }
    }
}
