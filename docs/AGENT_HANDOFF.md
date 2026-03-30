# 에이전트 인수인계 — Tarot (Unity)

다음 작업자용 요약입니다. 코딩 규칙·품질 기준은 루트 **`AGENTS.md`** 를 따릅니다.

## 경로·구조

| 항목 | 위치 |
|------|------|
| 타로 스크립트·씬·에셋 | **`Assets/Tarot/`** |
| 메인 씬 | **`Assets/Tarot/Scenes/TodayScene.unity`** |
| 빌드 씬 등록 | **`ProjectSettings/EditorBuildSettings.asset`** → 위 씬 |

레거시 `Assets/Scripts/`, `Assets/Scenes/` 등은 제거 상태입니다. 경로는 `Assets/Tarot/` 와 `EditorBuildSettings`로 확인하세요.

## 런타임 플로우 (`TarotGameManager`)

1. **뽑기 버튼** `OnDrawButtonClicked`
   - `IsReady` false → 결과 TMP에 안내.
   - 고민 공백 → `_defaultUIText`에 안내.
   - 통과 시: 뽑기·리셋 버튼 비활성, **`HideInputUI`**, `ExecuteTarotReadingFlowAsync`.
2. **`ExecuteTarotReadingFlowAsync`**
   - `HideCardUiArea`, `HideCard`, 결과 TMP 비움, 진행 안내.
   - `DrawRandomCard` → `BuildUserPrompt` → **`RequestTarotReadingAsync`** (완료 await).
   - **`ExtractReadingResult`**: 프롬프트 접두 제거 → **`[점괘]`** (`ReadingSectionTag`) 뒤만 사용 → **`SanitizeReadingText`**(후처리) → `". "` → `".\n"`.
   - `HideDefaultUI` → `ShowCardUiArea` → `ShowCardAsync` → `UpdateCardNameUI` → `StreamReadingResultAsync`.
   - 끝나면 **`SetResetButtonInteractable(true)`**, 뽑기 버튼 다시 활성.
3. **리셋 버튼** `OnResetButtonClicked`
   - 처리 중이면 무시. `HideCard`, **`ClearConcernInput`**, **`ResetUI`**.

**엔진 초기화** (`TarotAIManager`): `ShowEnginePreparingUi` → `UpdateModelSetupProgress` → 준비 후 `SetGameplayUiVisible(true)` + `ResetUI`.

## 후처리 (`SanitizeReadingText`)

소형 로컬 모델은 프롬프트 규칙을 무시하고 마크다운·이모지·특수문자를 넣을 수 있습니다. `ExtractReadingResult` 안에서 **`SanitizeReadingText`**가 점괘 텍스트를 정리합니다.

| 대상 | 처리 |
|------|------|
| `*`, `#`, `_`, `~`, `` ` `` | `string.Replace`로 제거 (마크다운) |
| 이모지·기호·포맷 문자 | `SymbolAndEmojiPattern` Regex (`[\p{Cs}\p{So}\p{Cf}]`) |
| 연속 공백 | 단일 공백으로 축소 |

- `\p{Cs}` — 서로게이트 쌍(U+10000 이상 이모지: 😀🌙💖 등).
- `\p{So}` — BMP 기호 문자(☀✦♠☆□■ 등).
- `\p{Cf}` — ZWJ·Variation Selector 등 보이지 않는 포맷 문자.

> **주의**: 기존 `\U0001F000` 리터럴 이스케이프 방식은 Unity Mono에서 `ArgumentException`을 발생시킵니다. 유니코드 **카테고리 패턴**(`\p{...}`)은 Mono에서 정상 동작합니다.

새로운 특수문자가 빠져나오면 `SanitizeReadingText`의 `Replace` 체인에 한 줄 추가하면 됩니다.

## 프롬프트 (`TarotPromptBuilder`)

소형 로컬 모델용으로 최적화된 구조입니다.

- **시스템 프롬프트** — 4줄 핵심 지시만 포함: 역할 부여, `[생각]`/`[점괘]` 구조, "카드 이름을 꼭 언급하며 고민에 맞는 조언", 해요체, 한국어 전용, 이모지 금지.
- **유저 프롬프트** — **인라인 few-shot 예시**를 먼저 보여준 뒤 `[실제 질문]` 섹션에 고민·카드·키워드·의미를 배치하고 `[생각]`으로 생성을 유도.
- 카드 정보는 **한글 이름만** 유저 프롬프트에 포함(`NameEn` 제외).

**설계 의도**: 소형 모델은 긴 규칙 나열보다 예시를 훨씬 잘 모방합니다. 규칙 9개를 4줄로 압축하고, 예시를 생성 직전에 배치하여 형식 추종률을 높였습니다.

## LLM 파라미터 (`TarotAIManager`)

| 파라미터 | 값 | 비고 |
|----------|------|------|
| `temperature` | 0.5 | 말투 안정화 (0.7에서 하향) |
| `repeatPenalty` | 1.2 | 반복 방지 |
| `topP` | 0.95 | 기본값 유지 |

매 요청마다 `ClearHistory`를 호출하여 이전 대화 문맥이 간섭하지 않도록 합니다.

## 주요 클래스

| 역할 | 파일 |
|------|------|
| 전체 플로우·후처리 | `Assets/Tarot/Scripts/TarotGameManager.cs` |
| UI·가시성·스트리밍·ScrollRect | `Assets/Tarot/Scripts/UI/TarotUIManager.cs` |
| LLMUnity 초기화·채팅 | `Assets/Tarot/Scripts/Core/TarotAIManager.cs` |
| 시스템/유저 프롬프트 | `Assets/Tarot/Scripts/Core/TarotPromptBuilder.cs` |
| 카드 애니 (LitMotion) | `Assets/Tarot/Scripts/UI/TarotCardAnimator.cs` |
| 카드 스프라이트 로드 | `Assets/Tarot/Scripts/Data/TarotCardResourceLoader.cs` |
| 덱 | `Assets/Tarot/Scripts/Core/TarotDeckController.cs` |
| 카드 데이터 | `Assets/Tarot/Scripts/Data/TarotCardDatabase.cs`, `TarotCardData.cs` |

## UI 인스펙터 연결

에디터에서 직렬화 필드 연결이 필요합니다. 누락 시 해당 기능만 스킵됩니다.

- **`TarotUIManager`**: `_gameplayUiRoot`(선택), `_defaultUIText`, `_inputUiRoot`, `_concernInputField`, `_drawCardButton`, `_resetButton`, `_cardUiRoot`, `_cardNameText`, `_readingResultText`, 스크롤(선택): `_readingResultAreaRoot`, `_readingResultScrollRect`, `_readingResultScrollContent`.
- **`TarotAIManager`**: `_uiManager`, `_llm`, `_llmAgent`.
- **`TarotGameManager`**: `_aiManager`, `_uiManager`, `_cardAnimator`, `_resourceLoader`.

`UpdateCardNameUI`는 `선택된 카드:\n이름Kr (이름En)` 형식으로 표시합니다. 영문 표기를 빼려면 이 메서드를 수정하세요.

## 패키지·빌드

- **LLMUnity**: `ai.undream.llm` (Git).
- **LitMotion** (+ Animation): Git URL.
- **NuGet For Unity**: `com.github-glitchenzo.nugetforunity`; `Assets/Packages/`, `packages.config` 등.
- **URP**: `com.unity.render-pipelines.universal`.
- `.gitignore`에 `LLMUnityBuild/`, `Assets/Plugins/Android/LLMUnity/`, `Assets/StreamingAssets/LLMManager.json`, `*.gguf` 등이 제외되어 있음.

## 주의

- **`FindObjectOfType`** 지양. 참조는 `SerializeField`/주입.
- 점괘 추출은 **`[점괘]`** 태그가 모델 출력에 있어야 `ExtractReadingResult`와 맞습니다. 태그·프롬프트 변경 시 같이 맞출 것.
- 이모지 후처리는 유니코드 **카테고리 패턴**(`\p{Cs}\p{So}\p{Cf}`)을 사용합니다. **`\U` 리터럴 이스케이프는 Unity Mono에서 크래시**하므로 사용 금지.
- **씬 에셋(`.unity`) 자동 수정 전** 담당자 확인 필요.
