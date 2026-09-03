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
| **스프레드** `SpreadPastPresentFuture` | `BeginReadingFlowPresentation` → `ShuffleAndDrawThreeCards` → `ShowCardUiArea` → **`WaitForSpreadCenterPlacementAsync`** (가운데 3장 → 과거·현재·미래 순으로 슬롯 배치; 슬롯 홀더 배경 `Image`는 카드가 들어가면 비활성화, `ResetSpreadPickCardsToCenter`·`HideSpreadThreeCardUi`에서 재활성화) → `BuildSpreadThreeCardPrompt` → `RequestTarotReadingAsync` → `ExtractReadingResult` → `BuildSpreadDisplayText` → `HideDefaultUI` / `ShowCardUiArea` / `BringReadingResultAreaToFront` → **`RunSpreadFlipAllAndShowResultAsync`** (세 슬롯 동시 뒤집기 → 본문 한 덩어리를 `StreamReadingResultAsync`로 타이핑 표시) |

- **리셋** `OnResetButtonClicked`: `HideSpreadThreeCardUi` (스프레드 슬롯 이름 TMP 비우기·슬롯 배경 이미지 복구 포함), `ClearConcernInput`, `ResetUI` → 다시 카테고리부터.

### 점괘 후처리 (`ExtractReadingResult`)

1. 응답이 프롬프트로 시작하면 해당 접두 제거  
2. **`[점괘]`** (`ReadingSectionTag`) **마지막** 출현 이후만 사용 (`LastIndexOf`) — 현행 프롬프트는 이 태그를 쓰지 않지만 방어용으로 유지  
3. `StripStandaloneMetaTagLines` — 단독 줄 `[생각]` / `[점괘]` 제거  
4. `StripEchoLabelLines` — 두 종류로 나눠 처리  
   - `EchoLineLabelPrefixes`(`고민`·`분야`·`카드`…): 질문란 에코 → **줄 전체 제거**  
   - `BodyLabelPrefixes`(`답`·`과거`·`현재`·`미래`·`지금`·`조언`·`본문`·`결론`…): 구간 라벨 → **라벨만 벗기고 문장은 유지** (`StripBodyLabel`, 라벨 문자열 완전 일치)  
   - 에코 구분자는 `:`·`：`·`=` (`EchoLabelSeparators`) — 프롬프트가 `고민 = …` 꼴이라 모델이 그대로 되받는 줄도 잡는다  
5. 라벨 제거로 전부 비면 제거 전 텍스트로 되돌림 → 그래도 비면 실패 메시지  
5-1. `DropLeadingConcernRestatement` — 첫 줄이 고민 문장의 되풀이(공백·문장부호 제거 후 포함 관계)면 그 줄만 제거  
6. `SanitizeReadingText`(마크다운 문자·이모지류 Regex 1종) → `". "` → `".\n"`  

**데일리 전용** (`StripPastFutureSectionsForDailyReading`): `[과거]`/`[미래]` 줄, `과거:`/`미래:` 헤더 줄 제거, 본문 맨 앞 `현재:` 접두 제거(4단계에서 대부분 이미 처리됨).

**스프레드**는 구간 분리 없이 세 줄을 한 덩어리로 표시한다(`BuildSpreadDisplayText` → `RunSpreadFlipAllAndShowResultAsync`). 과거의 `TarotSpreadReadingParser`는 삭제됨.

> 2026-09 장애 기록: 시스템 프롬프트에 "'본문:', '조언:', '결론:' 라벨 금지"라고 쓰자 1B 모델이 오히려 그 라벨로만 답했고(12/12), 후처리가 해당 라벨 줄을 통째로 지워 결과가 항상 비었다. 소형 모델에 금지문은 역효과라는 점과, 라벨은 "지우지 말고 벗긴다"는 원칙이 여기서 나왔다. 원문 응답은 `[TarotGameManager] AI 원문 응답` 로그로 남긴다.

## 프롬프트 (`TarotPromptBuilder`)

Gemma 3 4B(Q4_K_M) 기준. 로컬 gguf를 ollama로 띄워 `docs/prompt-bench`로 검증한 형식이다(스프레드 24/24, 데일리 15/16 정상·고민 적중).

소형 모델용 작성 원칙 (이 프로젝트에서 실측으로 확인됨):
- **금지문을 쓰지 않는다.** "본문:/조언: 같은 라벨 금지"라고 쓰면 그 토큰이 오히려 유도된다.
- **원하는 출력을 템플릿 + 예시 1개로 그대로 보여 준다.** 모델은 설명보다 모방을 잘한다.
- **라벨 형식을 허용하고 코드에서 벗긴다.** "라벨 없이 3문장"보다 "과거:/현재:/미래: 세 줄"이 훨씬 안정적.
- **말투는 예시 문장으로 고정한다.** 예시 문장을 전부 '~요'로 끝내면 해요체가 유지된다.
- **분야(카테고리) 설명 줄은 넣지 않는다.** `갈림길, 선택, 결정` 같은 줄이 고민보다 세게 작용해 "점심 뭐 먹을지"에 "중요한 결정" 얘기를 하게 만든다.
- **고민은 유저 메시지 맨 끝에 두고 마지막 줄에서 한 번 더 인용한다.** `'…' 이 고민에 대해 형식대로 …`.
- **첫 줄은 `답:`(고민에 대한 직접 답).** 모델이 고민을 먼저 붙잡아 뒤 줄이 벗어나지 않는다. 되풀이 줄은 `DropLeadingConcernRestatement`가 뺀다.
- **예시에 `고민 = …` 줄을 넣지 않는다.** 모델이 베끼다가 고민을 바꿔 적는다. 예시 제목에 괄호로만 표기.
- **입력 줄은 출력 형식과 다르게 쓴다.** 카드 줄을 `카드1 과거 = 바보 (역방향) 키워드`처럼 `=`로.
- 역방향 해석 규칙은 시스템 프롬프트에 한 줄로 두고, 카드 줄에는 넣지 않는다.

- **시스템** (`GetSystemPrompt(mode)`): 공통 페르소나 2줄(`PersonaLines`) + 모드별 형식 템플릿 + 예시. `RequestTarotReadingAsync`에서 모드별로 매 요청 설정.
- **데일리** `BuildUserPrompt`: `카드 =` / `고민 =` / 인용 리마인드. 모델은 `지금: …`/`조언: …` 두 줄. `category` 인자는 현재 프롬프트에 쓰지 않는다(시그니처 유지).
- **스프레드** `BuildSpreadThreeCardPrompt`: `카드1 과거 =`/`카드2 현재 =`/`카드3 미래 =` / `고민 =` / 인용 리마인드. 모델은 `답:`/`과거:`/`현재:`/`미래:` 네 줄.
- 키워드는 `ShortKeywords`로 앞 3개만 주입.

## LLM (`TarotAIManager`)

| 파라미터 | 값 |
|----------|-----|
| `temperature` | 0.35 |
| `repeatPenalty` | 1.1 |
| `topP` | 0.9 |
| `numPredict` | 데일리 160 / 스프레드 200 (4B 실측 최대 ~125 토큰) |

`RequestTarotReadingAsync`: 모드별 시스템 프롬프트·`numPredict` 설정 → 히스토리 클리어 → `Chat` (세부는 `TarotAIManager.cs`).

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
- 점괘 후처리는 모델이 `답:`/`과거:`/`현재:`/`미래:`(데일리는 `지금:`/`조언:`) 라벨 줄을 낸다는 전제. 프롬프트 형식을 바꾸면 `BodyLabelPrefixes`도 함께 맞출 것. 프롬프트를 고칠 때는 실제 모델로 여러 번 샘플링해 검증할 것(금지문 추가 금지).
- **`ResultReadingScroll`**: `UpdateReadingResultUI`만으로는 스크롤 루트가 켜지지 않음 — **뽑기 성공 시** `SetReadingResultScrollActive(true)`, 리셋/중단 시 끔.
- UI 일부(카테고리·모드·2장)는 **참조가 비어 있으면** 코드에서 동적 생성. **3장 스프레드 UI는** `TarotSpreadThreePanel`이 있는 씬 인스턴스 또는 프리팹을 반드시 연결 — 미연결 시 에러 로그.
- 스프레드 패널 **형제 순서**는 런타임에서 `SetAsFirstSibling` 등으로 바꾸지 않음(씬/프리팹 순서 유지).
- 씬(`.unity`) 대량 자동 수정 전 담당자 확인 권장.
