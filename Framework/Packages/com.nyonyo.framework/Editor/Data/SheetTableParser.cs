using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Nyonyo.Framework.Editor.Data
{
    /// <summary>
    /// 시트 내용(행과 열의 문자열)을 <see cref="SheetTable"/>로 해석하고 규칙을 검사합니다.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>1행은 주석, 2행은 자료형, 3행은 변수명, 4행부터 데이터입니다.</item>
    /// <item>첫 번째 열은 유니크 키입니다. 자료형은 int, long, string 중 하나이고, 키가 겹치면 에러입니다.</item>
    /// <item>변수명이 _로 시작하는 열과, 키가 _로 시작하는 행은 건너뜁니다. 변수명과 자료형이 모두 빈 열, 키가 빈 빈 행도 건너뜁니다.</item>
    /// <item>배열은 자료형을 <c>int[]</c>(또는 <c>Array&lt;int&gt;</c>, <c>List&lt;int&gt;</c>)로 적고, 같은 변수명의 열들이 왼쪽부터 원소가 됩니다. 빈 칸은 건너뜁니다.</item>
    /// <item>단일 값의 빈 칸은 기본값(0, false, 빈 문자열, enum 0)입니다.</item>
    /// </list>
    /// 에러는 모두 모아서 "시트!셀" 위치와 함께 돌려줍니다.
    /// </remarks>
    internal sealed class SheetTableParser
    {
        public const int CommentRow = 0;
        public const int TypeRow = 1;
        public const int NameRow = 2;
        public const int FirstDataRow = 3;

        private static readonly Regex s_identifier = new("^[A-Za-z][A-Za-z0-9_]*$");
        private static readonly Regex s_arrayType = new(@"^(?:(?<element>.+)\[\]|(?:Array|List)<(?<element>.+)>)$", RegexOptions.IgnoreCase);

        // 생성 코드에서 행 클래스의 멤버로 이미 쓰는 이름입니다.
        private static readonly HashSet<string> s_reservedFieldNames = new() { "Read", "OnLoaded" };

        private static readonly HashSet<string> s_keywords = new()
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
            "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
            "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
            "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
            "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile", "while",
        };

        private readonly EnumFinder _findEnum;

        public SheetTableParser(EnumFinder findEnum)
        {
            _findEnum = findEnum ?? throw new ArgumentNullException(nameof(findEnum));
        }

        /// <summary>C# 식별자로 쓸 수 있는 이름인지 확인합니다(영문자로 시작, 키워드 아님).</summary>
        public bool IsValidIdentifier(string name) => name != null && s_identifier.IsMatch(name) && !s_keywords.Contains(name);

        /// <param name="errors">찾은 에러를 덧붙입니다.</param>
        /// <returns>에러가 없으면 해석한 테이블, 있으면 null입니다.</returns>
        public SheetTable Parse(string sheetName, IReadOnlyList<string[]> grid, List<string> errors)
        {
            var errorCount = errors.Count;
            if (!IsValidIdentifier(sheetName))
            {
                errors.Add($"{sheetName}: 시트 이름은 클래스 이름이 되므로 영문자로 시작하는 영문, 숫자, _만 쓸 수 있습니다. 가져오지 않을 시트는 이름을 _로 시작하세요.");
                return null;
            }

            if (grid.Count < FirstDataRow)
            {
                errors.Add($"{sheetName}: 1~3행(주석, 자료형, 변수명)이 필요합니다.");
                return null;
            }

            var fields = ParseHeader(sheetName, grid, errors);
            if (errors.Count > errorCount)
                return null;

            var rows = ParseRows(sheetName, grid, fields, errors);
            return errors.Count > errorCount ? null : new SheetTable(sheetName, fields, rows);
        }

        #region 머리글

        private List<SheetField> ParseHeader(string sheet, IReadOnlyList<string[]> grid, List<string> errors)
        {
            var fields = new List<SheetField>();
            var byName = new Dictionary<string, SheetField>();
            var columnCount = Math.Max(Width(grid[TypeRow]), Width(grid[NameRow]));

            for (var column = 0; column < columnCount; column++)
            {
                var name = Cell(grid, NameRow, column).Trim();
                var typeText = Cell(grid, TypeRow, column).Trim();
                var comment = Cell(grid, CommentRow, column).Trim();
                var nameCell = CellName(sheet, NameRow, column);

                if (column == 0)
                {
                    var key = ParseKeyColumn(sheet, name, typeText, comment, errors);
                    if (key == null)
                        return fields;
                    fields.Add(key);
                    byName.Add(name, key);
                    continue;
                }

                if (name.Length == 0 && typeText.Length == 0)
                    continue;
                if (name.StartsWith("_"))
                    continue;

                if (name.Length == 0)
                {
                    errors.Add($"{nameCell}: 변수명이 비어 있습니다. 쓰지 않을 열이면 변수명을 _로 시작하세요.");
                    continue;
                }

                if (!IsValidIdentifier(name) || s_reservedFieldNames.Contains(name) || name == sheet + "Data")
                {
                    errors.Add($"{nameCell}: 변수명 '{name}'은 쓸 수 없습니다. 영문자로 시작하는 영문, 숫자, _만 쓰고, C# 키워드와 Read, OnLoaded, {sheet}Data는 피하세요.");
                    continue;
                }

                if (typeText.Length == 0)
                {
                    errors.Add($"{CellName(sheet, TypeRow, column)}: '{name}'의 자료형이 비어 있습니다.");
                    continue;
                }

                var type = ParseType(typeText, out var typeError);
                if (type == null)
                {
                    errors.Add($"{CellName(sheet, TypeRow, column)}: {typeError}");
                    continue;
                }

                if (!byName.TryGetValue(name, out var existing))
                {
                    var field = new SheetField(name, comment, type, column);
                    fields.Add(field);
                    byName.Add(name, field);
                    continue;
                }

                if (!type.IsArray || !existing.Type.IsArray)
                    errors.Add($"{nameCell}: 변수명 '{name}'이 겹칩니다. 여러 열을 배열로 묶으려면 같은 변수명의 열 모두 자료형을 배열(예: int[])로 적으세요.");
                else if (type.TypeName != existing.Type.TypeName)
                    errors.Add($"{CellName(sheet, TypeRow, column)}: 배열 '{name}'의 자료형({type.TypeName})이 앞 열({existing.Type.TypeName})과 다릅니다.");
                else
                    existing.AddColumn(column, comment);
            }

            return fields;
        }

        private SheetField ParseKeyColumn(string sheet, string name, string typeText, string comment, List<string> errors)
        {
            if (name.Length == 0 || name.StartsWith("_"))
            {
                errors.Add($"{CellName(sheet, NameRow, 0)}: 첫 번째 열은 유니크 키라서 변수명이 필요하고, _로 시작해 건너뛸 수 없습니다.");
                return null;
            }

            if (!IsValidIdentifier(name) || s_reservedFieldNames.Contains(name) || name == sheet + "Data")
            {
                errors.Add($"{CellName(sheet, NameRow, 0)}: 변수명 '{name}'은 쓸 수 없습니다. 영문자로 시작하는 영문, 숫자, _만 쓰고, C# 키워드는 피하세요.");
                return null;
            }

            var type = ParseType(typeText, out _);
            if (type == null || type.IsArray || (type.Kind != SheetValueKind.Int && type.Kind != SheetValueKind.Long && type.Kind != SheetValueKind.String))
            {
                errors.Add($"{CellName(sheet, TypeRow, 0)}: 첫 번째 열(유니크 키)의 자료형은 int, long, string 중 하나여야 합니다. 지금은 '{typeText}'입니다.");
                return null;
            }

            return new SheetField(name, comment, type, 0);
        }

        private SheetColumnType ParseType(string text, out string error)
        {
            error = null;
            var isArray = false;
            var element = text;
            var match = s_arrayType.Match(text);
            if (match.Success)
            {
                isArray = true;
                element = match.Groups["element"].Value.Trim();
            }

            var kind = ParseBuiltInKind(element);
            if (kind.HasValue)
                return new SheetColumnType(kind.Value, isArray);

            var enumType = _findEnum(element, out var enumError);
            if (enumType != null)
                return new SheetColumnType(SheetValueKind.Enum, isArray, enumType);

            error = $"자료형 '{text}'를 알 수 없습니다. int, long, float, double, bool, string, enum 이름, 또는 그 배열(int[])을 쓰세요. ({enumError})";
            return null;
        }

        private SheetValueKind? ParseBuiltInKind(string text)
        {
            var lower = text.ToLowerInvariant();
            if (lower == "int")
                return SheetValueKind.Int;
            if (lower == "long")
                return SheetValueKind.Long;
            if (lower == "float")
                return SheetValueKind.Float;
            if (lower == "double")
                return SheetValueKind.Double;
            if (lower == "bool")
                return SheetValueKind.Bool;
            if (lower == "string")
                return SheetValueKind.String;
            return null;
        }

        #endregion

        #region 데이터

        private List<object[]> ParseRows(string sheet, IReadOnlyList<string[]> grid, List<SheetField> fields, List<string> errors)
        {
            var rows = new List<object[]>();
            var keyRows = new Dictionary<object, int>();
            var key = fields[0];

            for (var row = FirstDataRow; row < grid.Count; row++)
            {
                var keyText = Cell(grid, row, 0).Trim();
                if (keyText.StartsWith("_"))
                    continue;

                if (keyText.Length == 0)
                {
                    if (HasAnyValue(grid, row, fields))
                        errors.Add($"{CellName(sheet, row, 0)}: 값이 있는 행인데 키가 비어 있습니다. 쓰지 않을 행이면 키를 _로 시작하세요.");
                    continue;
                }

                var values = new object[fields.Count];
                var rowHasError = false;
                for (var i = 0; i < fields.Count; i++)
                {
                    var field = fields[i];
                    if (field.Type.IsArray)
                    {
                        var list = new List<object>();
                        foreach (var column in field.Columns)
                        {
                            var text = Cell(grid, row, column);
                            if (text.Trim().Length == 0)
                                continue;
                            if (TryParseValue(field.Type, text, out var element, out var error))
                                list.Add(element);
                            else
                                rowHasError |= AddError(errors, $"{CellName(sheet, row, column)}: {error}");
                        }
                        values[i] = list;
                    }
                    else if (TryParseValue(field.Type, Cell(grid, row, field.Columns[0]), out var value, out var error))
                    {
                        values[i] = value;
                    }
                    else
                    {
                        rowHasError |= AddError(errors, $"{CellName(sheet, row, field.Columns[0])}: {error}");
                    }
                }

                if (rowHasError)
                    continue;

                var keyValue = values[0];
                if (keyRows.TryGetValue(keyValue, out var firstRow))
                {
                    errors.Add($"{CellName(sheet, row, 0)}: 키 '{keyValue}'가 {CellName(sheet, firstRow, 0)}과 겹칩니다. 첫 번째 열은 유니크 키라서 겹치면 안 됩니다.");
                    continue;
                }

                keyRows.Add(keyValue, row);
                rows.Add(values);
            }

            return rows;
        }

        /// <summary>셀 문자열을 자료형에 맞는 값으로 바꿉니다. enum은 int로 바꿉니다.</summary>
        public bool TryParseValue(SheetColumnType type, string text, out object value, out string error)
        {
            error = null;
            var trimmed = (text ?? "").Trim();

            if (type.Kind == SheetValueKind.String)
            {
                value = text ?? "";
                return true;
            }

            if (type.Kind == SheetValueKind.Int || type.Kind == SheetValueKind.Long)
            {
                var isInt = type.Kind == SheetValueKind.Int;
                if (TryParseInteger(trimmed, isInt ? int.MinValue : long.MinValue, isInt ? int.MaxValue : long.MaxValue, out var number))
                {
                    value = isInt ? (object)(int)number : number;
                    return true;
                }
                return Fail($"'{text}'는 {(isInt ? "int" : "long")}가 아닙니다.", out value, out error);
            }

            if (type.Kind == SheetValueKind.Float || type.Kind == SheetValueKind.Double)
            {
                var number = 0d;
                if (trimmed.Length == 0 || double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                {
                    value = type.Kind == SheetValueKind.Float ? (object)(float)number : number;
                    return true;
                }
                return Fail($"'{text}'는 {(type.Kind == SheetValueKind.Float ? "float" : "double")}가 아닙니다.", out value, out error);
            }

            if (type.Kind == SheetValueKind.Bool)
            {
                var lower = trimmed.ToLowerInvariant();
                if (lower.Length == 0 || lower == "false" || lower == "0" || lower == "no" || lower == "n" || lower == "x")
                {
                    value = false;
                    return true;
                }
                if (lower == "true" || lower == "1" || lower == "yes" || lower == "y" || lower == "o")
                {
                    value = true;
                    return true;
                }
                return Fail($"'{text}'는 bool이 아닙니다. TRUE/FALSE, 1/0, O/X 중 하나로 적으세요.", out value, out error);
            }

            if (trimmed.Length == 0)
            {
                value = 0;
                return true;
            }

            // enum은 이름으로만 받습니다. 숫자를 허용하면 정의되지 않은 값이 들어가도 알아차리기 어렵습니다.
            if (char.IsLetter(trimmed[0]) && Enum.TryParse(type.EnumType, trimmed, true, out var parsed))
            {
                var number = Convert.ToInt64(parsed);
                if (number >= int.MinValue && number <= int.MaxValue)
                {
                    value = (int)number;
                    return true;
                }
            }
            return Fail($"'{text}'는 {type.EnumType.Name}의 멤버가 아닙니다. 멤버: {string.Join(", ", Enum.GetNames(type.EnumType))}", out value, out error);
        }

        // 1.0처럼 소수점이 붙은 정수나 1E+3도 정수면 받습니다(엑셀이 숫자를 그렇게 내보낼 때가 있음).
        private bool TryParseInteger(string text, long min, long max, out long value)
        {
            value = 0;
            if (text.Length == 0)
                return true;
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return value >= min && value <= max;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || Math.Floor(number) != number)
                return false;
            if (number < min || number > max)
                return false;
            value = (long)number;
            return true;
        }

        private bool Fail(string message, out object value, out string error)
        {
            value = null;
            error = message;
            return false;
        }

        private bool AddError(List<string> errors, string message)
        {
            errors.Add(message);
            return true;
        }

        private bool HasAnyValue(IReadOnlyList<string[]> grid, int row, List<SheetField> fields)
        {
            foreach (var field in fields)
            foreach (var column in field.Columns)
            {
                if (Cell(grid, row, column).Trim().Length > 0)
                    return true;
            }
            return false;
        }

        #endregion

        private string Cell(IReadOnlyList<string[]> grid, int row, int column)
        {
            if (row >= grid.Count)
                return "";
            var cells = grid[row];
            return cells != null && column < cells.Length ? cells[column] ?? "" : "";
        }

        private int Width(string[] cells) => cells?.Length ?? 0;

        /// <summary>엑셀 형식 셀 이름입니다(예: Item!C5).</summary>
        public string CellName(string sheet, int row, int column)
        {
            var letters = "";
            for (var n = column + 1; n > 0; n = (n - 1) / 26)
                letters = (char)('A' + (n - 1) % 26) + letters;
            return $"{sheet}!{letters}{row + 1}";
        }
    }
}
