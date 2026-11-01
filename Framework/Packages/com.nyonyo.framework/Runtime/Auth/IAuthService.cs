using System;
using UnityEngine;

namespace Nyonyo.Framework.Auth
{
    /// <summary>
    /// 로그인 서비스입니다. 게임 코드는 이 인터페이스만 쓰고, 실제 로그인(Firebase 등)은 Installer에서 고릅니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 기본 흐름은 "게스트로 바로 시작하고, 나중에 구글(Android)이나 애플(iOS) 계정을 연결"입니다.
    /// 서비스가 시작될 때 로그인된 계정이 없으면 게스트로 로그인하고(설정으로 끌 수 있음), 한 번 로그인한 계정은 다음 실행 때도 유지됩니다.
    /// </para>
    /// <para>
    /// 시작은 게임을 막지 않고 뒤에서 진행합니다. 계정이 필요한 곳에서는 <see cref="WaitUntilInitializedAsync"/>를 기다린 뒤 <see cref="CurrentUser"/>를 확인하세요.
    /// 네트워크가 없으면 처음 게스트 로그인이 실패할 수 있으므로 <see cref="CurrentUser"/>가 null인 경우도 처리하세요.
    /// </para>
    /// <para>로그인, 계정 연결, 계정 삭제는 한 번에 하나만 진행합니다. 진행 중에 다른 작업을 요청하면 바로 <see cref="AuthResult.Busy"/>입니다.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var auth = Services.Get&lt;IAuthService&gt;();
    /// await auth.WaitUntilInitializedAsync();
    /// Debug.Log(auth.CurrentUser?.UserId);
    ///
    /// // 계정 연결 버튼 (await 없이 쓰려면 auth.Link(AuthProvider.Google, result =&gt; ...))
    /// var result = await auth.LinkAsync(AuthProvider.Google);
    /// if (result == AuthResult.AlreadyInUse)
    /// {
    ///     // "이미 다른 게임 계정에 연결된 구글 계정입니다. 그 계정으로 전환할까요?"를 묻고 전환을 고르면
    ///     await auth.SignInAsync(AuthProvider.Google);
    /// }
    /// </code>
    /// </example>
    public interface IAuthService
    {
        /// <summary>시작(저장된 로그인 확인, 필요하면 게스트 로그인)이 끝나면 한 번 호출합니다.</summary>
        event Action Initialized;

        /// <summary>로그인, 계정 전환, 계정 연결, 로그아웃, 계정 삭제로 계정 정보가 바뀌면 호출합니다. 로그아웃하면 null을 넘깁니다.</summary>
        event Action<AuthUser> UserChanged;

        bool IsInitialized { get; }

        /// <summary>로그인한 계정입니다. 로그인하지 않았으면 null입니다.</summary>
        AuthUser CurrentUser { get; }

        bool IsSignedIn { get; }

        /// <summary>로그인이나 계정 연결 같은 작업이 진행 중이면 true입니다.</summary>
        bool IsBusy { get; }

        /// <summary>
        /// 이 플랫폼에서 쓸 수 있는 로그인 방식이면 true입니다. 구글, 애플 버튼을 보일지 정할 때 씁니다.
        /// </summary>
        bool IsProviderSupported(AuthProvider provider);

        /// <summary>시작이 끝날 때까지 기다립니다. 이미 끝났으면 바로 끝납니다.</summary>
        Awaitable WaitUntilInitializedAsync();

        /// <summary>
        /// 로그인합니다. <see cref="AuthProvider.Guest"/>는 이미 로그인되어 있으면 아무것도 하지 않습니다.
        /// 구글, 애플은 로그인 창을 띄우고, 다른 계정에 로그인되어 있었다면 그 계정으로 바꿉니다(이전 게스트 계정의 진행은 이어지지 않음).
        /// </summary>
        Awaitable<AuthResult> SignInAsync(AuthProvider provider);

        /// <summary>
        /// 지금 계정에 구글이나 애플 계정을 연결합니다. 계정 ID와 진행은 그대로이고, 다른 기기에서 그 계정으로 로그인할 수 있게 됩니다.
        /// 이미 연결되어 있으면 바로 <see cref="AuthResult.Success"/>입니다.
        /// </summary>
        Awaitable<AuthResult> LinkAsync(AuthProvider provider);

        /// <summary>
        /// 로그아웃합니다. 게스트 계정은 로그아웃하면 다시 찾을 수 없으니, 게스트에게는 로그아웃 버튼을 보이지 마세요.
        /// </summary>
        void SignOut();

        /// <summary>
        /// 계정을 삭제하고 로그아웃합니다. 계정을 만들 수 있는 앱은 스토어 정책상 계정 삭제 기능을 제공해야 합니다.
        /// 서버에 저장한 게임 데이터는 따로 지워야 합니다.
        /// </summary>
        Awaitable<AuthResult> DeleteAccountAsync();

        /// <summary>
        /// 게임 서버에 보낼 로그인 토큰(ID 토큰)을 받습니다. 로그인하지 않았거나 토큰이 없는 로그인(로컬 게스트)이면 null입니다.
        /// </summary>
        /// <param name="forceRefresh">true면 만료 전이어도 새 토큰을 받습니다.</param>
        Awaitable<string> GetIdTokenAsync(bool forceRefresh = false);
    }
}
