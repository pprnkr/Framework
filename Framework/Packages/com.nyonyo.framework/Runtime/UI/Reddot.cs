using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nyonyo.Framework.UI
{
    /// <summary>
    /// 레드닷을 켜고 끕니다. 레드닷 종류는 게임에서 enum으로 정합니다.
    /// </summary>
    /// <remarks>
    /// 프리팹에 붙인 <see cref="ReddotObject{TType}"/>가 Awake에서 자신을 등록하고, 지금 상태대로 대상 오브젝트를 켜거나 끕니다.
    /// 상태는 오브젝트와 따로 기억하므로, 레드닷 오브젝트가 아직 없을 때(팝업을 열기 전) 켜 두어도 나중에 생긴 오브젝트에 그대로 적용됩니다.
    /// enum 종류마다 상태를 따로 관리하므로 여러 enum을 함께 써도 됩니다.
    /// </remarks>
    /// <example>
    /// <code>
    /// Reddot.Activate(ReddotType.Mail, true);
    /// if (Reddot.IsActive(ReddotType.Mail)) { ... }
    /// Reddot.DeactivateAll&lt;ReddotType&gt;();
    /// </code>
    /// </example>
    // 씬 오브젝트가 서비스 등록 순서와 상관없이 Awake에서 자신을 등록하고, 어디서든 Reddot.Activate로 켜고 끌 수 있게 하려는
    // 사용자 요구(2026-11-01)로 static입니다. Domain Reload를 꺼도 안전하도록 SubsystemRegistration에서 초기화합니다.
    public static class Reddot
    {
        private static readonly Dictionary<Type, object> s_groups = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Clear();

        /// <summary>레드닷을 켜거나 끕니다. 등록된 모든 <paramref name="type"/> 레드닷 오브젝트에 바로 적용됩니다.</summary>
        public static void Activate<TType>(TType type, bool active) where TType : struct, Enum
        {
            GetGroup<TType>().Activate(type, active);
        }

        /// <summary>레드닷이 켜져 있는지 확인합니다.</summary>
        public static bool IsActive<TType>(TType type) where TType : struct, Enum
        {
            return GetGroup<TType>().IsActive(type);
        }

        /// <summary><typeparamref name="TType"/>의 모든 레드닷을 끕니다. 로그아웃이나 계정 전환 때 씁니다.</summary>
        public static void DeactivateAll<TType>() where TType : struct, Enum
        {
            GetGroup<TType>().DeactivateAll();
        }

        internal static void Register<TType>(ReddotObject<TType> reddot) where TType : struct, Enum
        {
            GetGroup<TType>().Register(reddot);
        }

        internal static void Unregister<TType>(ReddotObject<TType> reddot) where TType : struct, Enum
        {
            GetGroup<TType>().Unregister(reddot);
        }

        internal static void Clear() => s_groups.Clear();

        private static Group<TType> GetGroup<TType>() where TType : struct, Enum
        {
            if (s_groups.TryGetValue(typeof(TType), out var group))
                return (Group<TType>)group;

            var created = new Group<TType>();
            s_groups.Add(typeof(TType), created);
            return created;
        }

        private sealed class Group<TType> where TType : struct, Enum
        {
            private readonly HashSet<TType> _activeTypes = new();
            private readonly Dictionary<TType, List<ReddotObject<TType>>> _reddots = new();

            public bool IsActive(TType type) => _activeTypes.Contains(type);

            public void Activate(TType type, bool active)
            {
                if (active)
                    _activeTypes.Add(type);
                else
                    _activeTypes.Remove(type);

                Apply(type);
            }

            public void DeactivateAll()
            {
                var types = new List<TType>(_activeTypes);
                _activeTypes.Clear();
                foreach (var type in types)
                    Apply(type);
            }

            public void Register(ReddotObject<TType> reddot)
            {
                if (!_reddots.TryGetValue(reddot.Type, out var list))
                {
                    list = new List<ReddotObject<TType>>();
                    _reddots.Add(reddot.Type, list);
                }

                if (!list.Contains(reddot))
                    list.Add(reddot);

                reddot.Apply(IsActive(reddot.Type));
            }

            public void Unregister(ReddotObject<TType> reddot)
            {
                if (_reddots.TryGetValue(reddot.Type, out var list))
                    list.Remove(reddot);
            }

            private void Apply(TType type)
            {
                if (!_reddots.TryGetValue(type, out var list))
                    return;

                list.RemoveAll(reddot => reddot == null);

                // 대상을 켜는 순간 그 아래 레드닷이 Awake에서 등록될 수 있어서 복사본을 돕니다.
                var active = IsActive(type);
                foreach (var reddot in list.ToArray())
                {
                    if (reddot != null)
                        reddot.Apply(active);
                }
            }
        }
    }
}
