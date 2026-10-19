# 코드 리뷰 가이드 (Unity / C#)

이 저장소는 Unity로 제작하는 3D FPS 게임 프로젝트이다.
**모든 리뷰 코멘트와 요약은 한국어로 작성한다.**

## 리뷰 우선순위

1. 버그·크래시 가능성 (NullReference, 잘못된 순서의 초기화, 파괴된 오브젝트 접근)
2. 성능 문제 (특히 매 프레임 실행되는 코드)
3. 가독성·유지보수성
4. 스타일 (포매팅은 dotnet format이 처리하므로 지적하지 않는다)

## Unity 관련 체크리스트

### 생명주기
- `Awake`에서는 자기 자신 초기화, `Start`에서는 다른 오브젝트 참조를 사용하는지 확인한다.
- `OnEnable`에서 구독한 이벤트를 `OnDisable`에서 해제하는지 확인한다.
- 빈 `Update`, `Start` 등 내용 없는 Unity 메시지 메서드는 제거를 권한다.

### 성능
- `Update` / `FixedUpdate` / `LateUpdate` 안에서 다음을 지적한다:
  - `GetComponent`, `Find`, `FindObjectOfType`, `Camera.main` 반복 호출 → 캐싱 권장
  - `new` 로 인한 매 프레임 GC 할당 (리스트, 배열, 문자열 결합, LINQ)
  - `Debug.Log` 남용
- 총알·이펙트처럼 자주 생성/파괴되는 오브젝트는 오브젝트 풀링을 고려하도록 제안한다.
- 물리 이동은 `FixedUpdate`에서, 입력은 `Update`에서 처리하는지 확인한다.
- `Time.deltaTime` 누락으로 프레임 의존적인 이동이 없는지 확인한다.

### Null / 오브젝트 비교
- `UnityEngine.Object`에는 `?.`, `??`, `is null` 사용을 지적한다 (Unity의 null 오버로드가 우회됨).
- 인스펙터 할당 필드가 비어 있을 때 대비가 필요한 곳을 지적한다.

### 직렬화 / 인스펙터
- 외부 접근이 필요 없는 인스펙터 필드는 `public` 대신 `[SerializeField] private`를 권장한다.
- 매직 넘버는 `[SerializeField]` 필드나 상수로 빼도록 제안한다.

### Input System
- 이 프로젝트는 New Input System(`InputSystem_Actions.inputactions`)을 사용한다.
  `Input.GetKey` 등 구 Input Manager API 사용을 지적한다.

### 코루틴 / 비동기
- 오브젝트 비활성화·파괴 시 코루틴이 정리되는지 확인한다.
- 루프 안에서 `new WaitForSeconds(...)`를 매번 생성하면 캐싱을 권장한다.

## C# 규칙

- 클래스·메서드·프로퍼티: PascalCase
- private 필드: `_camelCase`
- 지역 변수·매개변수: camelCase
- 한 클래스는 하나의 책임만 갖도록 한다. 지나치게 큰 MonoBehaviour는 분리를 제안한다.
- 의미 없는 주석보다 의도가 드러나는 이름을 권장한다.

## 리뷰 태도

- 문제를 지적할 때는 **이유**와 **수정 예시 코드**를 함께 제시한다.
- 확실하지 않은 내용은 추측임을 밝힌다.
- 변경된 코드와 무관한 부분은 리뷰하지 않는다.
