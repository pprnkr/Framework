using System;
using System.Collections.Generic;
using Nyonyo.Framework.Save;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Auth
{
    /// <summary>
    /// 에디터에서 로그인 흐름을 확인하는 가짜 로그인입니다. 서버 없이 모든 로그인 방식을 흉내 냅니다.
    /// </summary>
    /// <remarks>
    /// <para>게스트 로그인은 바로 성공합니다. 구글, 애플 로그인과 계정 연결은 화면에 창을 띄우고, 성공, 취소, 이미 다른 계정에 연결됨, 네트워크 오류 중에서 결과를 고르게 합니다.</para>
    /// <para>
    /// 어느 구글, 애플 계정이 어느 게임 계정에 연결되었는지 기억합니다. 게스트에 구글을 연결하고 로그아웃한 뒤 구글로 로그인하면 원래 계정으로 돌아옵니다.
    /// SaveService를 넘기면 전역 키 <c>auth.fake</c>에 저장해서 Play를 다시 눌러도 이어집니다.
    /// </para>
    /// </remarks>
    public sealed class FakeAuthService : AuthServiceBase
    {
        public const string SaveKey = "auth.fake";

        private readonly LocalAuthStore _store;
        private readonly GameObject _root;
        private AwaitableCompletionSource<AuthResult> _pending;

        /// <param name="parent">가짜 로그인 창을 붙일 부모입니다. 보통 <see cref="Bootstrapper.Root"/>를 넘깁니다.</param>
        /// <param name="save">넘기면 가짜 계정 정보를 저장합니다.</param>
        /// <param name="signInAsGuestOnStart">true면 시작할 때 로그인된 계정이 없을 경우 게스트로 로그인합니다.</param>
        public FakeAuthService(Transform parent, SaveService save = null, bool signInAsGuestOnStart = true)
            : base(signInAsGuestOnStart)
        {
            _store = new LocalAuthStore(save, SaveKey);

            // 자동 테스트에서는 parent가 null이고 Play 모드가 아니므로 창을 만들지 않습니다.
            if (parent != null || Application.isPlaying)
            {
                _root = new GameObject("[Fake Auth]");
                if (parent != null)
                    _root.transform.SetParent(parent, false);
                else
                    Object.DontDestroyOnLoad(_root);
                _root.AddComponent<FakeAuthView>().Service = this;
            }
        }

        /// <summary>가짜 로그인 창이 떠 있으면 true입니다.</summary>
        internal bool IsPresenting => _pending != null;

        internal AuthProvider PresentingProvider { get; private set; }

        /// <summary>떠 있는 창이 계정 연결이면 true, 로그인이면 false입니다.</summary>
        internal bool IsPresentingLink { get; private set; }

        public override bool IsProviderSupported(AuthProvider provider) => true;

        /// <summary>
        /// 떠 있는 가짜 로그인 창의 결과를 정합니다. 창의 버튼이 호출하고, 자동 테스트에서도 직접 호출합니다.
        /// </summary>
        internal void Respond(AuthResult result)
        {
            if (_pending == null)
                return;

            var source = _pending;
            _pending = null;
            source.SetResult(result);
        }

        public override void Dispose()
        {
            Respond(AuthResult.Cancelled);
            base.Dispose();

            if (_root == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(_root);
            else
                Object.DestroyImmediate(_root);
        }

        protected override Awaitable InitializeCoreAsync()
        {
            if (!string.IsNullOrEmpty(_store.Data.userId))
                SetUser(CreateUser(_store.Data.userId));
            return base.InitializeCoreAsync();
        }

        protected override Awaitable<AuthResult> SignInCoreAsync(AuthProvider provider)
        {
            if (provider == AuthProvider.Guest)
            {
                ChangeUser(NewUserId("guest"));
                return FromResult(AuthResult.Success);
            }

            return SignInWithAccountAsync(provider);
        }

        protected override async Awaitable<AuthResult> LinkCoreAsync(AuthProvider provider)
        {
            var result = await PresentAsync(provider, true);
            if (result != AuthResult.Success)
                return result;

            // 연결된 계정이 이미 다른 게임 계정에 있으면 실제 서버처럼 거절합니다.
            if (FindAccount(provider) != null)
                return AuthResult.AlreadyInUse;

            _store.Data.accounts.Add(new LocalAuthAccount { provider = provider, userId = CurrentUser.UserId });
            ChangeUser(CurrentUser.UserId);
            return AuthResult.Success;
        }

        protected override void SignOutCore()
        {
            _store.Data.userId = "";
            _store.Save();
        }

        protected override Awaitable<AuthResult> DeleteAccountCoreAsync()
        {
            var userId = CurrentUser.UserId;
            _store.Data.accounts.RemoveAll(account => account.userId == userId);
            SignOutCore();
            return FromResult(AuthResult.Success);
        }

        protected override Awaitable<string> GetIdTokenCoreAsync(bool forceRefresh) =>
            FromResult("fake-token-" + CurrentUser.UserId);

        private async Awaitable<AuthResult> SignInWithAccountAsync(AuthProvider provider)
        {
            var result = await PresentAsync(provider, false);
            if (result != AuthResult.Success)
                return result;

            // 연결된 게임 계정이 있으면 그 계정으로, 없으면 새 계정을 만듭니다.
            var account = FindAccount(provider);
            if (account == null)
            {
                account = new LocalAuthAccount { provider = provider, userId = NewUserId(provider.ToString().ToLowerInvariant()) };
                _store.Data.accounts.Add(account);
            }

            ChangeUser(account.userId);
            return AuthResult.Success;
        }

        private Awaitable<AuthResult> PresentAsync(AuthProvider provider, bool isLink)
        {
            // 서비스가 하나의 작업만 진행시키므로 창은 한 번에 하나만 뜹니다.
            _pending = new AwaitableCompletionSource<AuthResult>();
            PresentingProvider = provider;
            IsPresentingLink = isLink;
            return _pending.Awaitable;
        }

        private void ChangeUser(string userId)
        {
            _store.Data.userId = userId;
            _store.Save();
            SetUser(CreateUser(userId));
        }

        private AuthUser CreateUser(string userId)
        {
            var providers = new List<AuthProvider>();
            foreach (var account in _store.Data.accounts)
            {
                if (account.userId == userId)
                    providers.Add(account.provider);
            }

            if (providers.Count == 0)
                return new AuthUser(userId);
            return new AuthUser(userId, providers, "Fake User", "fake@example.com");
        }

        private LocalAuthAccount FindAccount(AuthProvider provider) =>
            _store.Data.accounts.Find(account => account.provider == provider);

        private string NewUserId(string prefix) => $"fake-{prefix}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
    }
}
