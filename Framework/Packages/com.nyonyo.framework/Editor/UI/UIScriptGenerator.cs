using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Nyonyo.Framework.Assets;
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
        private const string RegionEnd = "#endregion";
        private const string Indent = "    ";

        // 인스펙터에서 연결하는 필드라 코드에서 쓰지 않으면 나는 경고(사용 안 함, 할당 안 함)를 끕니다.
        private const string WarningDisable = "#pragma warning disable CS0169, CS0649";
        private const string WarningRestore = "#pragma warning restore CS0169, CS0649";

        private static readonly Regex s_usingPattern = new(@"^\s*using\s+([\w.]+)\s*;");
        private static readonly Regex s_namespacePattern = new(@"^\s*namespace\s+([\w.]+)");

        /// <summary>
        /// 프리팹 경로로 <see cref="PopupAttribute"/>에 넣을 경로와 로드 방식을 정합니다.
        /// Resources 폴더 안이면 Resources 기준 경로(확장자 제외), 아니면 에셋 경로를 address로 쓰는 Addressables입니다.
        /// </summary>
        public static (string path, LoadType loadType) GetPopupLoadInfo(string prefabPath)
        {
            const string resourcesFolder = "/Resources/";
            var index = prefabPath.LastIndexOf(resourcesFolder, StringComparison.Ordinal);
            if (index < 0)
                return (prefabPath, LoadType.Addressables);

            var relative = prefabPath.Substring(index + resourcesFolder.Length);
            var extension = System.IO.Path.GetExtension(relative);
            return (relative.Substring(0, relative.Length - extension.Length), LoadType.Resources);
        }

        /// <summary>
        /// <paramref name="baseType"/>(<see cref="PopupBase"/> 또는 <see cref="UIView"/>)를 상속하는 새 스크립트를 만듭니다.
        /// 팝업이고 <paramref name="popupPath"/>가 있으면 <see cref="PopupAttribute"/>도 붙입니다.
        /// </summary>
        public static string CreateScript(string className, Type baseType, string scriptNamespace, IReadOnlyList<UIBinding> bindings,
            string popupPath = null, LoadType popupLoadType = LoadType.Addressables)
        {
            var isPopup = typeof(PopupBase).IsAssignableFrom(baseType);
            var addPopupAttribute = isPopup && !string.IsNullOrEmpty(popupPath);

            var usings = new SortedSet<string>(StringComparer.Ordinal)
            {
                "UnityEngine",
                typeof(Button).Namespace,
                baseType.Namespace,
            };
            if (addPopupAttribute)
                usings.Add(typeof(LoadType).Namespace);
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
            if (addPopupAttribute)
                builder.AppendLine($"{classIndent}[Popup(Path = \"{popupPath}\", LoadType = LoadType.{popupLoadType})]");
            builder.AppendLine($"{classIndent}public class {className} : {baseType.Name}");
            builder.AppendLine($"{classIndent}{{");
            foreach (var line in RegionLines(member, bindings))
                builder.AppendLine(line);
            builder.AppendLine();

            if (isPopup)
            {
                builder.AppendLine($"{member}protected override void PostOpen()");
                builder.AppendLine($"{member}{{");
                builder.AppendLine($"{member}}}");
                builder.AppendLine();
            }

            // 이름이 같은 버튼이 있어도 헷갈리지 않도록 이름 대신 필드 참조로 비교합니다.
            builder.AppendLine($"{member}protected override void OnButtonClick(Button button)");
            builder.AppendLine($"{member}{{");
            var buttons = bindings.Where(b => typeof(Button).IsAssignableFrom(b.FieldType)).ToList();
            if (buttons.Count == 0)
            {
                builder.AppendLine($"{body}base.OnButtonClick(button);");
            }
            else
            {
                for (var i = 0; i < buttons.Count; i++)
                {
                    builder.AppendLine($"{body}{(i == 0 ? "if" : "else if")} (button == {buttons[i].FieldName})");
                    builder.AppendLine($"{body}{{");
                    builder.AppendLine($"{body}}}");
                }

                builder.AppendLine($"{body}else");
                builder.AppendLine($"{body}{{");
                builder.AppendLine($"{body}{Indent}base.OnButtonClick(button);");
                builder.AppendLine($"{body}}}");
            }
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

        private static IEnumerable<string> RegionLines(string indent, IReadOnlyList<UIBinding> bindings)
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

        private static void AddMissingUsings(List<string> lines, IReadOnlyList<UIBinding> bindings)
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
        private static bool IsVisibleFrom(string fileNamespace, string ns) =>
            fileNamespace != null && (fileNamespace == ns || fileNamespace.StartsWith(ns + ".", StringComparison.Ordinal));

        private static string TypeName(Type type) => type.IsNested ? type.FullName.Replace('+', '.') : type.Name;

        private static string LeadingWhitespace(string line) => line.Substring(0, line.Length - line.TrimStart().Length);
    }
}
