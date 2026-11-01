# Changelog

이 패키지의 변경 사항을 기록합니다. 버전 규칙은 [Semantic Versioning](https://semver.org/lang/ko/)을 따릅니다.
버전은 코어와 따로 관리하고, 이 패키지가 바뀌었을 때만 올립니다. git 태그는 `firebase/v버전`입니다(예: `firebase/v0.1.0`).

## [0.1.0] - 2026-11-01

### Added
- `FirebaseAuthService`: Firebase Authentication으로 구현한 `IAuthService`. 게스트(익명) 로그인, 구글(Android)과 애플(iOS) 로그인과 계정 연결, 계정 삭제, ID 토큰. 연결하려는 계정이 이미 다른 게임 계정에 있으면 그 계정 정보를 기억해 전환할 때 창을 다시 띄우지 않습니다.
- 구글 로그인: Android Credential Manager(`GoogleSignInBridge.java`). 웹 클라이언트 ID는 google-services.json에서 자동으로 읽습니다.
- 애플 로그인: AuthenticationServices(`NyonyoAppleSignIn.mm`), 요청마다 nonce. iOS 빌드에 Sign in with Apple 권한을 자동으로 넣습니다.
- `FirebaseSettings`: 시작할 때 게스트 로그인, 구글/애플 사용 여부, 웹 클라이언트 ID, 에디터 모드(Fake, Firebase), `CreateAuthService`.
- 에디터: FirebaseSettings 인스펙터(SDK 설치와 설정 파일 상태), Android/iOS 빌드 전 검사, Tools > Nyonyo Framework > Settings > Firebase Settings.
- Firebase Unity SDK는 패키지 의존성이 아니라 게임에 따로 설치합니다(14.0.0 이상, `NYONYO_FIREBASE_AUTH`). 없으면 컴파일은 되고 기기 안의 게스트 로그인으로 동작합니다.
- 의존성: `com.nyonyo.framework` 0.2.1.
