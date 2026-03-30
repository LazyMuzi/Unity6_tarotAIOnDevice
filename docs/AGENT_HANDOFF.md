# 에이전트 인수인계 — Tarot (Unity)

다음 작업자가 맥락을 잡기 위한 요약입니다. 상세 규칙은 저장소 루트의 `AGENTS.md`를 참고하세요.

## 경로·구조

- 타로 관련 스크립트·씬·에셋은 **`Assets/Tarot/`** 기준입니다.
- 예전 **`Assets/Scripts/`**, **`Assets/Scenes/MainScene`**, **`Assets/Font`**, **`Assets/Images/card`** 등은 정리·삭제된 상태일 수 있습니다. 빌드 씬은 **`ProjectSettings/EditorBuildSettings.asset`**에서 확인하세요.

## 런타임 플로우 (`TarotGameManager`)

1. 사용자 고민 입력 후 뽑기.
2. 카드 구역 숨김 (`HideCardUiArea`), 안내는 **`_defaultUIText`**에 로딩 문구 (`UpdateDefaultUIText`).
3. **`TarotAIManager.RequestTarotReadingAsync`**로 추론이 **끝날 때까지 대기**.
4. **`HideDefaultUI`** → **`ShowCardUiArea`**(CardUIRoot 활성) → **`TarotCardAnimator.ShowCardAsync`** (LitMotion 슬라이드 + X축 플립).
5. **`UpdateCardNameUI`** 후 **`StreamReadingResultAsync`**로 결과만 타이핑 연출.

`ExtractReadingResult`는 프롬프트 접두 제거, **`[점괘]`** 이후만 사용, `". "` → `".\n"` 가독성 처리.

## 주요 클래스

| 역할 | 파일 |
|------|------|
| 전체 플로우 | `Assets/Tarot/Scripts/TarotGameManager.cs` |
| UI·가시성·스트리밍 | `Assets/Tarot/Scripts/UI/TarotUIManager.cs` |
| LLM 초기화·추론 | `Assets/Tarot/Scripts/Core/TarotAIManager.cs` |
| 시스템/유저 프롬프트 | `Assets/Tarot/Scripts/Core/TarotPromptBuilder.cs` — `[생각]`/`[점괘]`, 점괘는 **부드러운 해요체·귀여운 가이드 톤** |
| 카드 애니메이션 | `Assets/Tarot/Scripts/UI/TarotCardAnimator.cs` |
| 스프라이트 참조 | `Assets/Tarot/Scripts/Data/TarotCardResourceLoader.cs` |
| 덱·DB | `TarotDeckController`, `TarotCardDatabase`, `TarotCardData` |

## UI·인스펙터 연결

- **`TarotUIManager`**: `_gameplayUiRoot`(선택), **`_cardUiRoot` = Hierarchy의 CardUIRoot**, `_defaultUIText`, 입력/버튼, 카드 이름/결과 TMP.
- **`TarotAIManager`**: **`_uiManager` 필수** (엔진 준비 메시지·진행률·초기 UI 숨김).
- **`TarotGameManager`**: AI, UI, `TarotCardAnimator`, `TarotCardResourceLoader`.

엔진 초기화 중: **`ShowEnginePreparingUi`**, 모델 복사/다운로드 진행은 **`UpdateModelSetupProgress(progress)`**로 `_defaultUIText`에 퍼센트 표시.

## 패키지

- **LitMotion**: `Packages/manifest.json` (Git URL).
- NuGet 관련 파일이 `Assets/`에 있을 수 있음 — 팀 정책에 따라 커밋 여부 결정.

## 주의

- `FindObjectOfType` 지양, 참조는 SerializeField·주입 위주.
- 점괘 파싱은 **`[점괘]`** 리터럴과 맞춰야 함 (`TarotGameManager` 상수 `ReadingSectionTag`).
