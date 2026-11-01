using System.Collections.Generic;
using Nyonyo.Framework.Auth;
using Nyonyo.Framework.Save;
using NUnit.Framework;

namespace Nyonyo.Framework.Editor.Tests
{
    public class AuthServiceTests
    {
        private SaveService _save;
        private FakeAuthService _auth;
        private List<AuthUser> _changes;

        [SetUp]
        public void SetUp()
        {
            _save = new SaveService(new MemorySaveStorage());
            _changes = new List<AuthUser>();
            _auth = StartFake();
        }

        [TearDown]
        public void TearDown() => _auth.Dispose();

        [Test]
        public void Start_SignsInAsGuest()
        {
            Assert.IsTrue(_auth.IsInitialized);
            Assert.IsTrue(_auth.IsSignedIn);
            Assert.IsTrue(_auth.CurrentUser.IsGuest);
            Assert.AreEqual(1, _changes.Count, "게스트 로그인으로 계정이 한 번 바뀝니다.");
        }

        [Test]
        public void Start_WithoutGuestOption_StaysSignedOut()
        {
            _auth.Dispose();
            _save = new SaveService(new MemorySaveStorage());
            _auth = StartFake(signInAsGuestOnStart: false);

            Assert.IsTrue(_auth.IsInitialized);
            Assert.IsFalse(_auth.IsSignedIn);
        }

        [Test]
        public void Start_RestoresSavedUser()
        {
            var userId = _auth.CurrentUser.UserId;
            _auth.Dispose();

            _auth = StartFake();

            Assert.AreEqual(userId, _auth.CurrentUser.UserId, "저장된 계정을 이어서 씁니다.");
        }

        [Test]
        public void SignInAsGuest_WhenSignedIn_KeepsUser()
        {
            var userId = _auth.CurrentUser.UserId;

            Assert.AreEqual(AuthResult.Success, Wait(_auth.SignInAsync(AuthProvider.Guest)));
            Assert.AreEqual(userId, _auth.CurrentUser.UserId);
        }

        [Test]
        public void Link_KeepsUserIdAndAddsProvider()
        {
            var userId = _auth.CurrentUser.UserId;

            var link = _auth.LinkAsync(AuthProvider.Google).GetAwaiter();
            Assert.IsFalse(link.IsCompleted, "가짜 로그인 창에서 결과를 고를 때까지 기다립니다.");
            Assert.IsTrue(_auth.IsBusy);
            Assert.IsTrue(_auth.IsPresentingLink);
            _auth.Respond(AuthResult.Success);

            Assert.AreEqual(AuthResult.Success, link.GetResult());
            Assert.AreEqual(userId, _auth.CurrentUser.UserId);
            Assert.IsFalse(_auth.CurrentUser.IsGuest);
            Assert.IsTrue(_auth.CurrentUser.IsLinked(AuthProvider.Google));
            Assert.IsFalse(_auth.IsBusy);
        }

        [Test]
        public void Link_Cancelled_KeepsGuest()
        {
            var link = _auth.LinkAsync(AuthProvider.Apple).GetAwaiter();
            _auth.Respond(AuthResult.Cancelled);

            Assert.AreEqual(AuthResult.Cancelled, link.GetResult());
            Assert.IsTrue(_auth.CurrentUser.IsGuest);
        }

        [Test]
        public void Link_WhenAlreadyLinked_SucceedsWithoutWindow()
        {
            LinkGoogle();

            var link = _auth.LinkAsync(AuthProvider.Google).GetAwaiter();

            Assert.IsTrue(link.IsCompleted);
            Assert.AreEqual(AuthResult.Success, link.GetResult());
        }

        [Test]
        public void Link_Guest_IsNotSupported()
        {
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("게스트는 연결할 수 있는 계정이 아닙니다"));

            Assert.AreEqual(AuthResult.NotSupported, Wait(_auth.LinkAsync(AuthProvider.Guest)));
        }

        [Test]
        public void Link_WhenSignedOut_ReturnsNotSignedIn()
        {
            _auth.SignOut();

            Assert.AreEqual(AuthResult.NotSignedIn, Wait(_auth.LinkAsync(AuthProvider.Google)));
        }

        [Test]
        public void SecondOperation_WhileBusy_ReturnsBusy()
        {
            var link = _auth.LinkAsync(AuthProvider.Google).GetAwaiter();

            Assert.AreEqual(AuthResult.Busy, Wait(_auth.SignInAsync(AuthProvider.Apple)));

            _auth.Respond(AuthResult.Success);
            Assert.AreEqual(AuthResult.Success, link.GetResult());
        }

        [Test]
        public void SignInWithLinkedProvider_ReturnsToLinkedAccount()
        {
            var linkedUserId = _auth.CurrentUser.UserId;
            LinkGoogle();
            _auth.SignOut();
            Assert.IsFalse(_auth.IsSignedIn);
            Assert.IsNull(_changes[_changes.Count - 1], "로그아웃하면 null로 알립니다.");

            var signIn = _auth.SignInAsync(AuthProvider.Google).GetAwaiter();
            _auth.Respond(AuthResult.Success);

            Assert.AreEqual(AuthResult.Success, signIn.GetResult());
            Assert.AreEqual(linkedUserId, _auth.CurrentUser.UserId, "다른 기기에서 로그인한 것처럼 원래 계정으로 돌아옵니다.");
        }

        [Test]
        public void Link_ProviderUsedByOtherAccount_ReturnsAlreadyInUse()
        {
            LinkGoogle();
            _auth.SignOut();
            Wait(_auth.SignInAsync(AuthProvider.Guest));
            var otherGuest = _auth.CurrentUser.UserId;

            var link = _auth.LinkAsync(AuthProvider.Google).GetAwaiter();
            _auth.Respond(AuthResult.Success);

            Assert.AreEqual(AuthResult.AlreadyInUse, link.GetResult());
            Assert.AreEqual(otherGuest, _auth.CurrentUser.UserId, "연결에 실패하면 지금 계정을 그대로 둡니다.");
            Assert.IsTrue(_auth.CurrentUser.IsGuest);
        }

        [Test]
        public void DeleteAccount_SignsOutAndForgetsLinks()
        {
            LinkGoogle();

            Assert.AreEqual(AuthResult.Success, Wait(_auth.DeleteAccountAsync()));
            Assert.IsFalse(_auth.IsSignedIn);

            var signIn = _auth.SignInAsync(AuthProvider.Google).GetAwaiter();
            _auth.Respond(AuthResult.Success);
            signIn.GetResult();
            Assert.IsTrue(_auth.CurrentUser.UserId.StartsWith("fake-google-"), "삭제한 계정 대신 새 계정이 만들어집니다.");
        }

        [Test]
        public void IdToken_IsNullWhenSignedOut()
        {
            Assert.IsNotNull(Wait(_auth.GetIdTokenAsync()));

            _auth.SignOut();

            Assert.IsNull(Wait(_auth.GetIdTokenAsync()));
        }

        [Test]
        public void Null_SupportsOnlyGuestAndKeepsId()
        {
            var auth = new NullAuthService(_save);
            auth.Initialize();
            var userId = auth.CurrentUser.UserId;

            Assert.IsTrue(auth.IsProviderSupported(AuthProvider.Guest));
            Assert.IsFalse(auth.IsProviderSupported(AuthProvider.Google));
            Assert.AreEqual(AuthResult.NotSupported, Wait(auth.LinkAsync(AuthProvider.Google)));
            Assert.AreEqual(AuthResult.NotSupported, Wait(auth.SignInAsync(AuthProvider.Apple)));
            Assert.IsNull(Wait(auth.GetIdTokenAsync()));
            auth.Dispose();

            var restarted = new NullAuthService(_save);
            restarted.Initialize();
            Assert.AreEqual(userId, restarted.CurrentUser.UserId, "저장된 게스트 ID를 이어서 씁니다.");
            restarted.Dispose();
        }

        [Test]
        public void Callback_ReceivesResult()
        {
            AuthResult? received = null;

            _auth.Link(AuthProvider.Google, result => received = result);
            _auth.Respond(AuthResult.NetworkError);

            Assert.AreEqual(AuthResult.NetworkError, received);
        }

        private FakeAuthService StartFake(bool signInAsGuestOnStart = true)
        {
            var auth = new FakeAuthService(null, _save, signInAsGuestOnStart);
            auth.UserChanged += user => _changes.Add(user);
            auth.Initialize();
            return auth;
        }

        private void LinkGoogle()
        {
            var link = _auth.LinkAsync(AuthProvider.Google).GetAwaiter();
            _auth.Respond(AuthResult.Success);
            Assert.AreEqual(AuthResult.Success, link.GetResult());
        }

        private T Wait<T>(UnityEngine.Awaitable<T> awaitable)
        {
            var awaiter = awaitable.GetAwaiter();
            Assert.IsTrue(awaiter.IsCompleted, "가짜 로그인은 창이 없는 작업을 바로 끝냅니다.");
            return awaiter.GetResult();
        }
    }
}
