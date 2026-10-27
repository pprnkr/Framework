using System.IO;
using System.IO.Compression;
using System.Text;
using Nyonyo.Framework.Editor.Data;
using NUnit.Framework;

namespace Nyonyo.Framework.Editor.Tests
{
    public class XlsxReaderTests
    {
        private const string MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private const string PackageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        [Test]
        public void Read_ReturnsSheetsInWorkbookOrder()
        {
            var sheets = new XlsxReader().Read(CreateXlsx());

            Assert.AreEqual(2, sheets.Count);
            Assert.AreEqual("Item", sheets[0].Name);
            Assert.AreEqual("_Memo", sheets[1].Name);
        }

        [Test]
        public void Read_ConvertsCellTypes()
        {
            var rows = new XlsxReader().Read(CreateXlsx())[0].Rows;

            CollectionAssert.AreEqual(new[] { "Id", "Name", "Price" }, rows[0], "공유 문자열과 서식이 나뉜 글자를 이어 붙입니다.");
            CollectionAssert.AreEqual(new[] { "1001", "인라인", "0.1" }, rows[1], "0.10000000000000001은 0.1로 읽습니다.");
            CollectionAssert.AreEqual(new[] { "TRUE", "", "12345678901234567" }, rows[2], "bool과 큰 정수, 빠진 셀을 처리합니다.");
        }

        [Test]
        public void Read_KeepsEmptyRowsInPlace()
        {
            var rows = new XlsxReader().Read(CreateXlsx())[0].Rows;

            Assert.AreEqual(5, rows.Count);
            CollectionAssert.IsEmpty(rows[3], "빠진 4행은 빈 행입니다.");
            Assert.AreEqual("last", rows[4][0]);
        }

        [Test]
        public void Read_NotXlsx_Throws()
        {
            Assert.Catch<InvalidDataException>(() => new XlsxReader().Read(Encoding.UTF8.GetBytes("<html>login</html>")));
        }

        private byte[] CreateXlsx()
        {
            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                Add(archive, "xl/workbook.xml",
                    $"<workbook xmlns=\"{MainNs}\" xmlns:r=\"{RelNs}\"><sheets>" +
                    "<sheet name=\"Item\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"_Memo\" sheetId=\"2\" r:id=\"rId2\"/>" +
                    "</sheets></workbook>");
                Add(archive, "xl/_rels/workbook.xml.rels",
                    $"<Relationships xmlns=\"{PackageRelNs}\">" +
                    "<Relationship Id=\"rId1\" Target=\"worksheets/sheet1.xml\"/>" +
                    "<Relationship Id=\"rId2\" Target=\"/xl/worksheets/sheet2.xml\"/>" +
                    "</Relationships>");
                Add(archive, "xl/sharedStrings.xml",
                    $"<sst xmlns=\"{MainNs}\">" +
                    "<si><t>Id</t></si>" +
                    "<si><r><t>Na</t></r><r><t>me</t></r></si>" +
                    "<si><t>Price</t></si>" +
                    "<si><t>last</t></si>" +
                    "</sst>");
                Add(archive, "xl/worksheets/sheet1.xml",
                    $"<worksheet xmlns=\"{MainNs}\"><sheetData>" +
                    "<row r=\"1\"><c r=\"A1\" t=\"s\"><v>0</v></c><c r=\"B1\" t=\"s\"><v>1</v></c><c r=\"C1\" t=\"s\"><v>2</v></c></row>" +
                    "<row r=\"2\"><c r=\"A2\"><v>1001</v></c><c r=\"B2\" t=\"inlineStr\"><is><t>인라인</t></is></c><c r=\"C2\"><v>0.10000000000000001</v></c></row>" +
                    "<row r=\"3\"><c r=\"A3\" t=\"b\"><v>1</v></c><c r=\"C3\"><v>12345678901234567</v></c></row>" +
                    "<row r=\"5\"><c r=\"A5\" t=\"s\"><v>3</v></c></row>" +
                    "</sheetData></worksheet>");
                Add(archive, "xl/worksheets/sheet2.xml", $"<worksheet xmlns=\"{MainNs}\"><sheetData/></worksheet>");
            }
            return stream.ToArray();
        }

        private void Add(ZipArchive archive, string path, string xml)
        {
            using var writer = new StreamWriter(archive.CreateEntry(path).Open(), new UTF8Encoding(false));
            writer.Write(xml);
        }
    }
}
