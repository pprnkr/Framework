using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Nyonyo.Framework.Save
{
    /// <summary>
    /// 게임 데이터를 키 단위로 저장하고 불러옵니다. 세이브 슬롯과 전역 데이터(설정)를 구분합니다.
    /// </summary>
    /// <remarks>
    /// <see cref="Save{T}"/>는 호출 즉시 저장소에 기록합니다. WebGL은 게임 종료 시점을 알 수 없어서,
    /// 종료할 때 한꺼번에 저장하는 방식에 기대지 않습니다.
    /// 기본 저장소는 WebGL이면 PlayerPrefs, 그 밖에는 파일(<c>persistentDataPath/Saves</c>)입니다.
    /// 키에는 영문, 숫자, <c>_ . -</c>만 쓸 수 있고, <c>__</c>로 시작하는 키는 내부용입니다.
    /// </remarks>
    public sealed class SaveService
    {
        const string MetaKey = "__meta";

        static readonly Regex s_keyPattern = new("^[A-Za-z0-9_.-]+$");

        [Serializable]
        sealed class SlotMeta
        {
            public long lastSavedUtcTicks;
            public List<string> keys = new();
        }

        readonly ISaveStorage _storage;
        readonly ISaveSerializer _serializer;
        int _currentSlot;

        /// <summary>
        /// 서비스로 등록된 SaveService입니다. <c>Services.Get&lt;SaveService&gt;()</c>와 같습니다.
        /// </summary>
        public static SaveService Instance => Services.Get<SaveService>();

        /// <param name="storage">저장소입니다. null이면 <see cref="CreateDefaultStorage"/>를 씁니다.</param>
        /// <param name="serializer">직렬화 방식입니다. null이면 <see cref="JsonSaveSerializer"/>를 씁니다.</param>
        public SaveService(ISaveStorage storage = null, ISaveSerializer serializer = null)
        {
            _storage = storage ?? CreateDefaultStorage();
            _serializer = serializer ?? new JsonSaveSerializer();
        }

        /// <summary>
        /// <see cref="SaveScope.Slot"/> 데이터를 저장하고 불러올 슬롯 번호(0부터)입니다.
        /// </summary>
        public int CurrentSlot
        {
            get => _currentSlot;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "슬롯 번호는 0 이상이어야 합니다.");
                _currentSlot = value;
            }
        }

        /// <summary>
        /// 플랫폼에 맞는 기본 저장소입니다. WebGL이면 <see cref="PlayerPrefsSaveStorage"/>, 그 밖에는 <see cref="FileSaveStorage"/>입니다.
        /// </summary>
        public static ISaveStorage CreateDefaultStorage()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new PlayerPrefsSaveStorage();
#else
            return new FileSaveStorage();
#endif
        }

        /// <summary>
        /// 데이터를 저장합니다. <see cref="IVersionedSaveData"/>라면 최신 버전 번호를 붙여서 저장합니다.
        /// </summary>
        public void Save<T>(string key, T data, SaveScope scope = SaveScope.Slot)
        {
            ValidateKey(key);
            object boxed = data;
            if (boxed is IVersionedSaveData versioned)
                versioned.Version = versioned.LatestVersion;

            _storage.Write(FullKey(key, scope), _serializer.Serialize(boxed));
            if (scope == SaveScope.Slot)
                UpdateMeta(_currentSlot, key, true);
        }

        /// <summary>
        /// 데이터를 불러옵니다. 없거나 읽지 못하면 <paramref name="fallback"/>을 돌려줍니다.
        /// </summary>
        public T Load<T>(string key, T fallback = default, SaveScope scope = SaveScope.Slot) =>
            TryLoad(key, out T data, scope) ? data : fallback;

        /// <summary>
        /// 데이터를 불러옵니다. 예전 버전 데이터면 <see cref="IVersionedSaveData.Migrate"/>로 고친 뒤 다시 저장합니다.
        /// 데이터가 손상돼서 읽지 못하면 에러를 남기고 false를 돌려줍니다.
        /// </summary>
        public bool TryLoad<T>(string key, out T data, SaveScope scope = SaveScope.Slot)
        {
            ValidateKey(key);
            data = default;
            var fullKey = FullKey(key, scope);
            if (!_storage.TryRead(fullKey, out var text))
                return false;

            try
            {
                data = _serializer.Deserialize<T>(text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nyonyo Framework] 저장 데이터 {fullKey}을(를) 읽지 못했습니다. 데이터가 손상됐을 수 있습니다. {e.Message}");
                data = default;
                return false;
            }

            if (data == null)
                return false;

            object boxed = data;
            if (boxed is not IVersionedSaveData versioned)
                return true;

            try
            {
                if (!Migrate(versioned, fullKey))
                    return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nyonyo Framework] 저장 데이터 {fullKey}의 버전 변환에 실패했습니다.");
                Debug.LogException(e);
                data = default;
                return false;
            }

            data = (T)boxed;
            Save(key, data, scope);
            return true;
        }

        public bool Exists(string key, SaveScope scope = SaveScope.Slot)
        {
            ValidateKey(key);
            return _storage.Exists(FullKey(key, scope));
        }

        public void Delete(string key, SaveScope scope = SaveScope.Slot)
        {
            ValidateKey(key);
            _storage.Delete(FullKey(key, scope));
            if (scope == SaveScope.Slot)
                UpdateMeta(_currentSlot, key, false);
        }

        /// <summary>
        /// 슬롯에 저장된 데이터가 하나라도 있는지 확인합니다.
        /// </summary>
        public bool SlotExists(int slot) => _storage.Exists(SlotMetaKey(slot));

        public bool TryGetSlotInfo(int slot, out SaveSlotInfo info)
        {
            var meta = ReadMeta(slot);
            if (meta == null)
            {
                info = default;
                return false;
            }

            info = new SaveSlotInfo(slot, new DateTime(meta.lastSavedUtcTicks, DateTimeKind.Utc), meta.keys.Count);
            return true;
        }

        /// <summary>
        /// 슬롯의 데이터를 모두 지웁니다. 전역 데이터와 다른 슬롯은 그대로 둡니다.
        /// </summary>
        public void DeleteSlot(int slot)
        {
            var meta = ReadMeta(slot);
            if (meta == null)
                return;

            foreach (var key in meta.keys)
                _storage.Delete(SlotKey(slot, key));
            _storage.Delete(SlotMetaKey(slot));
        }

        // 슬롯에 어떤 키가 있는지 따로 기록해 둡니다. PlayerPrefs는 키 목록을 조회할 수 없기 때문입니다.
        void UpdateMeta(int slot, string key, bool saved)
        {
            var meta = ReadMeta(slot) ?? new SlotMeta();
            if (saved)
            {
                if (!meta.keys.Contains(key))
                    meta.keys.Add(key);
                meta.lastSavedUtcTicks = DateTime.UtcNow.Ticks;
            }
            else
            {
                meta.keys.Remove(key);
            }

            if (meta.keys.Count == 0)
                _storage.Delete(SlotMetaKey(slot));
            else
                _storage.Write(SlotMetaKey(slot), _serializer.Serialize(meta));
        }

        SlotMeta ReadMeta(int slot)
        {
            if (!_storage.TryRead(SlotMetaKey(slot), out var text))
                return null;

            try
            {
                return _serializer.Deserialize<SlotMeta>(text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nyonyo Framework] 슬롯 {slot} 정보를 읽지 못했습니다. {e.Message}");
                return null;
            }
        }

        static bool Migrate(IVersionedSaveData data, string fullKey)
        {
            var saved = data.Version;
            var latest = data.LatestVersion;
            if (saved == latest)
                return false;

            if (saved > latest)
            {
                Debug.LogWarning(
                    $"[Nyonyo Framework] 저장 데이터 {fullKey}의 버전({saved})이 코드 버전({latest})보다 높습니다. 그대로 불러옵니다.");
                return false;
            }

            data.Migrate(saved);
            data.Version = latest;
            Debug.Log($"[Nyonyo Framework] 저장 데이터 {fullKey}을(를) 버전 {saved}에서 {latest}(으)로 바꿨습니다.");
            return true;
        }

        string FullKey(string key, SaveScope scope) => scope == SaveScope.Global ? $"global.{key}" : SlotKey(_currentSlot, key);

        static string SlotKey(int slot, string key) => $"slot{slot}.{key}";

        static string SlotMetaKey(int slot) => SlotKey(slot, MetaKey);

        static void ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key) || !s_keyPattern.IsMatch(key))
                throw new ArgumentException($"저장 키 '{key}'에는 영문, 숫자, _ . -만 쓸 수 있습니다.", nameof(key));
            if (key.StartsWith("__", StringComparison.Ordinal))
                throw new ArgumentException($"저장 키 '{key}': __로 시작하는 키는 내부용입니다.", nameof(key));
        }
    }
}
