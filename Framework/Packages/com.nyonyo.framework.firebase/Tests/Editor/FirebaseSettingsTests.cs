using Nyonyo.Framework.Auth;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Firebase.Editor.Tests
{
    public class FirebaseSettingsTests
    {
        private FirebaseSettings _settings;

        [SetUp]
        public void SetUp() => _settings = ScriptableObject.CreateInstance<FirebaseSettings>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_settings);

        [Test]
        public void Defaults_SignInAsGuestAndUseBothProviders()
        {
            Assert.IsTrue(_settings.SignInAsGuestOnStart);
            Assert.IsTrue(_settings.GoogleSignIn);
            Assert.IsTrue(_settings.AppleSignIn);
            Assert.AreEqual(FirebaseAuthEditorMode.Fake, _settings.AuthEditorMode);
        }

        [Test]
        public void CreateAuthService_InEditor_UsesFakeByDefault()
        {
            var auth = _settings.CreateAuthService(null);

            Assert.IsInstanceOf<FakeAuthService>(auth);
            ((FakeAuthService)auth).Dispose();
        }

        [Test]
        public void CreateAuthService_PassesGuestOption()
        {
            var serialized = new SerializedObject(_settings);
            serialized.FindProperty("_signInAsGuestOnStart").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var auth = (FakeAuthService)_settings.CreateAuthService(null);
            auth.Initialize();

            Assert.IsTrue(auth.IsInitialized);
            Assert.IsFalse(auth.IsSignedIn, "게스트 자동 로그인을 끄면 로그인하지 않습니다.");
            auth.Dispose();
        }

        [Test]
        public void AppleNonce_IsSha256OfRawValue()
        {
            var apple = new AppleSignInIOS();

            // SHA-256("abc")는 표준 테스트 값입니다.
            Assert.AreEqual("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", apple.HashNonce("abc"));

            var raw = apple.CreateRawNonce();
            Assert.AreEqual(64, raw.Length);
            Assert.AreNotEqual(raw, apple.CreateRawNonce(), "요청마다 다른 값입니다.");
        }
    }
}
