using System.Collections.Generic;
using System.Text;

namespace Nyonyo.Framework.Data
{
    /// <summary>
    /// CSV 텍스트를 행과 칸으로 나눕니다. 큰따옴표로 감싼 칸 안의 쉼표, 줄바꿈, 큰따옴표("" 표기)를 처리합니다.
    /// </summary>
    /// <remarks>
    /// 엑셀이나 구글 시트에서 "CSV(쉼표로 구분)"로 내보낸 파일을 그대로 읽을 수 있습니다. 파일 앞의 BOM은 무시합니다.
    /// </remarks>
    public static class CsvParser
    {
        public static List<string[]> Parse(string text)
        {
            var rows = new List<string[]>();
            var row = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;
            var start = text.Length > 0 && text[0] == '﻿' ? 1 : 0;

            for (var i = start; i < text.Length; i++)
            {
                var c = text[i];
                if (inQuotes)
                {
                    if (c != '"')
                        field.Append(c);
                    else if (i + 1 < text.Length && text[i + 1] == '"')
                        field.Append(text[++i]);
                    else
                        inQuotes = false;
                    continue;
                }

                switch (c)
                {
                    case '"' when field.Length == 0:
                        inQuotes = true;
                        break;
                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                    case '\n':
                        if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                            i++;
                        EndRow(rows, row, field);
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }

            if (field.Length > 0 || row.Count > 0)
                EndRow(rows, row, field);

            return rows;
        }

        static void EndRow(List<string[]> rows, List<string> row, StringBuilder field)
        {
            row.Add(field.ToString());
            field.Clear();
            rows.Add(row.ToArray());
            row.Clear();
        }
    }
}
