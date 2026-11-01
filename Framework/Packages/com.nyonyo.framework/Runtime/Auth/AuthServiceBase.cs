using System;
using System.Threading;
using UnityEngine;

namespace Nyonyo.Framework.Auth
{
    /// <summary>
    /// 로그인 방식과 상관없는 공통 규칙(시작할 때 게스트 로그인, 한 번에 하나의 작업, 시작 대기, 이벤트)을 처리합니다.
    /// 로그인 모듈(Firebase 등)은 이 클래스를 상속해 실제 로그인만 구현합니다.
    /// </summary>
    /// <remarks>
    /// 하위 클래스는 로그인이나 계정 연결에 성공하면 <see cref="SetUser"/>로 새 계정 정보를 넘깁니다.
    /// 로그아웃과 계정 삭제 뒤의 <c>SetUser(null)</c>은 이 클래스가 합니다.
    /// </remarks>
    public abstract class AuthServiceBase : IAuthService, IInitializable, IDisposable
    {
        private readonly bool _signInAsGuestOnStart;
        private readonly CancellationTokenSource _cts = new();
        private AuthUser _currentUser;
        private bool _isBusy;
        private bool _isStarted;
        private bool _isDisposed;

        /// <param name="signInAsGuestOnStart">true면 시작할 때 로그인된 계정이 없을 경우 게스트로 로그인합니다.</param>
        protected AuthServiceBase(bool signInAsGuestOnStart)
        {
            _signInAsGuestOnStart = signInAsGuestOnStart;
        }

        public event Action Initialized;
        public event Action<AuthUser> UserChanged;

        public bool IsInitialized { get; private set; }

        public AuthUser CurrentUser => _currentUser;

        public bool IsSignedIn => _currentUser != null;

        public bool IsBusy => _isBusy;

        /// <summary>서비스가 정리되면 취소되는 토큰입니다. 하위 클래스가 기다리는 작업에 넘깁니다.</summary>
        protected CancellationToken DisposeToken => _cts.Token;

        public abstract bool IsProviderSupported(AuthProvider provider);

        /// <summary>
        /// 서비스를 등록하면 초기화 단계에서 불립니다. 게임 시작을 막지 않고 뒤에서 진행하며, 끝나면 <see cref="Initialized"/>가 발행됩니다.
        /// </summary>
        public void Initialize()
        {
            if (_isStarted)
                return;

            _isStarted = true;
            RunAsync(StartAsync());
        }

        /// <summary>
        /// 시작이 끝날 때까지 기다립니다. 서비스를 ServiceRegistry에 등록하지 않고 직접 만들었다면 <see cref="Initialize"/>를 먼저 부르세요.
        /// </summary>
        public async Awaitable WaitUntilInitializedAsync()
        {
            while (!IsInitialized)
                await Awaitable.NextFrameAsync(_cts.Token);
        }

        public Awaitable<AuthResult> SignInAsync(AuthProvider provider) => RunOperationAsync(() => SignInIfNeededAsync(provider));

        public Awaitable<AuthResult> LinkAsync(AuthProvider provider) => RunOperationAsync(() => LinkIfNeededAsync(provider));

        public Awaitable<AuthResult> DeleteAccountAsync() => RunOperationAsync(DeleteIfSignedInAsync);

        public void SignOut()
        {
            if (_isBusy)
            {
                Debug.LogWarning("[Nyonyo Framework] 로그인 작업이 진행 중이라 로그아웃하지 않았습니다. 작업이 끝난 뒤 다시 시도하세요.");
                return;
            }
            if (!IsSignedIn)
                return;

            SignOutCore();
            SetUser(null);
        }

        public async Awaitable<string> GetIdTokenAsync(bool forceRefresh = false)
        {
            await WaitUntilInitializedAsync();
            if (!IsSignedIn)
                return null;

            try
            {
                return await GetIdTokenCoreAsync(forceRefresh);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public virtual void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _cts.Cancel();
        }

        #region 하위 클래스가 구현

        /// <summary>
        /// 저장된 로그인을 확인하는 등 시작 작업을 합니다. 로그인되어 있으면 <see cref="SetUser"/>를 부르세요.
        /// 예외를 던져도 서비스는 시작된 것으로 처리되므로, 이후 작업에서 실패를 돌려주세요.
        /// </summary>
        protected virtual Awaitable InitializeCoreAsync()
        {
            var source = new AwaitableCompletionSource();
            source.SetResult();
            return source.Awaitable;
        }

        /// <summary>지원하는 방식으로 로그인합니다. 게스트는 로그인되지 않은 상태에서만 불립니다.</summary>
        protected abstract Awaitable<AuthResult> SignInCoreAsync(AuthProvider provider);

        /// <summary>로그인된 상태에서, 아직 연결되지 않은 구글이나 애플 계정을 연결합니다.</summary>
        protected abstract Awaitable<AuthResult> LinkCoreAsync(AuthProvider provider);

        protected abstract void SignOutCore();

        /// <summary>로그인된 계정을 삭제합니다. 성공을 돌려주면 이 클래스가 로그아웃 상태로 바꿉니다.</summary>
        protected abstract Awaitable<AuthResult> DeleteAccountCoreAsync();

        protected abstract Awaitable<string> GetIdTokenCoreAsync(bool forceRefresh);

        #endregion

        /// <summary>계정 정보를 바꿉니다. 값이 달라졌을 때만 <see cref="UserChanged"/>를 발행합니다.</summary>
        protected void SetUser(AuthUser user)
        {
            var isSame = _currentUser == null ? user == null : _currentUser.HasSameValues(user);
            _currentUser = user;
            if (isSame)
                return;

            try
            {
                UserChanged?.Invoke(user);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>이미 끝난 결과를 돌려줍니다. 기다릴 것이 없는 구현에서 씁니다.</summary>
        protected Awaitable<T> FromResult<T>(T value)
        {
            var source = new AwaitableCompletionSource<T>();
            source.SetResult(value);
            return source.Awaitable;
        }

        private async Awaitable StartAsync()
        {
            try
            {
                await InitializeCoreAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (_signInAsGuestOnStart && !IsSignedIn && IsProviderSupported(AuthProvider.Guest))
                await SignInAsGuestOnStartAsync();

            if (_isDisposed)
                return;

            IsInitialized = true;
            try
            {
                Initialized?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private async Awaitable SignInAsGuestOnStartAsync()
        {
            var result = AuthResult.Failed;
            try
            {
                result = await SignInCoreAsync(AuthProvider.Guest);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (result != AuthResult.Success)
                Debug.LogWarning($"[Nyonyo Framework] 시작할 때 게스트로 로그인하지 못했습니다({result}). 네트워크가 연결된 뒤 SignInAsync(AuthProvider.Guest)로 다시 시도하세요.");
        }

        private async Awaitable<AuthResult> RunOperationAsync(Func<Awaitable<AuthResult>> operation)
        {
            if (_isBusy)
                return AuthResult.Busy;

            _isBusy = true;
            try
            {
                await WaitUntilInitializedAsync();
                return await operation();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return AuthResult.Failed;
            }
            finally
            {
                _isBusy = false;
            }
        }

        private Awaitable<AuthResult> SignInIfNeededAsync(AuthProvider provider)
        {
            if (!IsProviderSupported(provider))
                return FromResult(AuthResult.NotSupported);
            if (provider == AuthProvider.Guest && IsSignedIn)
                return FromResult(AuthResult.Success);

            return SignInCoreAsync(provider);
        }

        private Awaitable<AuthResult> LinkIfNeededAsync(AuthProvider provider)
        {
            if (provider == AuthProvider.Guest)
            {
                Debug.LogError("[Nyonyo Framework] 게스트는 연결할 수 있는 계정이 아닙니다. LinkAsync에는 Google이나 Apple을 넘기세요.");
                return FromResult(AuthResult.NotSupported);
            }
            if (!IsProviderSupported(provider))
                return FromResult(AuthResult.NotSupported);
            if (!IsSignedIn)
                return FromResult(AuthResult.NotSignedIn);
            if (_currentUser.IsLinked(provider))
                return FromResult(AuthResult.Success);

            return LinkCoreAsync(provider);
        }

        private async Awaitable<AuthResult> DeleteIfSignedInAsync()
        {
            if (!IsSignedIn)
                return AuthResult.NotSignedIn;

            var result = await DeleteAccountCoreAsync();
            if (result == AuthResult.Success)
                SetUser(null);
            return result;
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
    }
}
