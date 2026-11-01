namespace Nyonyo.Framework.Auth
{
    /// <summary>
    /// 로그인 방식입니다.
    /// </summary>
    public enum AuthProvider
    {
        /// <summary>
        /// 게스트(익명) 계정입니다. 이 기기에만 묶여 있어서 앱을 지우거나 기기를 바꾸면 되찾을 수 없습니다.
        /// 구글이나 애플 계정을 연결(<see cref="IAuthService.LinkAsync"/>)하면 다른 기기에서도 같은 계정으로 로그인할 수 있습니다.
        /// </summary>
        Guest,

        /// <summary>구글 계정입니다. Android에서 씁니다.</summary>
        Google,

        /// <summary>애플 계정입니다. iOS에서 씁니다.</summary>
        Apple,
    }
}
