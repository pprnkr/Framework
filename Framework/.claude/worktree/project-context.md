# Nyonyo Framework 작업 기록

앞으로 만들 Unity 2D 게임들이 공용으로 쓸 프레임워크를 UPM 패키지로 개발하는 프로젝트입니다.
새 세션에서 이어서 작업할 때 이 문서를 먼저 읽으세요.

마지막 갱신: 2026-10-26

## 환경

| 항목 | 내용 |
|---|---|
| Unity | 6000.3.27f1, URP 2D 템플릿 |
| Unity 프로젝트 경로 | `C:\Users\devil\Framework\Framework` |
| git 저장소 루트 | `C:\Users\devil\Framework` (Unity 프로젝트보다 한 단계 위) |
| 원격 저장소 | `https://github.com/pprnkr/Framework.git`, 브랜치 `main` |
| 셸 | Windows PowerShell 5.1 |

- **git:** `C:\Program Files\Git\cmd\git.exe` (2.56)에 설치되어 있습니다. 전역 설정은 `pprnkr <devilplu2@gmail.com>`이고, 사용자가 직접 설정했습니다. 저장소 루트가 한 단계 위라서 `git -C ..`로 실행합니다.
  - Claude의 PowerShell에서는 PATH가 갱신되지 않을 수 있습니다. 그럴 때는 `$env:Path`를 Machine과 User 값으로 다시 읽어 옵니다.
  - GitHub 인증(Git Credential Manager)은 로그인 창이 필요해서 Claude 터미널에서 처음 푸시할 수 없습니다. 사용자가 VS Code 터미널에서 한 번 로그인하면 이후에는 Claude도 푸시할 수 있습니다.
- **Unity 검증 완료 (2026-10-26):** 컴파일 에러 없음, EditMode 테스트 16개 모두 통과, Play 모드에서 `[Nyonyo Framework]`가 DontDestroyOnLoad에 생성되는 것까지 확인했습니다.
  - 테스트 중 콘솔에 찍히는 `InvalidOperationException: dispose failed`는 `Dispose_WhenServiceThrows_ContinuesAndLogsException` 테스트가 의도한 로그입니다(`LogAssert.Expect`로 검증).

## 문제 해결 기록

- **Running Backend에서 멈춤 (2026-10-26):** Unity를 강제 종료하면 `bee_backend.exe`가 고아 프로세스로 남습니다. 그러면 다음에 켠 Unity가 `More than one copy of bee_backend running` 에러를 내고 멈춥니다(`%LOCALAPPDATA%\Unity\Editor\Editor.log`에서 확인).
  - 해결: Unity를 끄고, 부모 Unity가 이미 종료된 `bee_backend`, `BeeLocalCacheTool`, `VBCSCompiler`를 종료한 뒤 다시 엽니다.
  - VS Code의 `dotnet` 프로세스(UnityCodeModel, csdevkit, MSBuild)는 건드리지 않습니다.

## 확정된 결정

| 항목 | 결정 |
|---|---|
| 배포 형태 | 임베디드 UPM 패키지: `Packages/com.nyonyo.framework/` |
| 패키지 이름 | `com.nyonyo.framework` (패키지 이름은 소문자만 가능) |
| 표시 이름 / 작성자 | `Nyonyo Framework` / `Nyonyo` |
| 어셈블리 / 네임스페이스 | `Nyonyo.Framework` (PascalCase) |
| GitHub 계정 | `pprnkr` 유지. `nyonyo`는 다른 사람이 이미 쓰는 계정이라 바꿀 수 없음 |
| 설치 URL | `https://github.com/pprnkr/Framework.git?path=/Framework/Packages/com.nyonyo.framework#v0.1.0` |
| 의존성 관리 방식 | 서비스 로케이터. 싱글톤은 쓰지 않고, DI 프레임워크(VContainer, Zenject)는 무거워서 보류 |
| 비동기 | Unity 내장 `Awaitable` 사용. UniTask 같은 외부 의존성은 쓰지 않음 |
| 부트스트랩 방식 | `RuntimeInitializeOnLoadMethod`로 자동 시작. Boot 씬은 쓰지 않음 |
| 서비스 스코프 | 지금은 전역 서비스만. 씬 스코프 서비스는 나중에 필요할 때 추가 |

README의 설치 URL에 남은 `pprnkr`는 실제 저장소 주소라서 일부러 그대로 둔 것입니다. 바꾸면 설치가 깨집니다.

## 패키지 구조

```
Packages/com.nyonyo.framework/
├── package.json, README.md, CHANGELOG.md
├── Runtime/                Nyonyo.Framework (AssemblyInfo.cs: Editor/Tests에 internal 공개)
│   └── Core/               부트스트랩과 서비스 관리
├── Editor/                 Nyonyo.Framework.Editor (Editor 전용)
└── Tests/
    ├── Runtime/            Nyonyo.Framework.Tests (PlayMode, 아직 테스트 없음)
    └── Editor/             Nyonyo.Framework.Editor.Tests (EditMode)
```

## 로드맵

| 순서 | 기능 | 상태 |
|---|---|---|
| 0 | 패키지 구조와 asmdef | 완료, Unity 검증됨 |
| 1 | 부트스트랩과 서비스 관리 | 완료, Unity 검증됨 |
| 2 | 이벤트 시스템 (타입 기반 EventBus) | 완료 (Unity 검증 안 됨) |
| 3 | 씬 관리 (비동기 로드, 로딩 화면, 페이드) | **다음 작업** |
| 4 | UI 관리 (화면/팝업 스택, 뒤로가기) | 예정 |
| 5 | 오디오 (BGM 크로스페이드, SFX 풀링, Mixer 볼륨) | 예정 |
| 6 | 저장/로드 (JSON, 슬롯, 버전 마이그레이션) | 예정 |
| 7 | 풀링, FSM, 타이머 | 예정 |
| 8 | 디버그 도구 (로거, 치트 콘솔, 에디터 툴) | 예정 |

그 밖에 추천해 둔 것들입니다.
- **Input System 래퍼:** 액션맵 전환과 키 리바인딩
- **공용 기능:** 일시정지와 타임스케일 관리, 2D 카메라 유틸, ScriptableObject 데이터 정의
- **패키지 활용:** Cinemachine 3, Addressables, Localization, PrimeTween/DOTween은 직접 만들지 말고 패키지 위에 얇은 래퍼만 씌웁니다.
- **불필요한 패키지 정리:** `visualscripting`, `multiplayer.center`, `collab-proxy`는 쓸 계획이 없으면 manifest에서 빼는 게 좋습니다. 아직 빼지 않았습니다.

## 구현 완료: 부트스트랩과 서비스 관리 (`Runtime/Core/`)

| 타입 | 역할 |
|---|---|
| `Bootstrapper` | `BeforeSceneLoad`에서 자동 실행. `[Nyonyo Framework]` DontDestroyOnLoad 루트를 만들고, Installer를 실행한 뒤 서비스를 초기화합니다. `State`, `IsReady`, `Root`, `WaitUntilReadyAsync()`를 제공합니다. |
| `ServiceRegistry` | 타입별로 등록하고 조회합니다(`Register`/`Get`/`TryGet`/`Contains`). `InitializeAsync`는 한 번만 호출할 수 있고, `Dispose`는 역순으로 정리합니다. |
| `Services` | 정적 진입점(`Get`, `TryGet`). `SetRegistry`는 internal입니다. |
| `GameInstaller` | 게임별 서비스 등록용 ScriptableObject 추상 클래스(`Install(ServiceRegistry)`) |
| `FrameworkSettings` | `Resources/NyonyoFrameworkSettings`에 있는 Installer 목록 |
| `IInitializable` / `IAsyncInitializable` | 서비스 초기화 인터페이스. 정리는 `System.IDisposable`을 씁니다. |
| `FrameworkSettingsMenu` (Editor) | 메뉴 Tools > Nyonyo Framework > Create Settings |

### 동작 규칙

**등록과 초기화**
- 등록은 초기화가 시작되기 전에만 할 수 있습니다. 같은 타입을 두 번 등록하면 예외가 납니다.
- 같은 인스턴스를 여러 타입으로 등록해도 초기화와 정리는 한 번만 실행됩니다.
- 초기화는 등록 순서대로 진행됩니다. 하나가 실패하면 거기서 멈추고, 이미 초기화된 서비스만 정리 대상이 됩니다.

**정리(Dispose)**
- 정리는 역순으로 진행됩니다. 하나가 예외를 던져도 `Debug.LogException`으로 남기고 나머지를 계속 정리합니다.
- 비동기 초기화 도중에 Dispose가 호출되면 방금 초기화한 서비스까지 정리하고 `OperationCanceledException`을 던집니다.

**종료와 Play 모드**
- 종료는 `Application.quitting`에서 처리하고, 취소 토큰은 `Application.exitCancellationToken`을 씁니다.
- 서비스는 씬 오브젝트보다 먼저 정리됩니다. 그래서 `OnDestroy`/`OnDisable`에서는 `Services.TryGet`을 쓰도록 문서화했습니다.
- `SubsystemRegistration`에서 static 값을 초기화합니다. Domain Reload를 끈 빠른 Play 모드에 대응하기 위해서입니다.
- 설정 파일이 없으면 서비스 없이 시작하고 경고를 띄웁니다. 이 Framework 프로젝트에는 아직 설정 파일이 없습니다.

## 구현 완료: 이벤트 시스템 (`Runtime/Events/`, 네임스페이스 `Nyonyo.Framework.Events`)

| 타입 | 역할 |
|---|---|
| `IEvent` | 이벤트 표시용 인터페이스. 이벤트는 `struct`만 허용합니다(`where TEvent : struct, IEvent`). |
| `IEventBus` / `EventBus` | `Subscribe`(구독 해제용 `IDisposable` 반환), `Unsubscribe`, `Publish`. `EventBus.Dispose`는 모든 구독을 해제합니다. |
| `EventBusExtensions` | `Subscribe(handler, CancellationToken)`. MonoBehaviour의 `destroyCancellationToken`으로 자동 해제할 때 씁니다. |

결정 사항은 다음과 같습니다.

**등록 방식**
- 프레임워크가 자동으로 등록하지 않습니다. 게임의 Installer가 다른 서비스보다 먼저 직접 등록합니다.
- 나중에 프레임워크 기본 서비스를 한꺼번에 등록하는 기본 Installer를 둘지 검토합니다.

**발행과 구독**
- 발행 시 메모리 할당이 없습니다. 발행 중 해제된 핸들러는 자리만 null로 비워 두고, 발행이 끝난 뒤에 정리합니다.
- 발행 중 새로 구독한 핸들러는 다음 발행부터 호출됩니다. 아직 호출되지 않은 핸들러를 해제하면 이번 발행에서도 건너뜁니다.
- 같은 이벤트를 발행 도중에 다시 발행(중첩 발행)해도 동작합니다.
- 핸들러가 예외를 던지면 `Debug.LogException`으로 남기고 나머지 핸들러를 계속 호출합니다.
- 중복 구독은 C# event처럼 허용합니다. `Unsubscribe`는 마지막으로 구독한 것 하나만 제거합니다.

**보류한 것**
- 메인 스레드 전용입니다.
- SO 이벤트 채널과 정적 단축 API(`Events.Publish`)는 필요할 때 추가합니다.

### 테스트 (`Tests/Editor/`)

- **테스트 파일:** `ServiceRegistryTests` 13개, `ServicesTests` 3개(검증됨), `EventBusTests` 16개(Unity 검증 안 됨).
- **의도된 에러 로그:** `EventBusTests`의 `handler failed` 예외 로그도 `LogAssert.Expect`로 검증하는 의도된 로그입니다.
- **비동기 처리:** 테스트 서비스가 모두 즉시 완료되므로 `InitializeAsync().GetAwaiter()`로 동기 검증합니다. async 테스트 지원 여부에 의존하지 않으려고 이렇게 했습니다.
- **Bootstrapper:** 자동 테스트가 없습니다. Play 모드에서 직접 확인해야 합니다.

## 코드 컨벤션

**이름과 구조**
- private 필드는 `_camelCase`, private static 필드는 `s_camelCase`로 씁니다.
- 접근 제한자는 private이면 생략합니다.
- 기본 네임스페이스는 `Nyonyo.Framework`입니다. 클래스 이름과 겹치는 하위 네임스페이스는 만들지 않습니다(예: `Services` 클래스).
- 기능별 폴더는 `Runtime/<기능>/` 아래에 둡니다.

**주석과 메시지**
- XML 문서 주석, 예외 메시지, 로그 메시지는 한국어로 씁니다.
- 로그에는 `[Nyonyo Framework]` 접두어를 붙입니다.
- 예외 메시지에는 원인과 해결 방법을 함께 적습니다.

**테스트와 기록**
- 핵심 로직은 EditMode 테스트로 검증합니다.
- 기능을 추가할 때마다 README 사용법과 CHANGELOG를 갱신합니다.

## 사용자 작업 스타일

- 한국어로 대화합니다.
- 선택지를 제시할 때는 추천안을 함께 주고, 사용자가 고른 뒤에 진행합니다.
- 기능 하나를 완료할 때마다 다음 단계를 진행할지 묻습니다.
