using System.Threading.Tasks;
using UnityEngine;
using Tarot.Core;
using Tarot.Data;
using Tarot.UI;

namespace Tarot
{
    /// <summary>
    /// 타로 게임의 전체 흐름을 제어하는 게임 매니저 클래스입니다.
    /// 플로우: 고민 입력 → AI 추론 대기 → 카드 등장 애니메이션 → 결과 텍스트 스트리밍
    /// </summary>
    public class TarotGameManager : MonoBehaviour
    {
        private const string ReadingInProgressMessage = "카드를 섞고 운명의 메시지를 읽는 중입니다...";
        private const string AiNotReadyMessage = "AI 엔진이 아직 준비되지 않았습니다. 잠시만 기다려주세요.";
        private const string EmptyConcernMessage = "먼저 고민거리를 입력해 주세요!";
        private const string ReadingParseFailedMessage = "카드의 메시지를 해석하지 못했습니다. 다시 시도해 주세요.";
        private const string ReadingSectionTag = "[점괘]";

        [Header("Managers")]
        [SerializeField] private TarotAIManager _aiManager;
        [SerializeField] private TarotUIManager _uiManager;
        [SerializeField] private TarotCardAnimator _cardAnimator;
        [SerializeField] private TarotCardResourceLoader _resourceLoader;

        private TarotDeckController _deckController;
        private TarotPromptBuilder _promptBuilder;
        private bool _isProcessing;

        private void Awake()
        {
            _deckController = new TarotDeckController();
            _promptBuilder = new TarotPromptBuilder();
        }

        private void Start()
        {
            _uiManager.UpdateReadingResultUI("");
            _cardAnimator.Initialize(_resourceLoader.GetCardBackSprite());
            _uiManager.AddDrawButtonListener(OnDrawButtonClicked);
            _uiManager.AddResetButtonListener(OnResetButtonClicked);
            _uiManager.SetResetButtonInteractable(false);
        }

        /// <summary>
        /// 사용자가 타로 뽑기 버튼을 클릭했을 때의 메인 흐름을 실행합니다.
        /// </summary>
        private async void OnDrawButtonClicked()
        {
            if (_isProcessing) return;

            if (_aiManager == null || !_aiManager.IsReady)
            {
                _uiManager.UpdateReadingResultUI(AiNotReadyMessage);
                return;
            }

            string userConcern = _uiManager.GetUserConcern();
            if (string.IsNullOrWhiteSpace(userConcern))
            {
                _uiManager.UpdateDefaultUIText(EmptyConcernMessage);
                return;
            }

            _isProcessing = true;
            _uiManager.SetDrawButtonInteractable(false);
            _uiManager.SetResetButtonInteractable(false);
            _uiManager.HideInputUI();

            await ExecuteTarotReadingFlowAsync(userConcern);

            _uiManager.SetDrawButtonInteractable(true);
            _isProcessing = false;
        }

        private async Task ExecuteTarotReadingFlowAsync(string userConcern)
        {
            _uiManager.HideCardUiArea();
            _cardAnimator.HideCard();
            _uiManager.UpdateReadingResultUI(string.Empty);
            _uiManager.UpdateDefaultUIText(ReadingInProgressMessage);

            TarotCardData drawnCard = _deckController.DrawRandomCard();

            string prompt = _promptBuilder.BuildUserPrompt(userConcern, drawnCard);
            Debug.Log($"[TarotGameManager] 생성된 유저 프롬프트:\n{prompt}");

            string rawReply = await _aiManager.RequestTarotReadingAsync(prompt);
            string readingResult = ExtractReadingResult(rawReply, prompt);

            Sprite frontSprite = _resourceLoader.GetCardFrontSprite(drawnCard.Id);
            _uiManager.HideDefaultUI();
            _uiManager.ShowCardUiArea();
            await _cardAnimator.ShowCardAsync(frontSprite);

            _uiManager.UpdateCardNameUI(drawnCard.NameKr, drawnCard.NameEn);

            await _uiManager.StreamReadingResultAsync(readingResult);
            _uiManager.SetResetButtonInteractable(true);
        }

        /// <summary>
        /// 점괘 표시가 끝난 뒤 리셋 버튼으로 입력 화면으로 돌아갑니다.
        /// </summary>
        private void OnResetButtonClicked()
        {
            if (_isProcessing) return;

            _cardAnimator.HideCard();
            _uiManager.ClearConcernInput();
            _uiManager.ResetUI();
        }

        /// <summary>
        /// AI 원본 응답에서 프롬프트 접두사와 [점괘] 태그를 제거하여 순수 결과만 추출합니다.
        /// </summary>
        private string ExtractReadingResult(string rawReply, string prompt)
        {
            string result = rawReply;

            if (result.StartsWith(prompt))
            {
                result = result.Substring(prompt.Length).TrimStart();
            }

            int readingIndex = result.IndexOf(ReadingSectionTag, System.StringComparison.Ordinal);
            if (readingIndex != -1)
            {
                result = result.Substring(readingIndex + ReadingSectionTag.Length).TrimStart();
            }

            if (string.IsNullOrWhiteSpace(result))
                return ReadingParseFailedMessage;

            result = result.Replace(". ", ".\n");

            return result;
        }
    }
}
