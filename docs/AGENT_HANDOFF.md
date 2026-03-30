# 에이전트 인수인계 — Tarot (Unity)

다음 작업자용 요약입니다. 코딩 규칙·품질 기준은 루트 **`AGENTS.md`** 를 따릅니다.

## 경로·구조

| 항목 | 위치 |
|------|------|
| 타로 스크립트·씬·에셋 | **`Assets/Tarot/`** |
| 메인 씬 | **`Assets/Tarot/Scenes/TodayScene.unity`** |
| 빌드 씬 등록 | **`ProjectSettings/EditorBuildSettings.asset`** → 위 씬 |

레거시 `Assets/Scripts/`, `Assets/Scenes/` 등은 이 브랜치에서는 제거된 상태일 수 있습니다. 경로는 실제 `Assets/Tarot/` 와 `EditorBuildSettings` 로 확인하세요.

## 런타임 플로우 (`TarotGameManager`)

1. **뽑기 버튼** `OnDrawButtonClicked`  
   - `IsReady` false → 결과 TMP에 안내 (`UpdateReadingResultUI`).  
   - 고민 공백 → `_defaultUIText`에 안내 (`UpdateDefaultUIText`).  
   - 통과 시: `SetDrawButtonInteractable(false)`, `SetResetButtonInteractable(false)`, **`HideInputUI`**, `ExecuteTarotReadingFlowAsync(userConcern)`.
2. **`ExecuteTarotReadingFlowAsync`**  
   - `HideCardUiArea`, 카드 애니 `HideCard`, 결과 TMP 비움, `_defaultUIText`에 진행 안내.  
   - `TarotDeckController.DrawRandomCard` → `TarotPromptBuilder.BuildUserPrompt` → **`TarotAIManager.RequestTarotReadingAsync`** (완료까지 await).  
   - **`ExtractReadingResult`**: 유저 프롬프트 접두 제거 → **`[점괘]`** (`ReadingSectionTag`) 뒤만 사용 → 빈 값이면 파싱 실패 메시지 → `". "` → `".\n"`.  
   - `HideDefaultUI` → `ShowCardUiArea` → `TarotCardAnimator.ShowCardAsync` → `UpdateCardNameUI(NameKr, NameEn)` → `StreamReadingResultAsync`.  
   - 끝나면 **`SetResetButtonInteractable(true)`**, 뽑기 버튼 다시 활성.
3. **리셋 버튼** `OnResetButtonClicked`  
   - 처리 중이면 무시. `HideCard`, **`ClearConcernInput`**, **`ResetUI`** (스트리밍 취소, 입력 영역·기본 문구·카드 UI 복구 등).

**엔진 초기화** (`TarotAIManager`): `ShowEnginePreparingUi` → 모델/setup 대기 시 `UpdateModelSetupProgress` → 준비 후 `SetGameplayUiVisible(true)` + `ResetUI`.

## 주요 클래스

| 역할 | 파일 |
|------|------|
| 전체 플로우 | `Assets/Tarot/Scripts/TarotGameManager.cs` |
| UI·가시성·스트리밍·ScrollRect 연동 | `Assets/Tarot/Scripts/UI/TarotUIManager.cs` |
| LLMUnity 초기화·채팅 요청 | `Assets/Tarot/Scripts/Core/TarotAIManager.cs` |
| 시스템/유저 프롬프트 | `Assets/Tarot/Scripts/Core/TarotPromptBuilder.cs` — `[생각]` · `[점괘]`, 해요체 점괘, 카드 비인격 표현, **한국어 전용** 규칙 및 유저 프롬프트 하단 `[출력 형식 주의]` |
| 카드 애니 (LitMotion) | `Assets/Tarot/Scripts/UI/TarotCardAnimator.cs` |
| 카드 스프라이트 로드 | `Assets/Tarot/Scripts/Data/TarotCardResourceLoader.cs` |
| 덱 | `Assets/Tarot/Scripts/Core/TarotDeckController.cs` |
| 카드 데이터 | `Assets/Tarot/Scripts/Data/TarotCardDatabase.cs`, `TarotCardData.cs` |

## UI 인스펙터 연결 (`TarotUIManager`)

에디터에서 직렬화 필드 연결이 필요합니다. 누락 시 런타임에서 해당 기능만 스킵되거나 동작이 어색해질 수 있습니다.

- **`_gameplayUiRoot`** (선택): AI 준비 전 게임플레이 루트.  
- **`_defaultUIText`**, **`_inputUiRoot`**, **`_concernInputField`**, **`_drawCardButton`**, **`_resetButton`**.  
- **`_cardUiRoot`**, **`_cardNameText`**, **`_readingResultText`**.  
- 긴 점괘 스크롤(선택): **`_readingResultAreaRoot`**, **`_readingResultScrollRect`**, **`_readingResultScrollContent`**.  
- **`TarotAIManager`**: **`_uiManager`**, `_llm`, `_llmAgent`.  
- **`TarotGameManager`**: `_aiManager`, `_uiManager`, `_cardAnimator`, `_resourceLoader`.

`UpdateCardNameUI`는 UI에 `이름Kr (이름En)` 형식으로 표시합니다. 영문 표기를 빼려면 이 메서드/씬 바인딩을 조정하면 됩니다.

## 패키지·빌드 (요약)

- **LLMUnity**: `ai.undream.llm` (Git, `Packages/manifest.json`).  
- **LitMotion** (+ Animation 옵션): Git URL.  
- **NuGet For Unity**: `com.github-glitchenzo.nugetforunity`; **`Assets/Packages/`**, `Assets/NuGet.config`, `packages.config` 등이 R3 등 의존성과 함께 존재할 수 있음.  
- **URP**: `com.unity.render-pipelines.universal`.

대용량 모델은 **`Assets/StreamingAssets`** 쪽에 둘 수 있으므로, `.gitignore`·커밋 범위를 확인하세요.

## 주의

- **`FindObjectOfType`** 지양. 참조는 `SerializeField`/주입.  
- 점괘 추출은 모델 출력에 **`[점괘]`** 태그가 있어야 `ExtractReadingResult`와 맞습니다. 태그·프롬프트 변경 시 같이 맞출 것.  
- **`TodayScene.unity` 등 씬 에셋을 자동으로 고치기 전**, 저장소 담당자 동의(또는 직접 편집) 여부를 확인하는 것이 안전합니다.
