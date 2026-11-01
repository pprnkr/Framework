namespace Nyonyo.Framework.Firebase
{
    /// <summary>
    /// 에디터 Play 모드에서 쓸 로그인입니다.
    /// </summary>
    public enum FirebaseAuthEditorMode
    {
        /// <summary>
        /// 프레임워크의 <see cref="Auth.FakeAuthService"/>를 씁니다. 서버 없이 게스트, 구글, 애플 로그인과 계정 연결 흐름을 확인할 수 있습니다.
        /// </summary>
        Fake,

        /// <summary>
        /// 실제 Firebase 프로젝트에 게스트(익명)로 로그인합니다(Firebase 데스크톱 SDK, 베타).
        /// 구글, 애플 로그인은 에디터에서 쓸 수 없습니다.
        /// </summary>
        Firebase,
    }
}
