using System;
using System.Collections.Generic;
using Nyonyo.Framework.Data;
using NUnit.Framework;
using UnityEngine;

namespace Nyonyo.Framework.Editor.Tests
{
    public class DataTableTests
    {
        [Test]
        public void Parse_HandlesQuotesCommasAndNewlines()
        {
            var rows = CsvParser.Parse("﻿id,text\r\na,\"hello, \"\"world\"\"\"\r\nb,\"line1\nline2\"\n");

            Assert.AreEqual(3, rows.Count);
            CollectionAssert.AreEqual(new[] { "id", "text" }, rows[0]);
            CollectionAssert.AreEqual(new[] { "a", "hello, \"world\"" }, rows[1]);
            CollectionAssert.AreEqual(new[] { "b", "line1\nline2" }, rows[2]);
        }

        [Test]
        public void Parse_KeepsEmptyCells()
        {
            var rows = CsvParser.Parse("a,,c");

            CollectionAssert.AreEqual(new[] { "a", "", "c" }, rows[0]);
        }

        [Test]
        public void Map_FillsFieldsOfEachType()
        {
            const string csv =
                "id,name,price,weight,stackable,grade,level,#memo\n" +
                "potion,Potion,100,0.5,true,Rare,3,ignored\n" +
                "sword,Sword,1500,3.25,0,common,,\n";

            var rows = CsvRowMapper.Map<TestItemRow>(csv);

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("potion", rows[0].id);
            Assert.AreEqual(100, rows[0].price);
            Assert.AreEqual(0.5f, rows[0].weight);
            Assert.IsTrue(rows[0].stackable);
            Assert.AreEqual(TestItemGrade.Rare, rows[0].grade);
            Assert.AreEqual(3, rows[0].Level, "private [SerializeField] _level도 level 머리글로 채웁니다.");
            Assert.AreEqual(TestItemGrade.Common, rows[1].grade);
            Assert.AreEqual(1, rows[1].Level, "빈 칸은 필드 초기값을 유지합니다.");
            Assert.AreEqual("none", rows[1].note);
        }

        [Test]
        public void Map_SkipsCommentAndEmptyRows()
        {
            const string csv = "# 아이템 테이블\nid,price\n# 임시 행\npotion,1\n,\n\nsword,2\n";

            var rows = CsvRowMapper.Map<TestItemRow>(csv);

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("sword", rows[1].id);
        }

        [Test]
        public void Map_UnknownHeader_Throws()
        {
            var e = Assert.Throws<FormatException>(() => CsvRowMapper.Map<TestItemRow>("id,hp\npotion,1\n"));

            StringAssert.Contains("hp", e.Message);
        }

        [Test]
        public void Map_InvalidValue_ThrowsWithLineNumber()
        {
            var e = Assert.Throws<FormatException>(() => CsvRowMapper.Map<TestItemRow>("id,price\npotion,1\nsword,abc\n"));

            StringAssert.Contains("3번째 줄", e.Message);
        }

        [Test]
        public void Table_ImportCsvAndGetById()
        {
            var table = ScriptableObject.CreateInstance<TestItemTable>();
            try
            {
                table.ImportCsv("id,price\npotion,100\nsword,1500\n");

                Assert.AreEqual(2, table.Count);
                Assert.AreEqual(1500, table.Get("sword").price);
                Assert.IsTrue(table.TryGet("potion", out var potion));
                Assert.AreEqual(100, potion.price);
                Assert.IsFalse(table.Contains("shield"));
                Assert.Throws<KeyNotFoundException>(() => table.Get("shield"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(table);
            }
        }
    }
}
