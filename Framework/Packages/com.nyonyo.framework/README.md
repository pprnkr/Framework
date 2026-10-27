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

메뉴 **Tools > Nyonyo Framework > Settings > Framework Settings**를 실행하면 `Assets/Resources/NyonyoFrameworkSettings.asset`이 만들어집니다. 이 파일이 없으면 서비스 없이 시작하고 콘솔에 경고를 띄웁니다.

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
Services.Get<ISceneService>().Load("Stage1");

// 전환이 끝날 때까지 기다리거나 결과(중복 요청이면 false)가 필요할 때만
var loaded = await Services.Get<ISceneService>().LoadAsync("Stage1");
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

### 클래스 구조

| 클래스 | 담당 |
|---|---|
| `UIBase` | 공통 기본(직접 상속하지 않음). 버튼 이벤트(OnClick 진입점 `OnButtonEvent(Button)` → 클릭 효과음 → `protected virtual OnButtonClick(Button)`), 에디터 자동 매핑 |
| `PopupBase : UIBase` | 팝업. **Open/Close**: `PopupManager.Open<T>()`로 생성되고 `Close()`로 즉시 Destroy됩니다. 층, 뒤로가기, `PrevOpen`/`PostOpen`/`PrevClose`/`PostClose`, `OnClosed`, `WaitUntilClosedAsync` |
| `UIView : UIBase` | 팝업 안의 UI 묶음(탭, 목록 항목 등). **Show/Hide**: `SetActive`만 켜고 끄며, 파괴하지 않습니다. `OnShow`/`OnHide`, `IsVisible` |

- **단어 규칙:** 팝업은 Open/Close(생성과 파괴), UIView는 Show/Hide(켜기와 끄기)만 씁니다. 팝업에는 숨겨 두는 상태가 없습니다.
- **함께 파괴:** 팝업 자식으로 둔 `UIView`는 팝업을 닫으면 함께 파괴됩니다. 이때 `OnHide`는 호출되지 않습니다.
- **재정의 규칙:** 바깥에서 부르는 함수(`Close`, `Show`, `Hide`, `OnButtonEvent`)는 재정의할 수 없고, 상속한 쪽은 훅 함수(팝업의 `PrevOpen`, `PostOpen`, `PrevClose`, `PostClose`, UIView의 `OnShow`, `OnHide`, 공통 `OnButtonClick`)만 재정의합니다.

### 1. 팝업 만들기

`PopupBase`를 상속한 컴포넌트를 프리팹 루트에 붙입니다. 프리팹 루트는 Canvas가 아니라 일반 RectTransform이어야 합니다. 직접 작성해도 되지만, 아래 [자동 매퍼](#ui-자동-매퍼-에디터)로 만드는 걸 추천합니다.

```csharp
[Popup(Path = "Assets/Popups/PopupTest.prefab", LoadType = LoadType.Addressables)]
public class PopupTest : PopupBase
{
    protected override void PostOpen() { /* 열린 직후 초기화 */ }

    protected override void PrevClose() { /* 닫히기 시작할 때 정리 */ }

    [SerializeField] private Button BTN_Ok;

    // OnClick에 OnButtonEvent를 연결한 버튼은 클릭 효과음이 나온 뒤 여기로 모입니다.
    protected override void OnButtonClick(Button button)
    {
        if (button == BTN_Ok)
        {
            Close();
        }
        else
        {
            base.OnButtonClick(button);
        }
    }
}
```

- **프리팹 경로 `[Popup]`:** `PopupManager`가 이 값으로 프리팹을 불러옵니다.
  - **Addressables:** `Path`는 address입니다. 기본 address는 에셋 경로입니다.
  - **Resources:** `Path`는 Resources 폴더 기준 경로이고 확장자를 뺍니다(예: `Popups/Loading`).
  - **자동 매퍼:** 자동 매퍼로 만들면 프리팹 위치에 맞춰 이 값이 자동으로 들어가고, Addressables에도 자동으로 등록됩니다.
- **버튼 연결:** 버튼 클릭은 자동으로 연결되지 않습니다. Button의 **OnClick**에 `OnButtonEvent`를 연결하고 인자로 그 Button을 넘기세요. 자동 매퍼의 **Assign Object**를 쓰면 `BTN_` 버튼은 자동으로 연결됩니다.
- **클릭 처리:** `OnButtonEvent`는 재정의할 수 없습니다. 클릭 처리는 `OnButtonClick`을 재정의하세요.
- **버튼 구분:** 버튼은 이름(`button.name`)이 아니라 필드와 비교합니다. 이름이 같은 버튼이 있어도 헷갈리지 않습니다. C#의 `case` 라벨에는 상수만 쓸 수 있어서 `switch` 대신 `if (button == BTN_Ok) ... else if ...`로 분기합니다.
- **클릭 효과음:** 모든 `OnButtonEvent` 버튼은 클릭할 때 `SoundManager.PlaySFX("Click")`을 재생합니다(`UIBase.ButtonClickSound`, 기본 경로 `SFX/Click`). 이름은 `Click`으로 고정이니 게임마다 이 이름으로 효과음을 준비하세요. `SoundManager`를 등록하지 않았으면 재생하지 않습니다. 특정 UI만 다른 소리를 내거나 끄려면 `PlayButtonSound(Button)`을 재정의하세요.
- **닫기 버튼:** OnClick에 `Close`를 직접 연결하거나 `OnButtonClick`에서 `Close()`를 호출하세요. OnClick에 `Close`를 직접 연결하면 클릭 효과음은 나오지 않습니다.

인스펙터에서는 다음 항목을 설정합니다.

| 항목 | 내용 |
|---|---|
| **Layer** | 기본으로 뜰 층 |
| **Back Key** | 뒤로가기 키 동작. `Close`(닫기), `Block`(막기), `Ignore`(건너뛰기) 중에서 고릅니다. |
| **Close On Scene Change** | 씬이 바뀔 때 자동으로 닫을지 여부 |

열고 닫을 때 훅이 다음 순서로 호출됩니다. `Close()`는 재정의할 수 없고, 마지막에 팝업 오브젝트를 즉시 `Destroy`합니다.

```
열기: 생성 → PrevOpen → 열린 팝업 목록에 추가 → PostOpen
닫기: Close → PrevClose → 열린 팝업 목록에서 제거 → PostClose → OnClosed 콜백 → Destroy
```

| 훅 | 언제 쓰나 |
|---|---|
| `PrevOpen` | 목록에 들어가기 전. 아직 `Find`나 `GetOpenPopups`에 나오지 않습니다. |
| `PostOpen` | 보통의 초기화. 목록에 들어간 뒤라 같은 층의 다른 팝업 수 등을 볼 수 있습니다. |
| `PrevClose` | 닫히기 시작할 때. 아직 목록에 남아 있습니다. |
| `PostClose` | 목록에서 빠진 뒤, 연 쪽의 `OnClosed` 콜백보다 먼저. |

- **팝업 안에서 할 일:** 위 훅을 재정의합니다.
- **팝업을 연 쪽에서 할 일:** `Open<T>().OnClosed(() => ...)`로 등록합니다. 여러 번 등록할 수 있고, 이미 닫힌 팝업에 등록하면 바로 실행합니다.

### 2. 등록하기

**Tools > Nyonyo Framework > Settings > UI Settings**로 설정 에셋을 만듭니다. 이 에셋에서 화면 방향, 기준 해상도, 뒤로가기 처리를 정합니다. 그다음 Installer에서 등록합니다. 프리팹 목록은 따로 없고, 각 팝업 클래스의 `[Popup]`으로 불러옵니다.

| 항목 | 내용 |
|---|---|
| **Orientation** | 게임 화면 방향. `Portrait`(세로형, 기본값) 또는 `Landscape`(가로형) |
| **Reference Resolution** | UI를 디자인한 기준 해상도. 기본값은 1080x1920입니다. 가로·세로 순서는 Orientation에 맞춰 자동으로 정렬되므로, 가로형으로 바꿀 때 해상도를 다시 입력하지 않아도 됩니다. |
| **Handle Back Key** | 뒤로가기 키로 맨 위 팝업을 닫을지 여부 |

- **Player Settings 맞추기:** UI Settings 인스펙터는 Player Settings의 화면 방향이나 기본 창 크기가 Orientation과 다르면 경고하고 **Player Settings에 적용** 버튼을 보여 줍니다. 누르면 자동 회전을 해당 방향으로만 허용하고(세로형은 정방향 세로만, 가로형은 좌우 가로), 창 모드 기본 크기를 기준 해상도로 맞춥니다. 코드에서는 `OrientationPlayerSettings.Apply(settings)`를 호출합니다.
- **화면 맞춤:** Canvas Scaler는 `Expand`로 고정입니다. 기준 해상도 영역은 어떤 비율의 화면에서도 잘리지 않고 다 보이며, 화면이 더 길거나 넓으면 그쪽으로 캔버스가 늘어납니다. 가장자리에 붙는 UI는 앵커를 화면 모서리에 두세요.
- **Game 뷰:** 에디터 Game 뷰의 해상도는 직접 1080x1920(세로) 등으로 골라 주세요.

```csharp
[SerializeField] UISettings _uiSettings;

public override void Install(ServiceRegistry services)
{
    var events = new EventBus();
    services.Register<IEventBus>(events);

    var assets = new AssetLoader();
    services.Register<IAssetLoader>(assets);

    services.Register(new PopupManager(_uiSettings, Bootstrapper.Root, events, assets));
}
```

- **프리팹 로드 시점:** 프리팹은 처음 `Open`할 때 불러오고 그 뒤로는 재사용합니다. `PopupManager`가 정리될 때 로더에 반환합니다.
- **EventSystem:** Play 중에 EventSystem이 없으면 `PopupManager`가 직접 만들어서 유지합니다. 그러니 **씬에는 EventSystem을 두지 마세요.**
- **씬 전환 시 자동 닫기:** `eventBus`를 넘기면 씬이 바뀔 때 `Close On Scene Change` 팝업을 자동으로 닫습니다.

### 3. 사용하기

```csharp
// 열기: 프리팹에 설정된 층에 뜹니다.
var popup = PopupManager.Instance.Open<PopupTest>();

// 층 직접 지정
PopupManager.Instance.Open<PopupTest>(UILayer.System);

// 비동기로 불러와서 열기, 미리 불러 두기
var shop = await PopupManager.Instance.OpenAsync<PopupShop>();
await PopupManager.Instance.PreloadAsync<PopupShop>();

// 닫힌 뒤 할 일을 열면서 바로 정하기
PopupManager.Instance.Open<PopupShop>().OnClosed(() => RefreshGold());

// 닫힐 때까지 기다리기 (확인창 응답 등)
await popup.WaitUntilClosedAsync();

// 조회와 닫기
PopupManager.Instance.IsOpen<PopupTest>();
PopupManager.Instance.Find<PopupTest>();       // 가장 위에 있는 인스턴스
PopupManager.Instance.CloseAll(UILayer.Popup);  // 층을 생략하면 전부 닫기
```

- **`PopupManager.Instance`:** `Services.Get<PopupManager>()`의 줄임말입니다. 싱글톤이 아니어서 도메인 리로드를 꺼도 안전합니다.
- **`Open`과 `OpenAsync`:** 프리팹을 아직 불러오지 않았다면 `Open`은 그 자리에서 불러옵니다(Addressables의 `WaitForCompletion`). WebGL에서는 `WaitForCompletion`을 쓸 수 없으니 `OpenAsync`나 `PreloadAsync`를 쓰세요.
- **미리 불러오기:** `[Popup(..., Preload = true)]`를 주면 게임 시작(부트스트랩 초기화) 때 프리팹을 미리 불러옵니다. 그래서 WebGL에서도 `Open`으로 바로 열 수 있습니다. 비동기 초기화이므로, 첫 씬에서 바로 열 때는 `await Bootstrapper.WaitUntilReadyAsync()` 뒤에 여세요.
- **뒤로가기 키:** ESC 키와 안드로이드 뒤로가기는 열린 팝업 중 가장 위 팝업부터 `Back Key` 설정대로 처리합니다. 닫을 팝업이 없으면 `BackKeyUnhandled` 이벤트가 호출됩니다. 게임 종료 확인창을 띄울 때 쓰면 됩니다. iOS에는 뒤로가기 버튼이 없으니, 모든 팝업에 화면 닫기 버튼도 두세요.
- **안전 영역(노치):** 버튼과 글자를 담는 오브젝트에 `SafeAreaFitter`를 붙이면 노치, 펀치홀, 홈 인디케이터를 피합니다. 배경과 Dim은 그 밖에 두어 화면 끝까지 채웁니다. PC와 WebGL에서는 화면 전체에 맞춰지므로 같은 프리팹을 그대로 씁니다.
- **팝업이 아닌 UI:** HUD처럼 팝업이 아닌 UI를 같은 층 규칙으로 그리려면 `GetLayerRoot(UILayer.Screen)`의 자식으로 붙이세요.

## 에셋 로딩 (`Nyonyo.Framework.Assets`)

Resources와 Addressables를 같은 인터페이스(`IAssetLoader`)로 불러옵니다. 게임 시작에 꼭 필요한 최소한의 에셋만 Resources에 두고, 나머지는 Addressables를 쓰는 것을 기본으로 합니다.

| `LoadType` | 경로 |
|---|---|
| `Resources` | Resources 폴더 기준 경로, 확장자 제외 (예: `Popups/Loading`) |
| `Addressables` | address (기본값은 에셋 경로, 예: `Assets/Popups/PopupTest.prefab`) |

```csharp
var assets = Services.Get<IAssetLoader>();
var icon = await assets.LoadAsync<Sprite>("Assets/Icons/Gold.png", LoadType.Addressables);
// ... 다 쓰면 반환
assets.Release("Assets/Icons/Gold.png", LoadType.Addressables);
```

- **등록 순서:** `AssetLoader`를 서비스로 등록하고 `PopupManager`와 `SoundManager`에 넘겨서 함께 씁니다. 다른 서비스보다 먼저 등록하세요. 그래야 정리할 때 가장 나중에 정리됩니다.
- **Release 규칙:** Addressables 에셋은 불러온 횟수만큼 `Release`해야 메모리에서 내려갑니다. Resources 에셋은 `Release`가 아무 일도 하지 않습니다.
- **Addressables 패키지:** 이 패키지는 Addressables 4.1.1에 의존합니다. 설치하면 함께 설치됩니다.
- **Play 모드에서 쓸 때:** 에디터 Play 모드에서는 Addressables Groups 창의 Play Mode Script가 **Use Asset Database**(기본값)여야 빌드 없이 불러올 수 있습니다.
- **에셋 등록:** `AddressableEditorUtility.SetAddress(assetPath, address)`로 에셋을 Addressables에 등록할 수 있습니다(에디터 전용).

## 사운드 (`Nyonyo.Framework.Audio`)

BGM과 효과음을 이름으로 재생합니다. 이름은 `SoundSettings`의 경로 형식으로 바뀌어서 불러옵니다. 기본 형식은 `BGM/{0}`, `SFX/{0}`입니다.

### 1. 사운드 준비

1. 사운드 파일을 BGM 폴더와 효과음 폴더에 넣습니다. 기본 폴더는 `Assets/Sounds/BGM`, `Assets/Sounds/SFX`입니다.
2. **Tools > Nyonyo Framework > Settings > Sound Settings**로 설정 에셋을 만듭니다. 폴더를 바꿨다면 여기서 고칩니다.
3. 메뉴 **Tools > Nyonyo Framework > Sound > Register Sounds to Addressables**를 실행합니다. 그러면 `Assets/Sounds/BGM/Title.wav`가 address `BGM/Title`로 등록됩니다.

Load Type을 Resources로 바꾸면 사운드를 `Resources/BGM`, `Resources/SFX`에 두면 되고, 등록은 필요 없습니다.

### 2. 등록하기

```csharp
[SerializeField] SoundSettings _soundSettings;

services.Register(new SoundManager(_soundSettings, Bootstrapper.Root, assets));
```

### 3. 사용하기

```csharp
SoundManager.Instance.PlayBGM("Title");                      // BGM/Title
SoundManager.Instance.PlayBGM("Battle", fadeSeconds: 1f);    // 1초 크로스페이드
SoundManager.Instance.StopBGM(1f);

SoundManager.Instance.PlaySFX("Hit");                        // SFX/Hit
SoundManager.Instance.PlaySFX("Coin", 0.5f);                 // 이 효과음만 절반 볼륨

// 볼륨 (0~1)
SoundManager.Instance.MasterVolume = 0.8f;
SoundManager.Instance.BgmVolume = 0.5f;
SoundManager.Instance.SfxVolume = 1f;
SoundManager.Instance.VolumeChanged += RefreshSliders;

// 첫 재생 지연을 없애려면 미리 불러 두기, 다 쓰면 내리기
await SoundManager.Instance.PreloadSFXAsync("Hit", "Coin");
SoundManager.Instance.UnloadAllSFX();

// 끝날 때까지 기다려야 할 때만 Async 버전을 씁니다.
await SoundManager.Instance.PlayBGMAsync("Ending", fadeSeconds: 2f);
```

- **비동기 처리:** `PlayBGM`, `StopBGM`, `PlaySFX`는 `void`입니다. 불러오기와 페이드는 내부에서 진행되므로 `await`나 `_ =` 없이 부르면 됩니다. 이미 불러온 효과음은 그 자리에서 바로 재생됩니다. 크로스페이드가 끝날 때까지 기다리는 등 완료 시점이 필요하면 `PlayBGMAsync`, `StopBGMAsync`, `PlaySFXAsync`를 쓰세요.

- **실제 볼륨:** BGM은 `MasterVolume × BgmVolume × 재생 volume`, 효과음은 `MasterVolume × SfxVolume × 재생 volume`입니다. 볼륨을 바꾸면 재생 중인 소리에도 바로 반영됩니다.
- **동시 재생 수:** 효과음은 기본 8개까지 동시에 들립니다(`SoundSettings`의 SFX Channels). 다 차면 가장 오래된 효과음을 끊고 재생합니다.
- **같은 BGM 재요청:** 이미 재생 중인 BGM을 다시 요청하면 처음부터 다시 틀지 않고 볼륨만 바꿉니다.
- **메모리:** 효과음은 처음 재생할 때 불러와서 `UnloadSFX`/`UnloadAllSFX`를 부를 때까지 메모리에 둡니다. BGM은 다른 BGM으로 바뀌거나 멈추면 메모리에서 내립니다.
- **볼륨 저장:** 생성자에 `SaveService`를 넘기면 볼륨을 전역 데이터(`sound.volume`)로 저장하고 다음 실행 때 불러옵니다. 마지막으로 바꾸고 0.5초 뒤에 저장합니다(실시간 기준이라 일시정지 중에도 저장됨).
- **재생 함수와 await:** 재생 함수는 `Awaitable`을 돌려줍니다. 기다릴 필요가 없으면 그냥 호출하면 됩니다. 사운드를 찾지 못하면 예외 대신 에러 로그를 남깁니다.

## 저장 (`Nyonyo.Framework.Save`)

데이터를 키 단위로 저장합니다. 진행 데이터는 세이브 슬롯(`SaveScope.Slot`)에, 설정은 전역(`SaveScope.Global`)에 둡니다.

```csharp
services.Register(new SaveService());   // 기본: WebGL은 PlayerPrefs, 그 밖은 persistentDataPath/Saves 파일

[Serializable]
public class PlayerData { public int gold; public string nickname = ""; }

var save = SaveService.Instance;
save.CurrentSlot = 1;
save.Save("player", data);                                 // slot1.player
var loaded = save.Load("player", new PlayerData());        // 없으면 기본값
save.Save("settings", settings, SaveScope.Global);         // 슬롯과 상관없이 하나

save.SlotExists(1);
save.TryGetSlotInfo(1, out var info);                      // info.LastSavedUtc, info.KeyCount
save.DeleteSlot(1);
```

- **저장 시점:** `Save`는 호출 즉시 기록합니다. WebGL은 게임 종료 시점을 알 수 없으므로, 종료할 때 한꺼번에 저장하는 방식에 기대지 마세요.
- **데이터 형식:** 데이터는 JsonUtility로 저장합니다. `[Serializable]` 클래스의 public 필드나 `[SerializeField]` 필드만 저장되고, Dictionary와 프로퍼티는 저장되지 않습니다. int, string, 배열은 클래스 필드로 감싸세요(그대로 넘기면 예외).
- **버전 관리:** 데이터 구조가 바뀌면 `IVersionedSaveData`를 구현하세요. 예전 데이터를 불러올 때 `Migrate(fromVersion)`가 호출되고, 최신 버전으로 다시 저장됩니다.
- **안전성:** 파일 저장소는 임시 파일에 쓴 뒤 바꿔치기합니다. 그래서 쓰는 도중 꺼져도 데이터가 깨지지 않습니다. 손상된 데이터는 에러 로그를 남기고 기본값을 돌려줍니다.
- **키 규칙:** 키에는 영문, 숫자, `_ . -`만 쓸 수 있습니다. WebGL의 PlayerPrefs는 전체 1MB 정도로 제한됩니다.
- **저장소 바꾸기:** 암호화나 클라우드 저장이 필요하면 `ISaveStorage`를 구현해서 넘기세요.

## 오브젝트 풀링 (`Nyonyo.Framework.Pooling`)

```csharp
services.Register(new PoolManager(Bootstrapper.Root));

var bullet = PoolManager.Instance.Spawn(bulletPrefab, muzzle.position, muzzle.rotation);
PoolManager.Instance.Despawn(bullet);
PoolManager.Instance.DespawnAfter(effect, 1f);   // 기다려야 하면 DespawnAfterAsync
PoolManager.Instance.Prewarm(bulletPrefab, 20);
```

- **상태 초기화:** 재사용되는 오브젝트의 상태는 `IPoolable.OnSpawned()`에서 초기화하세요. `Health`는 꺼낼 때 체력이 자동으로 다시 채워집니다.
- **씬과의 관계:** `Spawn`할 때 부모를 정하지 않으면 현재 씬에 둡니다. 그래서 씬이 바뀌면 함께 파괴되고, 풀 개수도 자동으로 맞춰집니다.
- **보관 한도:** 풀마다 쉬고 있는 인스턴스는 기본 64개까지 보관합니다. 넘치면 Destroy합니다.

## 상태 머신 (`Nyonyo.Framework.StateMachines`)

```csharp
_fsm = new StateMachine<PlayerState>()
    .Add(PlayerState.Idle, update: dt => { if (moving) _fsm.ChangeState(PlayerState.Run); })
    .Add(PlayerState.Run, enter: () => animator.Play("Run"))
    .Add(PlayerState.Jump, new JumpState(this));   // IState 클래스
_fsm.ChangeState(PlayerState.Idle);

void Update() => _fsm.Update(Time.deltaTime);
```

- **Update 호출:** 상태 머신은 Unity 오브젝트가 아니라서, `Update`를 직접 불러 줘야 합니다.
- **안전한 전환:** Enter나 Exit 안에서 `ChangeState`를 불러도 안전합니다.
- **기타 기능:** `TimeInState`, `PreviousState`, `StateChanged` 이벤트를 제공합니다.

## 타이머와 일시정지 (`Nyonyo.Framework.Timing`)

```csharp
services.Register(new PauseService(events));
services.Register(new TimerService(Bootstrapper.Root));

TimerService.Instance.After(2f, Respawn, owner: this);           // owner가 파괴되면 자동 취소
var tick = TimerService.Instance.Every(0.5f, Regenerate);
tick.Cancel();

readonly Cooldown _dash = new(1.5f);
if (_dash.TryUse()) Dash();                                      // _dash.Progress로 게이지 표시

PauseService.Instance.Pause(this);      // 설정 팝업 열 때
PauseService.Instance.Resume(this);     // 닫을 때
PauseService.Instance.TimeScale = 0.3f; // 슬로 모션
```

- **일시정지 요청 관리:** 여러 곳에서 동시에 멈춰도 요청한 쪽이 모두 Resume해야 다시 흐릅니다. 그러니 `Time.timeScale`을 직접 바꾸지 마세요.
- **일시정지 중 동작:** 일시정지 중에도 움직여야 하는 UI 연출과 타이머는 unscaled time을 쓰세요.
- **소리:** 기본적으로 일시정지 중에도 BGM은 계속 나옵니다. 소리까지 멈추려면 `PauseAudio = true`로 설정하세요.

## 트윈 (`Nyonyo.Framework.Tweening`)

DOTween과 같은 이름과 체이닝으로 씁니다. 서비스 등록은 필요 없습니다.

```csharp
transform.DOMoveX(5f, 0.5f).SetEase(Ease.OutBack).SetDelay(0.2f);
icon.DOFade(0f, 1f).SetLoops(-1, LoopType.Yoyo);
button.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
rect.DOAnchorPosY(200f, 0.4f).SetRelative().OnComplete(() => Debug.Log("도착"));
await panel.DOFade(1f, 0.3f).SetUpdate(true);                  // 일시정지 중에도 움직임
await Tween.To(() => score, v => label.text = v.ToString(), 100, 1f);
tmp.maxVisibleCharacters = 0;
Tween.To(() => tmp.maxVisibleCharacters, v => tmp.maxVisibleCharacters = v, tmp.text.Length, 1f).SetEase(Ease.Linear); // 타자기

Sequence.Create()
    .Append(popup.DOScale(1f, 0.3f).From(Vector3.zero).SetEase(Ease.OutBack))
    .Join(group.DOFade(1f, 0.3f))
    .AppendInterval(1f)
    .AppendCallback(() => Debug.Log("끝"))
    .SetUpdate(true);

Tween.DelayedCall(2f, Respawn);
transform.DOKill();                                             // 이 Transform의 트윈 모두 멈춤
```

| 대상 | 함수 |
|---|---|
| Transform | `DOMove(X/Y/Z)`, `DOLocalMove(X/Y/Z)`, `DORotate`, `DOLocalRotate`, `DOScale(X/Y/Z)`, `DOJump`, `DOLocalJump`, `DOPunchPosition/Scale/Rotation`, `DOShakePosition/Rotation/Scale` |
| RectTransform | `DOAnchorPos(X/Y)`, `DOSizeDelta`, `DOPunchAnchorPos`, `DOShakeAnchorPos`, `DOJumpAnchorPos` |
| UI | `CanvasGroup.DOFade`, `Graphic.DOFade/DOColor`(Image, Text 등), `Image.DOFillAmount`, `Slider.DOValue`, `ScrollRect.DONormalizedPos/DOHorizontalNormalizedPos/DOVerticalNormalizedPos` |
| 기타 | `SpriteRenderer.DOFade/DOColor`, `Camera.DOOrthoSize/DOColor`, `AudioSource.DOFade/DOPitch`, `Material.DOColor/DOFade/DOFloat`, `Rigidbody2D.DOMove(X/Y)/DORotate/DOJump` |
| 범용 | `Tween.To`(float, int, Vector2, Vector3, Color, 직접 보간), `Tween.DelayedCall`, `Sequence.Create` |
| 설정 | `SetEase`(31종 또는 AnimationCurve), `SetDelay`, `SetLoops`(Restart, Yoyo, Incremental), `SetUpdate`, `SetRelative`, `From`, `SetAutoKill`, `SetId`, `SetLink`, `SetCancellation` |
| 콜백 | `OnStart`, `OnUpdate`, `OnStepComplete`, `OnComplete`, `OnKill` |
| 제어 | `Play`, `Pause`, `TogglePause`, `Restart`, `Rewind`, `Complete`, `Kill(complete)`, `Tween.KillTarget(대상 또는 Id)`, `Tween.KillAll`, `Tween.IsTweening` |

- **시작 시점:** 트윈은 다음 프레임부터 움직이고, 시작 값은 지연이 끝난 뒤 처음 움직일 때 읽습니다. 설정은 만든 직후에 이어 붙이세요.
- **끝과 정리:** 끝나면 자동으로 없어집니다. 다시 재생할 트윈은 `SetAutoKill(false)`로 만들고 `Restart()`하세요. 다 쓴 뒤에는 직접 Kill해야 합니다.
- **파괴와 취소:** 대상이 파괴되면 조용히 멈춥니다. `Tween.To`는 대상이 없으니 `SetLink(gameObject)`를 붙이세요. `SetCancellation(token)`으로 취소하면 await에서 `OperationCanceledException`이 납니다.
- **await:** 끝나거나 Kill되면 이어서 진행합니다. 무한 반복 트윈은 Kill하기 전까지 끝나지 않습니다.
- **DOKill 범위:** `transform.DOKill()`은 Transform 트윈만 멈춥니다. `image.DOFade`처럼 다른 컴포넌트가 대상인 트윈은 `image.DOKill()`로 멈추세요.
- **시퀀스:** 시퀀스에 넣은 트윈은 직접 제어할 수 없고, 이미 시작된 트윈이나 무한 반복 트윈은 넣을 수 없습니다. 시퀀스의 기본 곡선은 Linear입니다.
- **2D 기준:** float 세기의 Shake는 위치와 크기는 XY만, 회전은 Z만 흔듭니다. 카메라 흔들기는 `CameraShake`를 쓰세요(CameraFollow2D가 위치를 덮어씁니다).
- **동작 범위:** Play 모드의 메인 스레드에서만 움직입니다. Edit 모드 에디터 도구에서는 쓰지 마세요.

## 입력 (`Nyonyo.Framework.Inputs`, Input System 필요)

```csharp
services.Register(new InputService(save: save, initialMap: "Player"));   // 프로젝트 전체 액션 사용

var input = InputService.Instance;
if (input.GetAction("Jump").WasPressedThisFrame()) Jump();
input.PushMap("UI");     // 팝업 열 때
input.PopMap();          // 닫을 때

var changed = await input.RebindAsync(input.GetAction("Jump"));   // 다음에 누른 키로 변경, ESC 취소
label.text = input.GetBindingDisplay(input.GetAction("Jump"));
input.ResetBindings();
```

- **액션맵:** 한 번에 하나의 액션맵만 켭니다.
- **리바인딩 저장:** `SaveService`를 넘기면 리바인딩 결과를 저장하고 불러옵니다.
- **UI 클릭:** `PopupManager`가 만드는 EventSystem의 UI 입력 모듈은 별도의 기본 UI 액션을 씁니다. 그래서 액션맵을 바꿔도 UI 클릭에는 영향이 없습니다.

## 2D 카메라 (`Nyonyo.Framework.Cameras`)

| 컴포넌트 | 기능 |
|---|---|
| `CameraFollow2D` | 대상을 부드럽게 따라갑니다(SmoothDamp). 데드존, 이동 영역 제한(Collider2D 또는 `SetBounds`), `SnapToTarget()`을 지원합니다. |
| `CameraShake` | 흔들림 세기(trauma)를 누적하는 방식으로 흔듭니다. `CameraShake.ShakeMain(0.4f)`로 메인 카메라를 흔들 수 있습니다. 따라가기와 함께 써도 위치가 밀리지 않습니다. |
| `CameraMath2D` | 영역 제한과 데드존 계산을 담은 정적 함수입니다. |

픽셀 아트라면 URP의 **Pixel Perfect Camera** 컴포넌트를 함께 쓰세요.

## 게임 데이터 (`Nyonyo.Framework.Data`)

구글 스프레드시트의 시트마다 클래스를 자동으로 만들고, 데이터는 암호화된 바이너리(`.bytes`)로 저장합니다. 런타임에는 리플렉션 없이 생성 코드가 바로 읽으므로 IL2CPP(AOT)에서도 빠릅니다.

### 1. 시트 작성

| 행 | 내용 | 예 |
|---|---|---|
| 1행 | 주석(생성 코드의 문서 주석) | 아이템 ID / 공격력 / 공격력 / 이름 |
| 2행 | 자료형 | int / int[] / int[] / string |
| 3행 | 변수명 | Id / Attack / Attack / Name |
| 4행부터 | 데이터 | 1001 / 10 / 20 / 검 → `Attack[0]=10`, `Attack[1]=20` |

- **유니크 키:** 첫 번째 열은 무조건 유니크 키입니다. 자료형은 int, long, string 중 하나이고, 키가 겹치면 가져오기가 에러로 멈춥니다.
- **자료형:** int, long, float, double, bool, string, enum(프로젝트의 C# enum 이름), 그리고 각각의 배열(`int[]`)을 씁니다.
  - **배열 표기:** `Array<int>`, `List<int>`라고 적어도 배열로 만듭니다.
- **배열 열:** 같은 변수명을 가진 열들이 왼쪽부터 원소가 됩니다. 빈 칸은 건너뜁니다.
- **빈 칸:** 단일 값의 빈 칸은 기본값(0, false, 빈 문자열, enum 0)입니다.
- **건너뛰기:** 아래 경우는 가져오지 않습니다(기획 메모용).
  - 변수명이 `_`로 시작하는 열
  - 키가 `_`로 시작하는 행
  - 이름이 `_`로 시작하는 시트
  - 키가 빈 빈 행
- **이름 규칙:** 시트 이름은 클래스 이름이 되므로 영문, 숫자, `_`만 씁니다. 시트 `Item`이면 행 클래스는 `ItemData`, 테이블은 `ItemTable`입니다.
- **공유 설정:** 스프레드시트의 일반 액세스는 **"제한됨"** 그대로 둡니다. 아래에서 만드는 **서비스 계정 이메일만 뷰어로 추가**합니다.

### 2. 서비스 계정 준비 (한 번만)

시트를 공개하지 않고 읽기 위해, 구글 서비스 계정(사람이 아닌 읽기 전용 계정)을 씁니다.

1. [Google Cloud Console](https://console.cloud.google.com)에서 프로젝트를 고르거나 만들고, **API 및 서비스 > 라이브러리**에서 **Google Drive API**를 사용 설정합니다.
2. **IAM 및 관리자 > 서비스 계정 > 서비스 계정 만들기**를 누릅니다. 역할은 주지 않아도 됩니다.
3. 만든 서비스 계정에서 **키 > 키 추가 > 새 키 만들기 > JSON**을 고르면 키 파일이 내려받아집니다.
4. 키 파일은 **git 저장소 밖**(예: 문서 폴더)에 둡니다. 비밀번호와 같으니 커밋하거나 메신저로 보내지 마세요.
   - **팀원:** 팀원마다 같은 서비스 계정에서 키를 따로 만들어 쓰면, 퇴사자의 키만 지울 수 있습니다.
   - **조직 정책:** 회사 Google Workspace 정책으로 키 만들기가 막혀 있으면 관리자에게 요청하세요.

### 3. 설정과 가져오기

1. Tools > Nyonyo Framework > Settings > Game Data Settings를 엽니다.
2. **[키 파일 선택]**으로 JSON 키를 고릅니다.
   - **저장 위치:** 경로는 이 컴퓨터의 EditorPrefs에만 저장되고 에셋에는 들어가지 않습니다.
   - **빌드 서버:** 비워 두면 환경 변수 `GOOGLE_APPLICATION_CREDENTIALS`를 씁니다.
3. 표시된 서비스 계정 이메일을 **[이메일 복사]**로 복사합니다. 구글 시트 **[공유]**에 붙여 넣고 **뷰어**로 추가합니다.
4. Spreadsheets에 스프레드시트 주소를 넣습니다. 시트 목록을 비워 두면 `_`로 시작하지 않는 모든 시트를 가져옵니다.
5. 인스펙터의 **가져오기**(또는 Tools > Nyonyo Framework > Game Data > Import All)를 누릅니다.
   - **에러 안내:** 서비스 계정 공유 누락, Drive API 꺼짐, 키 오류는 원인과 해결 방법을 그대로 알려 줍니다.
   - **크기 제한:** Drive API 내보내기는 스프레드시트 하나당 10MB까지라, 넘으면 스프레드시트를 나눕니다.
   - **에러가 있을 때:** 문제가 된 셀(`Item!C5`)을 모두 콘솔에 보여 주고, 파일은 하나도 바꾸지 않습니다.
   - **만들어지는 것:** 코드 폴더에 `*.g.cs`, 데이터 폴더에 `*.bytes`가 생깁니다. Addressables면 address(`GameData/Item`)도 등록합니다.
   - **바뀌지 않은 시트:** 같은 파일이 나오므로 git 변경으로 잡히지 않습니다.
6. Installer에서 등록합니다. 게임 시작 때 모든 테이블을 비동기로 불러옵니다.

```csharp
var assets = new AssetLoader();
services.Register<IAssetLoader>(assets);
services.Register(new GameData(assets));
```

### 4. 사용

```csharp
var sword = GameData.Instance.Item.Get(1001);          // 없으면 KeyNotFoundException
if (GameData.Instance.Item.TryGet(id, out var item)) { }
foreach (var monster in GameData.Instance.Monster.All) { }

// 생성 코드는 고치지 말고, 다른 폴더에 같은 이름의 partial 클래스로 덧붙입니다.
public sealed partial class ItemData
{
    public int TotalAttack { get; private set; }
    partial void OnLoaded() => TotalAttack = Attack.Sum();   // 행을 읽은 직후
}

public sealed partial class GameData
{
    protected override void OnLoaded() { /* 모든 테이블을 읽은 뒤: 테이블끼리 참조 연결 */ }
}
```

- **배열을 쓰는 이유:** 로드한 뒤 크기가 바뀌지 않는 데이터라 List보다 배열이 가볍고 빠릅니다. 값은 바꾸지 않는다는 약속으로 쓰세요.
- **코드와 데이터 확인:** 열 구성이 바뀌면 코드와 데이터 파일이 함께 바뀌어야 합니다. 서로 맞지 않으면 로드할 때 원인을 알려 주는 `InvalidDataException`이 납니다. enum 멤버가 바뀌어도 다시 가져와야 합니다.
- **암호화:** AES-256과 HMAC 변조 검사를 씁니다. 키는 설정 에셋에 저장되므로 함께 커밋하세요.
  - **한계:** 키가 앱에 들어가므로 가벼운 열람과 변조를 막는 수준입니다.
- **asmdef 사용 시:** 게임이 asmdef를 쓰면 코드 폴더가 들어 있는 asmdef가 `Nyonyo.Framework`를 참조해야 합니다.

## 2D 공용 컴포넌트 (`Nyonyo.Framework.Gameplay`)

| 컴포넌트 | 기능 |
|---|---|
| `Health` | 피해(`TakeDamage`), 회복(`Heal`), 사망을 처리하고, 피격 후 무적 시간을 둡니다. 이벤트는 `Damaged`, `Healed`, `Died`, `Changed`입니다. 죽어도 파괴하지 않습니다. |
| `DamageDealer2D` | 닿은 대상의 Health에 피해를 줍니다(레이어 필터, 자기 자신 제외). 총알처럼 맞으면 사라지게(풀 반환) 할 수 있습니다. |
| `GroundCheck2D` | 발밑 땅을 확인합니다(`IsGrounded`, `Landed`, `LeftGround`). 선택하면 씬 뷰에 확인 영역이 보입니다. |
| `SpriteFlash` | 피격 시 스프라이트 색을 잠깐 바꿉니다. 같은 오브젝트에 Health가 있으면 자동으로 동작합니다. |

## 로그 (`Nyonyo.Framework.Logging`)

```csharp
Log.Info($"스테이지 {stage} 시작");
static readonly LogChannel s_log = Log.Channel("Battle");
s_log.Debug($"데미지 {damage}");     // [Battle] 데미지 10
Log.MinimumLevel = LogLevel.Warning;
```

`Log.Debug`와 `Log.Info`는 에디터, Development Build, `NYONYO_LOG` 심볼이 있을 때만 컴파일됩니다. 릴리스 빌드에서는 문자열을 만드는 비용까지 사라집니다.

## 디버그 콘솔 (`Nyonyo.Framework.Debugging`)

```csharp
services.Register(new DebugConsole(Bootstrapper.Root));   // 에디터와 Development Build에서만 동작

[DebugCommand("gold", "골드를 더합니다")]
static string AddGold(int amount = 1000) { Wallet.Gold += amount; return $"골드: {Wallet.Gold}"; }
```

- **열고 닫기:** 키보드가 있으면 <code>`</code> 키(`ToggleKey`)로 엽니다. 키보드가 없는 모바일에서는 손가락 세 개로 동시에 화면을 탭합니다(`ToggleTouchCount`, 0이면 끔). 코드에서는 `DebugConsole.Instance.Toggle()`을 부릅니다.
- **IL2CPP 빌드:** `[DebugCommand]`는 `PreserveAttribute`를 상속하므로, Android, iOS, WebGL 빌드의 코드 제거에서도 명령 함수가 남습니다.
- **명령 등록:** `[DebugCommand]`가 붙은 static 메서드는 자동으로 등록됩니다. 인자는 string, int, float, bool, enum 등을 쓸 수 있고, 기본값이 있으면 생략할 수 있습니다.
- **기본 명령:** `help`, `clear`, `timescale <값>`, `scene <이름>`입니다.

## 광고 (`Nyonyo.Framework.Ads`)

게임 코드는 `IAdService`만 씁니다. 실제 광고 네트워크는 선택 패키지로 설치합니다(AdMob: `com.nyonyo.framework.admob`, 설치와 설정은 그 패키지의 README 참고).

```csharp
var ads = Services.Get<IAdService>();

ads.ShowBanner();                                           // 초기화 전에 불러도 끝나면 보여 줌
ads.ShowRewarded(onRewarded: () => coins += 100);           // 버튼 OnClick 등 await 없이
if (await ads.ShowRewardedAsync()) coins += 100;            // async 함수 안에서
ads.ShowInterstitial(onClosed: LoadNextStage);              // 광고가 없어도 onClosed는 호출됨
rewardButton.interactable = ads.IsRewardedReady;

ads.AdsRemoved = true;                                      // 광고 제거 구매 후. 보상형만 남음
privacyButton.SetActive(ads.IsPrivacyOptionsRequired);      // 설정 화면 "개인정보 설정" 버튼
await ads.ShowPrivacyOptionsAsync();

ads.SkipNextAppOpen();                                      // 결제·공유 화면에 다녀오기 직전
ads.RevenuePaid += revenue => Analytics.LogAdRevenue(revenue.Value, revenue.CurrencyCode);
```

| 구현 | 쓰는 곳 |
|---|---|
| `AdMobAdService` | Android, iOS (AdMob 패키지) |
| `FakeAdService` | 에디터. 화면에 TEST 배너와 전체 화면 창을 띄우고, 보상 받기/건너뛰기를 골라 확인합니다. `SimulateNoFill`로 광고 없음도 확인합니다. |
| `NullAdService` | WebGL, PC. 보상형 광고는 광고를 본 것으로 보고 바로 보상을 줍니다(기본값). 전면, 앱 오프닝, 배너는 나오지 않습니다. |

- **웹과 PC의 보상형 광고:** 따로 설정하지 않아도, 광고가 없는 플랫폼 이용자는 보상 버튼을 누르면 바로 보상을 받습니다. 게임 코드는 모바일과 같습니다. 그 플랫폼에서 보상을 막으려면 `AdPolicy`의 **Reward Without Ad On Unsupported Platform**을 끄고, `IsRewardedReady`가 false인 보상 버튼을 숨기세요. 광고 열림/닫힘 이벤트는 그대로 발행되므로, 광고 시청 수를 분석에 남긴다면 플랫폼을 구분하세요.

- **광고는 실패할 수 있습니다:** 모든 Show는 보여 줄 광고가 없으면 아무것도 하지 않고 false를 돌려줍니다. 광고가 없어도 게임이 멈추지 않게 짜세요.
- **노출 규칙(`AdPolicy`):** 전면과 앱 오프닝 광고는 마지막 전체 화면 광고(보상형 포함)가 닫힌 뒤 `FullScreenCooldownSeconds`(기본 10초)가 지나야 다시 나옵니다. 전체 화면 광고는 한 번에 하나만 뜹니다. 앱이 백그라운드에서 돌아올 때 앱 오프닝 광고를 자동으로 띄우려면 `ShowAppOpenOnResume`을 켜세요(기본 꺼짐).
- **광고 제거:** `AdsRemoved`는 SaveService를 넘기면 `ads.removed` 전역 키로 저장됩니다. 구매 복원 결과로 다시 설정하세요.
- **배너 높이:** `BannerHeight`(화면 픽셀)만큼 UI를 올리면 배너에 가려지지 않습니다. 불러오기 전에는 0입니다.
- **다른 광고 네트워크:** `AdServiceBase`를 상속해 불러오기와 띄우기(`IsLoaded`, `PresentAsync`, 배너 3개)만 구현하면 규칙과 이벤트는 그대로 쓸 수 있습니다.

## 에디터 도구

| 메뉴 | 기능 |
|---|---|
| Tools > Nyonyo Framework > Settings > ... | 모든 설정 에셋(Framework, UI, Sound, UI Prefix, Game Data, AdMob)을 여는 곳입니다. 프로젝트에 있으면 선택하고, 없으면 기본 위치에 만듭니다. 설정 에셋은 Create 메뉴로 만들지 않습니다. |
| Tools > Nyonyo Framework > Scene Switcher | 씬 목록 창. 클릭으로 열기, Play 버튼으로 바로 실행 |
| Tools > Nyonyo Framework > Sound > Register Sounds to Addressables | 사운드 폴더를 Addressables에 등록 |
| Tools > Create UI Script > Popup / UI | UI 스크립트 자동 생성 (아래 자동 매퍼) |
| Tools > Nyonyo Framework > Game Data > Import All | 모든 Game Data Settings의 구글 시트 가져오기 |

## 플랫폼 지원

Android, iOS, WebGL, PC(Windows, macOS, Linux)를 모두 지원합니다. 스레드와 Task를 쓰지 않고 Unity `Awaitable`만 씁니다.

### 공통

| 항목 | 내용 |
|---|---|
| 화면 방향 | UI Settings 인스펙터의 **Player Settings에 적용**이 네 플랫폼을 한 번에 맞춥니다. 모바일은 회전 방향 제한, WebGL은 기본 캔버스 크기, PC는 창 크기(세로형은 창 모드로 시작)입니다. 창과 캔버스 크기는 기준 해상도의 절반입니다. |
| 코드 제거(IL2CPP) | Android, iOS, WebGL 빌드는 쓰지 않는 코드를 지웁니다. 프레임워크가 리플렉션으로 찾는 `[Popup]`과 `[DebugCommand]`는 보존되도록 처리했습니다. 게임 코드에서 리플렉션으로만 쓰는 타입은 `[Preserve]`를 붙이세요. |
| 앱 종료 | 모바일과 WebGL은 `Application.quitting` 없이 내려갈 수 있습니다. `Save`는 호출 즉시 기록되고, 볼륨은 포커스를 잃을 때도 저장됩니다. 게임에서 미뤄 둔 저장이 있다면 `Application.focusChanged`에서 저장하세요. |

### Android, iOS

| 항목 | 내용 |
|---|---|
| 안전 영역 | 노치와 홈 인디케이터는 `SafeAreaFitter`로 피합니다. |
| 뒤로가기 | Android 뒤로가기는 ESC로 들어와 팝업을 닫습니다. iOS에는 뒤로가기가 없으니 화면 닫기 버튼을 두세요. |
| 디버그 콘솔 | 손가락 세 개로 동시에 탭해서 엽니다. |
| 저장 위치 | `Application.persistentDataPath/Saves`입니다. iOS는 이 폴더가 iCloud 백업에 포함됩니다. |

### PC

| 항목 | 내용 |
|---|---|
| 창 | 세로형은 창 모드(기준 해상도의 절반 크기)로, 가로형은 전체 화면으로 시작합니다. |
| 뒤로가기 | ESC로 팝업을 닫습니다. |

### WebGL

| 항목 | 내용 |
|---|---|
| Addressables 동기 로드 | WebGL은 `WaitForCompletion`을 지원하지 않습니다. `PopupManager.Open`으로 바로 열 Addressables 팝업은 `[Popup(Preload = true)]`로 두거나 `OpenAsync`를 쓰세요. 동기 로드를 시도하면 원인을 알려 주는 `NotSupportedException`이 납니다. 에디터의 빌드 대상이 WebGL이면 미리 경고합니다. |
| 종료 처리 | 탭을 닫으면 `Application.quitting`이 호출되지 않습니다. 저장은 `Save` 호출 즉시 기록되고, 볼륨은 바뀐 뒤 0.5초 또는 탭이 포커스를 잃을 때, 리바인딩은 바뀔 때 저장됩니다. |
| 저장소 | 기본 저장소는 PlayerPrefs(브라우저 IndexedDB)이고, 전체 1MB 정도로 제한됩니다. |
| 소리 | 브라우저 정책상 사용자가 처음 클릭하거나 키를 누르기 전에는 소리가 나지 않습니다. 타이틀 화면에서 "터치해서 시작" 같은 입력을 받은 뒤 BGM을 트세요. |
| ESC 키 | 브라우저 전체 화면에서는 ESC가 전체 화면 해제에 먼저 쓰입니다. 뒤로가기 동작은 다른 키나 버튼으로도 열 수 있게 하세요. |

## UI 자동 매퍼 (에디터)

프리팹의 게임오브젝트 이름에 접두사(`IMG_`, `TXT_` 등)를 붙여 두면, 그 오브젝트를 담는 변수를 스크립트에 자동으로 만들고 연결합니다.

### 접두사 규칙

규칙은 `UIPrefixSettings` 에셋에서 관리합니다. 처음 사용할 때 `Assets/Editor/UIPrefixSettings.asset`이 기본값으로 만들어지고, **Tools > Nyonyo Framework > Settings > UI Prefix Settings**로 직접 만들 수도 있습니다.

| 접두사 | 변수 타입 |
|---|---|
| `IMG_` | `Image` |
| `RIMG_` | `RawImage` |
| `TXT_` | `TextMeshProUGUI` |
| `BTN_` | `Button` |
| `TGL_` | `Toggle` |
| `SLD_` | `Slider` |
| `SCR_` | `ScrollRect` |
| `INP_` | `TMP_InputField` |
| `DRP_` | `TMP_Dropdown` |
| `RT_` | `RectTransform` |
| `GO_` | `GameObject` |
| `UI_` | 오브젝트에 붙은 `UIView` 하위 클래스(자식 UI) |

- **타입 이름:** 타입은 전체 이름(예: `UnityEngine.UI.Image`)으로 적습니다.
- **접두사가 겹칠 때:** 여러 접두사가 맞으면 가장 긴 접두사를 씁니다.
- **기타 설정:** 새 스크립트의 namespace와 버튼 클릭 자동 연결 여부도 이 에셋에서 정합니다.

### 1. 새 스크립트 만들기

1. 프리팹을 구성하고 오브젝트 이름에 접두사를 붙입니다. 예: `IMG_A`, `TXT_B`, `BTN_Ok`.
2. Project 창에서 프리팹을 선택하거나 프리팹 모드를 연 상태에서 메뉴를 실행합니다.
   - **Tools > Create UI Script > Popup**: `PopupBase`를 상속한 스크립트를 만듭니다.
   - **Tools > Create UI Script > UI**: `UIView`를 상속한 스크립트를 만듭니다. 팝업 안에서 쓰는 자식 UI용입니다.
3. 저장 위치를 고릅니다. 파일 이름은 프리팹 이름이 기본값이고, 파일 이름이 곧 클래스 이름이 됩니다.
4. 컴파일이 끝나면 스크립트가 프리팹 루트에 자동으로 붙고, 오브젝트도 변수에 자동으로 연결됩니다.
5. **Popup**으로 만들면 프리팹 위치에 맞는 `[Popup(Path, LoadType)]`도 자동으로 들어갑니다.
   - **Resources 폴더 밖:** Addressables 방식이 되고, 프리팹도 자동으로 등록됩니다.
   - **Resources 폴더 안:** Resources 방식이 됩니다.

```csharp
public class Test : PopupBase
{
    #region Auto Generated UI
    #pragma warning disable CS0169, CS0649
    [SerializeField] private Image IMG_A;
    [SerializeField] private TextMeshProUGUI TXT_B;
    [SerializeField] private Button BTN_Ok;
    #pragma warning restore CS0169, CS0649
    #endregion
    ...
}
```

### 2. 인스펙터 버튼

`PopupBase`나 `UIView`를 상속한 컴포넌트의 인스펙터에 두 버튼이 나타납니다.

| 버튼 | 동작 |
|---|---|
| **Generate Script** | 현재 자식 오브젝트로 `#region Auto Generated UI` 영역만 다시 씁니다. 영역 밖에 직접 쓴 코드는 그대로 남습니다. 스크립트가 바뀌면 컴파일 후 자동으로 연결합니다. 새 스크립트를 만들 때는 `OnButtonClick`에 `BTN_` 버튼마다 `if (button == BTN_X)` / `else if` 분기를 넣어 줍니다. |
| **Assign Object** | 접두사 오브젝트를 같은 이름의 변수에 연결합니다. `BTN_` 버튼은 OnClick에 `OnButtonEvent`도 연결합니다. 이미 연결된 것은 다시 추가하지 않습니다. |

- **자식 UI:** 자식 오브젝트에 `UIView`가 붙어 있으면 그 자식까지만 포함합니다. 그 아래 오브젝트는 자식 UI의 스크립트가 관리합니다.
- **오브젝트 이름:** 오브젝트 이름이 곧 변수 이름입니다. 같은 UI 안에서 이름이 겹치면 첫 번째만 쓰고 경고를 띄웁니다.
- **어셈블리 참조:** 스크립트를 asmdef가 있는 폴더에 저장한다면, 그 asmdef가 `Nyonyo.Framework`, `UnityEngine.UI`, `Unity.TextMeshPro`를 참조해야 합니다.

## 개발

이 패키지는 `Framework` Unity 프로젝트의 `Packages/` 폴더에 임베디드 패키지로 들어 있습니다. 프로젝트를 열면 바로 수정하고 테스트할 수 있습니다.

새 버전을 배포할 때는 `package.json`의 `version`을 올리고, `CHANGELOG.md`에 변경 사항을 적은 뒤, 같은 버전으로 git 태그(`v0.2.0` 등)를 만듭니다.
