# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 6 (`6000.3.22f1`, URP 17) 기반 3D FPS 학습 프로젝트. 씬은 `Assets/01.Scenes/GameScene.unity` 하나이며, 게임 코드는 전부 `Assets/02.Scripts/` 아래에 있다 (asmdef 없음 → `Assembly-CSharp`). 코드 주석·로그·UI 텍스트·커밋 메시지는 한국어로 작성한다.

## 명령어

빌드/실행/플레이 테스트는 Unity 에디터에서 한다. CLI에서 쓸 수 있는 것은 포맷 검사뿐이다.

```sh
# pre-commit 훅이 실행하는 것과 동일한 포맷/네이밍 검사 (.sln은 Unity가 생성)
dotnet format 3DFPSGame.sln --verify-no-changes --severity warn --include Assets/02.Scripts/

# 자동 수정
dotnet format 3DFPSGame.sln --severity warn --include Assets/02.Scripts/

# Husky.Net 훅 설치 (최초 1회)
dotnet tool restore && dotnet husky install
```

- 유닛 테스트 어셈블리는 없다. `Assets/02.Scripts/Test/`는 C# 문법 연습용 MonoBehaviour이지 테스트가 아니다.
- Git LFS를 사용한다 (post-checkout/pre-push 훅).

## 커밋 규칙

`commit-msg` 훅이 다음 형식을 강제한다: `type: 설명` (선택적으로 `type(scope):`)
허용 type: `asset|build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test` — 에셋 추가/변경은 `asset:`을 쓴다.

`main`에 `Assets/02.Scripts/**/*.cs` 변경이 푸시되면 GitHub Actions(`.github/workflows/gemini-commit-review.yml`)가 Gemini로 커밋 리뷰를 남긴다. 리뷰 기준은 `Docs/CodeReviewGuide.md`.

## 코드 스타일 (`.editorconfig`, `Assets/02.Scripts/**.cs`에만 적용)

- private 필드 `_camelCase`, private `const`/`static readonly`는 `PascalCase`, 지역변수 `camelCase`, 타입명 PascalCase (`_` 금지).
  - 예외: UI 스크립트 클래스는 `UI_` 접두사를 쓴다 (`UI_PlayerStats`, `UI_Minimap` 등).
- 중괄호는 Allman 스타일. 한 줄 조기 리턴(`if (...) return;`)은 중괄호 생략 허용.
- `switch`의 case는 `case X: { ...; break; }` 블록 형태로 쓴다.
- 인스펙터 노출은 `[SerializeField] private` + `[Header("스탯")]` 식으로 묶고, 외부에서 읽을 값은 `=>` 읽기 전용 프로퍼티로 노출한다.
- 코루틴 메서드 이름은 `Xxx_Coroutine` 접미사.
- 로그: `GameLogger.Log`는 `[Conditional("ENABLE_LOG")]`라서 해당 심볼이 없으면 호출 자체가 제거된다. 경고/에러는 `GameLogger.LogWarning/LogError`. (일부 기존 코드는 아직 `Debug.Log`를 직접 사용)

## 아키텍처

**게임 상태 게이팅** — `GameManager`는 정적 `Instance` 싱글턴이며 `GameState { Ready, Playing, GameOver }`를 가진다. 시작 시 "준비 → 시작" 코루틴 후 `Playing`이 된다. 입력/AI를 처리하는 `Update`는 맨 앞에서 `if (GameManager.Instance.State != GameState.Playing) return;`으로 가드하는 것이 관례이므로, 새 게임플레이 스크립트도 이를 따른다. 플레이어 체력이 0이 되면 `Player`가 `GameManager.Instance.GameOver()`를 호출한다.

**대미지 시스템** — 맞을 수 있는 모든 것(`Player`, `Zombie`, `Drum`, `Box`)은 `IDamageable.TakeDamage(DamageInfo)`를 구현한다. `DamageInfo`는 `Value`, `HitPoint`, `HitNormal`을 담는 구조체로, 피격 이펙트 위치/방향에 쓰인다. 공격 측은 구체 타입이 아닌 인터페이스로 호출한다:
- 총: `PlayerGunFire`가 메인 카메라에서 `Physics.Raycast` → `GetComponent<IDamageable>()?.TakeDamage(...)`
- 범위 피해: `Drum`/`Bomb`이 `Physics.OverlapSphere(..., LayerMask.GetMask("Drum", "Zombie"))`로 대상 수집 → 드럼통 연쇄 폭발. 범위 피해 대상을 추가하려면 레이어 마스크도 같이 바꿔야 한다.
- 좀비: 공격 범위 안에서 `_player.TryGetComponent(out IDamageable ...)`.

**좀비 FSM** — `Zombie`는 `ZombieState` enum + `Update`의 `switch`로 동작한다 (Idle → Trace → Attack, 시작 위치에서 너무 멀어지면 Comeback). `Hit`/`Death`는 `Update`에서 처리하지 않고 `TakeDamage`가 `StopAllCoroutines()` 후 `Hit_Coroutine`/`Death_Coroutine`을 시작해 전이한다. 이동은 `CharacterController.SimpleMove`, 플레이어는 `"Player"` 태그로 찾는다.

**UI는 폴링 방식** — `UI_PlayerStats`, `ZombieHealthBar` 등은 이벤트 없이 매 프레임 대상의 읽기 전용 프로퍼티(`Health`/`MaxHealth`, `PlayerMove.Stamina`/`MaxStamina`)를 읽어 갱신한다. 참조는 인스펙터에서 `[SerializeField]`로 연결한다.

**카메라** — `CameraViewSwitcher`가 V 키로 프리셋(1인칭/3인칭/숄더/쿼터/탑다운/사이드)을 순환하며 `LateUpdate`에서 위치를 잡고, 프리셋에 따라 `CameraRotate`(마우스 회전)를 켜고 끈다. 미니맵은 별도 `MinimapCamera` → RenderTexture → 마스크로 둥글게 처리한 `UI_Minimap`.

**입력** — Player Settings의 Active Input Handling이 "Both"이지만, 현재 스크립트는 모두 레거시 `Input.GetKey*/GetButton`을 사용한다 (`InputSystem_Actions.inputactions`는 미사용). `CursorManager`가 커서를 잠그고 LeftAlt를 누르는 동안 해제하며, `PlayerGunFire`는 `Cursor.visible`이면 발사하지 않는다.
