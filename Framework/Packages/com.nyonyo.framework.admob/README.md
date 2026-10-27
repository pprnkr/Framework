# Nyonyo Framework - AdMob

Nyonyo Framework의 광고 서비스(`IAdService`)를 Google AdMob으로 구현한 선택 모듈입니다. 광고가 필요한 게임만 설치합니다.

- **지원 형식:** 배너(적응형), 전면, 보상형, 앱 오프닝
- **동의 처리:** Google UMP 동의(GDPR) 창, 개인정보 설정(동의 변경) 창
- **노출 규칙:** 광고 제거 구매, 전체 화면 광고 사이 대기 시간, 실패 시 자동 재시도
- **에디터:** 가짜 광고 또는 AdMob 플레이스홀더 광고로 흐름 확인

## 설치

게임 프로젝트의 `Packages/manifest.json`에 OpenUPM 레지스트리와 두 패키지를 넣습니다. git 태그는 두 패키지가 같게 맞춥니다.

```json
{
  "scopedRegistries": [
    { "name": "OpenUPM", "url": "https://package.openupm.com", "scopes": ["com.google.ads.mobile"] }
  ],
  "dependencies": {
    "com.nyonyo.framework": "https://github.com/pprnkr/Framework.git?path=/Framework/Packages/com.nyonyo.framework#v0.2.0",
    "com.nyonyo.framework.admob": "https://github.com/pprnkr/Framework.git?path=/Framework/Packages/com.nyonyo.framework.admob#v0.2.0"
  }
}
```

- **자동 설치:** AdMob 플러그인(`com.google.ads.mobile` 11.6.0)과 External Dependency Manager는 자동으로 따라 들어옵니다.
- **두 줄 모두 필요:** UPM은 패키지 의존성에 git URL을 쓸 수 없습니다. 그래서 코어 패키지도 직접 적어야 합니다.
- **요구 사항:** Android 최소 API 24, IL2CPP를 권장합니다.

## 설정

1. **앱 ID:** Assets > Google Mobile Ads > Settings...에 AdMob 콘솔의 앱 ID(`ca-app-pub-...~...`)를 넣습니다.
   - Android는 앱 ID가 없으면 실행하자마자 종료됩니다.
   - 개발 중에는 AdMobSettings 인스펙터의 **빈 앱 ID에 테스트 ID 넣기** 버튼을 써도 됩니다.
2. **설정 에셋:** Tools > Nyonyo Framework > Settings > AdMob Settings로 설정 에셋을 만듭니다.
   - **Use Test Ads:** 켜져 있으면 Google 테스트 광고를 씁니다(기본값). 출시 빌드 전에 끄고, Android와 iOS의 광고 단위 ID를 넣습니다.
   - **Request Consent:** 유럽(EEA), 영국 등에 출시하면 켭니다. AdMob 콘솔의 개인정보 보호 및 메시지에서 GDPR 메시지를 만들어 두어야 창이 뜹니다.
   - **Consent Debug Geography:** 한국에서 동의 창을 시험하려면 EEA로 둡니다(테스트 기기만). 출시 전에 Disabled로 돌립니다.
   - **Policy:** 전체 화면 광고 사이 대기 시간(기본 10초), 앱 복귀 시 앱 오프닝 광고 자동 표시 여부(기본 끔), 웹과 PC에서 보상형을 광고 없이 바로 보상할지 여부(기본 켬).
   - **Editor Mode:** 에디터에서 쓸 광고입니다. Fake는 보상 성공, 실패, 광고 없음을 골라 확인할 수 있습니다. AdMob Placeholder는 플러그인의 플레이스홀더로 실제 흐름을 확인합니다.
3. **서비스 등록:** Installer에서 등록합니다.

```csharp
[SerializeField] private AdMobSettings _adMobSettings;

public override void Install(ServiceRegistry services)
{
    var save = new SaveService();
    services.Register(save);
    // Android/iOS는 AdMob, 에디터는 Editor Mode, 그 밖의 플랫폼은 광고 없음(NullAdService)
    services.Register<IAdService>(_adMobSettings.CreateService(Bootstrapper.Root, save));
}
```

사용법은 코어 README의 "광고" 절을 보세요.

웹(WebGL)과 PC에는 AdMob이 없어서 광고가 나오지 않습니다. 대신 보상형 광고는 기본으로 광고를 본 것으로 보고 바로 보상을 줍니다. 그 플랫폼에서 보상을 막으려면 AdMob Settings의 Policy에서 **Reward Without Ad On Unsupported Platform**을 끄세요.

## 동작

- **시작 순서:** 서비스가 초기화되면 동의 확인 → 필요하면 동의 창 → SDK 초기화 → 전면, 보상형, 앱 오프닝 광고 미리 불러오기 순서로 진행합니다. 게임 시작을 막지 않습니다.
- **재시도:** 불러오기에 실패하면 2, 4, 8 ... 최대 64초 간격으로 다시 시도합니다. 광고를 보여 준 뒤에는 다음 광고를 바로 불러 둡니다.
- **동의 거부:** 동의를 받지 못하면 광고를 시작하지 않습니다. 사용자가 개인정보 설정에서 동의하면 그때 시작합니다.
- **앱 오프닝 광고:** 불러온 뒤 4시간이 지나면 버리고 새로 불러옵니다(AdMob 정책).
- **일시정지:** 전체 화면 광고가 떠 있는 동안 Unity가 멈춥니다(Android 기본 동작, iOS는 `SetiOSAppPauseOnBackground`).
- **빌드 검사:** Android, iOS 빌드 때 앱 ID가 비어 있으면 에러를 남깁니다. 출시 빌드에 테스트 광고나 테스트 앱 ID가 남아 있으면 경고합니다.

## 출시 전 확인

- [ ] Use Test Ads 끄기, 실제 광고 단위 ID 넣기
- [ ] 앱 ID를 실제 값으로 바꾸기
- [ ] Consent Debug Geography를 Disabled로 돌리기
- [ ] 설정 화면에 `IsPrivacyOptionsRequired`일 때 "개인정보 설정" 버튼 넣기(Google 정책)
- [ ] app-ads.txt 등록(AdMob 콘솔 안내)
