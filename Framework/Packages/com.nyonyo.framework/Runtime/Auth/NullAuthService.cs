using System;
using Nyonyo.Framework.Save;
using UnityEngine;

namespace Nyonyo.Framework.Auth
{
    /// <summary>
    /// 로그인 서버가 없는 플랫폼(WebGL, PC)용입니다. 게스트 로그인만 되고, 계정 ID는 이 기기에만 저장합니다.
    /// 구글, 애플 로그인과 계정 연결은 <see cref="AuthResult.NotSupported"/>입니다.
    /// </summary>
    /// <remarks>
    /// <para>게임 코드는 다른 플랫폼과 똑같이 <see cref="AuthUser.UserId"/>를 쓸 수 있습니다. 다만 서버가 확인할 수 있는 토큰은 없어서 <see cref="IAuthService.GetIdTokenAsync"/>는 null입니다.</para>
    /// <para>계정 ID는 SaveService의 전역 키 <c>auth.local</c>에 저장합니다. SaveService를 넘기지 않으면 실행할 때마다 새 게스트가 됩니다.</para>
    /// </remarks>
    public sealed class NullAuthService : AuthServiceBase
    {
        public const string SaveKey = "auth.local";

        private readonly LocalAuthStore _store;

        /// <param name="save">넘기면 게스트 계정 ID를 저장해서 다음 실행 때도 같은 계정을 씁니다.</param>
        /// <param name="signInAsGuestOnStart">true면 시작할 때 게스트로 로그인합니다.</param>
        public NullAuthService(SaveService save = null, bool signInAsGuestOnStart = true)
            : base(signInAsGuestOnStart)
        {
            _store = new LocalAuthStore(save, SaveKey);
        }

        public override bool IsProviderSupported(AuthProvider provider) => provider == AuthProvider.Guest;

        protected override Awaitable InitializeCoreAsync()
        {
            if (!string.IsNullOrEmpty(_store.Data.userId))
                SetUser(new AuthUser(_store.Data.userId));
            return base.InitializeCoreAsync();
        }

        protected override Awaitable<AuthResult> SignInCoreAsync(AuthProvider provider)
        {
            _store.Data.userId = "local-" + Guid.NewGuid().ToString("N");
            _store.Save();
            SetUser(new AuthUser(_store.Data.userId));
            return FromResult(AuthResult.Success);
        }

        protected override Awaitable<AuthResult> LinkCoreAsync(AuthProvider provider) => FromResult(AuthResult.NotSupported);

        protected override void SignOutCore() => ClearUser();

        protected override Awaitable<AuthResult> DeleteAccountCoreAsync()
        {
            ClearUser();
            return FromResult(AuthResult.Success);
        }

        protected override Awaitable<string> GetIdTokenCoreAsync(bool forceRefresh) => FromResult<string>(null);

        private void ClearUser()
        {
            _store.Data.userId = "";
            _store.Save();
        }
    }
}
