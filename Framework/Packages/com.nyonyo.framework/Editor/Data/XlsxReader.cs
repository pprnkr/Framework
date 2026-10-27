using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Nyonyo.Framework.Editor.Data
{
    /// <summary>
    /// xlsx 파일에서 시트 이름과 셀 값(문자열)을 읽습니다. 구글 스프레드시트의 xlsx 내보내기를 읽는 용도라 서식은 무시합니다.
    /// </summary>
    /// <remarks>
    /// CSV 대신 xlsx를 쓰는 이유: 요청 한 번에 모든 탭을 이름과 함께 받을 수 있고, 시트 이름으로 받는 CSV(gviz)는 열의 자료형을 추정해서
    /// 숫자 열 위쪽의 주석, 자료형, 변수명 셀을 지워 버리는 문제가 있습니다. 숫자는 표시 형식이 아닌 원래 값으로 읽습니다.
    /// </remarks>
    internal sealed class XlsxReader
    {
        public sealed class Sheet
        {
            public Sheet(string name, List<string[]> rows)
            {
                Name = name;
                Rows = rows;
            }

            public string Name { get; }

            /// <summary>1행부터 순서대로입니다. 빈 행은 빈 배열입니다.</summary>
            public List<string[]> Rows { get; }
        }

        /// <exception cref="InvalidDataException">xlsx 형식이 아닌 경우</exception>
        public List<Sheet> Read(byte[] xlsx)
        {
            try
            {
                using var archive = new ZipArchive(new MemoryStream(xlsx), ZipArchiveMode.Read);
                var sharedStrings = ReadSharedStrings(archive);
                var targets = ReadRelationships(archive);
                var workbook = Load(archive, "xl/workbook.xml") ?? throw new InvalidDataException("xl/workbook.xml이 없습니다.");

                var sheets = new List<Sheet>();
                foreach (var element in workbook.Descendants().Where(e => e.Name.LocalName == "sheet"))
                {
                    var name = (string)element.Attribute("name");
                    var relationId = element.Attributes().FirstOrDefault(a => a.Name.LocalName == "id")?.Value;
                    if (name == null || relationId == null || !targets.TryGetValue(relationId, out var path))
                        continue;

                    var document = Load(archive, path) ?? throw new InvalidDataException($"시트 '{name}'의 파일({path})이 없습니다.");
                    sheets.Add(new Sheet(name, ReadRows(document, sharedStrings)));
                }
                return sheets;
            }
            catch (InvalidDataException e)
            {
                throw new InvalidDataException($"xlsx 파일을 읽지 못했습니다: {e.Message}", e);
            }
        }

        private List<string> ReadSharedStrings(ZipArchive archive)
        {
            var strings = new List<string>();
            var document = Load(archive, "xl/sharedStrings.xml");
            if (document == null)
                return strings;

            foreach (var item in document.Root.Elements().Where(e => e.Name.LocalName == "si"))
                strings.Add(ReadText(item));
            return strings;
        }

        // 관계 ID → 시트 파일 경로(zip 안의 경로)입니다.
        private Dictionary<string, string> ReadRelationships(ZipArchive archive)
        {
            var targets = new Dictionary<string, string>();
            var document = Load(archive, "xl/_rels/workbook.xml.rels");
            if (document == null)
                return targets;

            foreach (var element in document.Descendants().Where(e => e.Name.LocalName == "Relationship"))
            {
                var id = (string)element.Attribute("Id");
                var target = (string)element.Attribute("Target");
                if (id == null || target == null)
                    continue;
                targets[id] = target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target;
            }
            return targets;
        }

        private List<string[]> ReadRows(XDocument document, List<string> sharedStrings)
        {
            var rows = new List<string[]>();
            var sheetData = document.Root.Elements().FirstOrDefault(e => e.Name.LocalName == "sheetData");
            if (sheetData == null)
                return rows;

            var nextRow = 0;
            foreach (var rowElement in sheetData.Elements().Where(e => e.Name.LocalName == "row"))
            {
                var rowIndex = int.TryParse((string)rowElement.Attribute("r"), out var r) ? r - 1 : nextRow;
                nextRow = rowIndex + 1;

                var cells = new List<string>();
                var nextColumn = 0;
                foreach (var cell in rowElement.Elements().Where(e => e.Name.LocalName == "c"))
                {
                    var reference = (string)cell.Attribute("r");
                    var column = reference != null ? ColumnIndex(reference) : nextColumn;
                    nextColumn = column + 1;

                    while (cells.Count <= column)
                        cells.Add("");
                    cells[column] = ReadCell(cell, sharedStrings);
                }

                while (rows.Count < rowIndex)
                    rows.Add(Array.Empty<string>());
                if (rows.Count == rowIndex)
                    rows.Add(cells.ToArray());
            }
            return rows;
        }

        private string ReadCell(XElement cell, List<string> sharedStrings)
        {
            var type = (string)cell.Attribute("t");
            if (type == "inlineStr")
            {
                var inline = cell.Elements().FirstOrDefault(e => e.Name.LocalName == "is");
                return inline != null ? ReadText(inline) : "";
            }

            var value = cell.Elements().FirstOrDefault(e => e.Name.LocalName == "v")?.Value ?? "";
            if (type == "s")
                return int.TryParse(value, out var index) && index >= 0 && index < sharedStrings.Count ? sharedStrings[index] : "";
            if (type == "b")
                return value == "1" ? "TRUE" : "FALSE";
            if (type == "str" || type == "e" || value.Length == 0)
                return value;

            // 숫자 셀입니다. 0.1이 0.10000000000000001처럼 저장되어도 원래 값으로 보이게 다시 씁니다.
            // 정수는 그대로 둬서 long 범위의 큰 값이 double을 거치며 깎이지 않게 합니다.
            if (value.IndexOfAny(new[] { '.', 'E', 'e' }) < 0)
                return value;
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? number.ToString("R", CultureInfo.InvariantCulture)
                : value;
        }

        // 서식이 다른 글자 조각(r/t)은 이어 붙이고, 일본어 읽기(rPh)는 뺍니다.
        private string ReadText(XElement element)
        {
            var text = new StringBuilder();
            foreach (var t in element.Descendants().Where(e => e.Name.LocalName == "t"))
            {
                if (t.Parent?.Name.LocalName != "rPh")
                    text.Append(t.Value);
            }
            return text.ToString();
        }

        private int ColumnIndex(string reference)
        {
            var index = 0;
            foreach (var c in reference)
            {
                if (c < 'A' || c > 'Z')
                    break;
                index = index * 26 + (c - 'A' + 1);
            }
            return index - 1;
        }

        private XDocument Load(ZipArchive archive, string path)
        {
            var entry = archive.GetEntry(path);
            if (entry == null)
                return null;
            using var stream = entry.Open();
            return XDocument.Load(stream);
        }
    }
}
