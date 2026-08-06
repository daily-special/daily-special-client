# 코드 규약 — daily-special-client

> **코드를 쓰기 전에 읽는다.** 이 문서가 코드를 강제한다.
>
> **지금은 얇다. 지형을 알게 되면 늘린다.** Unity 버전도 렌더 파이프라인도 아직 안 정해졌고,
> 추측으로 규약을 박으면 며칠 뒤에 거짓말이 된다. 여기 있는 것은 **1일차에 정하지 않으면
> 나중에 비싸지는 것**만이다.
>
> 이 문서는 자체 완결로 쓴다. 외부 문서를 참조하지 않는다.
> 최종 수정: 2026-07-30

---

## 1. ⭐ 도메인은 `UnityEngine`을 모른다

**이 저장소에서 가장 중요한 규약이다.**

만족도 엔진은 파이썬 레퍼런스를 옮긴 것이고, 그쪽 테스트 23건이 이식 명세다. 그 케이스를 돌리려면 도메인이 게임 오브젝트에서 떨어져 있어야 한다.

```csharp
// ✅ 순수 C#. EditMode 테스트로 23건을 돌릴 수 있다
public static class SatisfactionEngine {
    public static Satisfaction Evaluate(GuestPersona persona, VisitState state, ...) { ... }
}

// ❌ MonoBehaviour. 이식이 맞는지 확인할 방법이 사라진다
public class SatisfactionEngine : MonoBehaviour {
    void Update() { ... }
}
```

**나중에 고치면 비싸다.** 3~4일차에 엔진을 붙이고 나서 "테스트가 안 돌아요"가 되면 그때 뜯어야 한다. 지금 정해두면 공짜다.

같은 규칙이 도메인 전체에 적용된다 — 만족도, 손님 판단, 요리 조합. **화면에 그리는 것만 Unity를 안다.**

## 2. 폴더

```
Assets/Scripts/
  Domain/     순수 C#. UnityEngine을 import하지 않는다
  Game/       하루 사이클, 상태 관리. MonoBehaviour
  UI/         화면. MonoBehaviour
  Data/       JSON 로딩·DTO
Assets/Tests/
  EditMode/   Domain 검증
```

`Domain/`에 어셈블리 정의(`.asmdef`)를 두고 **`UnityEngine` 참조를 빼면** 규약 1절이 컴파일 단계에서 강제된다. 사람이 지키는 것보다 도구가 막는 편이 낫다.

## 3. 테스트

**도메인만 테스트한다.** UI와 MonoBehaviour는 안 한다 — 11일짜리 일정에서 값이 안 나온다.

- Unity Test Framework, **EditMode**
- 이식 명세: `daily-special-pipeline`의 `tests/test_satisfaction.py` 23건
- 설계 결정은 주석이 아니라 테스트로 고정한다

## 4. 데이터 로딩 — `Resources`를 쓴다

**`StreamingAssets`는 안드로이드에서 `File.ReadAllText`로 못 읽는다.** APK 안에 압축돼 있어서다. 에디터에선 되고 폰에서 안 되는 전형적인 함정이라, 처음부터 피한다.

```csharp
var json = Resources.Load<TextAsset>("guests").text;
```

콘텐츠 JSON은 `Assets/Resources/*.json`에 둔다.

**JSON은 `snake_case`다** (계약 1-1절). Newtonsoft(`com.unity.nuget.newtonsoft-json`)에 네이밍 전략을 준다.

```csharp
var settings = new JsonSerializerSettings {
    ContractResolver = new DefaultContractResolver {
        NamingStrategy = new SnakeCaseNamingStrategy()
    }
};
```

**모르는 필드는 무시한다. major가 다르면 로드를 거부한다** (계약 3-2절). 파이프라인이 필드를 더할 때 클라가 안 깨지려면 앞엣것이, 밸런스가 어긋난 채 굴러가지 않으려면 뒤엣것이 필요하다.

## 5. 커밋·브랜치

파이프라인 저장소와 같다.

- 브랜치 `<type>/<영문-슬러그>` (예: `feat/day-cycle`). **main에서 작업하지 않는다**
- 커밋 `<type>: <한국어 설명>` 제목 한 줄, 50자 이내
- squash 머지. PR 제목이 곧 커밋 제목이므로 45자 안쪽
- `type`: `feat` `fix` `refactor` `docs` `test` `chore`

## 6. 한글 폰트 (Unity를 쓰는 경우)

TextMeshPro 기본 폰트에 **한글이 없다.** 그냥 두면 네모만 나온다. 이 게임은 텍스트가 전부 한국어라 1단계에서 반드시 확인한다.

- `Window → TextMeshPro → Font Asset Creator`
- Character Set을 `Unicode Range (Hex)`로: `20-7E,A1-FF,2013-2014,2018-201D,2026,AC00-D7A3`
- Atlas Resolution **4096×4096** (1024로는 한글이 다 안 들어간다)

**이 범위 문자열의 원본은 `data-contract.md` 1-3절이다.** 파이프라인 설정이 소유하고 거기서 만들어 낸다 — 여기 있는 것은 굽는 사람이 보라고 옮겨 적은 값이다. **직접 늘리지 마라.** 생성된 텍스트에 없는 글자가 나오면 파이프라인 검사에서 먼저 잡히므로, 계약이 바뀌고 나서 이 값을 맞춘다.

`20-7E`는 영문·숫자·기호, `A1-FF`와 `2013-2014`, `2018-201D`, `2026`은
문장부호·줄표·따옴표·말줄임표, `AC00-D7A3`은 한글 완성형 11,172자다.
