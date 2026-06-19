using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using LitMotion;
using LitMotion.Extensions;
using Tarot.Data;

namespace Tarot.UI
{
    /// <summary>
    /// 타로 게임의 메인 UI 요소들을 관리하는 스크립트입니다.
    /// </summary>
    public class TarotUIManager : MonoBehaviour
    {
        private const string EnginePreparingMessage =
            "타로를 보기 위해 준비 중이에요.\n잠시만 기다려 주세요.";

        private const string ModelSetupProgressFormat =
            "타로를 보기 위해 준비 중이에요.\n진행: {0}%";

        private const string CategoryStepWelcomeText =
            "어떤 고민이 있으신가요?";

        private const string ReadingModeStepWelcomeText =
            "어떤 방법으로 볼까요?";

        private const string FallbackWelcomeDefaultText =
            "당신의 고민을 입력하고, 카드를 뽑아 보세요.";

        [Header("Visibility")]
        [Tooltip("AI 엔진 초기화 전까지 숨길 게임플레이 UI의 루트(입력·버튼·안내 등). 비우면 입력·버튼만 비활성화합니다.")]
        [SerializeField] private GameObject _gameplayUiRoot;

        [Header("Default UI")]
        [Tooltip("기본 UI를 표시할 텍스트")]
        [SerializeField] private TextMeshProUGUI _defaultUIText;

        [Header("Category UI")]
        [Tooltip("씬에서 만든 카테고리 선택 UI 루트. 지정하면 런타임에 패널을 만들지 않습니다. 버튼 OnClick → SubmitCategoryByEnumIndex(0~5).")]
        [SerializeField] private GameObject _categorySelectionPanelRoot;

        [Tooltip("카테고리 패널을 런타임 생성할 때만 부모로 씁니다. 위에 씬 패널을 넣은 경우 비워도 됩니다.")]
        [SerializeField] private RectTransform _categoryPanelParentOverride;

        [Header("Two-card choice")]
        [Tooltip("2장 중 1장 선택 UI의 부모. 비우면 GamePlayUIRoot 아래에 패널을 만듭니다.")]
        [SerializeField] private RectTransform _twoCardChoiceParentOverride;

        [Tooltip("선택용 카드 슬롯 가로·세로(레이아웃).")]
        [SerializeField] private Vector2 _twoCardChoiceSlotPreferredSize = new Vector2(360f, 540f);

        [Tooltip("슬롯 최소 가로·세로.")]
        [SerializeField] private Vector2 _twoCardChoiceSlotMinSize = new Vector2(280f, 420f);

        [Header("Reading mode")]
        [Tooltip("씬에서 만든 리딩 모드 선택 UI 루트. 지정하면 런타임 생성하지 않습니다. 버튼 OnClick → SubmitReadingModeByEnumIndex(0=데일리, 1=스프레드).")]
        [SerializeField] private GameObject _readingModeSelectionPanelRoot;

        [Tooltip("리딩 모드 패널을 런타임 생성할 때만 부모로 씁니다. 씬 패널을 쓰는 경우 비워도 됩니다.")]
        [SerializeField] private RectTransform _readingModePanelParentOverride;

        [Header("Spread 3-card")]
        [Tooltip("씬에 미리 둔 스프레드 패널(루트에 TarotSpreadThreePanel). 지정 시 프리팹을 만들지 않고 이 오브젝트를 사용합니다.")]
        [SerializeField] private TarotSpreadThreePanel _spreadThreeSceneInstance;

        [Tooltip("SpreadThreeRoot 프리팹 — 루트에 TarotSpreadThreePanel, 인스펙터에서 슬롯·카드 참조 연결. _spreadThreeSceneInstance가 비어 있을 때만 Instantiate 합니다.")]
        [SerializeField] private GameObject _spreadThreePanelPrefab;

        [Tooltip("프리팹을 Instantiate 할 때의 부모. 비우면 GamePlayUIRoot 또는 이 컴포넌트 트랜스폼.")]
        [SerializeField] private RectTransform _spreadThreeParentOverride;

        private const float SpreadColumnNameUnderPreferredHeight = 56f;

        [SerializeField] private Vector2 _spreadSlotPreferredSize = new Vector2(339.2f, 505.6f);

        [Header("Input UI")]
        [Tooltip("사용자가 고민을 입력하는 필드")]
        [SerializeField] private GameObject _inputUiRoot;
        [SerializeField] private TMP_InputField _concernInputField;
        
        [Tooltip("타로 카드 뽑기를 실행하는 버튼")]
        [SerializeField] private Button _drawCardButton;

        [Tooltip("점괘 스트리밍이 끝난 뒤 입력 화면으로 돌아갈 리셋 버튼")]
        [SerializeField] private Button _resetButton;

        [Header("Card UI")]
        [Tooltip("CardNameText·CardDisplay 등 카드 관련 UI의 부모(CardUIRoot). 지정 시 이 오브젝트로 표시/숨김을 묶습니다.")]
        [SerializeField] private GameObject _cardUiRoot;

        [Header("Result UI")]
        [Tooltip("뽑힌 카드의 이름을 표시할 텍스트")]
        [SerializeField] private TextMeshProUGUI _cardNameText;
        
        [Tooltip("AI가 생성한 타로 리딩 결과를 스트리밍하여 보여줄 텍스트")]
        [SerializeField] private TextMeshProUGUI _readingResultText;

        [Tooltip("점괘 영역 전체(ScrollRect 루트). 비우면 ReadingResultText 오브젝트만 켜고 끕니다.")]
        [SerializeField] private GameObject _readingResultAreaRoot;

        [Tooltip("점괘 ScrollRect(선택). 긴 글 스크롤과 레이아웃 갱신에 사용합니다.")]
        [SerializeField] private ScrollRect _readingResultScrollRect;

        [Tooltip("ScrollRect의 Content RectTransform(선택). 텍스트 높이 재계산에 사용합니다.")]
        [SerializeField] private RectTransform _readingResultScrollContent;

        [Header("Streaming Settings")]
        [Tooltip("텍스트 스트리밍 시 글자당 딜레이(초)")]
        [SerializeField] private float _charDelay = 0.03f;

        private const int StreamingLayoutRefreshCharInterval = 16;

        private const float SpreadCardMoveDuration = 0.38f;
        private const string SpreadPickPastMessage = "과거 자리에 놓을 카드를 골라 주세요.";
        private const string SpreadPickPresentMessage = "현재 자리에 놓을 카드를 골라 주세요.";
        private const string SpreadPickFutureMessage = "미래 자리에 놓을 카드를 골라 주세요.";

        private CancellationTokenSource _streamingCts;
        private string _cachedWelcomeDefaultText;
        private GameObject _categoryPanelRoot;
        private TarotConcernCategory? _selectedCategory;

        private GameObject _readingModeRoot;
        private TarotReadingMode? _selectedReadingMode;

        private GameObject _spreadThreeRoot;
        private GameObject _spreadCenterRow;
        private RectTransform[] _spreadCenterPickColumnRoots;
        private Image[] _spreadCenterPickImages;
        private RectTransform[] _spreadCenterPickRects;
        private Button[] _spreadCenterPickButtons;
        private RectTransform[] _spreadSlotCardHolders;
        private Image[] _spreadThreeImages;
        private RectTransform[] _spreadThreeImageRects;
        private TextMeshProUGUI[] _spreadCardNameUnderTexts;
        private TaskCompletionSource<int> _spreadCenterClickTcs;

        // 컬럼 루트(부채꼴)의 원본 위치/회전. 카드가 빠지면 남은 카드를 가운데로 재배치하고, 리셋 때 복원합니다.
        private Vector2[] _spreadFanColPos;
        private float[] _spreadFanColRotZ;
        private const float SpreadRefanDuration = 0.22f;
        private const float SpreadFanOutDuration = 0.42f;
        private const float SpreadFanOutStagger = 0.05f;
        private const float SpreadFanOutArcFactor = 0.18f;

        private GameObject _twoCardChoiceRoot;
        private Button _twoCardChoiceButtonLeft;
        private Button _twoCardChoiceButtonRight;
        private Image _twoCardChoiceImageLeft;
        private Image _twoCardChoiceImageRight;
        private TaskCompletionSource<TarotCardData> _twoCardChoiceTcs;
        private TarotCardData _twoCardChoiceLeftData;
        private TarotCardData _twoCardChoiceRightData;

        /// <summary>
        /// 사용자가 고른 고민 분야를 반환합니다. 선택하지 않았으면 false입니다.
        /// </summary>
        public bool TryGetSelectedCategory(out TarotConcernCategory category)
        {
            if (_selectedCategory.HasValue)
            {
                category = _selectedCategory.Value;
                return true;
            }

            category = TarotConcernCategory.Other;
            return false;
        }

        /// <summary>
        /// 사용자가 고른 리딩 종류(데일리 / 3장 스프레드)를 반환합니다.
        /// </summary>
        public bool TryGetSelectedReadingMode(out TarotReadingMode mode)
        {
            if (_selectedReadingMode.HasValue)
            {
                mode = _selectedReadingMode.Value;
                return true;
            }

            mode = TarotReadingMode.DailySingleCard;
            return false;
        }

        /// <summary>
        /// 씬 UI 버튼용: <see cref="TarotConcernCategory"/> 선언 순서 인덱스(0=연애·관계 … 5=기타).
        /// </summary>
        public void SubmitCategoryByEnumIndex(int index)
        {
            var order = (TarotConcernCategory[])Enum.GetValues(typeof(TarotConcernCategory));
            if (index < 0 || index >= order.Length)
                return;
            OnCategoryChosen(order[index]);
        }

        /// <summary>
        /// 씬 UI 버튼용: 0=데일리(1장), 1=과거·현재·미래 스프레드.
        /// </summary>
        public void SubmitReadingModeByEnumIndex(int index)
        {
            var order = (TarotReadingMode[])Enum.GetValues(typeof(TarotReadingMode));
            if (index < 0 || index >= order.Length)
                return;
            OnReadingModeChosen(order[index]);
        }

        /// <summary>씬 UI 버튼에서 enum을 직접 넘길 수 있을 때(스크립트·커스텀 에디터).</summary>
        public void SubmitCategory(TarotConcernCategory category) => OnCategoryChosen(category);

        /// <summary>씬 UI 버튼에서 enum을 직접 넘길 수 있을 때.</summary>
        public void SubmitReadingMode(TarotReadingMode mode) => OnReadingModeChosen(mode);

        private void Awake()
        {
            if (_defaultUIText != null)
                _cachedWelcomeDefaultText = _defaultUIText.text;
        }

        /// <summary>
        /// AI 엔진 로딩 중: 안내 텍스트만 준비 메시지로 표시하고 입력·뽑기 등은 숨깁니다.
        /// </summary>
        public void ShowEnginePreparingUi()
        {
            if (_gameplayUiRoot != null)
                _gameplayUiRoot.SetActive(true);

            HideCategoryUi();
            HideReadingModeUi();
            HideTwoCardChoiceUi();
            HideSpreadThreeCardUi();
            SetConcernAndDrawVisible(false);
            SetReadingAreaVisible(false);
            SetResetButtonInteractable(false);
            HideCardUiArea();
            UpdateDefaultUIText(EnginePreparingMessage);
        }

        /// <summary>
        /// 입력·버튼·게임플레이 패널을 표시하거나 숨깁니다. AI 초기화 대기 중에는 숨깁니다.
        /// </summary>
        public void SetGameplayUiVisible(bool visible)
        {
            if (_gameplayUiRoot != null)
            {
                _gameplayUiRoot.SetActive(visible);
                return;
            }

            SetCoreGameplayWidgetsVisible(visible);
            if (_defaultUIText != null)
                _defaultUIText.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 입력·뽑기·결과 텍스트만 토글합니다. 카드 이름은 카드 등장 시에만 표시합니다.
        /// </summary>
        private void SetConcernAndDrawVisible(bool visible)
        {
            if (_concernInputField != null)
                _concernInputField.gameObject.SetActive(visible);
            if (_drawCardButton != null)
                _drawCardButton.gameObject.SetActive(visible);
        }

        private void SetReadingAreaVisible(bool visible)
        {
            GameObject readingArea = _readingResultAreaRoot != null
                ? _readingResultAreaRoot
                : _readingResultText != null ? _readingResultText.gameObject : null;
            if (readingArea != null)
                readingArea.SetActive(visible);
        }

        private void SetCoreGameplayWidgetsVisible(bool visible)
        {
            SetConcernAndDrawVisible(visible);
            SetReadingAreaVisible(visible);
        }

        private void EnsureCategoryPanelBuilt()
        {
            if (_categoryPanelRoot != null)
                return;

            if (_categorySelectionPanelRoot != null)
            {
                _categoryPanelRoot = _categorySelectionPanelRoot;
                return;
            }

            Transform parent = _categoryPanelParentOverride != null
                ? _categoryPanelParentOverride
                : _gameplayUiRoot != null ? _gameplayUiRoot.transform : transform;

            _categoryPanelRoot = new GameObject("CategoryUIRoot", typeof(RectTransform));
            RectTransform rootRt = _categoryPanelRoot.GetComponent<RectTransform>();
            rootRt.SetParent(parent, false);
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = new Vector2(48f, 280f);
            rootRt.offsetMax = new Vector2(-48f, -280f);

            var vlg = _categoryPanelRoot.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            var categories = (TarotConcernCategory[])Enum.GetValues(typeof(TarotConcernCategory));
            TMP_FontAsset font = _defaultUIText != null ? _defaultUIText.font : null;

            foreach (TarotConcernCategory cat in categories)
            {
                CreateCategoryButtonRow(cat, rootRt, font);
            }

            rootRt.SetAsLastSibling();
        }

        private void CreateCategoryButtonRow(TarotConcernCategory category, RectTransform parent, TMP_FontAsset font)
        {
            var row = new GameObject("Category_" + category, typeof(RectTransform));
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.SetParent(parent, false);

            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 68f;
            le.preferredHeight = 68f;
            le.flexibleWidth = 1f;

            var img = row.AddComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.22f, 0.92f);
            img.raycastTarget = true;

            var btn = row.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.2f, 0.24f, 0.34f, 1f);
            colors.pressedColor = new Color(0.28f, 0.32f, 0.44f, 1f);
            btn.colors = colors;

            TarotConcernCategory captured = category;
            btn.onClick.AddListener(() => OnCategoryChosen(captured));

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(rowRt, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(20f, 8f);
            labelRt.offsetMax = new Vector2(-20f, -8f);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = TarotConcernCategoryLabels.GetShortLabelKr(category);
            tmp.fontSize = 34f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            if (font != null)
                tmp.font = font;
        }

        private void OnCategoryChosen(TarotConcernCategory category)
        {
            _selectedCategory = category;
            HideCategoryUi();
            ShowReadingModeSelectionLayout();
        }

        private void HideCategoryUi()
        {
            if (_categoryPanelRoot != null)
                _categoryPanelRoot.SetActive(false);
        }

        private void ShowCategorySelectionLayout()
        {
            EnsureCategoryPanelBuilt();
            _selectedCategory = null;
            _selectedReadingMode = null;
            HideInputUI();
            HideReadingModeUi();
            HideTwoCardChoiceUi();
            HideSpreadThreeCardUi();
            SetConcernAndDrawVisible(false);
            SetReadingAreaVisible(true);
            if (_categoryPanelRoot != null)
                _categoryPanelRoot.SetActive(true);
            UpdateDefaultUIText(CategoryStepWelcomeText);
        }

        private void ShowReadingModeSelectionLayout()
        {
            EnsureReadingModePanelBuilt();
            HideInputUI();
            SetConcernAndDrawVisible(false);
            HideTwoCardChoiceUi();
            HideSpreadThreeCardUi();
            SetReadingAreaVisible(true);
            if (_readingModeRoot != null)
                _readingModeRoot.SetActive(true);
            UpdateDefaultUIText(ReadingModeStepWelcomeText);
        }

        private void EnsureReadingModePanelBuilt()
        {
            if (_readingModeRoot != null)
                return;

            if (_readingModeSelectionPanelRoot != null)
            {
                _readingModeRoot = _readingModeSelectionPanelRoot;
                return;
            }

            Transform parent = _readingModePanelParentOverride != null
                ? _readingModePanelParentOverride
                : _gameplayUiRoot != null ? _gameplayUiRoot.transform : transform;

            _readingModeRoot = new GameObject("ReadingModeUIRoot", typeof(RectTransform));
            RectTransform rootRt = _readingModeRoot.GetComponent<RectTransform>();
            rootRt.SetParent(parent, false);
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = new Vector2(48f, 280f);
            rootRt.offsetMax = new Vector2(-48f, -280f);

            var vlg = _readingModeRoot.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            TMP_FontAsset font = _defaultUIText != null ? _defaultUIText.font : null;
            var modes = (TarotReadingMode[])Enum.GetValues(typeof(TarotReadingMode));
            foreach (TarotReadingMode mode in modes)
            {
                CreateReadingModeButtonRow(mode, rootRt, font);
            }

            rootRt.SetAsLastSibling();
        }

        private void CreateReadingModeButtonRow(TarotReadingMode mode, RectTransform parent, TMP_FontAsset font)
        {
            var row = new GameObject("ReadingMode_" + mode, typeof(RectTransform));
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.SetParent(parent, false);

            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 72f;
            le.preferredHeight = 72f;
            le.flexibleWidth = 1f;

            var img = row.AddComponent<Image>();
            img.color = new Color(0.14f, 0.18f, 0.28f, 0.94f);
            img.raycastTarget = true;

            var btn = row.AddComponent<Button>();
            btn.targetGraphic = img;

            TarotReadingMode captured = mode;
            btn.onClick.AddListener(() => OnReadingModeChosen(captured));

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(rowRt, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(16f, 8f);
            labelRt.offsetMax = new Vector2(-16f, -8f);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = TarotReadingModeLabels.GetButtonLabelKr(mode);
            tmp.fontSize = 30f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            if (font != null)
                tmp.font = font;
        }

        private void OnReadingModeChosen(TarotReadingMode mode)
        {
            _selectedReadingMode = mode;
            HideReadingModeUi();
            ShowInputUI();
            SetConcernAndDrawVisible(true);
            SetReadingAreaVisible(true);

            if (_defaultUIText != null)
            {
                _defaultUIText.gameObject.SetActive(true);
                _defaultUIText.text = string.IsNullOrEmpty(_cachedWelcomeDefaultText)
                    ? FallbackWelcomeDefaultText
                    : _cachedWelcomeDefaultText;
            }
        }

        private void HideReadingModeUi()
        {
            if (_readingModeRoot != null)
                _readingModeRoot.SetActive(false);
        }

        /// <summary>
        /// 과거·현재·미래 3장을 뒷면으로 표시합니다. 확인 버튼 없이 바로 이후 단계(AI 등)로 이어집니다.
        /// </summary>
        public void ShowSpreadThreeCardsBacks(TarotCardData[] threeCards, Sprite cardBack)
        {
            if (threeCards == null || threeCards.Length < 3)
                return;

            EnsureSpreadThreePanelBuilt();
            if (_spreadCenterPickImages == null)
                return;

            for (int i = 0; i < 3; i++)
            {
                if (_spreadCenterPickImages[i] != null)
                {
                    _spreadCenterPickImages[i].sprite = cardBack;
                    _spreadCenterPickImages[i].preserveAspect = true;
                }

                if (_spreadCenterPickRects != null && i < _spreadCenterPickRects.Length && _spreadCenterPickRects[i] != null)
                    _spreadCenterPickRects[i].localEulerAngles = Vector3.zero;
            }

            if (_spreadThreeRoot != null)
                _spreadThreeRoot.SetActive(true);

            BringReadingResultAreaToFront();
        }

        /// <summary>
        /// 점괘 영역(스크롤)을 형제 UI보다 앞에 그려 텍스트가 카드에 가리지 않게 합니다.
        /// </summary>
        public void BringReadingResultAreaToFront()
        {
            if (_readingResultAreaRoot != null)
            {
                _readingResultAreaRoot.transform.SetAsLastSibling();
                return;
            }

            if (_readingResultText != null)
                _readingResultText.transform.SetAsLastSibling();
        }

        /// <summary>
        /// 가운데 제시된 N장(뒷면) 중 사용자가 차례로 3장을 골라 과거→현재→미래 슬롯으로 옮깁니다.
        /// 슬롯 인덱스(0=과거)별로 사용자가 고른 카드의 풀 인덱스를 반환합니다. 안 고른 카드는 센터 행과 함께 숨겨집니다.
        /// </summary>
        public async Task<int[]> WaitForSpreadCenterPlacementAsync(Sprite cardBack, TarotCardData[] poolCards)
        {
            if (poolCards == null || poolCards.Length < 3)
                return new[] { 0, 1, 2 };

            EnsureSpreadThreePanelBuilt();
            if (_spreadCenterPickImages == null || _spreadCenterPickRects == null)
                return new[] { 0, 1, 2 };

            ResetSpreadPickCardsToCenter(cardBack);

            if (_spreadThreeRoot != null)
                _spreadThreeRoot.SetActive(true);

            // 처음엔 가운데에 겹쳐 있다가 촤라락 펴지는 등장 모션.
            await PlaySpreadFanOutAsync();

            int pickCount = _spreadCenterPickRects.Length;
            var placed = new bool[pickCount];
            var pickIndexForSlot = new int[3];
            string[] pickMessages = { SpreadPickPastMessage, SpreadPickPresentMessage, SpreadPickFutureMessage };

            for (int step = 0; step < 3; step++)
            {
                UpdateDefaultUIText(pickMessages[step]);

                int pickIndex;
                do
                {
                    _spreadCenterClickTcs = new TaskCompletionSource<int>();
                    pickIndex = await _spreadCenterClickTcs.Task;
                } while (pickIndex < 0 || pickIndex >= pickCount || placed[pickIndex]);

                placed[pickIndex] = true;
                pickIndexForSlot[step] = pickIndex;

                await AnimateSpreadCardIntoSlotAsync(_spreadCenterPickRects[pickIndex], _spreadSlotCardHolders[step]);
                _spreadCenterPickButtons[pickIndex].interactable = false;

                // 다음 선택이 남아 있으면, 빠진 자리를 메우도록 남은 카드를 가운데로 재배치.
                if (step < 2)
                    await ReFanRemainingCardsAsync(placed);
            }

            for (int s = 0; s < 3; s++)
            {
                _spreadThreeImages[s] = _spreadCenterPickImages[pickIndexForSlot[s]];
                _spreadThreeImageRects[s] = _spreadCenterPickRects[pickIndexForSlot[s]];
            }

            if (_spreadCenterRow != null)
                _spreadCenterRow.SetActive(false);

            _spreadCenterClickTcs = null;
            return pickIndexForSlot;
        }

        /// <summary>
        /// 스프레드 센터에 깔 픽 카드 개수(N)를 반환합니다. 패널에 연결된 카드 수와 같습니다(기본 3 이상).
        /// 게임 매니저가 이 개수만큼 덱에서 뽑아 넘기도록 맞추는 데 사용합니다.
        /// </summary>
        public int GetSpreadPickCardCount()
        {
            EnsureSpreadThreePanelBuilt();
            return _spreadCenterPickImages?.Length ?? 3;
        }

        /// <summary>
        /// 세 슬롯의 카드를 동시에 뒤집고 과거·현재·미래 점괘를 표시합니다.
        /// </summary>
        public async Task RunSpreadFlipAllAndShowResultAsync(
            Sprite[] frontSpritesBySlot,
            TarotCardOrientation[] orientationsBySlot,
            string[] sectionTexts,
            string readingIntro,
            TarotCardData[] cardsBySlot)
        {
            if (frontSpritesBySlot == null || orientationsBySlot == null
                || frontSpritesBySlot.Length < 3 || orientationsBySlot.Length < 3)
                return;

            EnsureSpreadThreePanelBuilt();
            if (_spreadThreeImageRects == null || _spreadThreeImages == null)
                return;

            ClearSpreadCardNameUnderTexts();

            SetReadingResultScrollActive(true);
            UpdateReadingResultUI(string.Empty);
            BringReadingResultAreaToFront();

            if (_spreadThreeRoot != null)
                _spreadThreeRoot.SetActive(true);

            var flipTasks = new Task[3];
            for (int i = 0; i < 3; i++)
                flipTasks[i] = FlipSpreadSlotAsync(i, frontSpritesBySlot[i], orientationsBySlot[i]);
            await Task.WhenAll(flipTasks);

            var nameRevealed = new[] { true, true, true };
            for (int i = 0; i < 3; i++)
                SetSpreadSlotCardNameUnder(i, cardsBySlot[i], orientationsBySlot[i]);
            RebuildSpreadCardNameLines(cardsBySlot, orientationsBySlot, nameRevealed);

            string mergedReading = string.Empty;
            if (sectionTexts != null)
            {
                for (int i = 0; i < sectionTexts.Length; i++)
                {
                    string piece = sectionTexts[i]?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(piece))
                        continue;

                    mergedReading = string.IsNullOrEmpty(mergedReading)
                        ? piece
                        : mergedReading + " " + piece;
                }
            }

            if (string.IsNullOrWhiteSpace(mergedReading))
                mergedReading = "해석을 불러오지 못했어요. 다시 시도해 보세요.";

            // 1장과 동일하게 타이핑 효과로 표시(카드 뒤집힘 → 해석 스트리밍 순서 일치).
            string introPrefix = string.IsNullOrEmpty(readingIntro) ? string.Empty : readingIntro + "\n\n";
            await StreamReadingResultAsync(introPrefix + mergedReading);
        }

        private void ResetSpreadPickCardsToCenter(Sprite cardBack)
        {
            int pickCount = _spreadCenterPickRects.Length;
            for (int i = 0; i < pickCount; i++)
            {
                // 컬럼 루트를 원본 부채꼴 위치/회전으로 복원(이전 라운드 재배치 되돌림).
                if (_spreadFanColPos != null && i < _spreadFanColPos.Length)
                {
                    RectTransform root = _spreadCenterPickColumnRoots[i];
                    if (root != null)
                    {
                        root.anchoredPosition = _spreadFanColPos[i];
                        root.localEulerAngles = new Vector3(0f, 0f, _spreadFanColRotZ[i]);
                    }
                }

                RectTransform rt = _spreadCenterPickRects[i];
                rt.SetParent(_spreadCenterPickColumnRoots[i], false);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(_spreadSlotPreferredSize.x, _spreadSlotPreferredSize.y);
                rt.localScale = Vector3.one;
                rt.localEulerAngles = Vector3.zero;
                _spreadCenterPickImages[i].sprite = cardBack;
                _spreadCenterPickImages[i].preserveAspect = true;
                _spreadCenterPickButtons[i].interactable = true;
            }

            if (_spreadCenterRow != null)
                _spreadCenterRow.SetActive(true);

            SetSpreadSlotHolderBackgroundImagesEnabled(true);
        }

        /// <summary>
        /// SlotCardHolder 루트의 배경 <see cref="Image"/>를 켜거나 끕니다. 카드가 슬롯에 들어가면 끄고, 리셋·배치 시작 시 켭니다.
        /// </summary>
        private void SetSpreadSlotHolderBackgroundImagesEnabled(bool enabled)
        {
            if (_spreadSlotCardHolders == null)
                return;
            for (int i = 0; i < _spreadSlotCardHolders.Length; i++)
            {
                RectTransform holder = _spreadSlotCardHolders[i];
                if (holder == null)
                    continue;
                Image bg = holder.GetComponent<Image>();
                if (bg != null)
                    bg.enabled = enabled;
            }
        }

        private async Task AnimateSpreadCardIntoSlotAsync(RectTransform cardRt, RectTransform slotHolder)
        {
            // 선택 피드백: 살짝 키웠다 되돌리는 팝(탭 확인감).
            await LMotion.Create(1f, 1.08f, 0.07f).WithEase(Ease.OutQuad)
                .Bind(s => cardRt.localScale = new Vector3(s, s, 1f));
            await LMotion.Create(1.08f, 1f, 0.05f).WithEase(Ease.InQuad)
                .Bind(s => cardRt.localScale = new Vector3(s, s, 1f));

            // 슬롯으로 부모를 옮기되 화면상 위치를 유지한 채 시작합니다.
            cardRt.SetParent(slotHolder, true);

            Vector2 startPos = cardRt.anchoredPosition;
            Vector2 startSize = cardRt.sizeDelta;
            float startZ = Mathf.DeltaAngle(0f, cardRt.localEulerAngles.z); // -180~180 정규화(부채꼴 기울기)
            Vector2 targetSize = new Vector2(_spreadSlotPreferredSize.x, _spreadSlotPreferredSize.y);
            float dur = SpreadCardMoveDuration;

            // 위치(대각선)·회전(똑바로)·크기를 "동시에" 보간 — ㄱ자 이동과 기울어진 안착을 제거.
            MotionHandle posHandle = LMotion.Create(startPos, Vector2.zero, dur)
                .WithEase(Ease.OutCubic)
                .Bind(p => cardRt.anchoredPosition = p);
            MotionHandle rotHandle = LMotion.Create(startZ, 0f, dur)
                .WithEase(Ease.OutCubic)
                .Bind(z => cardRt.localEulerAngles = new Vector3(0f, 0f, z));
            MotionHandle sizeHandle = LMotion.Create(startSize, targetSize, dur)
                .WithEase(Ease.OutCubic)
                .Bind(s => cardRt.sizeDelta = s);

            await posHandle;
            await rotHandle;
            await sizeHandle;

            // 최종 정렬 고정.
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.localEulerAngles = Vector3.zero;
            cardRt.sizeDelta = targetSize;

            Image holderBg = slotHolder.GetComponent<Image>();
            if (holderBg != null)
                holderBg.enabled = false;
        }

        /// <summary>
        /// 등장 연출: 모든 컬럼 루트를 가운데에 겹쳐 둔 뒤, 왼→오 스태거로 원본 부채꼴 위치/회전으로 펼칩니다.
        /// </summary>
        private async Task PlaySpreadFanOutAsync()
        {
            if (_spreadCenterPickColumnRoots == null || _spreadFanColPos == null)
                return;

            int n = _spreadCenterPickColumnRoots.Length;

            // 시작점: 왼쪽 끝 슬롯(인덱스 0)에 전부 겹쳐 둠 — 첫 프레임은 왼쪽에 한 묶음.
            Vector2 startPos = _spreadFanColPos.Length > 0 ? _spreadFanColPos[0] : Vector2.zero;
            float startRotZ = _spreadFanColRotZ.Length > 0 ? Mathf.DeltaAngle(0f, _spreadFanColRotZ[0]) : 0f;

            for (int i = 0; i < n; i++)
            {
                RectTransform root = _spreadCenterPickColumnRoots[i];
                if (root == null)
                    continue;
                root.anchoredPosition = startPos;
                root.localEulerAngles = new Vector3(0f, 0f, startRotZ);
            }

            MotionHandle last = default;
            bool any = false;

            for (int i = 0; i < n; i++)
            {
                RectTransform root = _spreadCenterPickColumnRoots[i];
                if (root == null)
                    continue;

                Vector2 s = startPos;
                Vector2 e = _spreadFanColPos[i];
                float rs = startRotZ;
                float re = Mathf.DeltaAngle(0f, _spreadFanColRotZ[i]);
                float delay = i * SpreadFanOutStagger;

                // 이동 거리에 비례한 호 높이 — 멀리 가는 바깥(오른쪽) 카드일수록 위로 더 크게 휘어 부채처럼.
                float arc = Vector2.Distance(s, e) * SpreadFanOutArcFactor;
                RectTransform r = root;

                // 위치(호)와 회전을 진행값 t 하나로 함께 구동 — 직선 슬라이드 대신 곡선 스윕.
                last = LMotion.Create(0f, 1f, SpreadFanOutDuration)
                    .WithEase(Ease.OutCubic)
                    .WithDelay(delay)
                    .Bind(t =>
                    {
                        Vector2 p = Vector2.Lerp(s, e, t);
                        p.y += arc * Mathf.Sin(Mathf.PI * t);
                        r.anchoredPosition = p;
                        r.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(rs, re, t));
                    });
                any = true;
            }

            if (any)
                await last;
        }

        /// <summary>
        /// 아직 안 고른 카드들을 (왼→오 순서 유지하며) 원본 부채꼴의 가운데 K개 위치로 모아 빈자리를 메웁니다.
        /// 모든 컬럼 루트를 동시에 보간하고 마지막 모션만 await합니다(동일 duration).
        /// </summary>
        private async Task ReFanRemainingCardsAsync(bool[] placed)
        {
            if (_spreadCenterPickColumnRoots == null || _spreadFanColPos == null || placed == null)
                return;

            int n = _spreadCenterPickColumnRoots.Length;

            int k = 0;
            for (int i = 0; i < n; i++)
                if (!placed[i]) k++;
            if (k == 0)
                return;

            int targetStart = (n - k) / 2;

            int j = 0;
            bool any = false;
            MotionHandle lastPos = default;
            MotionHandle lastRot = default;

            for (int i = 0; i < n; i++)
            {
                if (placed[i])
                    continue;

                RectTransform root = _spreadCenterPickColumnRoots[i];
                int slot = targetStart + j;
                j++;
                if (root == null || slot < 0 || slot >= n)
                    continue;

                Vector2 startPos = root.anchoredPosition;
                Vector2 targetPos = _spreadFanColPos[slot];
                float startRotZ = Mathf.DeltaAngle(0f, root.localEulerAngles.z);
                float endRotZ = Mathf.DeltaAngle(0f, _spreadFanColRotZ[slot]);

                lastPos = LMotion.Create(startPos, targetPos, SpreadRefanDuration)
                    .WithEase(Ease.OutCubic)
                    .Bind(p => root.anchoredPosition = p);
                lastRot = LMotion.Create(startRotZ, endRotZ, SpreadRefanDuration)
                    .WithEase(Ease.OutCubic)
                    .Bind(z => root.localEulerAngles = new Vector3(0f, 0f, z));
                any = true;
            }

            if (any)
            {
                await lastPos;
                await lastRot;
            }
        }

        private void OnSpreadCenterCardClicked(int pickIndex)
        {
            if (_spreadCenterClickTcs == null)
                return;
            int pickCount = _spreadCenterPickImages?.Length ?? 0;
            if (pickIndex < 0 || pickIndex >= pickCount)
                return;

            _spreadCenterClickTcs.TrySetResult(pickIndex);
        }

        /// <summary>
        /// 스프레드 상단 제목만 표시합니다. 카드별 이름은 각 슬롯 아래 <see cref="SetSpreadSlotCardNameUnder"/>에서 냅니다.
        /// </summary>
        private void RebuildSpreadCardNameLines(
            TarotCardData[] cards,
            TarotCardOrientation[] orientations,
            bool[] revealed)
        {
            if (_cardNameText == null || cards == null || revealed == null)
                return;

            bool any = revealed[0] || revealed[1] || revealed[2];
            if (!any)
            {
                _cardNameText.gameObject.SetActive(false);
                return;
            }

            if (_cardUiRoot != null)
                _cardUiRoot.SetActive(true);

            _cardNameText.gameObject.SetActive(true);
            _cardNameText.text = "3장 스프레드 (과거·현재·미래)";
        }

        private void SetSpreadSlotCardNameUnder(int index, TarotCardData card, TarotCardOrientation orientation)
        {
            if (_spreadCardNameUnderTexts == null || index < 0 || index >= _spreadCardNameUnderTexts.Length)
                return;

            TextMeshProUGUI tmp = _spreadCardNameUnderTexts[index];
            if (tmp == null)
                return;

            string dir = TarotCardOrientationLabels.GetShortLabelKr(orientation);
            tmp.text = $"{card.NameKr}\n{card.NameEn}\n{dir}";
        }

        /// <summary>
        /// 슬롯 아래 카드명(한글·영문·방향) TMP를 비웁니다. 리셋·스프레드 결과 시작 시 사용합니다.
        /// </summary>
        private void ClearSpreadCardNameUnderTexts()
        {
            if (_spreadCardNameUnderTexts == null)
                return;
            for (int i = 0; i < _spreadCardNameUnderTexts.Length; i++)
            {
                if (_spreadCardNameUnderTexts[i] != null)
                    _spreadCardNameUnderTexts[i].text = string.Empty;
            }
        }

        private async Task FlipSpreadSlotAsync(int index, Sprite frontSprite, TarotCardOrientation orientation)
        {
            RectTransform rt = _spreadThreeImageRects[index];
            Image img = _spreadThreeImages[index];
            if (rt == null || img == null)
                return;

            float z = orientation == TarotCardOrientation.Reversed ? 180f : 0f;
            rt.localEulerAngles = new Vector3(0f, 0f, z);

            const float half = 0.16f;
            await LMotion.Create(rt.localScale.x, 0f, half)
                .WithEase(Ease.InOutQuad)
                .BindToLocalScaleX(rt);

            img.sprite = frontSprite;
            img.preserveAspect = true;

            await LMotion.Create(0f, 1f, half)
                .WithEase(Ease.InOutQuad)
                .BindToLocalScaleX(rt);
        }

        private void EnsureSpreadThreePanelBuilt()
        {
            if (_spreadThreeRoot != null && _spreadCenterRow != null)
                return;

            TarotSpreadThreePanel panel = null;

            if (_spreadThreeSceneInstance != null)
                panel = _spreadThreeSceneInstance;
            else if (_spreadThreePanelPrefab != null)
            {
                Transform parent = _spreadThreeParentOverride != null
                    ? _spreadThreeParentOverride
                    : _gameplayUiRoot != null ? _gameplayUiRoot.transform : transform;

                GameObject instance = Instantiate(_spreadThreePanelPrefab, parent, false);
                panel = instance.GetComponent<TarotSpreadThreePanel>();
                if (panel == null)
                {
                    Debug.LogError("[TarotUIManager] _spreadThreePanelPrefab 루트에 TarotSpreadThreePanel 컴포넌트가 필요합니다.");
                    Destroy(instance);
                    return;
                }
            }
            else
            {
                Debug.LogError("[TarotUIManager] 3장 스프레드: TarotSpreadThreePanel이 있는 씬 인스턴스 또는 프리팹을 지정해 주세요.");
                return;
            }

            WireSpreadReferencesFromPanel(panel);
            if (_spreadThreeRoot == null)
                return;

            _spreadThreeRoot.SetActive(false);
        }

        private void WireSpreadReferencesFromPanel(TarotSpreadThreePanel panel)
        {
            if (panel == null || !panel.ValidateBindings())
            {
                Debug.LogError("[TarotUIManager] TarotSpreadThreePanel 참조가 비었습니다. 프리팹에서 Center Row, 픽 카드(3장 이상), 슬롯×3, 이름 TMP×3를 연결해 주세요.");
                return;
            }

            _spreadThreeRoot = panel.gameObject;
            _spreadCenterRow = panel.CenterRow;
            _spreadCenterPickColumnRoots = panel.CenterPickColumnRoots;
            _spreadCenterPickImages = panel.CenterPickCardImages;
            _spreadCenterPickButtons = panel.CenterPickButtons;
            _spreadSlotCardHolders = panel.SlotCardHolders;
            _spreadCardNameUnderTexts = panel.CardNameUnderTexts;

            // 픽 카드 개수(N)는 인스펙터에 연결된 만큼 사용합니다(3 이상). 슬롯·이름은 과거·현재·미래 3개 고정.
            int pickCount = _spreadCenterPickImages.Length;
            _spreadCenterPickRects = new RectTransform[pickCount];
            for (int i = 0; i < pickCount; i++)
                _spreadCenterPickRects[i] = _spreadCenterPickImages[i].rectTransform;

            // 컬럼 루트의 원본 부채꼴 위치/회전을 저장(재배치·복원에 사용).
            _spreadFanColPos = new Vector2[pickCount];
            _spreadFanColRotZ = new float[pickCount];
            for (int i = 0; i < pickCount; i++)
            {
                RectTransform root = _spreadCenterPickColumnRoots[i];
                _spreadFanColPos[i] = root != null ? root.anchoredPosition : Vector2.zero;
                _spreadFanColRotZ[i] = root != null ? root.localEulerAngles.z : 0f;
            }

            _spreadThreeImages = new Image[3];
            _spreadThreeImageRects = new RectTransform[3];

            for (int i = 0; i < pickCount; i++)
            {
                int captured = i;
                _spreadCenterPickButtons[i].onClick.RemoveAllListeners();
                _spreadCenterPickButtons[i].onClick.AddListener(() => OnSpreadCenterCardClicked(captured));
            }
        }

        /// <summary>
        /// 3장 스프레드 패널을 숨깁니다.
        /// </summary>
        public void HideSpreadThreeCardUi()
        {
            _spreadCenterClickTcs = null;
            ClearSpreadCardNameUnderTexts();
            SetSpreadSlotHolderBackgroundImagesEnabled(true);
            if (_spreadThreeRoot != null)
                _spreadThreeRoot.SetActive(false);
        }

        /// <summary>
        /// 3장 스프레드 결과용 카드 요약 텍스트를 표시합니다.
        /// </summary>
        public void UpdateCardNameUIForSpread(
            TarotCardData past,
            TarotCardOrientation pastOr,
            TarotCardData present,
            TarotCardOrientation presentOr,
            TarotCardData future,
            TarotCardOrientation futureOr)
        {
            if (_cardNameText == null)
                return;

            if (_cardUiRoot != null)
                _cardUiRoot.SetActive(true);

            _cardNameText.gameObject.SetActive(true);

            string Line(string pos, TarotCardData c, TarotCardOrientation o)
            {
                return $"{pos}: {c.NameKr} ({c.NameEn}) · {TarotCardOrientationLabels.GetShortLabelKr(o)}";
            }

            _cardNameText.text =
                "3장 스프레드 (과거·현재·미래)\n" +
                $"{Line("과거", past, pastOr)}\n" +
                $"{Line("현재", present, presentOr)}\n" +
                $"{Line("미래", future, futureOr)}";
        }

        /// <summary>
        /// 덱에서 제시된 2장 중 사용자가 탭할 때까지 비동기로 대기합니다. 두 장 모두 뒷면으로 표시합니다.
        /// </summary>
        public async Task<TarotCardData> WaitForTwoCardSelectionAsync(
            TarotCardData cardLeft,
            TarotCardData cardRight,
            Sprite cardBack)
        {
            EnsureTwoCardChoicePanelBuilt();
            ApplyTwoCardChoiceSlotSizes();

            _twoCardChoiceLeftData = cardLeft;
            _twoCardChoiceRightData = cardRight;
            _twoCardChoiceTcs = new TaskCompletionSource<TarotCardData>();

            if (_twoCardChoiceImageLeft != null)
            {
                _twoCardChoiceImageLeft.sprite = cardBack;
                _twoCardChoiceImageLeft.preserveAspect = true;
            }

            if (_twoCardChoiceImageRight != null)
            {
                _twoCardChoiceImageRight.sprite = cardBack;
                _twoCardChoiceImageRight.preserveAspect = true;
            }

            if (_twoCardChoiceButtonLeft != null)
            {
                _twoCardChoiceButtonLeft.onClick.RemoveAllListeners();
                _twoCardChoiceButtonLeft.onClick.AddListener(OnTwoCardChoiceLeftClicked);
                _twoCardChoiceButtonLeft.interactable = true;
            }

            if (_twoCardChoiceButtonRight != null)
            {
                _twoCardChoiceButtonRight.onClick.RemoveAllListeners();
                _twoCardChoiceButtonRight.onClick.AddListener(OnTwoCardChoiceRightClicked);
                _twoCardChoiceButtonRight.interactable = true;
            }

            if (_twoCardChoiceRoot != null)
            {
                _twoCardChoiceRoot.SetActive(true);
                _twoCardChoiceRoot.transform.SetAsLastSibling();
            }

            return await _twoCardChoiceTcs.Task;
        }

        private void OnTwoCardChoiceLeftClicked()
        {
            CompleteTwoCardChoice(_twoCardChoiceLeftData);
        }

        private void OnTwoCardChoiceRightClicked()
        {
            CompleteTwoCardChoice(_twoCardChoiceRightData);
        }

        private void CompleteTwoCardChoice(TarotCardData card)
        {
            if (_twoCardChoiceButtonLeft != null)
                _twoCardChoiceButtonLeft.interactable = false;
            if (_twoCardChoiceButtonRight != null)
                _twoCardChoiceButtonRight.interactable = false;

            HideTwoCardChoiceUi();

            if (_twoCardChoiceButtonLeft != null)
                _twoCardChoiceButtonLeft.interactable = true;
            if (_twoCardChoiceButtonRight != null)
                _twoCardChoiceButtonRight.interactable = true;

            _twoCardChoiceTcs?.TrySetResult(card);
        }

        private void EnsureTwoCardChoicePanelBuilt()
        {
            if (_twoCardChoiceRoot != null)
                return;

            Transform parent = _twoCardChoiceParentOverride != null
                ? _twoCardChoiceParentOverride
                : _gameplayUiRoot != null ? _gameplayUiRoot.transform : transform;

            _twoCardChoiceRoot = new GameObject("TwoCardChoiceRoot", typeof(RectTransform));
            RectTransform rootRt = _twoCardChoiceRoot.GetComponent<RectTransform>();
            rootRt.SetParent(parent, false);
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = new Vector2(16f, 200f);
            rootRt.offsetMax = new Vector2(-16f, -240f);

            var hlg = _twoCardChoiceRoot.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 32f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(8, 8, 8, 8);

            _twoCardChoiceButtonLeft = CreateCardChoiceSlot(rootRt, "ChoiceLeft", out _twoCardChoiceImageLeft);
            _twoCardChoiceButtonRight = CreateCardChoiceSlot(rootRt, "ChoiceRight", out _twoCardChoiceImageRight);

            _twoCardChoiceRoot.SetActive(false);
        }

        private void ApplyTwoCardChoiceSlotSizes()
        {
            if (_twoCardChoiceRoot == null)
                return;

            for (int i = 0; i < _twoCardChoiceRoot.transform.childCount; i++)
            {
                var le = _twoCardChoiceRoot.transform.GetChild(i).GetComponent<LayoutElement>();
                if (le == null)
                    continue;

                le.preferredWidth = _twoCardChoiceSlotPreferredSize.x;
                le.preferredHeight = _twoCardChoiceSlotPreferredSize.y;
                le.minWidth = _twoCardChoiceSlotMinSize.x;
                le.minHeight = _twoCardChoiceSlotMinSize.y;
                le.flexibleWidth = 0f;
            }
        }

        private Button CreateCardChoiceSlot(RectTransform parent, string name, out Image cardImage)
        {
            var slot = new GameObject(name, typeof(RectTransform));
            var slotRt = slot.GetComponent<RectTransform>();
            slotRt.SetParent(parent, false);

            var le = slot.AddComponent<LayoutElement>();
            le.preferredWidth = _twoCardChoiceSlotPreferredSize.x;
            le.preferredHeight = _twoCardChoiceSlotPreferredSize.y;
            le.minWidth = _twoCardChoiceSlotMinSize.x;
            le.minHeight = _twoCardChoiceSlotMinSize.y;
            le.flexibleWidth = 0f;

            cardImage = slot.AddComponent<Image>();
            cardImage.color = Color.white;
            cardImage.preserveAspect = true;
            cardImage.raycastTarget = true;

            var btn = slot.AddComponent<Button>();
            btn.targetGraphic = cardImage;
            return btn;
        }

        /// <summary>
        /// 2장 선택 패널을 숨깁니다.
        /// </summary>
        public void HideTwoCardChoiceUi()
        {
            if (_twoCardChoiceRoot != null)
                _twoCardChoiceRoot.SetActive(false);
        }

        /// <summary>
        /// 셔플·선택 단계에서 흐름이 끊겼을 때 고민 입력 UI를 다시 표시합니다.
        /// </summary>
        public void RestoreConcernInputAfterInterruptedFlow()
        {
            HideTwoCardChoiceUi();
            HideSpreadThreeCardUi();
            HideCategoryUi();
            HideReadingModeUi();
            ShowInputUI();
            SetConcernAndDrawVisible(true);
            SetReadingAreaVisible(true);
            SetReadingResultScrollActive(false);
        }

        /// <summary>
        /// 카드 관련 UI 전체(CardUIRoot)를 숨기고 카드 이름 텍스트를 비웁니다.
        /// </summary>
        public void HideCardUiArea()
        {
            HideSpreadThreeCardUi();
            if (_cardUiRoot != null)
                _cardUiRoot.SetActive(false);

            ClearAndHideCardNameOnlyWhenNoRoot();
        }

        /// <summary>
        /// 카드 등장·애니메이션 직전에 CardUIRoot를 켭니다. 카드 이름은 애니메이션 후 UpdateCardNameUI에서 켜집니다.
        /// </summary>
        public void ShowCardUiArea()
        {
            if (_cardUiRoot != null)
                _cardUiRoot.SetActive(true);

            if (_cardNameText != null)
            {
                _cardNameText.text = string.Empty;
                _cardNameText.gameObject.SetActive(false);
            }
        }

        private void ClearAndHideCardNameOnlyWhenNoRoot()
        {
            if (_cardNameText == null) return;

            _cardNameText.text = string.Empty;
            if (_cardUiRoot == null)
                _cardNameText.gameObject.SetActive(false);
        }

        public void HideInputUI()
        {
            if (_inputUiRoot != null)
                _inputUiRoot.SetActive(false);
        }

        /// <summary>
        /// 고민 입력 영역(InputUIRoot)을 다시 표시합니다.
        /// </summary>
        public void ShowInputUI()
        {
            if (_inputUiRoot != null)
                _inputUiRoot.SetActive(true);
        }

        /// <summary>
        /// 고민 입력창의 내용을 반환합니다.
        /// </summary>
        public string GetUserConcern()
        {
            return _concernInputField?.text ?? string.Empty;
        }

        /// <summary>
        /// 뽑기 버튼 클릭 시 실행될 이벤트를 등록합니다.
        /// </summary>
        public void AddDrawButtonListener(UnityAction action)
        {
            if (action != null)
                _drawCardButton?.onClick.AddListener(action);
        }

        /// <summary>
        /// 리셋 버튼 클릭 시 실행될 이벤트를 등록합니다.
        /// </summary>
        public void AddResetButtonListener(UnityAction action)
        {
            if (action != null)
                _resetButton?.onClick.AddListener(action);
        }

        /// <summary>
        /// 리셋 버튼 입력 가능 여부 (점괘가 모두 표시된 뒤에만 true 권장).
        /// </summary>
        public void SetResetButtonInteractable(bool interactable)
        {
            if (_resetButton != null)
                _resetButton.gameObject.SetActive(interactable);
        }

        /// <summary>
        /// 고민 입력 필드를 비웁니다.
        /// </summary>
        public void ClearConcernInput()
        {
            if (_concernInputField != null)
                _concernInputField.text = string.Empty;
        }

        /// <summary>
        /// 버튼의 활성화 상태를 변경합니다. (AI 추론 중 중복 클릭 방지)
        /// </summary>
        public void SetDrawButtonInteractable(bool interactable)
        {
            if (_drawCardButton != null)
                _drawCardButton.interactable = interactable;
        }

        /// <summary>
        /// 화면에 뽑힌 카드 이름을 업데이트합니다.
        /// </summary>
        public void UpdateCardNameUI(string nameKr, string nameEn)
        {
            UpdateCardNameUI(nameKr, nameEn, TarotCardOrientation.Upright);
        }

        /// <summary>
        /// 화면에 뽑힌 카드 이름과 정·역방향을 업데이트합니다.
        /// </summary>
        public void UpdateCardNameUI(string nameKr, string nameEn, TarotCardOrientation orientation)
        {
            if (_cardNameText == null) return;

            if (string.IsNullOrWhiteSpace(nameKr) && string.IsNullOrWhiteSpace(nameEn))
            {
                HideCardUiArea();
                return;
            }

            if (_cardUiRoot != null)
                _cardUiRoot.SetActive(true);

            _cardNameText.gameObject.SetActive(true);
            string direction = TarotCardOrientationLabels.GetShortLabelKr(orientation);
            _cardNameText.text = $"선택된 카드:\n{nameKr} ({nameEn})\n방향: {direction}";
        }

        /// <summary>
        /// 점괘 스크롤(또는 결과 영역)을 켜거나 끕니다. 끄면 레이캐스트가 통과해 카드 터치가 살아납니다.
        /// </summary>
        public void SetReadingResultScrollActive(bool active)
        {
            if (_readingResultScrollRect != null)
                _readingResultScrollRect.gameObject.SetActive(active);
            else if (_readingResultAreaRoot != null)
                _readingResultAreaRoot.SetActive(active);
        }

        /// <summary>
        /// 화면에 타로 리딩 결과를 업데이트합니다. (스트리밍 용도)
        /// </summary>
        public void UpdateReadingResultUI(string resultText)
        {
            bool hasText = !string.IsNullOrEmpty(resultText);

            if (_readingResultText != null)
                _readingResultText.text = resultText ?? string.Empty;
            RefreshReadingResultScrollLayout(!hasText);
        }

        /// <summary>
        /// 완성된 텍스트를 한 글자씩 스트리밍 연출로 표시합니다.
        /// </summary>
        public async Task StreamReadingResultAsync(string fullText)
        {
            CancelStreaming();
            _streamingCts = new CancellationTokenSource();
            var token = _streamingCts.Token;

            if (_readingResultText == null) return;

            SetReadingResultScrollActive(true);
            _readingResultText.text = string.Empty;
            if (_readingResultScrollRect != null)
                _readingResultScrollRect.verticalNormalizedPosition = 1f;
            int delayMs = Mathf.Max(1, (int)(_charDelay * 1000));

            try
            {
                for (int i = 0; i < fullText.Length; i++)
                {
                    token.ThrowIfCancellationRequested();
                    _readingResultText.text = fullText.Substring(0, i + 1);
                    if ((i + 1) % StreamingLayoutRefreshCharInterval == 0)
                        RefreshReadingResultScrollLayout(false);
                    await Task.Delay(delayMs, token);
                }
            }
            catch (TaskCanceledException)
            {
                // 스트리밍이 외부에서 취소됨 — 정상 흐름
            }

            _readingResultText.text = fullText;
            RefreshReadingResultScrollLayout(false);
        }

        /// <summary>
        /// ScrollRect·ContentSizeFitter 기준으로 점괘 텍스트 높이를 다시 계산합니다.
        /// </summary>
        /// <param name="scrollToTop">true이면 스크롤을 맨 위(점괘 시작)로 맞춥니다.</param>
        private void RefreshReadingResultScrollLayout(bool scrollToTop)
        {
            RectTransform content = _readingResultScrollContent;
            if (content == null && _readingResultText != null)
                content = _readingResultText.rectTransform;

            if (content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            Canvas.ForceUpdateCanvases();

            if (scrollToTop && _readingResultScrollRect != null)
                _readingResultScrollRect.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// 진행 중인 텍스트 스트리밍을 취소합니다.
        /// </summary>
        public void CancelStreaming()
        {
            if (_streamingCts != null)
            {
                _streamingCts.Cancel();
                _streamingCts.Dispose();
                _streamingCts = null;
            }
        }

        /// <summary>
        /// LLMUnity 모델 복사·다운로드 진행률(0~1)을 기본 안내 텍스트에 표시합니다.
        /// </summary>
        public void UpdateModelSetupProgress(float progressNormalized)
        {
            if (_defaultUIText == null) return;

            int percent = Mathf.Clamp(Mathf.RoundToInt(progressNormalized * 100f), 0, 100);
            _defaultUIText.gameObject.SetActive(true);
            _defaultUIText.text = string.Format(ModelSetupProgressFormat, percent);
        }

        /// <summary>
        /// 기본 텍스트 내용을 갱신하고 표시합니다. (로딩 메시지 등)
        /// </summary>
        public void UpdateDefaultUIText(string text)
        {
            if (_defaultUIText == null) return;

            _defaultUIText.gameObject.SetActive(true);
            _defaultUIText.text = text;
        }

        /// <summary>
        /// 기본 안내 텍스트 영역을 숨깁니다. 카드 등장 직전에 호출합니다.
        /// </summary>
        public void HideDefaultUI()
        {
            _defaultUIText?.gameObject.SetActive(false);
        }

        /// <summary>
        /// 초기 UI 상태로 리셋합니다. 카테고리 선택 화면부터 다시 시작합니다.
        /// </summary>
        public void ResetUI()
        {
            CancelStreaming();

            SetDrawButtonInteractable(true);
            SetResetButtonInteractable(false);

            ShowCategorySelectionLayout();

            if (_readingResultText != null)
                _readingResultText.text = string.Empty;
            SetReadingResultScrollActive(false);

            HideCardUiArea();

            _readingResultAreaRoot.transform.SetSiblingIndex(1);
        }

        private void OnDestroy()
        {
            CancelStreaming();
        }
    }
}