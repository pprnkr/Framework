using System.Collections.Generic;
using Nyonyo.Framework.Save;

namespace Nyonyo.Framework.Editor.Tests
{
    /// <summary>
    /// 테스트용 저장소입니다. 메모리에만 저장합니다.
    /// </summary>
    public sealed class MemorySaveStorage : ISaveStorage
    {
        public readonly Dictionary<string, string> Data = new();

        public bool Exists(string key) => Data.ContainsKey(key);

        public bool TryRead(string key, out string data) => Data.TryGetValue(key, out data);

        public void Write(string key, string data) => Data[key] = data;

        public void Delete(string key) => Data.Remove(key);
    }
}
