using UnityEngine;

namespace Nyonyo.Framework.Gameplay
{
    /// <summary>
    /// <see cref="Health"/>가 받은 피해 정보입니다.
    /// </summary>
    public readonly struct DamageInfo
    {
        public DamageInfo(float amount, GameObject source)
        {
            Amount = amount;
            Source = source;
        }

        /// <summary>실제로 깎인 체력입니다(남은 체력보다 클 수 없음).</summary>
        public float Amount { get; }

        /// <summary>피해를 준 오브젝트입니다. 없을 수 있습니다.</summary>
        public GameObject Source { get; }
    }
}
