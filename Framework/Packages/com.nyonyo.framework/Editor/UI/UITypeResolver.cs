using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Editor.UI
{
    /// <summary>
    /// 접두사 규칙에 적힌 타입 이름(예: UnityEngine.UI.Image)을 실제 타입으로 바꿉니다.
    /// </summary>
    static class UITypeResolver
    {
        static readonly Dictionary<string, Type> s_cache = new();

        public static Type Resolve(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            typeName = typeName.Trim();
            if (s_cache.TryGetValue(typeName, out var cached))
                return cached;

            Type result = null;
            if (typeName == typeof(GameObject).FullName)
            {
                result = typeof(GameObject);
            }
            else
            {
                foreach (var type in TypeCache.GetTypesDerivedFrom<Component>())
                {
                    if (type.FullName == typeName)
                    {
                        result = type;
                        break;
                    }
                }
            }

            s_cache[typeName] = result;
            return result;
        }
    }
}
