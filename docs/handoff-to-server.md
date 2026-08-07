# 클라이언트에서 서버로 — 우편함

> 서버 쪽 요청은 `handoff-from-server.md`에 있다. **답과 질문을 여기 적고 PR로 올리면 서버가 읽는다.**
> 서버 저장소에 손댈 필요 없다.
>
> **이건 편지지 명세가 아니다.** 여기서 합의된 것이 계약이 되어야 하면, 그건 서버가
> `data-contract.md` 원본이나 서버 문서로 옮긴다. 옮겨간 항목은 여기서 지운다.

---

## 요청 1 — 임시 상태 구현의 필드 목록

**상태: 회신함 · 2026-08-06**

`client-design.md` 3절이 "서버 소유 상태를 임시로 만들되 **한 곳에 모아둔다**"고 정해뒀다.
서버는 그 한 곳의 필드 목록을 기다리고 있다 — **그게 서버 API 응답의 모양이 된다.**

완성 안 돼도 된다. 이름과 타입만 있으면 된다. 이런 모양이면 충분하다.

```
| 필드 | 타입 | 지금 클라가 채우는 방식 | 비고 |
|---|---|---|---|
| hunger | int | 로컬 난수 | |
| wallet | int | 로컬 난수 | 요리 가격과 비교함 |
| ... | | | |
```

### 답

클라이언트 1단계는 실제 Android 기기에서 끝냈다. 목 JSON을 `Assets/Resources/`에서 읽어
한글 이름·소개를 표시했고, APK에서도 같은 화면을 확인했다.

2단계의 첫 임시 상태 저장소는 아래 필드를 정확히 가진다. 아직 코드로 만들지는 않았으며,
서버 API가 준비되면 이 저장소의 공급자만 네트워크 호출로 바꾼다.

| 필드 | JSON 타입 | 용도 | 소유 |
|---|---|---|---|
| `save_id` | string | 같은 플레이어·날짜의 상태를 재현하는 식별자 | 서버 |
| `day_number` | integer | 게임 내 날짜 | 서버 |
| `guest_id` | string | 클라의 고정 손님 페르소나와 조인하는 키 | 계약 |
| `hunger` | integer | 오늘의 허기 (0~100) | 서버 |
| `condition` | string | `normal` · `injured` · `tired` | 서버 |
| `mood` | string | `gloomy` · `calm` · `elated` | 서버 |
| `wallet` | integer | 오늘 이 손님이 쓸 수 있는 돈 | 서버 |

`name`, `bio`, `voice`, 취향 구간, 식이 제약처럼 변하지 않는 정보는 응답에 넣지 않는다.
클라 번들의 `guests.json`을 `guest_id`로 읽는다. 오늘의 욕구, 관계·공개 취향, 재고,
소지금·평판, 시장 시세도 아직 이 화면에 필요 없으므로 이번 API 범위 밖이다.

---

## 완료 보고 — 3-1 생성 콘텐츠 교체

**상태: 완료 · 2026-08-07**

- 파이프라인 `out/packages/1.0.0/`의 `guests.json`, `ingredients.json`,
  `dishes.json`, `lines.json`을 클라 `Assets/Resources/`에 그대로 교체했다.
- `schema_version` 1.0.0과 생성분 일치, 요리→재료 참조, 대사→손님 말투 조인을 검사했다.
  수량은 손님 8 · 재료 12 · 요리 10 · 대사 80이다.
- 새 APK를 Android Studio Pixel 7 가상 기기에서 설치·실행했다. 첫 손님
  `guest_dusty_patrol_01`(도윤 / 변방 순찰자)의 한글 이름·소개가 정상 표시됐다.
- 막힌 점은 없다. 이번 재확인은 가상 기기에서 했으며, 실제 Android 기기에서의 1단계
  한글 표시 확인은 앞선 작업에서 완료했다.

---

## 클라이언트가 서버에 묻는 것

### 요청 1 — 오늘 손님 상태 조회 API를 시작해 달라

위 필드로 손님 한 명의 오늘 상태를 결정적으로 돌려주는 읽기 전용 API를 만들어 달라.
클라는 `save_id`, `day_number`, `guest_id`를 보내고, 응답은 위 표의 손님 상태여야 한다.
경로·HTTP 상태 코드·API 버전 표기는 서버가 정해 `handoff-from-server.md`에 알려 달라.

응답의 enum 문자열은 Java enum 이름 그대로 대문자로 내보내지 말고, 표의 소문자
`snake_case` 어휘로 고정해 달라. Unity 쪽 DTO도 그 값으로 만든다.

### 질문 1 — `prefers_affordable`의 출처

현재 서버의 `VisitStateGenerator`는 `GuestTraits.prefersAffordable`가 필요하다. 하지만
고정 손님 페르소나는 클라 번들 소유이고 서버가 이를 중계하면 안 된다. 서버가 이 값을
어떤 독립된 방식으로 얻을지(계약 스냅샷을 읽는 시딩, 별도 룩업 등)를 정해 알려 달라.
클라가 요청에 임의의 boolean을 실어 보내는 우회는 하지 않겠다.

---

## 완료 보고 — 3-2 폰트 아틀라스 확장

**상태: 완료 · 2026-08-07**

- `NotoSansCJKkr-Regular Extended SDF`를 `20-7E,A1-FF,2013-2014,2018-201D,2026,AC00-D7A3` 범위와 4096×4096 아틀라스로 생성했다.
- 런타임 UI를 해당 TMP 폰트를 사용하도록 전환하고, `lines.json`의 `line_greet_weary_01`을 직접 로드해 검증 화면에 표시한다.
- Unity Editor와 Android Studio Pixel 7 가상 기기 APK에서 `앉으니 살 것 같네…`를 확인했다. 한글과 U+2026 말줄임표 모두 깨지지 않았고, 소개문과도 분리돼 보인다.
- 막힌 지점은 없다. 에디터 복구 폴더는 작업 산출물에서 제외한다.

---

## 완료 보고 — 3-3 2단계 하루 사이클

**상태: 완료 · 2026-08-07**

- 장보기 → 손님 1명 → 요리 → 반응 → 하루 종료의 버튼 기반 시연 흐름을 만들었다.
- 임시 상태는 `LocalDayStateStore` 한 곳에 모았다. 그 안의 `VisitStateResponse`는 API 응답과 같은
  `save_id`, `day_number`, `guest_id`, `hunger`, `condition`, `mood`, `wallet`, `needs` 필드를 가진다.
- 로컬 값은 시연용 고정값(`tired` · `gloomy` · `mild` · `affordable`)이다. 서버 HTTP 호출,
  VisitState/NeedResolver, 만족도 계산은 의도적으로 이식하지 않았다.
- Unity Editor와 Android Studio Pixel 7 가상 기기 APK에서 전체 버튼 흐름을 끝까지 확인했다.
- 막힌 지점은 없다. 다음 날 진행·수치 조절은 3단계 이후의 규칙/상태 작업으로 남긴다.

---

## 완료 보고 — 3-4 오늘 상태와 욕구 이식

**상태: 완료 · 2026-08-08**

- `Assets/Scripts/Domain/`에 UnityEngine 참조 없는 순수 C# 도메인과 `DailySpecial.Domain.asmdef`를 만들었다.
  VisitSeed(SHA-256), SplitMix64, VisitStateGenerator, NeedResolver와 수치·어휘 모델을 서버 원본에서 이식했다.
- SplitMix64의 64비트 결과를 `long`으로 해석하고 floorMod를 적용했다. `ulong` 나머지로 계산하면 고정 벡터가 달라지는 함정을 별도 테스트로 고정했다.
- 서버의 VisitStateGeneratorTest·NeedResolverTest 케이스를 Unity EditMode 테스트로 옮겼다. 결과는 26/26 통과다.
- `LocalDayStateStore`는 고정 표본 대신 생성기 결과를 API 응답 모양으로 매핑한다. 다음 날 열기로 같은 손님의 날짜별 상태·욕구가 다시 생성된다.
- Unity Editor에서 1일차부터 20일차까지 날짜별 상태와 욕구가 달라지는 것을 확인했다.
- 만족도 엔진과 서버 HTTP 호출은 의도적으로 추가하지 않았다.

---

## 완료 보고 — 3-5 만족도 엔진 이식

**상태: 완료 · 2026-08-08**

- 파이프라인 `satisfaction.py`를 순수 C# `SatisfactionEngine`으로 이식했다. 입력은 콘텐츠
  레코드가 아닌 `GuestPersona`, `VisitState`, `ServedDish`, `ScoringNumbers`이며, 결과는
  총점·항별 점수·축 점수·미충족 욕구·위반 식이를 함께 돌려준다.
- `dietary_conflicts`는 재료 저촉의 합집합으로 `Data/`에서 만들고, `params`는 플레이어 조리
  슬라이더 값으로 받는다. 하루 사이클 반응 화면은 이 입력으로 만족도·미충족 욕구·축 피드백
  대사를 표시한다.
- `tests/test_satisfaction.py`의 23개 명세 케이스를 Unity EditMode 테스트로 옮겼다.
  전체 49/49, 새 만족도 명세 23/23 통과다.
- `VisitNumbers.AffordableWalletMax`는 `MidpointRounding.AwayFromZero`로 바꿔 Java 반올림과
  같은 방향을 유지한다. 서버 HTTP 호출은 추가하지 않았다.
