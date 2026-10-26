using System;
using Nyonyo.Framework.Data;
using UnityEngine;

namespace Nyonyo.Framework.Editor.Tests
{
    public enum TestItemGrade
    {
        Common,
        Rare,
    }

    [Serializable]
    public class TestItemRow : IDataRow
    {
        public string id;
        public string name;
        public int price;
        public float weight;
        public bool stackable;
        public TestItemGrade grade;
        [SerializeField] int _level = 1;
        public string note = "none";

        public string Id => id;

        public int Level => _level;
    }

    public class TestItemTable : DataTable<TestItemRow>
    {
    }
}
