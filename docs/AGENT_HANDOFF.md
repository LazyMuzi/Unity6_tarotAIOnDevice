# 에이전트 인수인계 — Tarot (Unity)

다음 작업자용 요약입니다. 코딩 규칙·품질 기준은 루트 **`AGENTS.md`** 를 따릅니다.

## 경로·구조

| 항목 | 위치 |
|------|------|
| 타로 스크립트·씬·에셋 | **`Assets/Tarot/`** |
| 빌드에 등록된 씬 | **`Assets/Tarot/Scenes/TodayScene.unity`** (`EditorBuildSettings`) |
| 추가 씬 | `Assets/Tarot/Scenes/` 아래 `Menu.unity` 등은 빌드 목록에 없을 수 있음 — 배포 전 `ProjectSettings/EditorBuildSettings.asset` 확인 |

## 런타임 플로우

### 초기화 (`TarotAIManager`)

1. `ShowEnginePreparingUi` → 모델 준비 중 `UpdateModelSetupProgress`
2. `LLM.WaitUntilModelSetup` → `_llm.WaitUntilReady` → `IsReady = true`
3. `SetGameplayUiVisible(true)` → `ResetUI()` → **`ShowCategorySelectionLayout`** (카테고리 단계부터)

### 사용자 단계 (UI)

1. **카테고리** (`TarotConcernCategory`) → **리딩 종류** 화면  
2. **리딩 종류** (`TarotReadingMode`: 데일리 1장 / 스프레드 3장) → **고민 입력** + 뽑기 버튼  
3. **뽑기** → `TarotGameManager.OnDrawButtonClicked` → `SetReadingResultScrollActive(true)` 후 `ExecuteTarotReadingFlowAsync`

### 분기 (`TarotGameManager.ExecuteTarotReadingFlowAsync`)

| 모드 | 핵심 호출 순서 |
|------|----------------|
| **데일리** `DailySingleCard` | `BeginReadingFlowPresentation` → `ShuffleAndDrawTwoCards` → `WaitForTwoCardSelectionAsync` → `RandomCardOrientation` → `BuildUserPrompt` → `RequestTarotReadingAsync` → `ExtractReadingResult` → `StripPastFutureSectionsForDailyReading` → `BuildReadingDisplayText` → `HideDefaultUI` / `ShowCardUiArea` → `TarotCardAnimator.ShowCardAsync` → `StreamReadingResultAsync` |
| **스프레드** `SpreadPastPresentFuture` | `BeginReadingFlowPresentation` → `ShuffleAndDrawThreeCards` → `ShowCardUiArea` → **`WaitForSpreadCenterPlacementAsync`** (가운데 3장 → 과거·현재·미래 순으로 슬롯 배치; 슬롯 홀더 배경 `Image`는 카드가 들어가면 비활성화, `ResetSpreadPickCardsToCenter`·`HideSpreadThreeCardUi`에서 재활성화) → `BuildSpreadThreeCardPrompt` → `RequestTarotReadingAsync` → `ExtractReadingResult` → `TarotSpreadReadingParser.SplitSections` → `HideDefaultUI` / `ShowCardUiArea` / `BringReadingResultAreaToFront` → **`RunSpreadFlipAllAndShowResultAsync`** (세 슬롯 동시 뒤집기 → `AppendReadingResultText`로 과거/현재/미래 구절 누적, 스트리밍 없음) |

- **리셋** `OnResetButtonClicked`: `HideSpreadThreeCardUi` (스프레드 슬롯 이름 TMP 비우기·슬롯 배경 이미지 복구 포함), `ClearConcernInput`, `ResetUI` → 다시 카테고리부터.

### 점괘 후처리 (`ExtractReadingResult`)

1. 응답이 프롬프트로 시작하면 해당 접두 제거  
2. **`[점괘]`** (`ReadingSectionTag`) **마지막** 출현 이후만 사용 (`LastIndexOf`)  
3. `StripStandaloneMetaTagLines` — 단독 줄 `[생각]` / `[점괘]` 제거  
4. `StripEchoLabelLines` — `고민:`·`분야:` 등 **라벨+콜론** 형태 줄 제거(접두 문자열 배열; 스프레드 본문 보호를 위해 **`의미`는 에코 목록에 없음**)  
5. 비어 있으면 실패 메시지  
6. `SanitizeReadingText`(마크다운 문자·이모지류 Regex 1종) → `". "` → `".\n"`  

**데일리 전용** (`StripPastFutureSectionsForDailyReading`): `[과거]`/`[미래]` 줄, `과거:`/`미래:` 헤더 줄 제거, 본문 맨 앞 `현재:` 접두 제거.

**스프레드 구간** (`TarotSpreadReadingParser.SplitSections`): 각 `[과거]`·`[현재]`·`[미래]`를 **문서 내 순서와 무관하게** 찾아, 해당 태그 직후~다른 태그 직전까지 자름. 전각 `［］`·`【】`는 `[]`로 정규화. 태그로 아무것도 못 잡으면 `\n\n` 문단 3개 fallback. 구간별 `SanitizeSpreadSection`으로 슬롯 메타 접두 제거.

## 프롬프트 (`TarotPromptBuilder`)

소형 온디바이스 모델 기준으로 **짧게** 유지합니다.

- **시스템** (`GetSystemPrompt`): 해요체, `[점괘]` 다음 본문, 질문란 반복 금지, 데일리 vs 스프레드 규칙 한 줄씩 — `TarotAIManager`에서 `LLMAgent.systemPrompt`에 1회 설정.
- **데일리** `BuildUserPrompt`: `분야`/`고민`/`카드`/`의미` 나열 후 **`[점괘]`** 로 끝(예시 블록 없음).
- **스프레드** `BuildSpreadThreeCardPrompt`: `과거`·`현재`·`미래` 줄 + `[점괘]` + 한 줄 답 형식 안내(`[과거]`…`[현재]`…`[미래]`).

## LLM (`TarotAIManager`)

| 파라미터 | 값 |
|----------|-----|
| `temperature` | 0.5 |
| `repeatPenalty` | 1.2 |
| `topP` | 0.95 |

`RequestTarotReadingAsync`: 히스토리 클리어 후 `Chat` (세부는 `TarotAIManager.cs`).

## 주요 클래스

| 역할 | 파일 |
|------|------|
| 전체 플로우·분기·점괘 후처리 | `Assets/Tarot/Scripts/TarotGameManager.cs` |
| UI·카테고리/모드/2장/3장·스트리밍·스크롤 표시 | `Assets/Tarot/Scripts/UI/TarotUIManager.cs` |
| 3장 스프레드 프리팹/씬 패널 참조 바인딩 | `Assets/Tarot/Scripts/UI/TarotSpreadThreePanel.cs` |
| 카드 등장·플립 (LitMotion) | `Assets/Tarot/Scripts/UI/TarotCardAnimator.cs` |
| LLMUnity 초기화·채팅 | `Assets/Tarot/Scripts/Core/TarotAIManager.cs` |
| 시스템/유저 프롬프트 | `Assets/Tarot/Scripts/Core/TarotPromptBuilder.cs` |
| 덱 셔플·2장/3장 뽑기 | `Assets/Tarot/Scripts/Core/TarotDeckController.cs` |
| 스프레드 구절 분리 | `Assets/Tarot/Scripts/Core/TarotSpreadReadingParser.cs` |
| 카드 스프라이트 | `Assets/Tarot/Scripts/Data/TarotCardResourceLoader.cs` |
| 카드 DB·데이터 | `Assets/Tarot/Scripts/Data/TarotCardDatabase.cs`, `TarotCardData.cs` |
| 고민 분야·리딩 모드·방향 | `TarotConcernCategory.cs`, `TarotReadingMode.cs`, `TarotCardOrientation.cs` |

## 인스펙터 연결 (`TarotUIManager`)

**필수에 가까운 항목**

- `_defaultUIText`, `_inputUiRoot`, `_concernInputField`, `_drawCardButton`, `_resetButton`, `_cardUiRoot`, `_cardNameText`, `_readingResultText`

**선택**

- `_gameplayUiRoot`, 점괘 영역 `_readingResultAreaRoot`, `_readingResultScrollRect`, `_readingResultScrollContent`
- **씬 제작 카테고리/모드 패널**: `_categorySelectionPanelRoot`, `_readingModeSelectionPanelRoot` — 지정 시 해당 루트를 쓰고 **런타임으로 패널을 만들지 않음**. 버튼은 `SubmitCategoryByEnumIndex(0~5)`, `SubmitReadingModeByEnumIndex(0=데일리, 1=스프레드)` 또는 `SubmitCategory` / `SubmitReadingMode` 로 연결.
- **런타임 생성 시 부모(카테고리/모드/2장만)**: `_categoryPanelParentOverride`, `_readingModePanelParentOverride`, `_twoCardChoiceParentOverride`
- **3장 스프레드 (필수)**: `_spreadThreeSceneInstance` **또는** `_spreadThreePanelPrefab` 중 하나 — 루트에 **`TarotSpreadThreePanel`** 컴포넌트, 인스펙터에서 가운데 카드×3·**슬롯 홀더**×3·이름 TMP×3 등 연결. **슬롯 배경을 끄려면** 각 `SlotCardHolder` **루트**에 `Image`가 있어야 함(`GetComponent<Image>`). 씬 인스턴스를 쓰면 프리팹 Instantiate 없음. 프리팹만 쓸 때 부모는 `_spreadThreeParentOverride` (비우면 `GameplayUiRoot` 또는 `TarotUIManager` 트랜스폼).
- **카드 크기**: `_spreadSlotPreferredSize` — 가운데·슬롯에 옮긴 뒤에도 동일 `sizeDelta`로 맞춤. 스프레드 **루트** 위치·크기는 코드에서 바꾸지 않음 — 씬/프리팹에서 배치.

**다른 매니저**

- `TarotAIManager`: `_uiManager`, `_llm`, `_llmAgent`
- `TarotGameManager`: `_aiManager`, `_uiManager`, `_cardAnimator`, `_resourceLoader`

## 패키지·빌드

- **LLMUnity** (`ai.undream.llm`)
- **LitMotion** (+ 선택 `LitMotion.Animation`)
- **TextMeshPro** (UI)
- **URP** (`com.unity.render-pipelines.universal`)
- **NuGetForUnity** — 레포에 포함(직접 쓰는 스크립트는 타로 코어와 무관할 수 있음)

세부는 `Packages/manifest.json` 기준.

## 주의

- **`FindObjectOfType`** 지양 — `SerializeField` 또는 주입.
- 점괘 추출은 모델이 **`[점괘]`** 를 출력한다는 전제; 스프레드는 **`[과거]`/`[현재]`/`[미래]`** 출력과 `TarotSpreadReadingParser`·프롬프트를 맞출 것.
- **`ResultReadingScroll`**: `UpdateReadingResultUI`만으로는 스크롤 루트가 켜지지 않음 — **뽑기 성공 시** `SetReadingResultScrollActive(true)`, 리셋/중단 시 끔.
- UI 일부(카테고리·모드·2장)는 **참조가 비어 있으면** 코드에서 동적 생성. **3장 스프레드 UI는** `TarotSpreadThreePanel`이 있는 씬 인스턴스 또는 프리팹을 반드시 연결 — 미연결 시 에러 로그.
- 스프레드 패널 **형제 순서**는 런타임에서 `SetAsFirstSibling` 등으로 바꾸지 않음(씬/프리팹 순서 유지).
- 씬(`.unity`) 대량 자동 수정 전 담당자 확인 권장.
