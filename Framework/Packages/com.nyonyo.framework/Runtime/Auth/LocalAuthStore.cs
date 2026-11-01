using System;
using System.Collections.Generic;
using Nyonyo.Framework.Save;

namespace Nyonyo.Framework.Auth
{
    /// <summary>
    /// 서버 없이 기기 안에서만 동작하는 로그인(<see cref="NullAuthService"/>, <see cref="FakeAuthService"/>)의 계정 정보를 저장합니다.
    /// SaveService가 없으면 메모리에만 두므로 앱을 다시 켜면 사라집니다.
    /// </summary>
    internal sealed class LocalAuthStore
    {
        private readonly SaveService _save;
        private readonly string _key;

        public LocalAuthStore(SaveService save, string key)
        {
            _save = save;
            _key = key;
            if (_save == null || !_save.TryLoad(key, out LocalAuthData data, SaveScope.Global) || data == null)
                data = new LocalAuthData();
            Data = data;
        }

        public LocalAuthData Data { get; }

        public void Save() => _save?.Save(_key, Data, SaveScope.Global);
    }

    [Serializable]
    internal sealed class LocalAuthData
    {
        /// <summary>로그인한 계정 ID입니다. 로그아웃 상태면 빈 문자열입니다.</summary>
        public string userId = "";

        /// <summary>구글, 애플 계정이 어느 게임 계정에 연결되어 있는지입니다. 서버의 계정 목록을 흉내 냅니다(FakeAuthService만 씀).</summary>
        public List<LocalAuthAccount> accounts = new();
    }

    [Serializable]
    internal sealed class LocalAuthAccount
    {
        public AuthProvider provider;
        public string userId = "";
    }
}
