using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace Nyonyo.Framework.Data
{
    /// <summary>
    /// CSV를 행 클래스 목록으로 바꿉니다. 첫 행(머리글)의 이름과 같은 필드에 값을 넣습니다.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>머리글은 필드 이름과 대소문자 구분 없이 맞춥니다. <c>_hp</c> 같은 private 필드는 <c>hp</c>로도 맞춥니다.</item>
    /// <item>public 필드나 [SerializeField] 필드만 채웁니다. 지원 타입은 string, int, long, float, double, bool, enum입니다.</item>
    /// <item><c>#</c>로 시작하는 머리글 칸은 메모 열로 보고 건너뜁니다. 첫 칸이 <c>#</c>로 시작하는 행과 빈 행도 건너뜁니다.</item>
    /// <item>빈 칸은 필드 초기값을 그대로 둡니다. 숫자는 지역 설정과 상관없이 소수점 <c>.</c>을 씁니다.</item>
    /// </list>
    /// </remarks>
    public static class CsvRowMapper
    {
        /// <exception cref="FormatException">머리글에 맞는 필드가 없거나 값을 변환하지 못한 경우(행 번호 포함)</exception>
        public static List<TRow> Map<TRow>(string csv) where TRow : new()
        {
            var rows = CsvParser.Parse(csv);
            var result = new List<TRow>();

            var headerIndex = rows.FindIndex(row => !IsSkippedRow(row));
            if (headerIndex < 0)
                return result;

            var columns = MapColumns<TRow>(rows[headerIndex]);
            for (var r = headerIndex + 1; r < rows.Count; r++)
            {
                var cells = rows[r];
                if (IsSkippedRow(cells))
                    continue;

                object boxed = new TRow();
                for (var c = 0; c < columns.Length; c++)
                {
                    var field = columns[c];
                    var cell = c < cells.Length ? cells[c].Trim() : "";
                    if (field == null || cell.Length == 0)
                        continue;

                    try
                    {
                        field.SetValue(boxed, Convert(cell, field.FieldType));
                    }
                    catch (Exception e) when (e is FormatException || e is OverflowException || e is ArgumentException)
                    {
                        throw new FormatException(
                            $"{r + 1}번째 줄 '{field.Name}' 칸의 값 '{cell}'을(를) {field.FieldType.Name}(으)로 바꿀 수 없습니다.", e);
                    }
                }

                result.Add((TRow)boxed);
            }

            return result;
        }

        static FieldInfo[] MapColumns<TRow>(string[] header)
        {
            var fields = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);
            for (var type = typeof(TRow); type != null && type != typeof(object); type = type.BaseType)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
                        continue;

                    fields.TryAdd(field.Name, field);
                    if (field.Name.StartsWith("_", StringComparison.Ordinal))
                        fields.TryAdd(field.Name.Substring(1), field);
                }
            }

            var columns = new FieldInfo[header.Length];
            for (var i = 0; i < header.Length; i++)
            {
                var name = header[i].Trim();
                if (name.Length == 0 || name.StartsWith("#", StringComparison.Ordinal))
                    continue;

                if (!fields.TryGetValue(name, out columns[i]))
                    throw new FormatException(
                        $"머리글 '{name}'에 맞는 필드가 {typeof(TRow).Name}에 없습니다. 메모 열이라면 이름 앞에 #을 붙이세요.");
            }

            return columns;
        }

        static object Convert(string cell, Type type)
        {
            var culture = CultureInfo.InvariantCulture;
            if (type == typeof(string))
                return cell;
            if (type == typeof(int))
                return int.Parse(cell, NumberStyles.Integer, culture);
            if (type == typeof(long))
                return long.Parse(cell, NumberStyles.Integer, culture);
            if (type == typeof(float))
                return float.Parse(cell, NumberStyles.Float, culture);
            if (type == typeof(double))
                return double.Parse(cell, NumberStyles.Float, culture);
            if (type == typeof(bool))
                return ParseBool(cell);
            if (type.IsEnum)
                return Enum.Parse(type, cell, true);

            throw new ArgumentException($"{type.Name} 타입은 CSV에서 읽을 수 없습니다.");
        }

        static bool ParseBool(string cell)
        {
            switch (cell.ToLowerInvariant())
            {
                case "true":
                case "1":
                case "yes":
                case "y":
                case "o":
                    return true;
                case "false":
                case "0":
                case "no":
                case "n":
                case "x":
                    return false;
                default:
                    throw new FormatException($"'{cell}'은(는) true/false 값이 아닙니다.");
            }
        }

        static bool IsSkippedRow(string[] cells)
        {
            if (cells.Length == 0 || cells[0].TrimStart().StartsWith("#", StringComparison.Ordinal))
                return true;

            foreach (var cell in cells)
            {
                if (!string.IsNullOrWhiteSpace(cell))
                    return false;
            }

            return true;
        }
    }
}
