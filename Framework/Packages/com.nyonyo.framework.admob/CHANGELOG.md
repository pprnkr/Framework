# Changelog

이 패키지의 변경 사항을 기록합니다. 버전 규칙은 [Semantic Versioning](https://semver.org/lang/ko/)을 따릅니다.

## [0.1.0] - 2026-10-27

### Added
- `AdMobAdService`: 배너(적응형), 전면, 보상형, 앱 오프닝 광고. 실패 시 재시도, 앱 오프닝 4시간 만료, 수익 이벤트.
- UMP 동의(GDPR): 시작 시 동의 확인과 동의 창, 개인정보 설정 창, 동의 후 광고 시작.
- `AdMobSettings`: 플랫폼별 광고 단위 ID, 테스트 광고, 테스트 기기, 동의 설정, 노출 규칙, 에디터 모드, `CreateService`.
- 에디터: AdMobSettings 인스펙터(앱 ID 상태, 테스트 앱 ID 채우기), Android/iOS 빌드 전 설정 검사.
- 의존성: `com.google.ads.mobile` 11.6.0 (OpenUPM).
