# Nyonyo Framework 작업 기록

앞으로 만들 Unity 2D 게임들이 공용으로 쓸 프레임워크를 UPM 패키지로 개발하는 프로젝트입니다.
새 세션에서 이어서 작업할 때 이 문서를 먼저 읽으세요.

마지막 갱신: 2026-10-27

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
  - **푸시는 사용자가 직접 합니다.** Claude 터미널에서는 GitHub 인증 창이 뜨지 않아 푸시가 실패합니다. 그래서 Claude는 커밋까지만 하고, 푸시할 커밋이 남아 있다고 알려 줍니다.
- **Unity 검증 완료 (2026-10-26):** 컴파일 에러 없음, EditMode 테스트 16개 모두 통과, Play 모드에서 `[Nyonyo Framework]`가 DontDestroyOnLoad에 생성되는 것까지 확인했습니다.
  - 테스트 중 콘솔에 찍히는 `InvalidOperationException: dispose failed`는 `Dispose_WhenServiceThrows_ContinuesAndLogsException` 테스트가 의도한 로그입니다(`LogAssert.Expect`로 검증).

## 사용 가이드

- **위치:** `.claude/artifact/nyonyo-framework-guide.html`. 브라우저로 여는 단일 HTML 파일이고, 외부 의존성이 없습니다.
- **내용:** 완료된 기능의 사용법, 예정 기능의 설계안, 규칙, 문제 해결.
- **갱신 규칙:** 기능을 추가하거나 바꾸면 README와 함께 이 가이드도 갱신합니다. 상태 배지(검증됨, 확인 필요, 예정)도 맞춰 바꿉니다.

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
| 화면 방향 | 세로형이 기본(1080x1920). 게임마다 `UISettings.Orientation` enum으로 정함 (2026-10-27 사용자 요청) |
| 서드파티 모듈 | 네이티브 SDK나 빌드 설정이 필요한 서드파티(AdMob, Firebase, 로그인 등)는 같은 저장소의 별도 패키지 `com.nyonyo.framework.<이름>`으로 분리. 인터페이스와 가짜/Null 구현은 코어에 둠. 가벼운 Unity 공식 패키지는 코어 안에서 `versionDefines`로 선택 컴파일 (2026-10-27 사용자 동의) |
| 모듈 버전 | 코어와 모듈은 같은 git 태그로 함께 올림. UPM은 package.json 의존성에 git URL을 못 쓰므로 게임 manifest에 코어와 모듈을 모두 적음 |

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

Packages/com.nyonyo.framework.admob/    선택 모듈 (의존성: 코어, com.google.ads.mobile 11.6.0)
├── Runtime/                Nyonyo.Framework.AdMob (네임스페이스 Nyonyo.Framework.Ads.AdMob)
├── Editor/                 Nyonyo.Framework.AdMob.Editor
└── Tests/Editor/           Nyonyo.Framework.AdMob.Editor.Tests
```

개발 프로젝트 manifest에 OpenUPM 스코프 레지스트리(`com.google.ads.mobile`만)를 추가했습니다. 이게 없으면 admob 모듈의 의존성을 풀지 못해 패키지 해석 전체가 실패합니다.

## 로드맵

| 순서 | 기능 | 상태 |
|---|---|---|
| 0 | 패키지 구조와 asmdef | 완료, Unity 검증됨 |
| 1 | 부트스트랩과 서비스 관리 | 완료, Unity 검증됨 |
| 2 | 이벤트 시스템 (타입 기반 EventBus) | 완료, Unity 검증됨 |
| 3 | 씬 관리 (비동기 로드, 페이드) | 완료 (외부 컴파일만 확인, Unity 검증 안 됨) |
| 4 | UI 관리 (5단계 레이어, PopupManager, 뒤로가기, UIBase) | 완료 (외부 컴파일만 확인, Unity 검증 안 됨) |
| 4-1 | UI 자동 매퍼 (접두사 SO, Create UI Script 메뉴, Generate/Assign) | 완료 (외부 컴파일과 생성기 실행만 확인, Unity 검증 안 됨) |
| 4-2 | 에셋 로딩 (Resources/Addressables, `[Popup]` 어트리뷰트) | 완료 (스텁 컴파일만 확인, Unity 검증 안 됨) |
| 5 | 사운드 (BGM 크로스페이드, 효과음 8채널, 볼륨 곱) | 완료 (스텁 컴파일만 확인, Unity 검증 안 됨) |
| 6 | 저장/로드 (JSON, 슬롯, 버전 마이그레이션, 볼륨 저장 연동) + WebGL 호환 수정 | 완료 (스텁/플레이어 DLL 컴파일만 확인) |
| 7 | 풀링, FSM, 타이머, 일시정지, 트윈 | 완료 (컴파일만 확인) |
| 8 | 입력, 2D 카메라, 데이터 테이블, 2D 공용 컴포넌트 | 완료 (컴파일만 확인) |
| 9 | 디버그 도구 (로거, 디버그 콘솔, 에디터 씬 전환 창) | 완료 (컴파일만 확인) |
| 10 | 광고: 코어 `IAdService` + AdMob 모듈(배너, 전면, 보상형, 앱 오프닝, UMP 동의) | 완료 (실제 AdMob 11.6.0 DLL로 컴파일만 확인, Unity와 기기 검증 안 됨) |
| 11 | 게임 데이터: 구글 시트 → partial 클래스 생성 + 암호화 `.bytes` (기존 Data 교체) | 완료 (.NET 하네스로 테스트 29개 실행, 생성 코드 컴파일과 로드 확인. 실제 구글 시트 내려받기와 Unity는 확인 안 됨) |
| **다음** | **배포 레벨(DEV/SERVICE)과 빌드 메뉴(Tools/Build)** | **검토만 완료, 구현 전.** 아래 "다음 할 일" 절 참고 |
| - | Localization, Cinemachine 연동 | 보류: 패키지 도입 여부를 사용자가 정해야 함 |

7~9단계는 2026-10-27 사용자 요청("처음 제안한 남은 기능 모두 구현")으로, 사용자가 자는 동안 한 번에 구현했습니다. 아직 Unity에서 Test Runner와 Play 모드 확인을 받지 않았습니다.

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
| `SettingsMenu` (Editor) | Tools > Nyonyo Framework > Settings 아래에 모든 설정 에셋(Framework, UI, Sound, UI Prefix, Game Data, AdMob). 있으면 선택, 없으면 기본 위치(`Assets/Settings/Nyonyo Framework`, Framework는 Resources, 에디터 전용은 `Assets/Editor`)에 생성. 설정 에셋의 CreateAssetMenu는 2026-10-27 사용자 요청으로 없앰 |

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

## 구현 완료: 씬 관리 (`Runtime/Scenes/`, 네임스페이스 `Nyonyo.Framework.Scenes`)

| 타입 | 역할 |
|---|---|
| `ISceneService` / `SceneService` | `Load(sceneName)`(void, 내부 Forget)과 `LoadAsync(sceneName)`(`Awaitable<bool>`, 중복 요청이면 false). `IsLoading`, `Progress`, `MinimumLoadingSeconds`를 제공하고, `IDisposable`입니다. |
| `ISceneTransition` / `FadeTransition` | `ShowAsync`, `SetProgress`, `HideAsync`. 페이드는 코드로 만든 UGUI Canvas를 쓰고, unscaled time으로 동작하며, 가리는 동안 UI 클릭을 막습니다. |
| `ISceneLoader` / `SceneManagerLoader` | 실제 씬 로딩입니다. Build Profiles의 Scene List에 없는 씬이면 예외를 던집니다. Addressables 로더로 교체할 수 있습니다. |
| `SceneLoadStarted` / `SceneLoaded` | EventBus로 발행되는 씬 전환 이벤트입니다. |

결정 사항은 다음과 같습니다.

**설계**
- **진행 순서:** 시작 이벤트 → Show → Load → (최소 시간 대기) → 완료 이벤트 → Hide.
- **의존성 주입:** 의존성은 생성자로 받습니다(transition, eventBus, loader 모두 선택). 서비스 안에서 `Services.Get`을 부르지 않습니다.
- **정리 시 동작:** `Dispose` 시 내부 CTS를 취소해서 진행 중인 전환을 멈춥니다(게임 종료, Play 모드 종료 대응). `Application.exitCancellationToken`은 EditMode 테스트에서의 동작이 불확실해서 쓰지 않았습니다.
- **UGUI 의존성:** Runtime asmdef가 `UnityEngine.UI`를 참조하고, package.json에 `com.unity.ugui` 2.0.0 의존성을 추가했습니다.

**알려진 제약**
- `SceneLoaded` 핸들러에서 바로 다른 씬을 로드하면 무시됩니다(아직 `IsLoading` 상태).
- 취소해도 Unity의 씬 로드 자체는 끝까지 진행됩니다.

**보류한 것**
- Additive 씬, `SceneReference`(인스펙터 드래그), 로딩 화면 프리팹.

**개발용 코드 (`Assets/Dev/`, 패키지 아님)**
- `DevInstaller`: EventBus, SceneService 등록과 `DevSceneSwitcher` 부착.
- `DevSceneSwitcher`: OnGUI로 빌드 씬 버튼을 띄웁니다.

## 구현 완료: UI와 팝업 (`Runtime/UI/`, 네임스페이스 `Nyonyo.Framework.UI`)

사용자 요구사항(2026-10-26):
- UI 레이어는 5단계로 나눕니다. 낮은 층에 팝업이 여러 개 있어도 띠알람은 높은 층에 보여야 합니다.
- 같은 층에서는 나중에 만든 팝업이 위에 보여야 합니다.
- `PopupManager.Instance.Open<PopupTest>()` 형태로 띄웁니다(처음 요구는 `Show`였으나 2026-10-27 Open/Close 정리로 이름 변경). 싱글톤이 아니어도 됩니다.
- `PopupBase`에 `Close`와 `ButtonEvent`를 공통 처리하는 virtual 함수를 두고, `Close` 안에서 `Destroy`합니다.

| 타입 | 역할 |
|---|---|
| `UILayer` | `Screen`(0), `Popup`(1), `System`(2), `Notice`(3), `Top`(4). 층마다 ScreenSpaceOverlay Canvas를 두고, 정렬 순서는 1000 + 층×1000입니다. 페이드(30000)가 그 위에 그려집니다. |
| `PopupManager` | `Open<T>(UILayer? layer)`, `OpenAsync<T>`, `Find<T>`, `IsOpen<T>`, `CloseAll(layer?)`, `HandleBack()`, `GetLayerRoot`, `GetOpenPopups`, `RegisterPrefab`, `BackKeyUnhandled` 이벤트를 제공합니다. `static Instance => Services.Get<PopupManager>()`(싱글톤 아님). |
| `UIBase` | PopupBase와 UIView의 공통 기본(DisallowMultipleComponent, 직접 상속하지 않음). 버튼은 `public OnButtonEvent(Button)`(재정의 불가, OnClick 진입점) → `protected virtual PlayButtonSound`(`UIBase.ButtonClickSound` const `"Click"` 고정을 SoundManager로 재생, SoundManager 없으면 생략. 설정 필드는 사용자 요청으로 두지 않음) → `protected virtual OnButtonClick`. 2026-10-27 사용자 요청으로 클릭음을 UIBase에서 처리 |
| `PopupBase : UIBase` | 층, 뒤로가기, 열기/닫기 담당. virtual 훅 `PrevOpen`(목록 추가 전)/`PostOpen`(추가 후)/`PrevClose`(제거 전)/`PostClose`(제거 후) — 2026-10-27 사용자 지정 이름, `OnClosed(Action)`(연 쪽에서 닫힌 뒤 할 일 등록, `Open<T>().OnClosed(...)`, 이미 닫혔으면 즉시 실행. 2026-10-27 사용자 요청으로 `Closed` 이벤트 대체), `Close()`(재정의 불가, PrevClose → 목록 제거 → PostClose → OnClosed 콜백 → 즉시 Destroy, Edit 모드에서는 DestroyImmediate). `WaitUntilClosedAsync`. 직렬화 필드는 `_layer`, `_backKey`, `_closeOnSceneChange`입니다. 자식 UIView는 함께 파괴됩니다. |
| `UIView : UIBase` | 팝업 안의 자식 UI(탭, 목록 항목). `Show`/`Hide`(SetActive만, 재정의 불가) + `OnShow`/`OnHide` 훅, `IsVisible`. |

**단어 규칙 (2026-10-27 사용자 결정):** 팝업은 Open/Close(생성/즉시 파괴), 자식 UI는 Show/Hide(SetActive)만 씁니다. 팝업에는 숨김 상태가 없습니다. 바깥에서 부르는 함수는 재정의 불가, 상속한 쪽은 `On~` 훅만 재정의합니다. "프레임워크는 최대한 이해하기 쉬운 방향"이 사용자 원칙입니다.
| `UISettings` (SO) | `Orientation`(`GameOrientation` Portrait 기본/Landscape), 기준 해상도(기본 1080x1920, `ReferenceResolution`은 Orientation에 맞춰 짧은 변/긴 변 순서를 정렬해서 반환), 뒤로가기 처리 여부. Canvas Scaler는 `Expand` 고정(match 옵션은 2026-10-27 사용자 요청으로 제거). 팝업 프리팹 목록은 2026-10-27 사용자 요청으로 제거했고, `[Popup]` 어트리뷰트로 대체했습니다. |
| `OrientationPlayerSettings` (Editor) | `IsApplied`/`Apply`. 자동 회전을 방향에 맞게만 허용(세로형은 정방향만, 가로형은 좌우), 창 모드와 Android 기본 창 크기를 기준 해상도로. `UISettingsEditor`가 실제 해상도 표시, 불일치 경고, 적용 버튼을 그립니다. `DevSampleSetup`도 호출합니다. |
| `PopupAttribute` | `[Popup(Path = "...", LoadType = LoadType.Addressables)]`. PopupManager가 처음 Open할 때 이 값으로 GameObject를 불러와 캐시하고, Dispose 때 Release합니다. `RegisterPrefab`으로 직접 등록한 프리팹이 우선합니다. `Open`은 동기(Addressables WaitForCompletion, WebGL 불가)이고, `OpenAsync`와 `PreloadAsync`도 있습니다. |
| `BackKeyBehavior` | `Close`, `Block`(아래 팝업도 막음), `Ignore`(건너뜀, 띠알람용) |
| `PopupBackKeyHandler` (internal) | Update에서 ESC를 확인합니다. Input System에서는 안드로이드 뒤로가기도 ESC로 들어옵니다. |

결정 사항은 다음과 같습니다.

**정렬과 생명주기**
- **같은 층 정렬:** 같은 층의 정렬은 형제 순서로 정합니다(`SetAsLastSibling`). 그래서 팝업 프리팹 루트에 Canvas를 두면 안 됩니다.
- **버튼 연결:** 런타임 `AddListener`를 쓰지 않습니다(사용자 요청, 2026-10-26). Button OnClick에 public `OnButtonEvent`를 persistent로 연결하며, 에디터 Assign Object가 `BTN_` 버튼을 자동으로 연결합니다. `_closeButton` 필드도 사용자 요청으로 제거했습니다.
- **목록 정리:** Close를 거치지 않고 Destroy된 팝업은 목록을 꺼낼 때 정리합니다(`RemoveAll(null)`).

**입력과 씬 전환**
- **EventSystem:** Play 중에 EventSystem이 없으면 `[UI]` 아래에 만들고, 씬에는 두지 않는 것을 규칙으로 합니다. Input System이 있으면 `InputSystemUIInputModule`을 씁니다.
- **입력 심볼:** asmdef에 `Unity.InputSystem` 참조와 versionDefine `NYONYO_INPUT_SYSTEM`을 추가했고, `ENABLE_INPUT_SYSTEM`과 함께 검사합니다. 프로젝트의 입력 방식은 Input System 전용(activeInputHandler 1)입니다.
- **씬 전환 시 닫기:** `SceneLoaded` 이벤트에서 `CloseOnSceneChange` 팝업을 닫습니다. 화면이 가려진 상태에서 닫히므로 사용자에게 보이지 않습니다.

**개발용 코드 (`Assets/Dev/`)**
- `PopupTest`: 같은 층에 쌓기, 띠알람 띄우기, OK.
- `PopupNotice`: Notice 층, 2초 뒤 자동으로 닫힘, BackKey Ignore.
- `DevSampleSetup`: 메뉴 Tools > Nyonyo Framework > Dev > Setup Samples. 프리팹, DevUISettings, DevInstaller, FrameworkSettings 연결, SampleScene2, Scene List를 자동으로 준비합니다.

## 구현 완료: UI 자동 매퍼 (`Editor/UI/`, 네임스페이스 `Nyonyo.Framework.Editor.UI`, 에디터 전용)

사용자 요구사항(2026-10-26):
- 프리팹 오브젝트 이름에 접두사(IMG_, TXT_ 등)를 붙이면, 스크립트 상단 `#region`에 `[SerializeField] private Image IMG_A;` 형태로 변수를 만듭니다.
- TXT_는 TextMeshPro로 만들고, 접두사 규칙은 SO로 관리합니다.
- 메뉴 Tools/Create UI Script/Popup 또는 UI로 상속 대상을 고릅니다. 경로는 저장 창에서 정하고, 스크립트 이름의 기본값은 프리팹 이름입니다.
- UIBase 하위 컴포넌트 인스펙터에 Generate Script와 Assign Object 버튼을 둡니다.
- UIView를 상속하는 스크립트는 팝업 안의 자식 UI용입니다(원래 UIBase였으나 2026-10-27 분리).

| 타입 | 역할 |
|---|---|
| `UIPrefixSettings` (SO, public) | 접두사 규칙(`Rule`: prefix, 타입 전체 이름), `ScriptNamespace`, `BindButtonClick`. `GetOrCreate()`가 없으면 `Assets/Editor/UIPrefixSettings.asset`을 만듭니다. 가장 긴 접두사가 우선합니다. |
| `UITypeResolver` | 타입 전체 이름을 `TypeCache` Component 타입에서 찾습니다. `UnityEngine.GameObject`는 따로 처리합니다. |
| `UIBindingCollector` (internal) | 루트의 자식을 계층 순서로 훑습니다. 자식에 UIBase가 있으면 그 자식만 포함하고 아래는 건너뜁니다. 필드 타입은 실제 컴포넌트 타입입니다(UI_는 구체 클래스). 이름이 겹치거나 컴포넌트가 없으면 경고 후 건너뜁니다. |
| `UIScriptGenerator` (internal) | `CreateScript`(using, namespace, region, PopupBase면 PostOpen, `OnButtonClick`의 `if (button == BTN_X)` / `else if` / `else base`. 이름이 같은 버튼 문제로 2026-10-27 사용자 요청에 따라 이름 비교 대신 필드 비교. `switch` + `case var _ when`은 사용자가 싫어해서 if 체인으로 결정). `UpdateScript`는 region만 교체하고, 없으면 클래스 `{` 다음에 삽입하며 빠진 using을 추가합니다. 줄바꿈 스타일은 유지합니다. region 안은 `#pragma warning disable CS0169, CS0649`로 감쌉니다. |
| `UIAutoMapper` (public) | `Assign(UIBase, settings?)`: SerializedObject로 필드를 연결하고(타입이 안 맞으면 경고), BTN_ OnClick에 `OnButtonEvent` persistent listener를 겹치지 않게 연결합니다. `GenerateScript(UIBase)`: 스크립트 region을 갱신하고, 바뀌었으면 컴파일 후 자동으로 Assign합니다(SessionState pending + `[DidReloadScripts]` + delayCall). 프리팹 모드면 stage root를 대상으로 하고, 아니면 `LoadPrefabContents`/`SaveAsPrefabAsset`을 씁니다. |
| `UIScriptMenu` | Tools/Create UI Script/Popup, UI. 대상은 프리팹 모드 또는 Project에서 선택한 프리팹입니다. 기존 UIBase가 있으면 거부합니다. `SaveFilePanelInProject`로 경로를 받고(마지막 폴더는 EditorPrefs에 기억), 클래스 이름 유효성과 중복을 검사합니다. 컴파일 후 컴포넌트를 추가하고 Assign합니다. |
| `UIBaseEditor` | `[CustomEditor(typeof(UIBase), true)]`. Generate Script와 Assign Object 버튼이 있고, 동작은 delayCall로 실행합니다. |
| `UIPrefixSettingsEditor` | 찾을 수 없는 타입, 빈 접두사, 중복 접두사를 경고합니다. |

참고 사항:
- **Editor 어셈블리 참조:** Editor asmdef에 `UnityEngine.UI` 참조를 추가했습니다.
- **네임스페이스 주의:** `Nyonyo.Framework.Editor` 네임스페이스 안에서는 `Editor`가 네임스페이스로 해석됩니다. 그래서 커스텀 에디터는 `UnityEditor.Editor`를 전체 이름으로 상속합니다.
- **개발용 샘플:** Dev 샘플 `PopupTest`는 자동 생성 영역(RT_Panel, BTN_*)을 씁니다. `DevSampleSetup`이 `UIAutoMapper.Assign`으로 필드와 버튼을 연결합니다. 샘플 텍스트는 legacy Text라서 TXT_를 붙이지 않았습니다.
- **생성기 결과 확인:** scratchpad의 `harness/`(.NET 10 콘솔)로 컴파일된 Editor DLL의 `UIScriptGenerator`를 리플렉션으로 실행해서 생성 결과를 확인했습니다.

## 구현 완료: 에셋 로딩 (`Runtime/Assets/`, 네임스페이스 `Nyonyo.Framework.Assets`)

사용자 방침(2026-10-27): 게임 구동에 꼭 필요한 최소한의 에셋만 Resources.Load를 쓰고, 나머지는 전부 Addressables를 씁니다.

| 타입 | 역할 |
|---|---|
| `LoadType` | `Resources`(Resources 기준 경로, 확장자 제외), `Addressables`(address, 기본값은 에셋 경로) |
| `IAssetLoader` | `Load<T>`(동기), `LoadAsync<T>`, `Release(path, type)` |
| `AssetLoader` | Addressables 핸들을 경로별 List로 보관합니다(불러올 때마다 핸들 추가, Release마다 하나 반환, Addressables가 참조 횟수를 셉니다). 실패나 취소 시 핸들을 바로 반환하고, Dispose는 남은 핸들을 모두 반환합니다. Resources의 Release는 아무 일도 하지 않습니다. 대기는 `Awaitable.NextFrameAsync` 폴링으로 합니다(Task 미사용). |

- **패키지 의존성:** `com.unity.addressables` **4.1.1**(2026-10-27 레지스트리 최신, unity 6000.0 이상)을 package.json dependencies에 추가했습니다. Runtime asmdef는 `Unity.Addressables`와 `Unity.ResourceManager`를, Editor asmdef는 `Unity.Addressables.Editor`를 참조합니다. 4.1.1 소스에서 API가 1.x/2.x와 같은 것을 확인했습니다.
- **에디터 유틸리티:** `Editor/Assets/AddressableEditorUtility.SetAddress(assetPath, address)`는 `GetSettings(true)`로 설정이 없으면 만들고, FindAssetEntry가 없으면 `CreateOrMoveEntry(DefaultGroup)`를 호출한 뒤 `SetAddress`합니다.
- **Create UI Script > Popup:** `UIScriptGenerator.GetPopupLoadInfo(prefabPath)`가 경로 안에 `/Resources/`가 있으면 Resources와 상대 경로를, 없으면 Addressables와 에셋 경로를 돌려줍니다. 이 값으로 `[Popup]`을 생성하고, Addressables면 프리팹을 자동으로 등록합니다.
- **팝업 인스펙터:** `UIBaseEditor`가 팝업에서 `[Popup]`이 없거나 프리팹 위치와 다르면 경고합니다.

## 구현 완료: 사운드 (`Runtime/Audio/`, 네임스페이스 `Nyonyo.Framework.Audio`)

사용자 요구사항(2026-10-27):
- BGM과 효과음을 구분하고, 효과음은 동시에 8개까지 재생합니다.
- 볼륨은 Master, BGM, SFX와 재생별 볼륨을 두고, 0~1 값의 곱으로 계산합니다.
- `SoundManager.Instance.PlayBGM`과 `PlaySFX`는 문자열 이름을 받아, 예약된 경로에서 찾아 불러온 뒤 재생합니다.

| 타입 | 역할 |
|---|---|
| `SoundSettings` (SO) | LoadType(기본 Addressables), BGM과 SFX 경로 형식(`BGM/{0}`, `SFX/{0}`), Addressables 등록용 폴더(`Assets/Sounds/BGM`, `SFX`), SFX 채널 수(8) |
| `SoundManager` | `static Instance => Services.Get<SoundManager>()`. BGM AudioSource 2개(크로스페이드), SFX AudioSource N개. `PlayBGM(name, volume, fadeSeconds, loop)`, `StopBGM(fade)`, `PlaySFX(name, volume)`는 void(내부에서 `Forget`, 2026-10-27 사용자 요청: 바깥에서 `_ =` 없이), 기다릴 때는 `PlayBGMAsync`/`StopBGMAsync`/`PlaySFXAsync`. `PreloadSFXAsync`, `StopAllSFX`, `UnloadSFX`, `UnloadAllSFX`, `MasterVolume`/`BgmVolume`/`SfxVolume`(clamp, 바뀌면 `VolumeChanged`), `CurrentBgm` |

설계 결정은 다음과 같습니다.

**효과음**
- **채널 선택:** 채널이 사용 중인지는 `EndTime`(시작 시각 + clip.length/pitch, realtimeSinceStartupAsDouble)으로 판단합니다. `isPlaying`은 Edit 모드에서 쓸 수 없기 때문입니다. 빈 채널이 없으면 StartTime이 가장 이른 채널을 씁니다.
- **효과음 캐시:** 효과음은 이름별로 캐시합니다. 동시에 처음 불러오면 먼저 끝난 것만 남기고 나머지는 Release합니다.

**BGM**
- **크로스페이드:** BGM은 바꾸거나 멈출 때 Release합니다. 크로스페이드는 unscaled time으로 진행합니다.
- **새 요청과 취소:** 새 요청은 `_bgmCts`를 Cancel해서 이전 로드나 페이드를 멈춥니다(Dispose하지 않음). 같은 이름을 다시 요청하면 페이드를 취소하고 Fade=1로 돌린 뒤 볼륨만 바꿉니다.

**오류와 미구현**
- **사운드를 찾지 못할 때:** 예외를 던지지 않고 LogError만 남깁니다.
- **볼륨 저장:** 볼륨 저장은 아직 없고, 저장 시스템에서 연동합니다. AudioMixer는 쓰지 않습니다.

**에디터 메뉴:** `Editor/Audio/SoundAddressRegistrar` = Tools > Nyonyo Framework > Sound > Register Sounds to Addressables. 폴더의 AudioClip을 `string.Format(형식, 파일명)` address로 등록합니다. public `Register(SoundSettings)`를 제공합니다.

**개발용 샘플:** `DevSampleSetup`이 사인파 WAV(DevBgmA, DevBgmB, Beep, Click)를 `Assets/Dev/Sounds`에 만들고, DevSoundSettings를 만들어 등록합니다. 팝업 프리팹도 Addressables에 등록합니다. `DevSceneSwitcher`에 BGM, SFX 버튼과 볼륨 슬라이더를 추가했습니다. DevInstaller는 AssetLoader를 서비스로 등록해서 PopupManager와 SoundManager가 함께 씁니다.

## 구현 완료: 저장과 WebGL 대응 (2026-10-27)

**저장 (`Runtime/Save/`, `Nyonyo.Framework.Save`)**
- **기본 구조:** `SaveService`는 `static Instance`를 제공하고 concrete 타입으로 등록합니다. Save, Load, TryLoad, Exists, Delete가 `SaveScope`(Slot, Global)를 받습니다.
- **키 형식:** 키는 `slot{n}.{key}`, `global.{key}` 형식입니다. 슬롯 메타 정보(`slot{n}.__meta`)에 키 목록과 마지막 저장 시각을 보관합니다. PlayerPrefs는 키 목록을 조회할 수 없기 때문입니다.
- **저장소:** `FileSaveStorage`는 임시 파일(tmp)에 쓴 뒤 교체하고, 본 파일이 없으면 tmp를 읽습니다. `PlayerPrefsSaveStorage`는 매번 Save합니다. 기본 저장소는 `#if UNITY_WEBGL && !UNITY_EDITOR`이면 PlayerPrefs입니다.
- **직렬화:** `JsonSaveSerializer`(JsonUtility)는 원시 타입, string, IEnumerable을 넘기면 ArgumentException을 던집니다.
- **버전 변환:** `IVersionedSaveData`는 불러올 때 Migrate한 뒤 다시 저장합니다. 저장된 버전이 코드보다 높으면 경고만 남깁니다.
- **볼륨 저장:** SoundManager는 `sound.volume` 전역 키로 저장합니다. 바뀐 뒤 0.5초 디바운스(realtime)를 두고, Dispose 때 flush합니다.

**WebGL 대응**
- **동기 로드 차단:** WebGL 빌드에서 `AssetLoader.Load`(Addressables 동기)는 NotSupportedException을 던지고, 에디터 빌드 대상이 WebGL이면 경고합니다.
- **미리 불러오기:** `[Popup(Preload = true)]`이고, PopupManager는 `IAsyncInitializable`입니다(실패해도 LogException 후 계속).
- **어셈블리 검색:** `GameAssemblies.Get()`은 Unity, System, nunit을 참조하는 테스트 어셈블리를 뺍니다. 에디터에서 Play할 때 테스트 팝업이 Preload되는 문제 때문입니다.
- **검증:** 실제 WebGL 빌드는 못 했습니다(WebGL 모듈 미설치, 설치된 것은 Android/Windows). 대신 compile-check.ps1의 "WebGL player" 패스(Android IL2CPP 플레이어 엔진 DLL + `UNITY_WEBGL`, `UNITY_EDITOR` 없음)로 확인했습니다.

## 구현 완료: 남은 기능 일괄 (2026-10-27, 사용자 취침 중)

| 네임스페이스 | 핵심 타입과 결정 |
|---|---|
| `Pooling` | `PoolManager`(Spawn/Despawn/DespawnAfter(void)/DespawnAfterAsync/Prewarm/GetPool, 기본 보관 64), `PrefabPool`(자체 Stack, 넘치면 Destroy, 부모가 null이면 활성 씬으로 이동), `PooledObject`(자동 부착, OnDestroy 시 개수 보정), `IPoolable` |
| `StateMachines` | `StateMachine<TState>`(함수 또는 `IState` 등록, 전환 큐로 Enter 안에서도 ChangeState 가능, 같은 상태 재요청은 무시하고 reenter 옵션 제공) |
| `Timing` | 네임스페이스를 `Time`이라고 하면 `UnityEngine.Time`이 가려져서 `Timing`으로 정했습니다. `TimerService`(After/Every, owner 파괴 시 취소, 내부 `Tick` 테스트용 internal 생성자, `TimerRunner`는 별도 파일), `GameTimer`(이름 충돌 회피), `Cooldown`(시계 주입 가능), `PauseService`(요청자 집합, TimeScale, PauseAudio, `PauseChanged`, Dispose 시 timeScale 1) |
| `Tweening` | 2026-10-27 사용자 요청으로 DOTween식 체이닝으로 다시 만들었습니다(아래 "트윈 재설계" 참고). |
| `Inputs` | `UnityEngine.Input`과 겹치지 않게 `Inputs`로 정했습니다. `#if NYONYO_INPUT_SYSTEM`입니다. `InputService`(기본값 `InputSystem.actions`, SwitchMap/PushMap/PopMap은 하나의 맵만 활성, `RebindAsync`는 ESC 취소, 리바인딩 JSON을 `input.bindings` 전역 키로 저장). 코드로 추가한 InputSystemUIInputModule은 DefaultInputActions를 써서 UI 맵 비활성과 무관합니다(소스로 확인). |
| `Cameras` | `CameraFollow2D`(실행 순서 50, 자체 위치를 기억해서 덮어씀), `CameraShake`(실행 순서 100, 지난 프레임에 쓴 값과 비교해서 기준 위치를 정하므로 누적 없음), `CameraMath2D`(정적 함수, 테스트 대상) |
| `Data` | 2026-10-27 구글 시트 기반 게임 데이터로 교체(아래 "게임 데이터" 참고). 예전 SO `DataTable<TRow>`, CSV 매퍼, Import CSV 메뉴는 제거 |
| `Gameplay` | `Health`(IPoolable로 재사용 시 초기화, 시계 주입), `DamageDealer2D`(레이어 마스크, 자기 자신 제외, removeOnHit이면 PoolManager에 반환), `GroundCheck2D`(OverlapBox + ContactFilter, 자기 자신 제외), `SpriteFlash`(color 곱, realtime) |
| `Logging` | `Log`/`LogChannel`. Debug와 Info는 `[Conditional(UNITY_EDITOR, DEVELOPMENT_BUILD, NYONYO_LOG)]`이고, MinimumLevel은 SubsystemRegistration에서 초기화합니다. |
| `Debugging` | `DebugConsole`(`Debug.isDebugBuild`나 옵션일 때만 동작, `Application.logMessageReceived` 구독 후 Dispose에서 해제, IMGUI 뷰, 백쿼트 토글, 기본 명령 help/clear/timescale/scene), `DebugCommandRegistry`(Tokenize는 internal instance, typed 인자, 예외를 문자열로 반환), `[DebugCommand]` |
| Editor | `Scenes/SceneSwitcherWindow`(Tools > Nyonyo Framework > Scene Switcher) |

- **개발용 설치기:** DevInstaller가 모든 서비스를 등록합니다(등록 순서 예시). DevSceneSwitcher에 Pause, Timer, Console 버튼과 `[DebugCommand("dev.notice")]` 예제가 있습니다.
- **경고 정책:** 패키지 코드는 CS0649 경고가 나지 않도록 인스펙터 필드에 명시적인 초기값을 둡니다.

**외부 컴파일 검증**
- Unity가 켜져 있으면 배치 모드를 쓸 수 없습니다. 그래서 scratchpad의 `compile-check.ps1`으로 Unity 내장 Roslyn(`Data/DotNetSdkRoslyn/csc.dll`)을 써서 5개 어셈블리를 컴파일합니다.
- 참조에는 netstandard와 mscorlib shim, Managed/UnityEngine, ScriptAssemblies를 씁니다.
- 2026-10-26 기준 에러 0, 경고 0입니다.
- **스크립트 개선(2026-10-27):**
  - asmdef의 references만 참조로 넘겨서 asmdef 참조 누락도 잡습니다.
  - Unity가 Addressables를 아직 받지 않았다면 scratchpad `stubs/`의 스텁(4.1.1 소스 시그니처를 옮긴 것)으로 컴파일합니다.
  - 결과는 패키지 3개 에러 0, 경고 0이고, Dev는 CS0649 경고만 있습니다.
- **인코딩 주의:** PowerShell 5.1은 BOM 없는 UTF-8 .ps1의 한글 문자열을 깨뜨립니다. 스크립트 안의 문자열은 영어로 씁니다.

### 트윈 재설계 (2026-10-27, `Runtime/Tweening/`)

사용자 요청: "Tween 함수를 DOTween 참고해서 추가(DOMoveX, DOMove, DOScale, DOFade 등)". API 형태는 DOTween식 체이닝(추천안)을 사용자가 골랐습니다. 기존 `*Async` 함수는 게임 코드에서 쓰이지 않아 DO* 함수로 대체했습니다.

| 타입 | 역할 |
|---|---|
| `Tween` (abstract) | 공통 핸들. `Kill(complete)`, `Complete`, `Restart`, `Rewind`, `TogglePause`, `GetAwaiter`/`WaitForCompletionAsync`(AwaitableCompletionSource를 대기마다 새로 만듦). 정적 `To`(float/int/Vector2/Vector3/Color/직접 보간), `DelayedCall`, `KillTarget`, `KillAll`, `IsTweening`, `ActiveCount` |
| `Tweener<T>` | 값 트윈. `SetRelative`, `From()`, `From(value)`. 시작 값은 처음 움직일 때(지연 뒤) 읽습니다. |
| `Sequence` | `Create`, `Append`, `Join`, `Insert`, `AppendInterval`, `AppendCallback`, `InsertCallback`. 기본 곡선 Linear |
| `TweenExtensions` | 제네릭 체이닝(`where T : Tween`): Set*, On*, Play, Pause |
| `TransformTweenExtensions`, `UITweenExtensions`, `ComponentTweenExtensions` | DO* 단축 함수와 `DOKill`/`DOComplete` |
| `TweenManager`(internal static), `TweenRunner` | 루트 트윈 목록. Play 모드에서 첫 트윈 생성 시 `[Tween]` 러너를 Bootstrapper.Root 아래(없으면 DontDestroyOnLoad)에 만듭니다. 테스트는 `TweenManager.Update(dt, udt)`와 `Clear()`를 직접 호출합니다. |

설계 결정은 다음과 같습니다.
- **시간 모델:** 모든 트윈은 `Goto(경과 시간)`으로 상태를 계산합니다(지연, 반복, Yoyo, Incremental 포함). 시퀀스는 자식에게 `Goto(시퀀스 시간 - 위치)`를 호출하고, 되감을 때는 뒤에서부터 처리해 앞 트윈의 시작 값이 남게 합니다. 같은 진행도면 다시 적용하지 않아 끝난 자식이 뒤 자식 값을 덮어쓰지 않습니다.
- **Restart 반복의 시퀀스:** 반복이 바뀌면 처음으로 되감은 뒤 다시 재생하고, 콜백도 다시 호출합니다.
- **중첩 트윈:** 시퀀스에 넣으면 매니저 목록에서 빠지고, 직접 제어하면 경고만 남깁니다. 시작된 트윈과 무한 반복 트윈은 넣을 수 없습니다(예외).
- **종료:** 대상(또는 SetLink) 파괴 시 조용히 Kill, setter 예외는 LogException 후 그 트윈만 Kill, 콜백 예외는 LogException 후 계속. Kill은 await를 정상 완료시키고, `SetCancellation` 토큰 취소만 `OperationCanceledException`입니다.
- **2D 기준:** float 세기 Shake는 위치와 크기는 XY, 회전은 Z만 흔듭니다. Jump의 기본 곡선은 Linear입니다. Punch와 Shake는 Incremental 반복을 Restart처럼 처리합니다.
- **DORotate:** `RotateMode.Fast`는 쿼터니언 Slerp(최단 경로), `FastBeyond360`은 오일러 각 보간입니다. 반환 타입은 둘 다 `Tweener<Vector3>`입니다.
- **보류:** DOPath, DOText(TMP 참조 없음, README에 `maxVisibleCharacters` 예시), DOBlendable*, PlayBackwards.

## 구현 완료: 광고 (2026-10-27)

사용자 요청: "애드몹을 넣고 싶다". 모듈 분리 구조(추천안)에 동의했고, 광고 형식 4종(배너, 전면, 보상형, 앱 오프닝)과 UMP 동의를 모두 지원하기로 했습니다.

**코어 (`Runtime/Ads/`, `Nyonyo.Framework.Ads`)**

| 타입 | 역할 |
|---|---|
| `IAdService` | 게임 코드가 쓰는 유일한 인터페이스. Show*Async는 보여 줄 광고가 없으면 바로 false |
| `AdServiceBase` | 공통 규칙: 광고 제거(`ads.removed` 전역 키 저장), 전체 화면 광고 한 번에 하나, `AdPolicy.FullScreenCooldownSeconds`(기본 10초, 2026-10-27 사용자 요청. 보상형 닫힘도 대기 시작, 실패는 제외), 초기화 전 배너 요청 기억, `OnAppForeground`(앱 오프닝, `ShowAppOpenOnResume` 기본 false로 2026-10-27 사용자 요청, `SkipNextAppOpen` 1회), 이벤트 예외 격리. 시계는 생성자 주입 |
| `AdShowResult` | 하위 클래스 `PresentAsync` 결과(Failed/Closed/Rewarded) |
| `FakeAdService`, `FakeAdView` | 에디터용. IMGUI로 TEST 배너와 전체 화면 창, UGUI 투명 막(sortingOrder short.MaxValue)으로 아래 UI 클릭 차단. 테스트는 internal `Respond`, `SimulateAppForeground` |
| `NullAdService` | WebGL, PC용. 보상형은 기본으로 광고를 본 것으로 보고 바로 보상(`AdPolicy.RewardWithoutAdOnUnsupportedPlatform`, 기본 true, 끌 수 있음). 2026-10-27 사용자 결정: 게임에서 설정하지 않아도 보상을 주는 쪽이 기본 |
| `AdServiceExtensions` | 콜백형 `ShowRewarded(onRewarded, onNotRewarded)`, `ShowInterstitial(onClosed)` (CLAUDE.md 규칙 1: 호출하는 쪽이 async를 신경 쓰지 않게) |

**AdMob 모듈 (`Packages/com.nyonyo.framework.admob/`)**
- **`AdMobSettings`(SO):** 플랫폼별 `AdUnitIds`, `UseTestAds`(기본 켬, `AdMobTestIds` 상수), 테스트 기기, `RequestConsent`, UMP `DebugGeography`, `AdPolicy`, `AdMobEditorMode`(Fake 기본/AdMobPlaceholder). `CreateService(parent, save)`는 에디터면 모드에 따라, Android/iOS면 AdMob, 그 밖은 Null을 돌려줍니다(정적 팩토리 대신 인스턴스 메서드).
- **`AdMobAdService`:** `IInitializable`에서 시작하고 게임 시작을 막지 않습니다. 순서는 UMP Update → LoadAndShowConsentFormIfRequired → `CanRequestAds`면 MobileAds.Initialize → 전면/보상형/앱 오프닝 미리 불러오기 → MarkInitialized입니다.
  - **스레드:** 모든 SDK 콜백은 `MobileAdsEventExecutor.ExecuteInUpdate`로 메인 스레드에 옮깁니다. `RaiseAdEventsOnUnityMainThread`는 쓰지 않습니다.
  - **재시도:** 2^n초(최대 64), realtime 기준입니다.
  - **앱 오프닝 만료:** 4시간(UTC)이 지나면 IsLoaded에서 버리고 다시 불러옵니다.
  - **보상 판정:** 보상 콜백이 늦는 기기를 위해 닫힘 뒤 한 프레임 기다려 결과를 정합니다.
  - **배너:** 적응형(`GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(FullWidth)`)입니다. 위치가 같으면 Show/Hide로 재사용하고, 숨긴 뒤 로드가 끝나도 다시 숨깁니다.
  - **일시정지:** `SetiOSAppPauseOnBackground(true)`로 iOS에서도 광고 중 Unity가 멈춥니다.
- **에디터:** `GoogleMobileAdsAppIds`가 플러그인 설정 에셋(`Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`, 클래스가 internal이라 필드 `adMobAndroidAppId`/`adMobIOSAppId`를 SerializedObject로 접근)을 읽고 씁니다. `AdMobSettingsEditor`(앱 ID 상태, 테스트 앱 ID 채우기), `AdMobBuildCheck`(앱 ID 비면 에러, 출시 빌드의 테스트 광고나 테스트 앱 ID는 경고)가 있습니다.
- **AdMob 11.6.0에서 확인한 사실:** 최소 Android API 24입니다(이 프로젝트는 25). 의존성은 `com.unity.external-dependency-manager` 2.1.0(Unity 레지스트리)이고, 플러그인이 EDM을 직접 설치합니다. DLL은 기본 임포터라 자동 참조됩니다. UMP는 `GoogleMobileAds.Ump.Api`입니다.
- **검증:** compile-check.ps1에 AdMob 패스를 추가했습니다. PackageCache에 없으면 scratchpad `admob-pkg/`에 받은 11.6.0 tarball DLL을 씁니다. Unity Play 모드와 실제 기기 광고는 확인하지 못했습니다.
- **보류:** 미디에이션, 네이티브 광고, 보상형 전면, 연령 제한 설정(`AgeRestrictedTreatment`), Dev 샘플 연결(다른 세션이 Dev 파일을 수정 중이라 건드리지 않음).

## 구현 완료: 게임 데이터 (2026-10-27, `Runtime/Data/`, `Editor/Data/`)

사용자 요청: 구글 스프레드시트에서 데이터를 가져와 클래스를 생성하는 기능입니다. 생성 클래스는 게임 폴더에 모이는 partial이고, 데이터는 AOT에서 가장 빠르게 읽히고 암호화되어야 합니다.
- **결정:** 코어에 넣고 기존 Data를 교체(사용자 선택)했습니다. 시트 이름은 SO에 적습니다.
- **시트 접근(2026-10-27 사용자 변경):** 처음에는 B. 링크 공유(뷰어)였습니다. 사용자가 "링크 공유는 쓰지 않는다"고 해서 **구글 서비스 계정** 방식으로 바꿨습니다. 시트는 "제한됨"으로 두고, 서비스 계정 이메일만 뷰어로 추가합니다. 링크 공유 코드는 지웠습니다.
- **시트 규칙(사용자 지정):**
  - 1행 주석, 2행 자료형, 3행 변수명입니다.
  - 첫 번째 열은 무조건 유니크 키이고, 겹치면 에러입니다.
  - 변수명이나 키가 `_`로 시작하면 건너뜁니다.
- **배열 결정:** List가 아니라 배열(추천안)입니다. 같은 변수명의 열이 원소가 됩니다.

**구현 결정**
- **서비스 계정 인증:** 문서로 확인한 규칙(developers.google.com/identity/protocols/oauth2/service-account)을 따릅니다.
  - **JWT:** `ServiceAccountAssertion`이 RS256 JWT를 만듭니다. iss는 client_email, scope는 `drive.readonly`, aud는 token_uri, exp는 iat+3600입니다.
  - **토큰:** `https://oauth2.googleapis.com/token`에 grant_type=jwt-bearer로 POST해 액세스 토큰을 받습니다.
  - **키 해석:** `RsaPrivateKeyParser`가 PKCS#8과 PKCS#1 PEM을 DER로 직접 읽습니다. Mono에는 ImportPkcs8PrivateKey가 없을 수 있어서, RSAParameters의 길이를 맞춰 `ImportParameters`로 넘깁니다.
- **키 파일 보관:** `ServiceAccountKey`가 JsonUtility로 읽습니다. 경로는 에셋이 아니라 EditorPrefs(`Nyonyo.GameData.ServiceAccountKey.{에셋 GUID}`)에 저장하고, 비어 있으면 환경 변수 `GOOGLE_APPLICATION_CREDENTIALS`를 씁니다.
  - **인스펙터:** 키 파일 선택, 서비스 계정 이메일 표시와 복사가 있습니다. 키가 git 저장소 안에 있으면 경고합니다.
- **xlsx 내려받기:** Drive API `GET https://www.googleapis.com/drive/v3/files/{id}/export?mimeType=...spreadsheetml.sheet`에 Bearer 토큰을 붙입니다. 내보내기 한도는 10MB입니다.
  - **에러 안내:** 403/404는 서비스 계정을 뷰어로 추가하라고 안내합니다. accessNotConfigured/SERVICE_DISABLED는 Drive API를 켜라고 하고, exportSizeLimitExceeded와 fileNotExportable도 각각 안내합니다.
  - **CSV 대신 xlsx를 쓰는 이유:** 시트 이름 CSV(gviz)는 열 자료형을 다수결로 추정해서 숫자 열의 1~3행 문자열을 지웁니다. xlsx는 한 번에 모든 탭을 이름과 원본 값으로 줍니다. `XlsxReader`가 ZipArchive와 XDocument로 직접 읽습니다(외부 라이브러리 없음).
- **SO 시트 목록:** 비워 두면 `_`로 시작하지 않는 모든 시트를 가져옵니다.
- **생성 코드:**
  - **클래스:** 시트 `Item`이면 `ItemData`(행, `{ get; private set; }`, `partial void OnLoaded()`, internal `Read`)와 `ItemTable : DataTable<TKey, ItemData>`(Name, SchemaHash, ReadRow, GetKey)를 만듭니다. 서비스 클래스 `GameData : GameDataBase`(테이블 속성, `Instance`, `s_encryptionKey`, LoadType, PathFormat)도 만듭니다.
  - **이름 충돌 방지:** 프레임워크 형식은 `global::`로 씁니다.
  - **확장:** 테이블과 GameData는 `protected override void OnLoaded()`로 확장합니다.
  - **생성 파일 표시:** 첫 줄에 `GameDataCodeGenerator.Marker`를 넣습니다. 이 표시가 있는 `*.g.cs`만 오래된 파일 정리 대상입니다.
- **바이너리:**
  - **구조:** `[Magic NYGD][Version 1][SchemaHash 8][행 수][행...]`입니다. 리틀 엔디언이고, 문자열은 바이트 수와 UTF-8, 배열은 개수와 값입니다.
  - **SchemaHash:** 시트 이름, 변수명, 자료형, enum 멤버 이름과 값의 FNV-1a 해시입니다. 코드와 데이터가 다르면 로드할 때 `InvalidDataException`이 납니다.
- **암호화:** `GameDataCrypto`는 AES-256-CBC와 HMAC-SHA256(encrypt-then-MAC)입니다.
  - **IV:** 원문의 HMAC에서 만듭니다. 결정적이라 바뀌지 않은 시트는 git diff가 없습니다.
  - **키:** 64바이트이고 `GameDataSettings._encryptionKey`(Base64, HideInInspector)에 저장해 커밋합니다.
  - **런타임:** 복호화만 하므로 RNG가 필요 없습니다.
- **가져오기 흐름(`GameDataImporter`, public):** 설정 검사 → 내려받기(UnityWebRequest 블로킹 루프, 취소 가능, 60초 타임아웃) → 해석 → 에러가 하나라도 있으면 파일을 하나도 바꾸지 않고 에러를 모두 보고 → 코드와 bytes 쓰기(내용이 같으면 건너뜀) → 오래된 파일 정리 → Refresh → Addressables 등록.
  - **Resources 경로:** LoadType이 Resources면 데이터 폴더가 Resources 안에 있어야 하고, 경로 형식을 자동으로 계산합니다.
- **enum 찾기:** 전체 이름이 맞으면 그것을 씁니다. 아니면 게임 어셈블리(Unity, System, Mono, nunit 제외)에서 짧은 이름으로 찾고, 여러 개면 네임스페이스까지 적으라는 에러를 냅니다.
- **값 해석:**
  - **빈 칸:** 단일 값은 기본값입니다.
  - **정수:** `3.0` 같은 정수 표현을 받습니다.
  - **bool:** TRUE/FALSE, 1/0, yes/no, y/n, O/X를 받습니다.
  - **enum:** 이름만 받습니다(숫자 거부).
  - **xlsx 숫자 셀:** 정수는 그대로 두고, 소수는 `R` 형식으로 다시 써서 0.1을 유지합니다.
- **검증:**
  - **하네스:** scratchpad `testrunner/`(.NET 10 콘솔)가 컴파일된 테스트 DLL을 리플렉션으로 실행합니다(test 모드). generate 모드는 예시 시트로 생성 코드와 Item.bytes를 만들고, csc로 컴파일한 뒤 verify 모드로 로드합니다.
  - **결과:** `Item.Get(1001)`과 게임 쪽 partial `OnLoaded`까지 확인했습니다.
  - **확인하지 못한 것:** 실제 구글 시트 내려받기, Unity 에디터 가져오기, Addressables 등록, 런타임 로드(`LoadAllAsync`)입니다.
- **보류:** 테이블 간 참조 자료형, Vector 같은 복합 자료형, 셀 하나에 구분자로 배열 넣기, 데이터 파일 하나로 합치기.

### 테스트 (`Tests/Editor/`)

- **테스트 파일:** `ServiceRegistryTests` 13개, `ServicesTests` 3개, `EventBusTests` 16개(여기까지 검증됨), `SceneServiceTests` 8개, `PopupManagerTests` 29개, `UIViewTests` 4개, `UIAutoMapperTests` 16개, `SoundManagerTests` 19개, `UISettingsTests` 5개, `SaveServiceTests` 15개, `PoolManagerTests` 8개, `StateMachineTests` 7개, `TimingTests` 11개, `TweenTests` 39개, `InputServiceTests` 5개, `GameplayTests` 10개, `DebuggingTests` 8개, `AdServiceTests` 17개, AdMob 모듈 `AdMobSettingsTests` 3개(Unity 검증 안 됨), 게임 데이터 `SheetTableParserTests` 15개, `XlsxReaderTests` 4개, `GameDataTests` 8개, `GameDataCodeGeneratorTests` 2개, `ServiceAccountTests` 5개(.NET 하네스로 실제 실행해 34개 통과, Unity Test Runner는 아직, 총 263개).
- **스크립트 실행 주의:** compile-check.ps1 실행 결과에 `| Select-Object -First N`을 붙이면 파이프라인이 멈춥니다. 그냥 실행하세요.
- **테스트용 MonoBehaviour 위치(2026-10-27 실패에서 배움):** Editor 전용 어셈블리의 MonoBehaviour는 `AddComponent`할 수 없습니다("Can't add script behaviour ... because it is an editor script"). 그래서 테스트용 컴포넌트는 런타임 테스트 어셈블리 `Tests/Runtime/TestComponents/`(네임스페이스 `Nyonyo.Framework.Tests`)에 둡니다. Editor 테스트 asmdef가 이 어셈블리를 참조합니다. ScriptableObject(`TestItemTable`)는 Editor 어셈블리에 있어도 됩니다. 외부 컴파일로는 잡을 수 없는 문제입니다.
- **PowerShell로 파일을 수정하지 말 것(2026-10-27 사고):** PowerShell 5.1의 `Get-Content -Raw`는 BOM 없는 UTF-8을 CP949로 읽어서 한글을 깨뜨립니다. 소스 파일 수정은 반드시 Edit/Write 도구로 하세요. 이번에 깨진 테스트 파일 4개는 git checkout으로 되돌렸습니다. `FakeAssetLoader`(Tests/Editor)는 로드와 반환을 기록하는 가짜 로더입니다. 실제 Addressables 로드와 `AssetLoader`는 자동 테스트가 없습니다.
- **asmdef 참조 수정:** 테스트 asmdef에 빠져 있던 `UnityEngine.UI` 참조를 2026-10-27에 추가했습니다.. `FadeTransition`과 뒤로가기 키 입력은 자동 테스트가 없습니다. 테스트용 팝업은 `Tests/Editor/TestPopups/`에 둡니다(파일 하나에 클래스 하나).
- **SceneService 테스트 방식:** 가짜 `ISceneLoader`와 `ISceneTransition`을 넣고, 즉시 완료되는 Awaitable로 동기 검증합니다.
- **의도된 에러 로그:** `EventBusTests`의 `handler failed` 예외 로그도 `LogAssert.Expect`로 검증하는 의도된 로그입니다.
- **비동기 처리:** 테스트 서비스가 모두 즉시 완료되므로 `InitializeAsync().GetAwaiter()`로 동기 검증합니다. async 테스트 지원 여부에 의존하지 않으려고 이렇게 했습니다.
- **Bootstrapper:** 자동 테스트가 없습니다. Play 모드에서 직접 확인해야 합니다.

## 플랫폼 대응 (Android, iOS, WebGL, PC)

2026-10-27 사용자 결정: 네 플랫폼을 처음부터 함께 대응합니다. 작업 규칙과 체크리스트는 CLAUDE.md 기본 규칙 5에 있습니다.
실제 빌드는 아직 어느 플랫폼에서도 해 보지 않았습니다(설치된 빌드 모듈: Android, Windows). WebGL은 compile-check의 WebGL 패스로만 확인합니다.

같은 날 점검한 결과와 조치는 다음과 같습니다.

| 기능 | Android | iOS | WebGL | PC | 비고 |
|---|---|---|---|---|---|
| 부트스트랩, 서비스 | O | O | O | O | `[assembly: AlwaysLinkAssembly]`로 IL2CPP에서 어셈블리가 빠지지 않게 함. 모바일과 WebGL은 `Application.quitting`이 안 올 수 있음 |
| 팝업, UI | O | O | O | O | `[Popup]`에 `[RequireAttributeUsages]`. WebGL은 Addressables 동기 로드 불가라 `Preload`나 `OpenAsync` |
| 안전 영역 | O | O | 해당 없음 | 해당 없음 | `SafeAreaFitter` 추가. PC와 WebGL은 화면 전체라 그대로 동작 |
| 뒤로가기 | 뒤로가기 버튼 | 없음(화면 버튼 필요) | ESC(브라우저가 가로챌 수 있음) | ESC | |
| 화면 방향 | 회전 제한 | 회전 제한 | 기본 캔버스 크기 | 창 크기, 세로형은 창 모드 | `OrientationPlayerSettings`가 한 번에 설정. 창과 캔버스는 기준 해상도의 절반 |
| 사운드 | O | O | O(첫 입력 전에는 브라우저가 소리를 막음) | O | 볼륨은 포커스를 잃을 때도 저장 |
| 저장 | 파일 | 파일 | PlayerPrefs(약 1MB) | 파일 | |
| 입력 | O | O | O | O | 리바인딩 취소는 ESC라 주로 PC와 게임패드용 |
| 디버그 콘솔 | 세 손가락 탭 | 세 손가락 탭 | 백쿼트 / 세 손가락 탭 | 백쿼트 | `[DebugCommand]`가 `PreserveAttribute`를 상속해 IL2CPP에서도 명령이 남음 |
| 트윈, 풀링, 타이머, FSM, 카메라, 데이터 | O | O | O | O | 스레드나 파일 입출력 없음 |
| 광고 | AdMob | AdMob | `NullAdService` | `NullAdService` | 웹과 PC는 광고 없음. 보상형은 기본으로 광고 없이 바로 보상(끌 수 있음). 웹 광고 네트워크 연동은 실제로 붙일 게임이 생기면 진행 |

## 다음 할 일: 배포 레벨(DEV/SERVICE)과 빌드 메뉴 (2026-10-27 검토, 구현 전)

**사용자 요구**
- 빌드마다 배포 레벨을 **DEV** 또는 **SERVICE**로 정합니다. Unity의 Development Build 체크와는 **별개**입니다(SERVICE + Development Build 같은 조합도 가능해야 함).
- 테스트용 설정(예: AdMob Use Test Ads)은 직접 체크하지 않고 **배포 레벨이 DEV일 때만 true**가 되게 합니다. 다른 설정 SO도 대응할 수 있는 것은 모두 대응합니다.
- 빌드는 에디터 메뉴(**Tools/Build** 같은 창)에서 진행하고, 거기서 DEV/SERVICE와 플랫폼(Android, iOS, WebGL, PC)을 입력받습니다.
- 사용자 원칙대로 설계안과 선택지는 추천안과 함께 먼저 보여 주고, 고른 뒤 구현합니다.

**런타임에서 레벨을 아는 방법 (추천안, 확정 전)**
- 빌드 창이 스크립팅 심볼 `NYONYO_DEV` 또는 `NYONYO_SERVICE`를 넣어 빌드합니다. 컴파일 시점에 정해지므로 DEV 전용 코드를 SERVICE 빌드에서 빼낼 수도 있습니다.
- 코드는 `DeployLevel` enum과 읽기 전용 진입점(예: `Deploy.Level`, `Deploy.IsDev`)으로 묻습니다. 심볼이 없으면(에디터 Play 등) DEV로 봅니다. 에디터에서 SERVICE로 시험할 수 있게 덮어쓰기 옵션을 둘지 정해야 합니다.
- 대안: 빌드할 때 Resources에 BuildInfo 에셋을 써 넣는 방식. 버전과 빌드 번호를 함께 담기 좋습니다.

**수정해야 할 곳 (검토 결과)**

| 대상 | 지금 | 바꿀 내용 |
|---|---|---|
| `AdMobSettings._useTestAds` | 직접 체크(기본 true) | 필드를 없애고 DEV면 테스트 광고, SERVICE면 실제 광고 단위 ID. 빌드 창이 SERVICE인데 실제 ID가 비어 있으면 빌드를 막음 |
| `AdMobSettings._testDeviceIds` | 항상 적용 | DEV에서만 적용 |
| `AdMobSettings._consentDebugGeography` | 직접 설정(출시 전에 Disabled로 돌려야 함) | SERVICE에서는 무조건 Disabled |
| `AdMobBuildCheck`, `GoogleMobileAdsAppIds`(에디터) | 앱 ID가 비면 경고, 테스트 앱 ID 넣기 버튼 | SERVICE 빌드는 테스트 앱 ID면 빌드 실패. 테스트 ID 넣기 버튼은 DEV 용도로 안내 |
| `DebugConsole.IsEnabled` | `Debug.isDebugBuild` 또는 `enableInReleaseBuild` | DEV 레벨 기준으로 변경(Development Build와 분리) |
| `Log`의 Debug, Info | `[Conditional]` UNITY_EDITOR, DEVELOPMENT_BUILD, NYONYO_LOG | `NYONYO_DEV`도 포함할지, SERVICE에서는 빼고 Warning 이상만 남길지 결정. `MinimumLevel` 기본값도 레벨별로 |
| `FrameworkSettings` 설치기 목록 | DevInstaller(Assets/Dev, DevSceneSwitcher 등)가 들어 있음 | SERVICE 빌드에 Dev 설치기와 Dev UI가 들어가지 않게 해야 함. 설치기에 "DEV 전용" 표시를 두거나, Assets/Dev asmdef에 `NYONYO_DEV` define constraint |
| 빌드 씬 목록 | Build Profiles Scene List(SampleScene, SampleScene2) | 빌드 창에서 레벨별 씬 목록을 정함(DEV 전용 테스트 씬 제외) |
| 저장(`SaveService`) | 레벨 구분 없음 | 같은 기기에 DEV와 SERVICE를 함께 설치하면 저장이 섞일 수 있음. 저장 폴더나 키 접두어를 레벨별로 나눌지 결정(번들 ID를 다르게 하면 불필요) |
| 번들 ID, 앱 이름 | Player Settings 한 벌 | DEV 빌드는 `.dev` 번들 ID와 "[DEV]" 앱 이름을 쓸지 결정(같은 기기에 두 빌드 공존) |
| `GameDataSettings`(다른 작업에서 만든 구글 시트 데이터) | 시트 주소 한 벌 | DEV와 SERVICE가 다른 시트를 쓸지 사용자에게 확인 |
| `UISettings`, `SoundSettings`, `UIPrefixSettings` | 레벨과 무관 | 변경 없음 |

**빌드 창(Tools/Build)에 넣을 것**
- **입력:** 배포 레벨(DEV/SERVICE), 플랫폼(Android, iOS, WebGL, Windows, macOS, Linux), Development Build 여부(레벨과 별개), 버전과 빌드 번호, 출력 폴더.
- **실행:** 스크립팅 심볼 설정 → 레벨별 검사(SERVICE면 테스트 광고 ID나 테스트 앱 ID가 남아 있지 않은지 등) → 씬 목록 → 플랫폼 전환 → `BuildPipeline.BuildPlayer` → 심볼 원복.
- **보관:** 마지막 입력값은 EditorPrefs에 기억합니다.
- **기존 설정 재사용:** Player Settings에 적용(`OrientationPlayerSettings`)과 Addressables 빌드도 빌드 창에서 함께 실행할지 결정합니다.

**그다음 할 일: DEV 워터마크 (2026-10-27 사용자 요청, 배포 레벨 작업 뒤에 진행)**
- **조건:** 배포 레벨이 DEV일 때만 앱의 최상단 레이어 하단에 워터마크를 띄웁니다. SERVICE에서는 만들지도 않습니다.
- **내용:** `[ 플랫폼, 클라 버전, 빌드 버전 ]` 형식으로 시작합니다(예: `[ Android, 1.2.0, 345 ]`).
- **표시 위치:** `UILayer`(게임 UI 5단계)에 넣지 않습니다. `UILayer.Top`은 게임 안에서 쓰는 레이어 중 하나일 뿐입니다(사용자 정정).
  - 워터마크는 모든 것보다 위에 그립니다. 씬 전환 페이드(30000), 디버그 콘솔, 광고 테스트 화면도 포함합니다.
  - **IMGUI(`OnGUI`)로 그립니다(2026-10-27 사용자 결정).** IMGUI는 모든 Canvas보다 위에 그려지므로, 페이드와 게임 UI 전부를 덮습니다.
  - 다른 IMGUI(DebugConsole `GUI.depth = -1000`, DevSceneSwitcher, FakeAdView)보다도 위에 오도록 `GUI.depth`를 가장 작게 둡니다(예: `short.MinValue`).
  - PopupManager와 무관한 MonoBehaviour로 만들어 Bootstrapper 루트에 붙입니다.
  - 해상도가 달라도 읽히도록 DebugConsoleView처럼 짧은 변 720 기준으로 크기를 조절합니다. IMGUI 라벨은 클릭을 막지 않습니다.
- **화면 처리:** 화면 하단에 두고 `SafeAreaFitter`로 홈 인디케이터를 피합니다. 클릭을 막지 않도록 `raycastTarget = false`로 둡니다.
- **값 출처:**
  - 플랫폼은 `Application.platform`에서 가져옵니다.
  - 클라 버전은 `Application.version`에서 가져옵니다.
  - 빌드 버전은 Android `bundleVersionCode`, iOS `buildNumber`처럼 런타임에서 바로 못 읽는 값입니다. 빌드 창이 BuildInfo 에셋 등에 기록해야 하므로, 배포 레벨 작업 때 BuildInfo 방식을 함께 고려합니다.

## 코드 컨벤션

**이름과 구조**
- private 필드는 `_camelCase`, private static 필드는 `s_camelCase`로 씁니다.
- 접근 제한자는 private도 생략하지 않고 항상 적습니다(2026-10-27 사용자 요청으로 변경, CLAUDE.md 기본 규칙 3).
- 기본 네임스페이스는 `Nyonyo.Framework`입니다. 클래스 이름과 겹치는 하위 네임스페이스는 만들지 않습니다(예: `Services` 클래스).
- 기능별 폴더는 `Runtime/<기능>/` 아래에 둡니다.
- 설정 에셋(ScriptableObject 설정)은 `[CreateAssetMenu]`를 쓰지 않고, `SettingsMenu.MenuRoot`(Tools > Nyonyo Framework > Settings)에 `SelectOrCreate`로 메뉴를 추가합니다(2026-10-27 사용자 요청). 선택 모듈은 자기 Editor 어셈블리에서 같은 메뉴에 붙입니다.

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
