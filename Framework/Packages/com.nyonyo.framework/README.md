# Nyonyo Framework

Unity 2D 게임 개발을 위한 공용 프레임워크 패키지입니다.

- Unity 6000.3 이상

## 설치

다른 프로젝트에서 이 패키지를 쓰려면 Package Manager에서 **Install package from git URL**을 선택하고 아래 주소를 입력합니다.

```
https://github.com/pprnkr/Framework.git?path=/Framework/Packages/com.nyonyo.framework#v0.1.0
```

`#v0.1.0` 부분은 사용할 버전의 git 태그입니다. 생략하면 `main` 브랜치의 최신 상태를 가져옵니다.

## 폴더 구조

| 폴더 | 어셈블리 | 용도 |
|---|---|---|
| `Runtime/` | `Nyonyo.Framework` | 게임 빌드에 포함되는 코드 |
| `Editor/` | `Nyonyo.Framework.Editor` | 에디터 전용 코드 (빌드에서 제외) |
| `Tests/Runtime/` | `Nyonyo.Framework.Tests` | PlayMode 테스트 |
| `Tests/Editor/` | `Nyonyo.Framework.Editor.Tests` | EditMode 테스트 |

네임스페이스는 어셈블리 이름을 따릅니다. 예: `Nyonyo.Framework.Audio`.

## 부트스트랩과 서비스

게임이 시작되면 `Bootstrapper`가 첫 씬보다 먼저 자동으로 실행됩니다. 이때 전역 서비스를 등록하고 초기화합니다. 어떤 씬에서 Play를 눌러도 똑같이 동작합니다.

### 1. 설정 파일 만들기

메뉴 **Tools > Nyonyo Framework > Create Settings**를 실행하면 `Assets/Resources/NyonyoFrameworkSettings.asset`이 만들어집니다. 이 파일이 없으면 서비스 없이 시작하고 콘솔에 경고를 띄웁니다.

### 2. 서비스 만들기

서비스는 인터페이스와 구현으로 나눠 만듭니다. 초기화나 정리가 필요하면 아래 인터페이스를 구현합니다.

| 인터페이스 | 호출 시점 |
|---|---|
| `IInitializable` | 초기화 단계 (동기) |
| `IAsyncInitializable` | 초기화 단계 (비동기, 끝날 때까지 다음 서비스가 기다림) |
| `IDisposable` | 게임 종료 또는 Play 모드 종료 |

```csharp
public interface ISaveService { /* ... */ }

public class JsonSaveService : ISaveService, IAsyncInitializable, IDisposable
{
    public async Awaitable InitializeAsync(CancellationToken cancellationToken) { /* 세이브 로드 */ }
    public void Dispose() { /* 정리 */ }
}
```

### 3. Installer로 등록하기

`GameInstaller`를 상속한 클래스를 만들고, 에셋으로 생성해서 설정 파일의 **Installers** 목록에 추가합니다.

```csharp
[CreateAssetMenu(menuName = "MyGame/Game Installer")]
public class MyGameInstaller : GameInstaller
{
    public override void Install(ServiceRegistry services)
    {
        services.Register<ISaveService>(new JsonSaveService());

        // MonoBehaviour 서비스는 씬이 바뀌어도 유지되는 Root에 붙입니다.
        var audio = Bootstrapper.Root.gameObject.AddComponent<AudioService>();
        services.Register<IAudioService>(audio);
    }
}
```

### 4. 사용하기

```csharp
Services.Get<ISaveService>().Save();

if (Services.TryGet(out IAudioService audio))
    audio.PlaySfx(clip);
```

### 규칙

- **초기화 순서:** 서비스는 등록한 순서대로 초기화되고, 정리는 그 역순입니다. 초기화 중에 다른 서비스를 쓴다면 그 서비스를 먼저 등록하세요.
- **비동기 초기화 기다리기:** 모든 서비스가 동기적으로 초기화되면 첫 씬의 `Awake`보다 먼저 준비가 끝납니다. 비동기 초기화가 있다면 `await Bootstrapper.WaitUntilReadyAsync();`로 기다린 뒤에 서비스를 쓰세요.
- **종료 시점:** 게임 종료나 Play 모드 종료 때는 서비스가 씬 오브젝트보다 먼저 정리됩니다. 그래서 `OnDestroy`와 `OnDisable`에서는 `Services.TryGet`을 쓰세요.

## 이벤트 (`Nyonyo.Framework.Events`)

시스템끼리 직접 참조하지 않고 이벤트로 소식을 주고받습니다. 예를 들어 플레이어가 죽으면 UI, 사운드, 저장 시스템이 각자 `PlayerDied` 이벤트를 받아 반응합니다.

### 1. 등록하기

다른 서비스보다 **먼저** 등록하세요. 그래야 다른 서비스가 초기화할 때 구독할 수 있고, 정리할 때도 가장 나중에 정리됩니다.

```csharp
public override void Install(ServiceRegistry services)
{
    services.Register<IEventBus>(new EventBus());
    // 다른 서비스 등록...
}
```

### 2. 이벤트 정의하기

이벤트는 `IEvent`를 구현한 struct로 만듭니다. 값을 바꿀 일이 없으니 `readonly struct`를 권장합니다.

```csharp
public readonly struct PlayerDied : IEvent
{
    public readonly int PlayerId;
    public PlayerDied(int playerId) => PlayerId = playerId;
}
```

### 3. 발행하고 구독하기

```csharp
// 발행
Services.Get<IEventBus>().Publish(new PlayerDied(playerId));

// MonoBehaviour에서 구독: 오브젝트가 파괴되면 자동으로 해제됩니다.
void Start()
{
    Services.Get<IEventBus>().Subscribe<PlayerDied>(OnPlayerDied, destroyCancellationToken);
}

// 직접 해제하고 싶을 때: 반환된 구독 객체를 Dispose합니다.
IDisposable _subscription;
void OnEnable() => _subscription = Services.Get<IEventBus>().Subscribe<PlayerDied>(OnPlayerDied);
void OnDisable() => _subscription?.Dispose();
```

### 규칙

- **스레드:** 메인 스레드에서만 사용하세요.
- **호출 순서와 예외:** 핸들러는 구독한 순서대로 호출됩니다. 핸들러 하나가 예외를 던져도 콘솔에 남기고 나머지 핸들러를 계속 호출합니다.
- **발행 도중 구독 변경:** 새로 구독한 핸들러는 다음 발행부터 호출됩니다. 아직 호출되지 않은 핸들러를 해제하면, 이번 발행에서도 호출되지 않습니다.
- **중복 구독:** 같은 핸들러를 두 번 구독하면 두 번 호출됩니다. C# `event`의 `+=`와 같습니다.
- **메모리:** 이벤트를 발행할 때 메모리를 할당하지 않습니다.

## 씬 관리 (`Nyonyo.Framework.Scenes`)

전환 연출과 함께 씬을 바꿉니다. 진행 순서는 다음과 같습니다.

1. `SceneLoadStarted` 이벤트 발행
2. 화면 가리기
3. 씬 로드
4. `SceneLoaded` 이벤트 발행
5. 화면 보이기

### 1. 등록하기

```csharp
public override void Install(ServiceRegistry services)
{
    var events = new EventBus();
    services.Register<IEventBus>(events);

    var transition = new FadeTransition(Bootstrapper.Root);  // 검은색, 0.3초
    services.Register<ISceneService>(new SceneService(transition, events));
}
```

- `SceneService` 생성자의 인자는 모두 생략할 수 있습니다.
  - 전환 연출을 넘기지 않으면 연출 없이 바로 전환합니다.
  - 이벤트 버스를 넘기지 않으면 이벤트를 발행하지 않습니다.
- `MinimumLoadingSeconds`를 설정하면 로드가 빨리 끝나도 화면을 가린 상태를 그 시간만큼 유지합니다. 화면이 번쩍이는 것을 막을 때 씁니다.
- `FadeTransition`으로 색, 시간, Canvas 정렬 순서를 바꿀 수 있습니다. 예: `new FadeTransition(Bootstrapper.Root, Color.white, 0.5f)`.

### 2. 사용하기

```csharp
await Services.Get<ISceneService>().LoadAsync("Stage1");
```

- **Scene List 등록:** 씬은 **Build Profiles의 Scene List**에 추가되어 있어야 합니다. 없으면 예외가 납니다.
- **중복 요청:** 이미 전환 중일 때 다시 요청하면 무시하고 경고를 띄우며 `false`를 반환합니다.
- **SceneLoaded 핸들러:** `SceneLoaded` 이벤트를 받는 시점에는 아직 전환이 끝나지 않은 상태입니다. 그래서 그 핸들러 안에서 바로 다른 씬을 불러오면 무시됩니다.
- **취소:** `CancellationToken`으로 취소하면 기다리는 것만 멈춥니다. Unity의 씬 로드 자체는 끝까지 진행됩니다.

### 확장하기

| 인터페이스 | 용도 |
|---|---|
| `ISceneTransition` | 로딩 화면 같은 다른 전환 연출을 만듭니다. `SetProgress`로 진행률(0~1)을 받습니다. |
| `ISceneLoader` | Addressables처럼 다른 방식으로 씬을 불러옵니다. 기본은 `SceneManagerLoader`입니다. |

## UI와 팝업 (`Nyonyo.Framework.UI`)

UI를 5개 층(`UILayer`)으로 나눠 그립니다. 높은 층은 항상 낮은 층 위에 보이고, 같은 층에서는 나중에 띄운 팝업이 위에 보입니다. 그래서 일반 팝업이 여러 개 떠 있어도 띠알람(`Notice`)은 그 위에 나옵니다.

| 층 | 용도 | Canvas 정렬 순서 |
|---|---|---|
| `Screen` | 메인 메뉴, HUD | 1000 |
| `Popup` | 일반 팝업 | 2000 |
| `System` | 확인창, 네트워크 오류 | 3000 |
| `Notice` | 띠알람, 토스트 | 4000 |
| `Top` | 로딩 인디케이터, 디버그 | 5000 |

씬 전환 페이드(30000)는 모든 층보다 위에 그려집니다.

### 1. 팝업 만들기

`PopupBase`를 상속한 컴포넌트를 프리팹 루트에 붙입니다. 프리팹 루트는 Canvas가 아니라 일반 RectTransform이어야 합니다.

```csharp
public class PopupTest : PopupBase
{
    protected override void OnOpen() { /* 생성 직후 초기화 */ }

    // 팝업 안의 모든 버튼 클릭이 여기로 모입니다.
    protected override void OnButtonEvent(Button button)
    {
        switch (button.name)
        {
            case "OkButton": Close(); break;
            default: base.OnButtonEvent(button); break;  // 닫기 버튼 처리
        }
    }
}
```

인스펙터에서는 다음 항목을 설정합니다.

| 항목 | 내용 |
|---|---|
| **Layer** | 기본으로 뜰 층 |
| **Back Key** | 뒤로가기 키 동작. `Close`(닫기), `Block`(막기), `Ignore`(건너뛰기) 중에서 고릅니다. |
| **Close On Scene Change** | 씬이 바뀔 때 자동으로 닫을지 여부 |
| **Close Button** | 누르면 팝업을 닫는 버튼 |

`Close()`는 팝업 오브젝트를 `Destroy`합니다. `Close()`를 재정의할 때는 마지막에 `base.Close()`를 호출하세요.

### 2. 등록하기

**Create > Nyonyo Framework > UI Settings**로 설정 에셋을 만들고, **Popup Prefabs**에 프리팹을 추가합니다. 그다음 Installer에서 등록합니다.

```csharp
[SerializeField] UISettings _uiSettings;

public override void Install(ServiceRegistry services)
{
    var events = new EventBus();
    services.Register<IEventBus>(events);
    services.Register(new PopupManager(_uiSettings, Bootstrapper.Root, events));
}
```

- **EventSystem:** Play 중에 EventSystem이 없으면 `PopupManager`가 직접 만들어서 유지합니다. 그러니 **씬에는 EventSystem을 두지 마세요.**
- **씬 전환 시 자동 닫기:** `eventBus`를 넘기면 씬이 바뀔 때 `Close On Scene Change` 팝업을 자동으로 닫습니다.

### 3. 사용하기

```csharp
// 띄우기: 프리팹에 설정된 층에 뜹니다.
var popup = PopupManager.Instance.Show<PopupTest>();

// 층 직접 지정
PopupManager.Instance.Show<PopupTest>(UILayer.System);

// 닫힐 때까지 기다리기 (확인창 응답 등)
await popup.WaitUntilClosedAsync();

// 조회와 닫기
PopupManager.Instance.IsOpen<PopupTest>();
PopupManager.Instance.Find<PopupTest>();       // 가장 위에 있는 인스턴스
PopupManager.Instance.CloseAll(UILayer.Popup);  // 층을 생략하면 전부 닫기
```

- **`PopupManager.Instance`:** `Services.Get<PopupManager>()`의 줄임말입니다. 싱글톤이 아니어서 도메인 리로드를 꺼도 안전합니다.
- **뒤로가기 키:** ESC 키와 안드로이드 뒤로가기는 가장 위 팝업부터 `Back Key` 설정대로 처리합니다. 닫을 팝업이 없으면 `BackKeyUnhandled` 이벤트가 호출됩니다. 게임 종료 확인창을 띄울 때 쓰면 됩니다.
- **팝업이 아닌 UI:** HUD처럼 팝업이 아닌 UI를 같은 층 규칙으로 그리려면 `GetLayerRoot(UILayer.Screen)`의 자식으로 붙이세요.

## 개발

이 패키지는 `Framework` Unity 프로젝트의 `Packages/` 폴더에 임베디드 패키지로 들어 있습니다. 프로젝트를 열면 바로 수정하고 테스트할 수 있습니다.

새 버전을 배포할 때는 `package.json`의 `version`을 올리고, `CHANGELOG.md`에 변경 사항을 적은 뒤, 같은 버전으로 git 태그(`v0.2.0` 등)를 만듭니다.
