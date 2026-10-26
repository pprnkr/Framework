using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Nyonyo.Framework.UI;
using UnityEngine.UI;

namespace Nyonyo.Framework.Editor.UI
{
    /// <summary>
    /// UI 스크립트를 새로 만들거나, 기존 스크립트의 자동 생성 영역(#region)만 다시 씁니다.
    /// </summary>
    static class UIScriptGenerator
    {
        public const string RegionStart = "#region Auto Generated UI";
        const string RegionEnd = "#endregion";
        const string Indent = "    ";

        // 인스펙터에서 연결하는 필드라 코드에서 쓰지 않으면 나는 경고(사용 안 함, 할당 안 함)를 끕니다.
        const string WarningDisable = "#pragma warning disable CS0169, CS0649";
        const string WarningRestore = "#pragma warning restore CS0169, CS0649";

        static readonly Regex s_usingPattern = new(@"^\s*using\s+([\w.]+)\s*;");
        static readonly Regex s_namespacePattern = new(@"^\s*namespace\s+([\w.]+)");

        /// <summary>
        /// <paramref name="baseType"/>(<see cref="PopupBase"/> 또는 <see cref="UIBase"/>)를 상속하는 새 스크립트를 만듭니다.
        /// </summary>
        public static string CreateScript(string className, Type baseType, string scriptNamespace, IReadOnlyList<UIBinding> bindings)
        {
            var usings = new SortedSet<string>(StringComparer.Ordinal)
            {
                "UnityEngine",
                typeof(Button).Namespace,
                baseType.Namespace,
            };
            foreach (var binding in bindings)
                usings.Add(binding.FieldType.Namespace);
            usings.RemoveWhere(ns => string.IsNullOrEmpty(ns) || ns == scriptNamespace);

            var builder = new StringBuilder();
            foreach (var ns in usings)
                builder.AppendLine($"using {ns};");
            builder.AppendLine();

            var classIndent = "";
            if (!string.IsNullOrEmpty(scriptNamespace))
            {
                builder.AppendLine($"namespace {scriptNamespace}");
                builder.AppendLine("{");
                classIndent = Indent;
            }

            var member = classIndent + Indent;
            var body = member + Indent;
            builder.AppendLine($"{classIndent}public class {className} : {baseType.Name}");
            builder.AppendLine($"{classIndent}{{");
            foreach (var line in RegionLines(member, bindings))
                builder.AppendLine(line);
            builder.AppendLine();

            if (typeof(PopupBase).IsAssignableFrom(baseType))
            {
                builder.AppendLine($"{member}protected override void OnOpen()");
                builder.AppendLine($"{member}{{");
                builder.AppendLine($"{member}}}");
                builder.AppendLine();
            }

            builder.AppendLine($"{member}public override void OnButtonEvent(Button button)");
            builder.AppendLine($"{member}{{");
            builder.AppendLine($"{body}switch (button.name)");
            builder.AppendLine($"{body}{{");
            foreach (var binding in bindings.Where(b => typeof(Button).IsAssignableFrom(b.FieldType)))
            {
                builder.AppendLine($"{body}{Indent}case \"{binding.FieldName}\":");
                builder.AppendLine($"{body}{Indent}{Indent}break;");
            }
            builder.AppendLine($"{body}{Indent}default:");
            builder.AppendLine($"{body}{Indent}{Indent}base.OnButtonEvent(button);");
            builder.AppendLine($"{body}{Indent}{Indent}break;");
            builder.AppendLine($"{body}}}");
            builder.AppendLine($"{member}}}");

            builder.AppendLine($"{classIndent}}}");
            if (!string.IsNullOrEmpty(scriptNamespace))
                builder.AppendLine("}");

            return builder.ToString();
        }

        /// <summary>
        /// 기존 스크립트의 자동 생성 영역만 <paramref name="bindings"/>로 다시 씁니다. 영역 밖의 코드는 그대로 둡니다.
        /// 영역이 없으면 클래스 첫 줄에 새로 만들고, 필요한 using도 추가합니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">영역도 없고 클래스 선언도 찾지 못한 경우</exception>
        public static string UpdateScript(string source, string className, IReadOnlyList<UIBinding> bindings)
        {
            var newline = source.Contains("\r\n") ? "\r\n" : "\n";
            var lines = source.Replace("\r\n", "\n").Split('\n').ToList();

            var start = lines.FindIndex(line => line.Trim() == RegionStart);
            if (start >= 0)
            {
                var end = lines.FindIndex(start + 1, line => line.Trim().StartsWith(RegionEnd, StringComparison.Ordinal));
                if (end < 0)
                    throw new InvalidOperationException($"{className}: {RegionStart}의 #endregion을 찾지 못했습니다.");

                var indent = LeadingWhitespace(lines[start]);
                lines.RemoveRange(start, end - start + 1);
                lines.InsertRange(start, RegionLines(indent, bindings));
            }
            else
            {
                var classPattern = new Regex($@"\bclass\s+{Regex.Escape(className)}\b");
                var classLine = lines.FindIndex(line => classPattern.IsMatch(line));
                if (classLine < 0)
                    throw new InvalidOperationException($"{className}: 클래스 선언을 찾지 못해서 자동 생성 영역을 만들 수 없습니다.");

                var braceLine = lines.FindIndex(classLine, line => line.Contains("{"));
                if (braceLine < 0)
                    throw new InvalidOperationException($"{className}: 클래스의 여는 중괄호를 찾지 못했습니다.");

                var indent = LeadingWhitespace(lines[classLine]) + Indent;
                var region = RegionLines(indent, bindings).ToList();
                region.Add("");
                lines.InsertRange(braceLine + 1, region);
            }

            AddMissingUsings(lines, bindings);
            return string.Join(newline, lines);
        }

        static IEnumerable<string> RegionLines(string indent, IReadOnlyList<UIBinding> bindings)
        {
            yield return indent + RegionStart;
            if (bindings.Count > 0)
            {
                yield return indent + WarningDisable;
                foreach (var binding in bindings)
                    yield return $"{indent}[SerializeField] private {TypeName(binding.FieldType)} {binding.FieldName};";
                yield return indent + WarningRestore;
            }

            yield return indent + RegionEnd;
        }

        static void AddMissingUsings(List<string> lines, IReadOnlyList<UIBinding> bindings)
        {
            var existing = new HashSet<string>(StringComparer.Ordinal);
            var lastUsing = -1;
            string fileNamespace = null;
            for (var i = 0; i < lines.Count; i++)
            {
                var usingMatch = s_usingPattern.Match(lines[i]);
                if (usingMatch.Success)
                {
                    existing.Add(usingMatch.Groups[1].Value);
                    lastUsing = i;
                }

                var namespaceMatch = s_namespacePattern.Match(lines[i]);
                if (namespaceMatch.Success && fileNamespace == null)
                    fileNamespace = namespaceMatch.Groups[1].Value;
            }

            var required = new SortedSet<string>(StringComparer.Ordinal) { "UnityEngine" };
            foreach (var binding in bindings)
                required.Add(binding.FieldType.Namespace);

            var missing = required
                .Where(ns => !string.IsNullOrEmpty(ns) && !existing.Contains(ns) && !IsVisibleFrom(fileNamespace, ns))
                .Select(ns => $"using {ns};")
                .ToList();
            if (missing.Count == 0)
                return;

            if (lastUsing < 0)
                missing.Add("");
            lines.InsertRange(lastUsing + 1, missing);
        }

        // 파일의 namespace와 같거나 그 상위 namespace의 타입은 using 없이 보입니다.
        static bool IsVisibleFrom(string fileNamespace, string ns) =>
            fileNamespace != null && (fileNamespace == ns || fileNamespace.StartsWith(ns + ".", StringComparison.Ordinal));

        static string TypeName(Type type) => type.IsNested ? type.FullName.Replace('+', '.') : type.Name;

        static string LeadingWhitespace(string line) => line.Substring(0, line.Length - line.TrimStart().Length);
    }
}
