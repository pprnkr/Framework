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

## 구현 완료: 씬 관리 (`Runtime/Scenes/`, 네임스페이스 `Nyonyo.Framework.Scenes`)

| 타입 | 역할 |
|---|---|
| `ISceneService` / `SceneService` | `LoadAsync(sceneName)`는 `Awaitable<bool>`을 반환합니다(중복 요청이면 false). `IsLoading`, `Progress`, `MinimumLoadingSeconds`를 제공하고, `IDisposable`입니다. |
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
- `PopupManager.Instance.Show<PopupTest>()` 형태로 띄웁니다. 싱글톤이 아니어도 됩니다.
- `PopupBase`에 `Close`와 `ButtonEvent`를 공통 처리하는 virtual 함수를 두고, `Close` 안에서 `Destroy`합니다.

| 타입 | 역할 |
|---|---|
| `UILayer` | `Screen`(0), `Popup`(1), `System`(2), `Notice`(3), `Top`(4). 층마다 ScreenSpaceOverlay Canvas를 두고, 정렬 순서는 1000 + 층×1000입니다. 페이드(30000)가 그 위에 그려집니다. |
| `PopupManager` | `Show<T>(UILayer? layer)`, `Find<T>`, `IsOpen<T>`, `CloseAll(layer?)`, `HandleBack()`, `GetLayerRoot`, `GetOpenPopups`, `RegisterPrefab`, `BackKeyUnhandled` 이벤트를 제공합니다. `static Instance => Services.Get<PopupManager>()`(싱글톤 아님). |
| `UIBase` | 모든 UI의 기본(DisallowMultipleComponent). `Show`/`Hide`(활성화 + `OnShow`/`OnHide` 훅), `IsVisible`, `public virtual OnButtonEvent(Button)`(기본은 아무것도 안 함) |
| `PopupBase : UIBase` | 층, 뒤로가기, 닫기 담당. virtual `OnOpen`, `Close()`(Destroy, Edit 모드에서는 DestroyImmediate). `Closed` 이벤트, `WaitUntilClosedAsync`. 직렬화 필드는 `_layer`, `_backKey`, `_closeOnSceneChange`입니다. |
| `UISettings` (SO) | 기준 해상도(1920x1080), match 0.5, 뒤로가기 처리 여부. 팝업 프리팹 목록은 2026-10-27 사용자 요청으로 제거했고, `[Popup]` 어트리뷰트로 대체했습니다. |
| `PopupAttribute` | `[Popup(Path = "...", LoadType = LoadType.Addressables)]`. PopupManager가 처음 Show할 때 이 값으로 GameObject를 불러와 캐시하고, Dispose 때 Release합니다. `RegisterPrefab`으로 직접 등록한 프리팹이 우선합니다. `Show`는 동기(Addressables WaitForCompletion, WebGL 불가)이고, `ShowAsync`와 `PreloadAsync`도 있습니다. |
| `BackKeyBehavior` | `Close`, `Block`(아래 팝업도 막음), `Ignore`(건너뜀, 띠알람용) |
| `PopupBackKeyHandler` (internal) | Update에서 ESC를 확인합니다. Input System에서는 안드로이드 뒤로가기도 ESC로 들어옵니다. |

결정 사항은 다음과 같습니다.

**정렬과 생명주기**
- **같은 층 정렬:** 같은 층의 정렬은 형제 순서로 정합니다(`SetAsLastSibling`). 그래서 팝업 프리팹 루트에 Canvas를 두면 안 됩니다.
- **버튼 연결:** 런타임 `AddListener`를 쓰지 않습니다(사용자 요청, 2026-10-26). Button OnClick에 public `OnButtonEvent`를 persistent로 연결하며, 에디터 Assign Object가 `BTN_` 버튼을 자동으로 연결합니다. `_closeButton` 필드도 사용자 요청으로 제거했습니다.
- **숨긴 팝업:** `HandleBack`은 `Hide()`로 숨긴(`!IsVisible`) 팝업을 건너뜁니다.
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
- UIBase를 상속하는 스크립트는 팝업 안의 자식 UI용입니다.

| 타입 | 역할 |
|---|---|
| `UIPrefixSettings` (SO, public) | 접두사 규칙(`Rule`: prefix, 타입 전체 이름), `ScriptNamespace`, `BindButtonClick`. `GetOrCreate()`가 없으면 `Assets/Editor/UIPrefixSettings.asset`을 만듭니다. 가장 긴 접두사가 우선합니다. |
| `UITypeResolver` | 타입 전체 이름을 `TypeCache` Component 타입에서 찾습니다. `UnityEngine.GameObject`는 따로 처리합니다. |
| `UIBindingCollector` (internal) | 루트의 자식을 계층 순서로 훑습니다. 자식에 UIBase가 있으면 그 자식만 포함하고 아래는 건너뜁니다. 필드 타입은 실제 컴포넌트 타입입니다(UI_는 구체 클래스). 이름이 겹치거나 컴포넌트가 없으면 경고 후 건너뜁니다. |
| `UIScriptGenerator` (internal) | `CreateScript`(using, namespace, region, PopupBase면 OnOpen, `OnButtonEvent`의 `switch(button.name)`와 BTN_ case). `UpdateScript`는 region만 교체하고, 없으면 클래스 `{` 다음에 삽입하며 빠진 using을 추가합니다. 줄바꿈 스타일은 유지합니다. region 안은 `#pragma warning disable CS0169, CS0649`로 감쌉니다. |
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
| `SoundManager` | `static Instance => Services.Get<SoundManager>()`. BGM AudioSource 2개(크로스페이드), SFX AudioSource N개. `PlayBGM(name, volume, fadeSeconds, loop)`, `StopBGM(fade)`, `PlaySFX(name, volume)`, `PreloadSFX`, `StopAllSFX`, `UnloadSFX`, `UnloadAllSFX`, `MasterVolume`/`BgmVolume`/`SfxVolume`(clamp, 바뀌면 `VolumeChanged`), `CurrentBgm` |

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
| `Pooling` | `PoolManager`(Spawn/Despawn/DespawnAfter/Prewarm/GetPool, 기본 보관 64), `PrefabPool`(자체 Stack, 넘치면 Destroy, 부모가 null이면 활성 씬으로 이동), `PooledObject`(자동 부착, OnDestroy 시 개수 보정), `IPoolable` |
| `StateMachines` | `StateMachine<TState>`(함수 또는 `IState` 등록, 전환 큐로 Enter 안에서도 ChangeState 가능, 같은 상태 재요청은 무시하고 reenter 옵션 제공) |
| `Timing` | 네임스페이스를 `Time`이라고 하면 `UnityEngine.Time`이 가려져서 `Timing`으로 정했습니다. `TimerService`(After/Every, owner 파괴 시 취소, 내부 `Tick` 테스트용 internal 생성자, `TimerRunner`는 별도 파일), `GameTimer`(이름 충돌 회피), `Cooldown`(시계 주입 가능), `PauseService`(요청자 집합, TimeScale, PauseAudio, `PauseChanged`, Dispose 시 timeScale 1) |
| `Tweening` | `Tween.To`와 확장 메서드, `Ease` 12종. 대상이 파괴되면 조용히 멈춥니다. |
| `Inputs` | `UnityEngine.Input`과 겹치지 않게 `Inputs`로 정했습니다. `#if NYONYO_INPUT_SYSTEM`입니다. `InputService`(기본값 `InputSystem.actions`, SwitchMap/PushMap/PopMap은 하나의 맵만 활성, `RebindAsync`는 ESC 취소, 리바인딩 JSON을 `input.bindings` 전역 키로 저장). 코드로 추가한 InputSystemUIInputModule은 DefaultInputActions를 써서 UI 맵 비활성과 무관합니다(소스로 확인). |
| `Cameras` | `CameraFollow2D`(실행 순서 50, 자체 위치를 기억해서 덮어씀), `CameraShake`(실행 순서 100, 지난 프레임에 쓴 값과 비교해서 기준 위치를 정하므로 누적 없음), `CameraMath2D`(정적 함수, 테스트 대상) |
| `Data` | `DataTable<TRow> : DataTableBase`(SO, id 인덱스는 NonSerialized), `CsvParser`, `CsvRowMapper`(머리글 대소문자 무시, `_필드`는 접두어 없이도 매칭, `#` 메모, 줄 번호 포함 FormatException). 에디터의 `CONTEXT/DataTableBase/Import CSV...`는 경로를 EditorPrefs(GUID 키)에 기억합니다. |
| `Gameplay` | `Health`(IPoolable로 재사용 시 초기화, 시계 주입), `DamageDealer2D`(레이어 마스크, 자기 자신 제외, removeOnHit이면 PoolManager에 반환), `GroundCheck2D`(OverlapBox + ContactFilter, 자기 자신 제외), `SpriteFlash`(color 곱, realtime) |
| `Logging` | `Log`/`LogChannel`. Debug와 Info는 `[Conditional(UNITY_EDITOR, DEVELOPMENT_BUILD, NYONYO_LOG)]`이고, MinimumLevel은 SubsystemRegistration에서 초기화합니다. |
| `Debugging` | `DebugConsole`(`Debug.isDebugBuild`나 옵션일 때만 동작, `Application.logMessageReceived` 구독 후 Dispose에서 해제, IMGUI 뷰, 백쿼트 토글, 기본 명령 help/clear/timescale/scene), `DebugCommandRegistry`(Tokenize, typed 인자, 예외를 문자열로 반환), `[DebugCommand]` |
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

### 테스트 (`Tests/Editor/`)

- **테스트 파일:** `ServiceRegistryTests` 13개, `ServicesTests` 3개, `EventBusTests` 16개(여기까지 검증됨), `SceneServiceTests` 8개, `PopupManagerTests` 25개, `UIBaseTests` 2개, `UIAutoMapperTests` 16개, `SoundManagerTests` 16개, `SaveServiceTests` 15개, `PoolManagerTests` 8개, `StateMachineTests` 7개, `TimingTests` 11개, `TweenTests` 5개, `InputServiceTests` 5개, `DataTableTests` 7개, `GameplayTests` 10개, `DebuggingTests` 8개(Unity 검증 안 됨, 총 175개).
- **스크립트 실행 주의:** compile-check.ps1 실행 결과에 `| Select-Object -First N`을 붙이면 파이프라인이 멈춥니다. 그냥 실행하세요. `FakeAssetLoader`(Tests/Editor)는 로드와 반환을 기록하는 가짜 로더입니다. 실제 Addressables 로드와 `AssetLoader`는 자동 테스트가 없습니다.
- **asmdef 참조 수정:** 테스트 asmdef에 빠져 있던 `UnityEngine.UI` 참조를 2026-10-27에 추가했습니다.. `FadeTransition`과 뒤로가기 키 입력은 자동 테스트가 없습니다. 테스트용 팝업은 `Tests/Editor/TestPopups/`에 둡니다(파일 하나에 클래스 하나).
- **SceneService 테스트 방식:** 가짜 `ISceneLoader`와 `ISceneTransition`을 넣고, 즉시 완료되는 Awaitable로 동기 검증합니다.
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
