using System;
using System.Collections.Generic;
using System.Text;
using Nyonyo.Framework.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Editor.UI
{
    /// <summary>
    /// 접두사가 붙은 자식 오브젝트 하나와, 그것을 담을 필드 정보입니다.
    /// </summary>
    readonly struct UIBinding
    {
        public UIBinding(string fieldName, Type fieldType, Object value)
        {
            FieldName = fieldName;
            FieldType = fieldType;
            Value = value;
        }

        public string FieldName { get; }

        public Type FieldType { get; }

        public Object Value { get; }
    }

    /// <summary>
    /// UI 루트 아래에서 접두사 규칙에 맞는 자식 오브젝트를 찾습니다.
    /// </summary>
    static class UIBindingCollector
    {
        /// <summary>
        /// <paramref name="root"/>의 자식을 계층 순서대로 훑습니다. 루트 자신은 포함하지 않습니다.
        /// 자식에 다른 <see cref="UIBase"/>가 있으면 그 자식까지만 포함하고, 그 아래는 자식 UI가 관리하므로 건너뜁니다.
        /// </summary>
        public static List<UIBinding> Collect(GameObject root, UIPrefixSettings settings)
        {
            var bindings = new List<UIBinding>();
            Visit(root.transform, settings, bindings, new HashSet<string>());
            return bindings;
        }

        /// <summary>
        /// 오브젝트 이름을 C# 변수 이름으로 바꿉니다. 쓸 수 없는 문자는 _로 바꿉니다.
        /// </summary>
        public static string ToFieldName(string objectName)
        {
            var builder = new StringBuilder(objectName.Length);
            foreach (var c in objectName)
                builder.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');

            if (builder.Length == 0 || char.IsDigit(builder[0]))
                builder.Insert(0, '_');

            return builder.ToString();
        }

        private static void Visit(Transform parent, UIPrefixSettings settings, List<UIBinding> bindings, HashSet<string> fieldNames)
        {
            foreach (Transform child in parent)
            {
                TryAdd(child.gameObject, settings, bindings, fieldNames);

                if (child.GetComponent<UIBase>() == null)
                    Visit(child, settings, bindings, fieldNames);
            }
        }

        private static void TryAdd(GameObject target, UIPrefixSettings settings, List<UIBinding> bindings, HashSet<string> fieldNames)
        {
            var rule = settings.FindRule(target.name);
            if (rule == null)
                return;

            var ruleType = rule.ResolveType();
            if (ruleType == null)
            {
                Debug.LogWarning(
                    $"[Nyonyo Framework] {target.name}: 접두사 {rule.Prefix}의 타입 {rule.TypeName}을(를) 찾을 수 없어서 건너뜁니다. UIPrefixSettings를 확인하세요.",
                    target);
                return;
            }

            Object value;
            Type fieldType;
            if (ruleType == typeof(GameObject))
            {
                value = target;
                fieldType = typeof(GameObject);
            }
            else
            {
                var component = target.GetComponent(ruleType);
                if (component == null)
                {
                    Debug.LogWarning(
                        $"[Nyonyo Framework] {target.name}에 {ruleType.Name} 컴포넌트가 없어서 건너뜁니다.", target);
                    return;
                }

                // UI_처럼 기본 타입으로 정한 규칙도 실제 컴포넌트 타입으로 변수를 만듭니다.
                value = component;
                fieldType = component.GetType();
            }

            var fieldName = ToFieldName(target.name);
            if (!fieldNames.Add(fieldName))
            {
                Debug.LogWarning(
                    $"[Nyonyo Framework] {target.name}: 같은 이름의 오브젝트가 이미 있어서 건너뜁니다. 이름을 다르게 지어 주세요.", target);
                return;
            }

            bindings.Add(new UIBinding(fieldName, fieldType, value));
        }
    }
}
