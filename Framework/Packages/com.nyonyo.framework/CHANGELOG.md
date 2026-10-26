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

### Changed
- `UISettings`의 Popup Prefabs 목록을 제거했습니다. 팝업 프리팹은 `[Popup]` 어트리뷰트로 불러옵니다.
- `PopupManager` 생성자에 `IAssetLoader` 인자를 추가했습니다.
- `PopupBase.OnButtonEvent`를 `UIBase`의 public 함수로 옮겼습니다. 버튼 클릭은 더 이상 자동으로 연결하지 않으니, OnClick에 연결하거나 Assign Object를 쓰세요.
- `PopupBase`의 Close Button 필드를 제거했습니다.
- 뒤로가기는 `Hide()`로 숨긴 팝업을 건너뜁니다.
