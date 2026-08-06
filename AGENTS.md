# daily-special-client 작업 지침

「오늘의 정식」(Daily Special)의 Unity 2D 클라이언트 저장소다.

## 먼저 읽을 문서

- `docs/client-design.md`: 클라이언트의 책임과 구현 순서
- `docs/conventions.md`: 계층, 테스트, 데이터 로딩 규약
- `docs/data-contract.md`: 파이프라인·서버와 공유하는 JSON 계약
- `docs/handoff-from-server.md`: 서버 쪽에서 온 편지. 명세가 아니라 **요청과 예고**다. 그 파일이 지워지면 이 줄도 지운다

저장소 문서는 각각 자체 완결이어야 한다. 다른 저장소의 문서를 참조하도록 새로 쓰지 않는다.

## 현재 단계

우선순위는 게임 로직보다 **목 JSON을 읽어 한글을 표시하고 Android APK에서 확인하는 것**이다.

1. `data/*.json`을 `Assets/Resources/`에서 로드한다.
2. 손님 이름과 소개를 한글 폰트로 화면에 표시한다.
3. 실제 Android 기기에서 APK를 확인한다.

에디터에서만 동작하거나 한글 폰트가 깨지면 Unity 진행은 go가 아니다. UI·도메인 구현을 늘리기 전에 이 검증을 끝낸다.

## 책임 경계

- 파이프라인: 고정 콘텐츠 JSON을 생성한다.
- 서버(Spring Boot): 플레이 중 변하는 상태를 소유한다. 오늘의 손님 상태·욕구·지갑, 재고, 소지금, 평판, 관계와 공개 취향이 대상이다.
- 클라이언트(Unity): 화면, 입력, 하루 사이클 표현, 콘텐츠 로딩, 만족도 엔진을 맡는다. 서버가 아직 없을 때의 상태는 한 곳에 모은 임시 구현만 둔다.

세 저장소는 JSON 계약으로만 만난다. 서버나 파이프라인의 코드를 이 저장소에 복사하지 않는다.

## 코드 규약

- 식별자는 영어, 주석·독스트링·오류 메시지는 한국어로 쓴다.
- `Assets/Scripts/Domain/`은 순수 C#이다. `UnityEngine`을 import하거나 `MonoBehaviour`를 사용하지 않는다.
- `Game/`은 하루 사이클과 상태 관리, `UI/`는 화면, `Data/`는 JSON 로딩과 DTO를 둔다.
- 도메인 검증은 Unity Test Framework의 EditMode 테스트로 작성한다. 설계 결정은 주석보다 테스트로 고정한다.
- JSON은 `snake_case`다. `Newtonsoft.Json`과 `SnakeCaseNamingStrategy`를 쓴다.
- JSON은 `Resources.Load<TextAsset>`로 읽는다. Android APK에서 `StreamingAssets`를 `File.ReadAllText`로 읽는 방식은 쓰지 않는다.
- 모르는 JSON 필드는 무시한다. `schema_version`의 major가 다르면 로드를 거부한다. `bible_version`은 로그 외 분기에 쓰지 않는다.

## 만족도 엔진

만족도는 런타임 LLM 호출 없이 결정적으로 계산한다.

```
만족 = 욕구 충족도 × 취향 일치도 × 예산 적합 × 식이 계수
```

이식할 때는 `daily-special/daily-special-pipeline`의 `src/daily_special/domain/satisfaction.py`와 `tests/test_satisfaction.py`를 원본 명세로 삼고, 테스트 케이스 전체를 C# EditMode 테스트로 옮긴다. 별도 요약 명세를 만들지 않는다.

## 작업 방식

코드나 파일을 변경하기 전에는 다음을 짧게 보고한다.

1. 무엇을 만들며, 어떤 파일을 새로 만들거나 수정하는가
2. 이번 범위 밖인 것은 무엇인가
3. 사용자 판단이 필요한 것이 있는가. 없으면 `결정 필요 없음`이라고 명시한다

작업 중 파일을 변경할 때는 무엇을 왜 바꾸는지 한 줄로 알린다. 계획이나 범위가 달라지면 계속 진행하지 말고 보고한다.

완료 후에는 변경 사항, 하지 않은 사항과 이유, 테스트·빌드 결과, 문서 반영 여부를 보고한다. 커밋은 사용자가 요청할 때만 하며, 요청이 없으면 권장 커밋 메시지만 제시한다.

## Git

- 작업은 `main`이 아닌 `<type>/<english-slug>` 브랜치에서 한다. 예: `feat/day-cycle`
- 커밋 메시지: `<type>: <한국어 설명>` 한 줄, 50자 이내
- 허용 type: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`
- squash merge를 기본으로 한다.
