using System;
using System.IO;
using System.Text.RegularExpressions;
using Nyonyo.Framework.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nyonyo.Framework.Editor.Tests
{
    public class SaveServiceTests
    {
        [Serializable]
        public class PlayerData
        {
            public int gold;
            public string nickname = "";
        }

        [Serializable]
        public class VersionedData : IVersionedSaveData
        {
            public int version;
            public int gold;
            public string nickname = "";

            [NonSerialized] public int MigratedFrom = -1;

            public int Version
            {
                get => version;
                set => version = value;
            }

            public int LatestVersion => 2;

            public void Migrate(int fromVersion)
            {
                MigratedFrom = fromVersion;
                if (fromVersion < 2)
                    nickname = "Player";
            }
        }

        MemorySaveStorage _storage;
        SaveService _save;

        [SetUp]
        public void SetUp()
        {
            _storage = new MemorySaveStorage();
            _save = new SaveService(_storage);
        }

        [Test]
        public void SaveAndLoad_RoundTrips()
        {
            _save.Save("player", new PlayerData { gold = 10, nickname = "A" });

            var loaded = _save.Load<PlayerData>("player");

            Assert.AreEqual(10, loaded.gold);
            Assert.AreEqual("A", loaded.nickname);
        }

        [Test]
        public void Load_Missing_ReturnsFallback()
        {
            var fallback = new PlayerData { gold = 5 };

            Assert.AreSame(fallback, _save.Load("player", fallback));
            Assert.IsFalse(_save.TryLoad("player", out PlayerData _));
        }

        [Test]
        public void Slots_AreIndependent()
        {
            _save.CurrentSlot = 0;
            _save.Save("player", new PlayerData { gold = 1 });
            _save.CurrentSlot = 1;
            _save.Save("player", new PlayerData { gold = 2 });

            Assert.AreEqual(2, _save.Load<PlayerData>("player").gold);
            _save.CurrentSlot = 0;
            Assert.AreEqual(1, _save.Load<PlayerData>("player").gold);
        }

        [Test]
        public void GlobalScope_IsSharedAcrossSlots()
        {
            _save.CurrentSlot = 0;
            _save.Save("settings", new PlayerData { gold = 7 }, SaveScope.Global);
            _save.CurrentSlot = 3;

            Assert.AreEqual(7, _save.Load<PlayerData>("settings", scope: SaveScope.Global).gold);
            Assert.IsFalse(_save.Exists("settings"));
            Assert.IsFalse(_save.SlotExists(0), "전역 데이터는 슬롯 정보에 들어가지 않습니다.");
        }

        [Test]
        public void Delete_RemovesData()
        {
            _save.Save("player", new PlayerData());

            _save.Delete("player");

            Assert.IsFalse(_save.Exists("player"));
            Assert.IsFalse(_save.SlotExists(0), "슬롯이 비면 슬롯 정보도 지웁니다.");
        }

        [Test]
        public void SlotInfo_TracksKeysAndSaveTime()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            _save.CurrentSlot = 2;
            _save.Save("player", new PlayerData());
            _save.Save("inventory", new PlayerData());
            _save.Save("player", new PlayerData());

            Assert.IsTrue(_save.SlotExists(2));
            Assert.IsTrue(_save.TryGetSlotInfo(2, out var info));
            Assert.AreEqual(2, info.KeyCount);
            Assert.GreaterOrEqual(info.LastSavedUtc, before);
            Assert.IsFalse(_save.TryGetSlotInfo(0, out _));
        }

        [Test]
        public void DeleteSlot_RemovesOnlyThatSlot()
        {
            _save.Save("settings", new PlayerData(), SaveScope.Global);
            _save.CurrentSlot = 0;
            _save.Save("player", new PlayerData());
            _save.CurrentSlot = 1;
            _save.Save("player", new PlayerData());
            _save.Save("inventory", new PlayerData());

            _save.DeleteSlot(1);

            Assert.IsFalse(_save.SlotExists(1));
            Assert.IsFalse(_save.Exists("player"));
            Assert.IsFalse(_save.Exists("inventory"));
            Assert.IsTrue(_save.SlotExists(0));
            Assert.IsTrue(_save.Exists("settings", SaveScope.Global));
        }

        [Test]
        public void Save_VersionedData_StoresLatestVersion()
        {
            _save.Save("player", new VersionedData { gold = 1 });

            StringAssert.Contains("\"version\":2", _storage.Data["slot0.player"]);
        }

        [Test]
        public void Load_OldVersion_MigratesAndSavesBack()
        {
            _storage.Data["slot0.player"] = "{\"version\":1,\"gold\":30}";

            var loaded = _save.Load<VersionedData>("player");

            Assert.AreEqual(1, loaded.MigratedFrom);
            Assert.AreEqual(2, loaded.Version);
            Assert.AreEqual("Player", loaded.nickname);
            Assert.AreEqual(30, loaded.gold);
            StringAssert.Contains("\"version\":2", _storage.Data["slot0.player"]);
        }

        [Test]
        public void Load_NewerVersion_WarnsAndLoadsAsIs()
        {
            _storage.Data["slot0.player"] = "{\"version\":9,\"gold\":30}";

            LogAssert.Expect(LogType.Warning, new Regex("버전"));
            var loaded = _save.Load<VersionedData>("player");

            Assert.AreEqual(-1, loaded.MigratedFrom);
            Assert.AreEqual(30, loaded.gold);
        }

        [Test]
        public void Load_CorruptedData_LogsErrorAndReturnsFallback()
        {
            _storage.Data["slot0.player"] = "{ not json";

            LogAssert.Expect(LogType.Error, new Regex("slot0.player"));
            var loaded = _save.Load("player", new PlayerData { gold = -1 });

            Assert.AreEqual(-1, loaded.gold);
        }

        [Test]
        public void Save_Primitive_Throws()
        {
            Assert.Throws<ArgumentException>(() => _save.Save("count", 5));
            Assert.Throws<ArgumentException>(() => _save.Save("name", "text"));
            Assert.Throws<ArgumentException>(() => _save.Save("list", new[] { 1, 2 }));
        }

        [Test]
        public void InvalidKey_Throws()
        {
            Assert.Throws<ArgumentException>(() => _save.Save("has space", new PlayerData()));
            Assert.Throws<ArgumentException>(() => _save.Save("a/b", new PlayerData()));
            Assert.Throws<ArgumentException>(() => _save.Save("__meta", new PlayerData()));
            Assert.Throws<ArgumentOutOfRangeException>(() => _save.CurrentSlot = -1);
        }

        [Test]
        public void FileStorage_WritesReadsAndDeletes()
        {
            var folder = Path.Combine(Path.GetTempPath(), "NyonyoSaveTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                var storage = new FileSaveStorage(folder);

                storage.Write("slot0.player", "{\"gold\":1}");
                storage.Write("slot0.player", "{\"gold\":2}");

                Assert.IsTrue(storage.TryRead("slot0.player", out var data));
                Assert.AreEqual("{\"gold\":2}", data);
                Assert.IsFalse(File.Exists(Path.Combine(folder, "slot0.player.json.tmp")));

                storage.Delete("slot0.player");
                Assert.IsFalse(storage.Exists("slot0.player"));
            }
            finally
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
        }

        [Test]
        public void FileStorage_ReadsTempFileWhenMainIsMissing()
        {
            var folder = Path.Combine(Path.GetTempPath(), "NyonyoSaveTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "slot0.player.json.tmp"), "{\"gold\":3}");
                var storage = new FileSaveStorage(folder);

                Assert.IsTrue(storage.TryRead("slot0.player", out var data));
                Assert.AreEqual("{\"gold\":3}", data);
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }
    }
}
