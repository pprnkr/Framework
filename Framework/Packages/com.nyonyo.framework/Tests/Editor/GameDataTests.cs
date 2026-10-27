using System;
using System.Collections.Generic;
using System.IO;
using Nyonyo.Framework.Data;
using Nyonyo.Framework.Editor.Data;
using NUnit.Framework;

namespace Nyonyo.Framework.Editor.Tests
{
    public class GameDataTests
    {
        private byte[] _key;
        private GameDataCrypto _crypto;

        [SetUp]
        public void SetUp()
        {
            _key = new byte[GameDataCrypto.KeySize];
            for (var i = 0; i < _key.Length; i++)
                _key[i] = (byte)(i * 7 + 3);
            _crypto = new GameDataCrypto(_key);
        }

        #region 읽기와 쓰기

        [Test]
        public void WriterAndReader_RoundTrip()
        {
            var writer = new GameDataWriter();
            writer.WriteInt(-123456);
            writer.WriteLong(long.MinValue + 1);
            writer.WriteFloat(1.25f);
            writer.WriteDouble(-0.1);
            writer.WriteBool(true);
            writer.WriteString("한글 abc");
            writer.WriteString("");
            writer.WriteInt(2);
            writer.WriteInt(7);
            writer.WriteInt(8);

            var reader = new GameDataReader(writer.ToArray());

            Assert.AreEqual(-123456, reader.ReadInt());
            Assert.AreEqual(long.MinValue + 1, reader.ReadLong());
            Assert.AreEqual(1.25f, reader.ReadFloat());
            Assert.AreEqual(-0.1, reader.ReadDouble());
            Assert.IsTrue(reader.ReadBool());
            Assert.AreEqual("한글 abc", reader.ReadString());
            Assert.AreEqual("", reader.ReadString());
            CollectionAssert.AreEqual(new[] { 7, 8 }, reader.ReadIntArray());
            Assert.IsTrue(reader.IsAtEnd);
        }

        [Test]
        public void Reader_TruncatedData_Throws()
        {
            var reader = new GameDataReader(new byte[] { 1, 2 });

            Assert.Throws<InvalidDataException>(() => reader.ReadInt());
        }

        #endregion

        #region 암호화

        [Test]
        public void Crypto_RoundTrip_AndIsDeterministic()
        {
            var plain = new byte[] { 1, 2, 3, 4, 5 };

            var encrypted = _crypto.Encrypt(plain);

            CollectionAssert.AreEqual(plain, _crypto.Decrypt(encrypted));
            CollectionAssert.AreEqual(encrypted, _crypto.Encrypt(plain), "같은 내용은 같은 파일이 되어 git 변경으로 잡히지 않습니다.");
            CollectionAssert.AreNotEqual(plain, encrypted);
        }

        [Test]
        public void Crypto_TamperedOrWrongKey_Throws()
        {
            var encrypted = _crypto.Encrypt(new byte[] { 1, 2, 3 });
            var tampered = (byte[])encrypted.Clone();
            tampered[20] ^= 1;
            var otherKey = (byte[])_key.Clone();
            otherKey[40] ^= 1;

            Assert.Throws<InvalidDataException>(() => _crypto.Decrypt(tampered));
            Assert.Throws<InvalidDataException>(() => new GameDataCrypto(otherKey).Decrypt(encrypted));
            Assert.Throws<InvalidDataException>(() => _crypto.Decrypt(new byte[10]));
        }

        [Test]
        public void Crypto_WrongKeySize_Throws()
        {
            Assert.Throws<ArgumentException>(() => new GameDataCrypto(new byte[16]));
        }

        #endregion

        #region 시트에서 런타임까지

        [Test]
        public void SheetToRuntime_LoadsRowsByKey()
        {
            var sheet = new TestSheets().ParseItem();
            var data = new TestGameData(new FakeAssetLoader(), _key, sheet.SchemaHash);

            data.LoadTable(data.Item, Encrypt(sheet));

            Assert.IsTrue(data.Item.IsLoaded);
            Assert.AreEqual(2, data.Item.Count);
            var sword = data.Item.Get(1001);
            Assert.AreEqual("검", sword.Name);
            CollectionAssert.AreEqual(new[] { 10, 20 }, sword.Attack);
            Assert.AreEqual(TestItemGrade.Rare, sword.Grade);
            Assert.AreEqual(1.5f, sword.Price);
            Assert.IsTrue(sword.LoadedHookCalled, "행의 partial void OnLoaded가 호출됩니다.");
            Assert.AreEqual(1, data.Item.LoadedCount, "테이블의 OnLoaded가 한 번 호출됩니다.");

            Assert.IsTrue(data.Item.TryGet(1003, out var shield));
            CollectionAssert.AreEqual(new[] { 5 }, shield.Attack);
            Assert.IsFalse(data.Item.Contains(1002), "키가 _로 시작한 행은 없습니다.");
            Assert.AreEqual(1001, data.Item.All[0].Id, "All은 시트 순서입니다.");
        }

        [Test]
        public void Get_MissingKey_Throws()
        {
            var sheet = new TestSheets().ParseItem();
            var data = new TestGameData(new FakeAssetLoader(), _key, sheet.SchemaHash);

            Assert.Throws<InvalidOperationException>(() => data.Item.Get(1001), "불러오기 전에는 안내 메시지로 알려 줍니다.");

            data.LoadTable(data.Item, Encrypt(sheet));
            Assert.Throws<KeyNotFoundException>(() => data.Item.Get(9999));
        }

        [Test]
        public void SchemaMismatch_IsRejected()
        {
            var sheet = new TestSheets().ParseItem();
            var data = new TestGameData(new FakeAssetLoader(), _key, sheet.SchemaHash + 1);

            var error = Assert.Throws<InvalidDataException>(() => data.LoadTable(data.Item, Encrypt(sheet)));

            StringAssert.Contains("열 구성", error.Message);
            Assert.IsFalse(data.Item.IsLoaded);
        }

        #endregion

        private byte[] Encrypt(SheetTable sheet) => _crypto.Encrypt(new GameDataBinaryBuilder().Build(sheet));
    }
}
