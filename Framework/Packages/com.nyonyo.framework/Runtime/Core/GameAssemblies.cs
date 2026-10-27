using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nyonyo.Framework
{
    /// <summary>
    /// 어트리뷰트로 표시한 타입이나 메서드를 찾을 때 훑어볼 게임 어셈블리 목록입니다.
    /// Unity, .NET 어셈블리와 에디터에서 함께 로드되는 테스트 어셈블리는 뺍니다.
    /// </summary>
    static class GameAssemblies
    {
        public static List<Assembly> Get()
        {
            var result = new List<Assembly>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!IsEngineAssembly(assembly.GetName().Name) && !IsTestAssembly(assembly))
                    result.Add(assembly);
            }

            return result;
        }

        /// <summary>
        /// 어셈블리의 타입을 돌려줍니다. 일부 타입을 불러오지 못해도 불러온 타입은 돌려줍니다.
        /// </summary>
        public static IEnumerable<Type> GetTypes(Assembly assembly)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            foreach (var type in types)
            {
                if (type != null)
                    yield return type;
            }
        }

        private static bool IsEngineAssembly(string name) =>
            name == "mscorlib" || name == "netstandard" ||
            name.StartsWith("System", StringComparison.Ordinal) ||
            name.StartsWith("Unity", StringComparison.Ordinal) ||
            name.StartsWith("Mono.", StringComparison.Ordinal) ||
            name.StartsWith("nunit", StringComparison.OrdinalIgnoreCase);

        private static bool IsTestAssembly(Assembly assembly)
        {
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name == "nunit.framework")
                    return true;
            }

            return false;
        }
    }
}
