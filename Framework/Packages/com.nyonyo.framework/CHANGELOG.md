# Changelog

이 패키지의 변경 사항을 기록합니다. 버전 규칙은 [Semantic Versioning](https://semver.org/lang/ko/)을 따릅니다.

## [0.1.0] - 2026-10-26

### Added
- 패키지 기본 구조와 어셈블리 정의(Runtime, Editor, Tests).
- 부트스트랩: `Bootstrapper`, `FrameworkSettings`, `GameInstaller`.
- 서비스 관리: `ServiceRegistry`, `Services`, `IInitializable`, `IAsyncInitializable`.
- 에디터 메뉴: Tools > Nyonyo Framework > Create Settings.
- 이벤트: `IEventBus`, `EventBus`, `IEvent`, `CancellationToken`으로 자동 해제하는 `Subscribe` 확장 메서드.
- 씬 관리: `ISceneService`, `SceneService`, `ISceneTransition`, `FadeTransition`, `ISceneLoader`, `SceneManagerLoader`, `SceneLoadStarted`/`SceneLoaded` 이벤트.
- `com.unity.ugui` 의존성 추가 (`FadeTransition`에서 사용).
- UI: `UILayer`(5단계), `PopupManager`, `PopupBase`, `UISettings`, `BackKeyBehavior`, 뒤로가기 키 처리, EventSystem 자동 생성.
- Input System이 있으면 `NYONYO_INPUT_SYSTEM` 심볼을 정의해서 Input System 기반으로 입력을 처리합니다.
- `UIBase`: 모든 UI의 기본 클래스(`Show`, `Hide`, `public virtual OnButtonEvent`). `PopupBase`가 이를 상속합니다.
- UI 자동 매퍼(에디터): `UIPrefixSettings` 접두사 규칙, Tools > Create UI Script > Popup / UI, 인스펙터 Generate Script / Assign Object.

- 에셋 로딩: `LoadType`(Resources, Addressables), `IAssetLoader`, `AssetLoader`. `com.unity.addressables` 4.1.1 의존성 추가.
- 팝업 로드: `[Popup(Path, LoadType)]` 어트리뷰트, `PopupManager.ShowAsync`, `PreloadAsync`.
- 사운드: `SoundManager`(BGM 크로스페이드, 효과음 8채널, Master/BGM/SFX/재생별 볼륨 곱), `SoundSettings`.
- 에디터: `AddressableEditorUtility.SetAddress`, Tools > Nyonyo Framework > Sound > Register Sounds to Addressables.
  Create UI Script > Popup이 `[Popup]`을 넣고 프리팹을 Addressables에 등록합니다. 팝업 인스펙터에서 `[Popup]` 누락이나 경로 불일치를 경고합니다.

- 저장: `SaveService`(슬롯, 전역 데이터, 슬롯 정보, `IVersionedSaveData` 버전 변환), `FileSaveStorage`, `PlayerPrefsSaveStorage`(WebGL 기본), `JsonSaveSerializer`.
- `SoundManager`가 `SaveService`를 받으면 볼륨을 자동으로 저장하고 불러옵니다.
- WebGL: `[Popup(Preload = true)]`로 게임 시작 때 팝업을 미리 불러옵니다(`PopupManager`가 `IAsyncInitializable`).
  WebGL 빌드에서 Addressables 동기 로드는 명확한 `NotSupportedException`을 던지고, 에디터의 빌드 대상이 WebGL이면 미리 경고합니다.

- 오브젝트 풀링: `PoolManager`, `PrefabPool`, `IPoolable`, `PooledObject`.
- 상태 머신: `StateMachine<TState>`, `IState`.
- 타이밍: `TimerService`, `GameTimer`, `Cooldown`, `PauseService`(`PauseChanged` 이벤트).
- 트윈: DOTween식 체이닝 API. `Tween`(공통 핸들, await 가능), `Tweener<T>`, `Sequence`(Append/Join/Insert/AppendInterval/AppendCallback),
  `TweenExtensions`(SetEase/SetDelay/SetLoops/SetUpdate/SetAutoKill/SetId/SetLink/SetCancellation, OnStart/OnUpdate/OnStepComplete/OnComplete/OnKill, Play/Pause),
  단축 함수 `TransformTweenExtensions`(DOMove(X/Y/Z), DOLocalMove, DORotate, DOScale, DOJump, DOPunch*, DOShake*),
  `UITweenExtensions`(DOAnchorPos, DOSizeDelta, DOFade, DOColor, DOFillAmount, DOValue, DONormalizedPos 등),
  `ComponentTweenExtensions`(SpriteRenderer, Camera, AudioSource, Material, Rigidbody2D, DOKill/DOComplete), `Ease` 31종, `LoopType`, `RotateMode`.
- 입력: `InputService`(액션맵 전환과 스택, 리바인딩과 저장). Input System이 있을 때만 컴파일.
- 2D 카메라: `CameraFollow2D`, `CameraShake`, `CameraMath2D`.
- 게임 데이터: 구글 스프레드시트(서비스 계정 인증, Drive API xlsx 내보내기, 시트 공유는 "제한됨" 유지)에서 시트마다 partial 클래스(`{시트}Data`, `{시트}Table`)와 서비스 클래스(`GameData`)를 생성하고,
  데이터는 AES-256 + HMAC으로 암호화한 `.bytes`로 저장합니다. 런타임: `GameDataBase`, `DataTable<TKey, TRow>`, `DataTableBase`, `GameDataReader`.
  에디터: `GameDataSettings`(SO), `GameDataImporter`, Tools > Nyonyo Framework > Game Data > Import All.
  시트 규칙: 1행 주석, 2행 자료형, 3행 변수명, 첫 열은 유니크 키(중복은 에러), 같은 변수명의 배열 열 묶기, `_`로 시작하는 열·행·시트 건너뛰기.
- 2D 게임플레이: `Health`, `DamageDealer2D`, `GroundCheck2D`, `SpriteFlash`, `DamageInfo`.
- 로그: `Log`, `LogChannel`, `LogLevel`.
- 디버그: `DebugConsole`, `DebugCommandRegistry`, `[DebugCommand]`.
- 에디터: Tools > Nyonyo Framework > Scene Switcher.
- 광고: `IAdService`(배너, 전면, 보상형, 앱 오프닝, 동의, 광고 제거, 수익 이벤트), `AdServiceBase`(광고 제거 저장, 전체 화면 광고 대기 시간, 한 번에 하나, 앱 복귀 시 앱 오프닝),
  `AdPolicy`, `FakeAdService`(에디터용 테스트 광고 화면), `NullAdService`(광고 없는 플랫폼), 콜백형 `ShowRewarded`/`ShowInterstitial`.
  실제 광고 네트워크는 선택 패키지(`com.nyonyo.framework.admob`)로 설치합니다.

### Changed
- `UISettings`의 Popup Prefabs 목록을 제거했습니다. 팝업 프리팹은 `[Popup]` 어트리뷰트로 불러옵니다.
- `PopupManager` 생성자에 `IAssetLoader` 인자를 추가했습니다.
- `PopupBase.OnButtonEvent`를 `UIBase`의 public 함수로 옮겼습니다. 버튼 클릭은 더 이상 자동으로 연결하지 않으니, OnClick에 연결하거나 Assign Object를 쓰세요.
- `PopupBase`의 Close Button 필드를 제거했습니다.
- UI 기본값을 세로형으로 바꿨습니다. `UISettings`에 `Orientation`(`GameOrientation.Portrait`/`Landscape`)을 추가했고, 기준 해상도 기본값은 1080x1920입니다.
  `ReferenceResolution`은 Orientation에 맞춰 가로·세로 순서를 정렬해서 돌려줍니다. 기존 에셋은 Orientation이 없으므로 세로형으로 해석됩니다.
- `UISettings`의 Match Width Or Height를 제거했습니다. Canvas Scaler는 `Expand`로 고정입니다.
- 에디터: UI Settings 인스펙터에 실제 기준 해상도 표시와 **Player Settings에 적용** 버튼(`OrientationPlayerSettings`)을 추가했습니다.
- UI 단어를 정리했습니다. 팝업은 Open/Close(생성과 파괴), 자식 UI는 Show/Hide(켜기와 끄기)만 씁니다.
  - `PopupManager.Show<T>`와 `ShowAsync<T>`를 `Open<T>`와 `OpenAsync<T>`로 이름을 바꿨습니다.
  - `Show`, `Hide`, `OnShow`, `OnHide`, `IsVisible`을 `UIBase`에서 새 클래스 `UIView`로 옮겼습니다. 팝업에는 숨겨 두는 상태가 없고, 뒤로가기의 숨긴 팝업 예외 처리도 없앴습니다.
  - 자식 UI 스크립트는 `UIBase` 대신 `UIView`를 상속합니다. Create UI Script > UI도 `UIView`를 상속한 스크립트를 만듭니다.
  - `PopupBase.Close()`는 재정의할 수 없습니다.
- 팝업 훅 `OnOpen`을 `PrevOpen`(목록에 들어가기 전)과 `PostOpen`(목록에 들어간 뒤)으로 나누고, 닫기 훅 `PrevClose`(목록에서 빠지기 전)와 `PostClose`(빠진 뒤)를 추가했습니다. 기존 `OnOpen`은 `PostOpen`으로 바꾸세요.
- `PopupBase.Closed` 이벤트를 없애고 `OnClosed(Action)`으로 바꿨습니다. `Open<T>().OnClosed(() => ...)`처럼 열면서 닫힌 뒤 할 일을 정합니다(`PostClose` 다음에 실행).
- 설정 에셋은 모두 **Tools > Nyonyo Framework > Settings** 메뉴에서 만들고 엽니다(Framework, UI, Sound, UI Prefix, Game Data, AdMob). 있으면 선택하고, 없으면 기본 위치에 만듭니다. 설정 에셋의 Create 메뉴 항목은 없앴고, 기존 **Create Settings** 메뉴는 **Settings > Framework Settings**가 되었습니다.
- 광고: 전체 화면 광고 대기 시간(`AdPolicy.FullScreenCooldownSeconds`)의 기본값을 60초에서 10초로 바꿨습니다.
- 광고: 앱 복귀 시 앱 오프닝 광고 자동 표시(`AdPolicy.ShowAppOpenOnResume`)의 기본값을 꺼짐으로 바꿨습니다.
- 광고: `AdPolicy.RewardWithoutAdOnUnsupportedPlatform`을 추가했습니다. 기본값(true)이면 광고가 없는 플랫폼(WebGL, PC)의 `NullAdService`가 보상형 광고 요청을 광고를 본 것으로 보고 바로 보상합니다. 끄면 그 플랫폼에서 보상형 광고가 준비되지 않은 상태가 됩니다.
- 플랫폼 대응(Android, iOS, WebGL, PC):
  - `SafeAreaFitter`를 추가했습니다. 노치와 홈 인디케이터를 피해 UI를 배치합니다.
  - 디버그 콘솔을 손가락 세 개로 동시에 탭해서 열 수 있습니다(`DebugConsole.ToggleTouchCount`, 기본 3).
  - `SoundManager`는 앱이 포커스를 잃을 때(모바일 백그라운드 전환, WebGL 탭 전환) 저장하지 않은 볼륨을 바로 저장합니다.
  - IL2CPP 코드 제거 대응: `DebugCommandAttribute`가 `PreserveAttribute`를 상속하고, `PopupAttribute`에 `[RequireAttributeUsages]`, 런타임 어셈블리에 `[assembly: AlwaysLinkAssembly]`를 붙였습니다.
  - Player Settings에 적용 버튼이 WebGL 기본 캔버스 크기, PC 창 크기와 전체 화면 모드(세로형은 창 모드)도 맞춥니다. 창과 캔버스는 기준 해상도의 절반입니다.
- `DebugCommandRegistry.Tokenize`와 `SaveService.CreateDefaultStorage`는 static이 아니게 되었습니다(각각 internal, private).
- 호출하는 쪽이 `_ =`를 쓰지 않도록 `ISceneService.Load(sceneName)`(void)를 추가했고, `PoolManager.DespawnAfter`는 void가 되었습니다. 기다려야 하면 `LoadAsync`, `DespawnAfterAsync`를 쓰세요.
- 코드 규칙(CLAUDE.md): `private` 접근 제한자를 생략하지 않고, 일반 클래스 안의 private static 헬퍼를 instance 멤버로 바꿨습니다. `UISettings.Orient`는 private이 되었습니다.
- `UIBase.OnButtonEvent`는 이제 재정의할 수 없는 진입점입니다. 클릭 효과음(`PlayButtonSound`, 이름은 `UIBase.ButtonClickSound` = `Click` 고정)을 재생한 뒤 `protected virtual OnButtonClick(Button)`을 호출합니다.
  기존 `public override void OnButtonEvent`는 `protected override void OnButtonClick`으로 바꾸세요. OnClick 연결은 그대로 둡니다.
- 자동 매퍼가 만드는 스크립트는 버튼 이름 대신 필드로 비교합니다(`if (button == BTN_X)` / `else if`).
- `SoundManager.PlayBGM`, `StopBGM`, `PlaySFX`는 `void`가 되었습니다. 기다려야 하면 `PlayBGMAsync`, `StopBGMAsync`, `PlaySFXAsync`를 쓰세요. `PreloadSFX`는 `PreloadSFXAsync`로 이름을 바꿨습니다.
- 디버그 콘솔 크기를 화면의 짧은 변 기준으로 계산해서 세로 화면에서도 폭이 좁아지지 않게 했습니다.
- 데이터 테이블을 구글 시트 기반 게임 데이터로 교체했습니다. `DataTable<TRow>`(ScriptableObject), `IDataRow`, `CsvParser`, `CsvRowMapper`, Import CSV 메뉴를 제거했습니다.
  같은 이름의 `DataTable<TKey, TRow>`는 생성 코드가 상속하는 새 기반 클래스입니다(키는 첫 번째 열).
