// Firebase Auth 패키지가 있을 때만 컴파일합니다. WebGL에서는 Firebase DLL이 꺼져 있어서 뺍니다.
#if NYONYO_FIREBASE_AUTH && !UNITY_WEBGL
using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Nyonyo.Framework.Auth;
using UnityEngine;
using AuthResult = Nyonyo.Framework.Auth.AuthResult;

namespace Nyonyo.Framework.Firebase
{
    /// <summary>
    /// Firebase Authentication으로 구현한 로그인 서비스입니다. 보통 <see cref="FirebaseSettings.CreateAuthService"/>로 만듭니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 시작하면 Firebase 의존성 확인(Android는 Google Play 서비스) → 저장된 로그인 확인 → 없으면 게스트(익명) 로그인 순서로 진행합니다.
    /// Firebase는 로그인을 기기에 저장하므로 다음 실행 때 같은 계정이 이어집니다.
    /// </para>
    /// <para>구글은 Android에서, 애플은 iOS에서만 쓸 수 있습니다. 에디터(데스크톱 SDK)에서는 게스트 로그인만 됩니다.</para>
    /// <para>
    /// 계정 연결이 <see cref="AuthResult.AlreadyInUse"/>로 끝나면 그 계정 정보를 잠시 기억해 둡니다.
    /// 바로 이어서 같은 방식으로 <see cref="IAuthService.SignInAsync"/>를 부르면 로그인 창을 다시 띄우지 않고 그 계정으로 전환합니다.
    /// </para>
    /// </remarks>
    public sealed class FirebaseAuthService : AuthServiceBase
    {
        private const string GoogleProviderId = "google.com";
        private const string AppleProviderId = "apple.com";

        private readonly FirebaseSettings _settings;
        private readonly GoogleSignInAndroid _google;
        private readonly AppleSignInIOS _apple = new();
        private FirebaseAuth _auth;
        private Credential _conflictCredential;
        private AuthProvider _conflictProvider;

        public FirebaseAuthService(FirebaseSettings settings)
            : base(settings != null && settings.SignInAsGuestOnStart)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "FirebaseSettings가 없습니다. Tools > Nyonyo Framework > Settings > Firebase Settings로 만들어 Installer에 연결하세요.");

            _settings = settings;
            _google = new GoogleSignInAndroid(settings.GoogleWebClientId);
        }

        public override bool IsProviderSupported(AuthProvider provider)
        {
            if (provider == AuthProvider.Google)
                return _settings.GoogleSignIn && Application.platform == RuntimePlatform.Android;
            if (provider == AuthProvider.Apple)
                return _settings.AppleSignIn && Application.platform == RuntimePlatform.IPhonePlayer;
            return true;
        }

        public override void Dispose()
        {
            if (_auth != null)
                _auth.StateChanged -= OnStateChanged;
            ForgetConflict();
            base.Dispose();
        }

        protected override async Awaitable InitializeCoreAsync()
        {
            var status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
            {
                Debug.LogError($"[Nyonyo Framework] Firebase를 시작하지 못했습니다({status}). Android는 Google Play 서비스가 설치되어 있어야 합니다. 로그인 없이 진행합니다.");
                return;
            }

            _auth = FirebaseAuth.DefaultInstance;
            _auth.StateChanged += OnStateChanged;
            SetUser(ToAuthUser(_auth.CurrentUser));
        }

        protected override async Awaitable<AuthResult> SignInCoreAsync(AuthProvider provider)
        {
            if (_auth == null)
                return NotStarted();

            try
            {
                if (provider == AuthProvider.Guest)
                {
                    var guest = await _auth.SignInAnonymouslyAsync();
                    SetUser(ToAuthUser(guest.User));
                    return AuthResult.Success;
                }

                var credential = TakeConflictCredential(provider);
                if (credential == null)
                {
                    var external = await RequestCredentialAsync(provider);
                    if (external.Result != AuthResult.Success)
                        return external.Result;
                    credential = external.Credential;
                }

                var signedIn = await _auth.SignInAndRetrieveDataWithCredentialAsync(credential);
                SetUser(ToAuthUser(signedIn.User));
                return AuthResult.Success;
            }
            catch (FirebaseAccountLinkException e)
            {
                return ToResult(e.ErrorCode, e, $"{provider} 로그인");
            }
            catch (FirebaseException e)
            {
                return ToResult(e.ErrorCode, e, $"{provider} 로그인");
            }
        }

        protected override async Awaitable<AuthResult> LinkCoreAsync(AuthProvider provider)
        {
            var user = _auth?.CurrentUser;
            if (user == null)
                return _auth == null ? NotStarted() : AuthResult.NotSignedIn;

            ForgetConflict();
            var external = await RequestCredentialAsync(provider);
            if (external.Result != AuthResult.Success)
                return external.Result;

            try
            {
                var linked = await user.LinkWithCredentialAsync(external.Credential);
                SetUser(ToAuthUser(linked.User));
                return AuthResult.Success;
            }
            catch (FirebaseAccountLinkException e)
            {
                // 이미 다른 계정에 연결된 구글/애플 계정이면, 전환할 때 창을 다시 띄우지 않도록 Firebase가 준 계정 정보를 기억합니다.
                var updated = e.UserInfo?.UpdatedCredential;
                if (updated != null && updated.IsValid())
                {
                    _conflictCredential = updated;
                    _conflictProvider = provider;
                }
                return ToResult(e.ErrorCode, e, $"{provider} 계정 연결");
            }
            catch (FirebaseException e)
            {
                return ToResult(e.ErrorCode, e, $"{provider} 계정 연결");
            }
        }

        protected override void SignOutCore()
        {
            ForgetConflict();
            _auth?.SignOut();
        }

        protected override async Awaitable<AuthResult> DeleteAccountCoreAsync()
        {
            var user = _auth?.CurrentUser;
            if (user == null)
                return _auth == null ? NotStarted() : AuthResult.NotSignedIn;

            try
            {
                await user.DeleteAsync();
                ForgetConflict();
                return AuthResult.Success;
            }
            catch (FirebaseException e)
            {
                return ToResult(e.ErrorCode, e, "계정 삭제");
            }
        }

        protected override async Awaitable<string> GetIdTokenCoreAsync(bool forceRefresh)
        {
            var user = _auth?.CurrentUser;
            if (user == null)
                return null;

            return await user.TokenAsync(forceRefresh);
        }

        private async Awaitable<ProviderCredential> RequestCredentialAsync(AuthProvider provider)
        {
            if (provider == AuthProvider.Google)
            {
                var google = await _google.SignInAsync();
                if (google.Result != AuthResult.Success)
                    return new ProviderCredential(google.Result, null);
                return new ProviderCredential(AuthResult.Success, GoogleAuthProvider.GetCredential(google.IdToken, null));
            }

            if (provider == AuthProvider.Apple)
            {
                var apple = await _apple.SignInAsync();
                if (apple.Result != AuthResult.Success)
                    return new ProviderCredential(apple.Result, null);
                return new ProviderCredential(AuthResult.Success, OAuthProvider.GetCredential(AppleProviderId, apple.IdToken, apple.RawNonce, null));
            }

            return new ProviderCredential(AuthResult.NotSupported, null);
        }

        private Credential TakeConflictCredential(AuthProvider provider)
        {
            if (_conflictCredential == null || _conflictProvider != provider)
            {
                ForgetConflict();
                return null;
            }

            var credential = _conflictCredential;
            _conflictCredential = null;
            return credential;
        }

        private void ForgetConflict()
        {
            _conflictCredential?.Dispose();
            _conflictCredential = null;
        }

        // Firebase는 토큰 만료나 다른 곳에서의 계정 삭제도 알려 줍니다. 다른 스레드에서 올 수 있어서 메인 스레드로 옮겨 처리합니다.
        private void OnStateChanged(object sender, EventArgs e) => RunAsync(RefreshUserAsync());

        private async Awaitable RefreshUserAsync()
        {
            await Awaitable.MainThreadAsync();
            if (_auth != null && !DisposeToken.IsCancellationRequested)
                SetUser(ToAuthUser(_auth.CurrentUser));
        }

        private AuthUser ToAuthUser(FirebaseUser user)
        {
            if (user == null || string.IsNullOrEmpty(user.UserId))
                return null;

            var providers = new List<AuthProvider>();
            foreach (var info in user.ProviderData)
            {
                if (info.ProviderId == GoogleProviderId)
                    providers.Add(AuthProvider.Google);
                else if (info.ProviderId == AppleProviderId)
                    providers.Add(AuthProvider.Apple);
            }

            var displayName = string.IsNullOrEmpty(user.DisplayName) ? null : user.DisplayName;
            var email = string.IsNullOrEmpty(user.Email) ? null : user.Email;
            return new AuthUser(user.UserId, providers, displayName, email);
        }

        private AuthResult ToResult(int errorCode, Exception exception, string action)
        {
            var error = (AuthError)errorCode;
            if (error == AuthError.CredentialAlreadyInUse || error == AuthError.AccountExistsWithDifferentCredentials)
                return AuthResult.AlreadyInUse;
            if (error == AuthError.RequiresRecentLogin)
                return AuthResult.RequiresRecentSignIn;
            if (error == AuthError.NetworkRequestFailed)
                return AuthResult.NetworkError;
            if (error == AuthError.Cancelled || error == AuthError.WebContextCancelled)
                return AuthResult.Cancelled;

            Debug.LogWarning($"[Nyonyo Framework] Firebase {action}에 실패했습니다({error}): {exception.Message}");
            return AuthResult.Failed;
        }

        private AuthResult NotStarted()
        {
            Debug.LogWarning("[Nyonyo Framework] Firebase가 시작되지 않아 로그인할 수 없습니다. 시작할 때 남은 에러 로그를 확인하세요.");
            return AuthResult.Failed;
        }

        // 기다리지 않고 실행합니다. 서비스가 정리되며 취소된 경우는 무시하고, 다른 예외는 로그로 남깁니다.
        private async void RunAsync(Awaitable work)
        {
            try
            {
                await work;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private readonly struct ProviderCredential
        {
            public ProviderCredential(AuthResult result, Credential credential)
            {
                Result = result;
                Credential = credential;
            }

            public AuthResult Result { get; }

            public Credential Credential { get; }
        }
    }
}
#endif
