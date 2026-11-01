using System;
using System.Collections.Generic;

namespace Nyonyo.Framework.Auth
{
    /// <summary>
    /// 로그인한 계정의 정보입니다. 값이 바뀌면 새 객체로 바뀌고 <see cref="IAuthService.UserChanged"/>가 발행됩니다.
    /// </summary>
    public sealed class AuthUser
    {
        private readonly List<AuthProvider> _linkedProviders;

        /// <param name="userId">계정을 구분하는 고유 ID입니다.</param>
        /// <param name="linkedProviders">연결된 구글, 애플 계정입니다. <see cref="AuthProvider.Guest"/>는 무시합니다.</param>
        public AuthUser(string userId, IEnumerable<AuthProvider> linkedProviders = null, string displayName = null, string email = null)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("계정 ID가 비어 있습니다.", nameof(userId));

            UserId = userId;
            DisplayName = displayName;
            Email = email;
            _linkedProviders = new List<AuthProvider>();
            if (linkedProviders == null)
                return;

            foreach (var provider in linkedProviders)
            {
                if (provider != AuthProvider.Guest && !_linkedProviders.Contains(provider))
                    _linkedProviders.Add(provider);
            }
            _linkedProviders.Sort();
        }

        /// <summary>계정을 구분하는 고유 ID입니다. 서버에 저장하는 데이터의 키로 씁니다.</summary>
        public string UserId { get; }

        /// <summary>
        /// 구글이나 애플 계정이 연결되지 않은 게스트 계정이면 true입니다.
        /// 앱을 지우거나 기기를 바꾸면 잃어버리므로, 진행이 쌓이면 계정 연결을 권하세요.
        /// </summary>
        public bool IsGuest => _linkedProviders.Count == 0;

        /// <summary>연결된 구글, 애플 계정입니다. 게스트면 비어 있습니다.</summary>
        public IReadOnlyList<AuthProvider> LinkedProviders => _linkedProviders;

        /// <summary>연결된 계정의 이름입니다. 없거나 공개하지 않았으면 null입니다(애플은 처음 연결할 때만 줌).</summary>
        public string DisplayName { get; }

        /// <summary>연결된 계정의 이메일입니다. 없거나 공개하지 않았으면 null입니다.</summary>
        public string Email { get; }

        public bool IsLinked(AuthProvider provider) => _linkedProviders.Contains(provider);

        public override string ToString() =>
            IsGuest ? $"{UserId} (게스트)" : $"{UserId} ({string.Join(", ", _linkedProviders)})";

        internal bool HasSameValues(AuthUser other)
        {
            if (other == null || other.UserId != UserId || other.DisplayName != DisplayName || other.Email != Email)
                return false;
            if (other._linkedProviders.Count != _linkedProviders.Count)
                return false;

            for (var i = 0; i < _linkedProviders.Count; i++)
            {
                if (other._linkedProviders[i] != _linkedProviders[i])
                    return false;
            }
            return true;
        }
    }
}
