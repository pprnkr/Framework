namespace Nyonyo.Framework.Auth
{
    /// <summary>
    /// 로그인, 계정 연결, 계정 삭제의 결과입니다.
    /// </summary>
    public enum AuthResult
    {
        Success,

        /// <summary>사용자가 로그인 창을 닫았습니다. 보통 아무것도 하지 않으면 됩니다.</summary>
        Cancelled,

        /// <summary>이 플랫폼에서 쓸 수 없는 로그인 방식입니다(예: iOS의 구글, WebGL의 애플).</summary>
        NotSupported,

        /// <summary>로그인된 계정이 없어서 할 수 없습니다. 먼저 로그인하세요.</summary>
        NotSignedIn,

        /// <summary>
        /// 이 구글/애플 계정은 이미 다른 게임 계정에 연결되어 있습니다(계정 연결에서만 나옴).
        /// 사용자에게 그 계정으로 전환할지 묻고, 전환하면 <see cref="IAuthService.SignInAsync"/>로 로그인하세요.
        /// 전환하면 지금 게스트 계정의 진행은 이어지지 않습니다.
        /// </summary>
        AlreadyInUse,

        /// <summary>
        /// 계정 삭제처럼 민감한 작업은 최근에 로그인한 상태여야 합니다.
        /// <see cref="IAuthService.SignInAsync"/>로 같은 계정에 다시 로그인한 뒤 다시 시도하세요.
        /// </summary>
        RequiresRecentSignIn,

        /// <summary>네트워크에 연결할 수 없습니다. 연결된 뒤 다시 시도하세요.</summary>
        NetworkError,

        /// <summary>다른 로그인 작업이 진행 중입니다. 끝난 뒤 다시 시도하세요.</summary>
        Busy,

        /// <summary>그 밖의 실패입니다. 자세한 원인은 로그에 남습니다.</summary>
        Failed,
    }
}
