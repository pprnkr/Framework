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
