using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Tarot.UI
{
    /// <summary>
    /// 타로 게임의 메인 UI 요소들을 관리하는 스크립트입니다.
    /// </summary>
    public class TarotUIManager : MonoBehaviour
    {
        private const string EnginePreparingMessage =
            "AI 엔진을 준비하고 있습니다.\n잠시만 기다려 주세요.";

        private const string ModelSetupProgressFormat =
            "AI 엔진을 준비하고 있습니다.\n모델 파일 준비: {0}%";

        private const string FallbackWelcomeDefaultText =
            "당신의 고민을 입력하고, 카드를 뽑아 보세요.";

        [Header("Visibility")]
        [Tooltip("AI 엔진 초기화 전까지 숨길 게임플레이 UI의 루트(입력·버튼·안내 등). 비우면 입력·버튼만 비활성화합니다.")]
        [SerializeField] private GameObject _gameplayUiRoot;

        [Header("Default UI")]
        [Tooltip("기본 UI를 표시할 텍스트")]
        [SerializeField] private TextMeshProUGUI _defaultUIText;

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

        private CancellationTokenSource _streamingCts;
        private string _cachedWelcomeDefaultText;

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

            SetCoreGameplayWidgetsVisible(false);
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
                if (visible)
                    SetCoreGameplayWidgetsVisible(true);

                return;
            }

            SetCoreGameplayWidgetsVisible(visible);
            if (_defaultUIText != null)
                _defaultUIText.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 입력·뽑기·결과 텍스트만 토글합니다. 카드 이름은 카드 등장 시에만 표시합니다.
        /// </summary>
        private void SetCoreGameplayWidgetsVisible(bool visible)
        {
            if (_concernInputField != null)
                _concernInputField.gameObject.SetActive(visible);
            if (_drawCardButton != null)
                _drawCardButton.gameObject.SetActive(visible);
            GameObject readingArea = _readingResultAreaRoot != null
                ? _readingResultAreaRoot
                : _readingResultText != null ? _readingResultText.gameObject : null;
            if (readingArea != null)
                readingArea.SetActive(visible);
        }

        /// <summary>
        /// 카드 관련 UI 전체(CardUIRoot)를 숨기고 카드 이름 텍스트를 비웁니다.
        /// </summary>
        public void HideCardUiArea()
        {
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
            if (_cardNameText == null) return;

            if (string.IsNullOrWhiteSpace(nameKr) && string.IsNullOrWhiteSpace(nameEn))
            {
                HideCardUiArea();
                return;
            }

            if (_cardUiRoot != null)
                _cardUiRoot.SetActive(true);

            _cardNameText.gameObject.SetActive(true);
            _cardNameText.text = $"선택된 카드:\n{nameKr} ({nameEn})";
        }

        /// <summary>
        /// 화면에 타로 리딩 결과를 업데이트합니다. (스트리밍 용도)
        /// </summary>
        public void UpdateReadingResultUI(string resultText)
        {
            if (_readingResultText != null)
                _readingResultText.text = resultText;
            RefreshReadingResultScrollLayout(string.IsNullOrEmpty(resultText));
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
        /// 초기 UI 상태로 리셋합니다.
        /// </summary>
        public void ResetUI()
        {
            CancelStreaming();

            ShowInputUI();
            SetCoreGameplayWidgetsVisible(true);
            SetDrawButtonInteractable(true);
            SetResetButtonInteractable(false);

            if (_defaultUIText != null)
            {
                _defaultUIText.gameObject.SetActive(true);
                _defaultUIText.text = string.IsNullOrEmpty(_cachedWelcomeDefaultText)
                    ? FallbackWelcomeDefaultText
                    : _cachedWelcomeDefaultText;
            }

            HideCardUiArea();
            UpdateReadingResultUI("");
        }

        private void OnDestroy()
        {
            CancelStreaming();
        }
    }
}