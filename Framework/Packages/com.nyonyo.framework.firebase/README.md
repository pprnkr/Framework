# Nyonyo Framework - Firebase

Nyonyo Framework의 로그인 서비스(`IAuthService`)를 Firebase Authentication으로 구현한 선택 모듈입니다. 로그인이 필요한 게임만 설치합니다.

- **로그인 방식:** 게스트(익명)로 바로 시작하고, 구글(Android)이나 애플(iOS) 계정을 연결합니다. 연결한 계정으로 다른 기기에서 로그인할 수 있습니다.
- **그 밖의 기능:** 계정 삭제(스토어 정책), 게임 서버용 ID 토큰
- **에디터:** 가짜 로그인으로 모든 흐름을 확인하거나, 실제 Firebase에 게스트로 로그인합니다.
- **앞으로:** Analytics, Remote Config, Crashlytics 같은 다른 Firebase 기능도 이 패키지에 추가합니다.

## 설치

### 1. Firebase 프로젝트 준비

1. [Firebase 콘솔](https://console.firebase.google.com)에서 프로젝트를 만듭니다. 개발용(DEV)과 출시용(SERVICE) 프로젝트를 **따로** 만드는 것을 권합니다. 테스트 계정이 실제 사용자 데이터와 섞이지 않습니다.
2. 프로젝트에 Android 앱과 iOS 앱을 등록합니다(패키지 이름, 번들 ID).
3. **Authentication > 로그인 방법**에서 **익명**, **Google**, **Apple**을 켭니다.
4. **Android 구글 로그인:** 프로젝트 설정의 Android 앱에 **SHA-1 지문**을 등록합니다. 디버그 키, 출시 키, Google Play 앱 서명 키를 모두 넣어야 각 빌드에서 로그인됩니다.
5. **iOS 애플 로그인:** Apple Developer의 App ID에서 **Sign in with Apple**을 켭니다.
6. 설정 파일을 받아 게임 프로젝트의 Assets 아래에 넣습니다.
   - Android: `google-services.json` (4번 뒤에 받아야 구글 로그인 정보가 들어 있습니다)
   - iOS: `GoogleService-Info.plist`

### 2. Firebase Unity SDK 설치

Firebase Unity SDK는 Unity 레지스트리에 없어서, 이 패키지의 의존성으로 자동 설치할 수 없습니다. [Google APIs for Unity 보관 페이지](https://developers.google.com/unity/archive)에서 `.tgz` 파일 세 개를 받습니다(이 모듈은 14.1.0으로 확인했습니다. 14.0.0 이상이 필요합니다).

- `com.google.external-dependency-manager-1.2.190.tgz`
- `com.google.firebase.app-14.1.0.tgz`
- `com.google.firebase.auth-14.1.0.tgz`

게임 프로젝트의 `Packages` 폴더 옆에 `GooglePackages` 폴더를 만들어 넣고, `Packages/manifest.json`에 적습니다.

```json
{
  "dependencies": {
    "com.google.external-dependency-manager": "file:../GooglePackages/com.google.external-dependency-manager-1.2.190.tgz",
    "com.google.firebase.app": "file:../GooglePackages/com.google.firebase.app-14.1.0.tgz",
    "com.google.firebase.auth": "file:../GooglePackages/com.google.firebase.auth-14.1.0.tgz",
    "com.nyonyo.framework": "https://github.com/pprnkr/Framework.git?path=/Framework/Packages/com.nyonyo.framework#core/v0.2.1",
    "com.nyonyo.framework.firebase": "https://github.com/pprnkr/Framework.git?path=/Framework/Packages/com.nyonyo.framework.firebase#firebase/v0.1.0"
  }
}
```

- **GooglePackages 폴더는 커밋하세요.** 다른 사람이 받았을 때도 같은 SDK가 설치됩니다. Firebase App 파일이 60MB 정도라 Git LFS를 쓰는 것도 좋습니다.
- **Firebase 제품은 버전을 맞춥니다.** App과 Auth(나중에 추가하는 다른 Firebase 제품도)는 같은 버전이어야 합니다.
- **AdMob 모듈과 함께 쓸 때:** AdMob은 Unity 공식 EDM(`com.unity.external-dependency-manager`)을, Firebase는 구버전 EDM(`com.google.external-dependency-manager`)을 씁니다. Unity 공식 EDM은 구버전이 함께 있으면 감지해서 어느 쪽을 쓸지 고르는 창을 띄웁니다. Unity 공식 EDM을 고르세요. (Unity EDM 2.1.0 코드로 확인한 동작이고, 실제 게임 프로젝트에서는 아직 확인하지 않았습니다.)
- **SDK가 없으면:** 이 모듈은 컴파일은 되지만 Android, iOS에서도 기기 안의 게스트 로그인(`NullAuthService`)으로 동작하고 에러 로그를 남깁니다. 설정 인스펙터와 빌드 검사가 알려 줍니다.

### 3. 설정과 등록

1. Tools > Nyonyo Framework > Settings > **Firebase Settings**로 설정 에셋을 만듭니다.
   - **Sign In As Guest On Start:** 시작할 때 로그인된 계정이 없으면 게스트로 로그인합니다(기본 켬).
   - **Google Sign In / Apple Sign In:** 구글(Android), 애플(iOS) 로그인을 씁니다(기본 켬). 애플을 켜면 iOS 빌드에 Sign in with Apple 권한이 자동으로 들어갑니다.
   - **Google Web Client Id:** 비워 두면 `google-services.json`의 값을 씁니다. 보통 비워 둡니다.
   - **Auth Editor Mode:** 에디터에서 쓸 로그인입니다. Fake는 서버 없이 모든 흐름을 흉내 내고, Firebase는 실제 프로젝트에 게스트로 로그인합니다(구글, 애플은 에디터에서 쓸 수 없음).
   - 인스펙터 아래에 SDK 설치 여부와 설정 파일 상태가 나옵니다.
2. Installer에서 등록합니다.

```csharp
[SerializeField] private FirebaseSettings _firebaseSettings;

public override void Install(ServiceRegistry services)
{
    var save = new SaveService();
    services.Register(save);
    // Android/iOS는 Firebase, 에디터는 Auth Editor Mode, WebGL과 PC는 기기 안의 게스트 로그인
    services.Register<IAuthService>(_firebaseSettings.CreateAuthService(Bootstrapper.Root, save));
}
```

사용법은 코어 README의 "로그인" 절을 보세요.

## 플랫폼

| 플랫폼 | 로그인 | 비고 |
|---|---|---|
| Android | Firebase: 게스트, 구글 | 구글 로그인은 Credential Manager(계정 선택 창)입니다. Google Play 서비스가 필요합니다. |
| iOS | Firebase: 게스트, 애플 | iOS 13 이상. 구글 로그인은 넣지 않았습니다(App Store는 다른 소셜 로그인을 넣으면 애플 로그인도 요구). |
| WebGL | 기기 안의 게스트(`NullAuthService`) | Firebase Unity SDK가 WebGL을 지원하지 않습니다. 웹 로그인이 필요한 게임이 생기면 JS SDK 연동을 추가합니다. |
| PC | 기기 안의 게스트(`NullAuthService`) | Firebase 데스크톱 SDK는 개발용(베타)이라 출시 빌드에 쓰지 않습니다. |
| 에디터 | Fake 또는 Firebase 게스트 | Auth Editor Mode로 고릅니다. |

## 동작

- **시작 순서:** 서비스가 초기화되면 Firebase 의존성 확인(Android는 Google Play 서비스) → 저장된 로그인 확인 → 없으면 게스트 로그인 순서로 진행합니다. 게임 시작을 막지 않습니다.
- **로그인 유지:** Firebase가 로그인을 기기에 저장하므로 다음 실행 때 같은 계정이 이어집니다.
- **이미 다른 계정에 연결된 구글/애플 계정:** 연결은 `AlreadyInUse`로 끝나고, Firebase가 준 그 계정 정보를 잠시 기억합니다. 바로 이어서 같은 방식으로 `SignInAsync`를 부르면 로그인 창 없이 그 계정으로 전환합니다.
- **계정 상태 변화:** 토큰 만료나 다른 곳에서의 계정 삭제도 `UserChanged`로 알려 줍니다.
- **스레드:** Firebase와 구글 로그인 결과는 다른 스레드에서 올 수 있어서 Unity 메인 스레드로 옮겨 처리합니다. 이 모듈의 Firebase 코드는 Android, iOS, 에디터에서만 컴파일되고 WebGL에서는 빠집니다.

## DEV와 SERVICE 프로젝트

DEV와 SERVICE가 서로 다른 Firebase 프로젝트를 쓰려면 빌드할 때 설정 파일(`google-services.json`, `GoogleService-Info.plist`)을 바꿔 넣어야 합니다. 프레임워크의 배포 레벨(DEV/SERVICE)과 빌드 메뉴를 만들 때 자동으로 바꾸도록 할 예정입니다. 그 전까지는 빌드할 프로젝트의 파일만 Assets에 두세요(같은 이름의 파일이 둘 있으면 Firebase가 어느 쪽을 쓸지 보장할 수 없습니다).

## 출시 전 확인

- [ ] Android: Google Play 앱 서명 키의 SHA-1을 Firebase에 등록하고 `google-services.json`을 다시 받기
- [ ] iOS: App ID에 Sign in with Apple 켜기, Firebase 콘솔 Apple 로그인 켜기
- [ ] 설정 화면에 계정 삭제 버튼 넣기(`DeleteAccount`, App Store와 Google Play 정책)
- [ ] 출시용 Firebase 프로젝트의 설정 파일로 빌드했는지 확인
