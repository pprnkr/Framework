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

## 개발

이 패키지는 `Framework` Unity 프로젝트의 `Packages/` 폴더에 임베디드 패키지로 들어 있습니다. 프로젝트를 열면 바로 수정하고 테스트할 수 있습니다.

새 버전을 배포할 때는 `package.json`의 `version`을 올리고, `CHANGELOG.md`에 변경 사항을 적은 뒤, 같은 버전으로 git 태그(`v0.2.0` 등)를 만듭니다.
