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

## 개발

이 패키지는 `Framework` Unity 프로젝트의 `Packages/` 폴더에 임베디드 패키지로 들어 있습니다. 프로젝트를 열면 바로 수정하고 테스트할 수 있습니다.

새 버전을 배포할 때는 `package.json`의 `version`을 올리고, `CHANGELOG.md`에 변경 사항을 적은 뒤, 같은 버전으로 git 태그(`v0.2.0` 등)를 만듭니다.
