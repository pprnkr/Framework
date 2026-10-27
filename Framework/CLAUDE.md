# Nyonyo Framework

프로젝트 배경, 확정된 결정, 진행 상황, 코드 컨벤션은 아래 문서에 정리되어 있습니다. 작업 전에 읽고, 작업이 끝나면 갱신하세요.

## 기본 규칙

모든 코드(패키지, 테스트, Dev 샘플)에 적용합니다. 새로 쓰는 코드는 물론, 손대는 기존 코드도 이 규칙에 맞춥니다.

1. **쉽게 읽혀야 합니다.** 프레임워크를 쓰는 쪽 코드가 설명 없이 읽혀야 합니다.
   - 단어 하나는 한 가지 뜻으로만 씁니다(예: 팝업은 Open/Close, 자식 UI는 Show/Hide).
   - 문법 트릭보다 평범한 코드를 씁니다(예: `case var _ when` 대신 `if / else if`).
   - 호출하는 쪽이 async 여부나 `_ =` 같은 처리를 신경 쓰지 않게 합니다.
2. **불필요한 전역 변수를 최소화합니다.** 바뀌는 값을 static 필드에 두지 않습니다. 상태는 인스턴스(서비스)가 가지고, 필요한 곳에 생성자로 넘깁니다.
3. **접근 제한자를 생략하지 않습니다.** `private`도 항상 적습니다(필드, 메서드, 속성, 이벤트, 생성자, 중첩 타입).
4. **불필요한 static은 쓰지 않습니다.** 꼭 static이어야 하는 기능은 static으로 둬도 됩니다. 인스턴스 멤버로 충분하면 인스턴스로 만듭니다.
   - 예외: 상수(`const`와, 상수처럼 쓰는 바뀌지 않는 `static readonly` 값), C#이나 Unity가 static을 요구하는 곳(확장 메서드, `RuntimeInitializeOnLoadMethod`, `[MenuItem]`, `[DebugCommand]`, 필드 초기화식에서 부르는 함수), 확정된 결정에 있는 서비스 로케이터(`Services`, `X.Instance`).
   - 예외를 쓸 때는 왜 static이어야 하는지 주석으로 남깁니다.
5. **Android, iOS, WebGL, PC(Windows, macOS, Linux)를 모두 지원합니다.** 기능을 만들 때 네 플랫폼을 처음부터 함께 대응하고, 나중으로 미루지 않습니다.
   플랫폼마다 동작이 다르면 코드 주석과 README에 적고, 지원할 수 없는 기능은 그 플랫폼에서 조용히 아무 일도 하지 않는 대체 구현(예: `NullAdService`)을 둡니다.
   작업할 때 아래를 확인합니다.
   - **WebGL:** 스레드(`Task.Run`, `Thread`)를 쓰지 않습니다. 동기 로드(Addressables `WaitForCompletion`)와 파일 입출력을 쓰지 않습니다. 저장은 PlayerPrefs(전체 약 1MB)입니다.
   - **IL2CPP(Android, iOS, WebGL):** 리플렉션으로 찾는 코드는 코드 제거(Managed Stripping)에 지워질 수 있습니다. `[Preserve]`나 `PreserveAttribute` 상속으로 보존합니다. 패키지 안의 `link.xml`은 인정되지 않습니다.
   - **앱 수명:** 모바일과 WebGL은 `Application.quitting` 없이 내려갈 수 있습니다. 저장이 미뤄진 값은 포커스를 잃을 때(`Application.focusChanged`) 저장합니다.
   - **입력:** 키보드 없이도 쓸 수 있어야 합니다(터치). Android 뒤로가기는 ESC로 들어오고, iOS에는 뒤로가기가 없으므로 화면 버튼으로도 닫을 수 있어야 합니다.
   - **화면:** 노치와 홈 인디케이터는 `SafeAreaFitter`로 피합니다. 해상도와 DPI가 달라도 읽을 수 있게 크기를 맞춥니다.
   - **검증:** compile-check의 WebGL 패스(`UNITY_WEBGL`, 에디터 심볼 없음)로 컴파일을 확인합니다.

@.claude/worktree/project-context.md
