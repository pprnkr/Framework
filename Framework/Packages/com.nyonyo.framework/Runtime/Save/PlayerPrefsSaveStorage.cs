using UnityEngine;

namespace Nyonyo.Framework.Save
{
    /// <summary>
    /// PlayerPrefs에 저장합니다. WebGL의 기본 저장소입니다(브라우저 IndexedDB에 저장됨).
    /// </summary>
    /// <remarks>
    /// 쓸 때마다 <c>PlayerPrefs.Save()</c>를 호출해서 바로 기록합니다. WebGL은 게임 종료 시점을 알 수 없기 때문입니다.
    /// WebGL의 PlayerPrefs는 전체 1MB 정도로 제한되므로, 큰 데이터는 저장하지 마세요.
    /// </remarks>
    public sealed class PlayerPrefsSaveStorage : ISaveStorage
    {
        private readonly string _prefix;

        public PlayerPrefsSaveStorage(string prefix = "nyonyo.save.")
        {
            _prefix = prefix;
        }

        public bool Exists(string key) => PlayerPrefs.HasKey(_prefix + key);

        public bool TryRead(string key, out string data)
        {
            if (!PlayerPrefs.HasKey(_prefix + key))
            {
                data = null;
                return false;
            }

            data = PlayerPrefs.GetString(_prefix + key);
            return true;
        }

        public void Write(string key, string data)
        {
            PlayerPrefs.SetString(_prefix + key, data);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(_prefix + key);
            PlayerPrefs.Save();
        }
    }
}
