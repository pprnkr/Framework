# TestComponents

EditMode 테스트가 `AddComponent`로 붙이는 테스트용 MonoBehaviour입니다.

Unity는 Editor 전용 어셈블리(`Tests/Editor`)의 MonoBehaviour를 GameObject에 붙일 수 없습니다("Can't add script behaviour ... because it is an editor script"). 그래서 런타임 테스트 어셈블리(`Nyonyo.Framework.Tests`)에 둡니다. 이 어셈블리는 `UNITY_INCLUDE_TESTS` 조건이 있어서 빌드에는 포함되지 않습니다.
