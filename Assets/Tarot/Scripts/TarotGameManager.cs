using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;
using Tarot.Core;
using Tarot.Data;
using Tarot.UI;

namespace Tarot
{
    /// <summary>
    /// 타로 게임의 전체 흐름을 제어하는 게임 매니저 클래스입니다.
    /// 플로우: 카테고리 → 리딩 종류(데일리/스프레드) → 고민 입력 → (데일리: 2장 중 1장·1장 리딩 / 스프레드: 가운데 3장 배치 → AI → 동시 뒤집기·점괘) → 결과
    /// </summary>
    public class TarotGameManager : MonoBehaviour
    {
        private const string ShufflingDeckMessage = "카드를 섞고 있어요...";
        private const string PickOneOfTwoCardsMessage = "마음에 드는 카드 한 장을 골라 주세요.";
        private const string ReadingInProgressMessage = "선택한 카드로 점괘를 읽는 중입니다...";
        private const int ShufflePresentationDelayMs = 900;
        private const string AiNotReadyMessage = "별이 아직 자리를 잡는 중이에요.\n 조금만 있다가 다시 시도해 주세요.";
        private const string EmptyConcernMessage = "먼저 고민거리를 입력해 주세요!";
        private const string CategoryNotSelectedMessage = "먼저 고민 주제를 골라 주세요!";
        private const string ReadingModeNotSelectedMessage = "먼저 리딩 종류를 골라 주세요!";
        private const string ReadingParseFailedMessage = "카드의 메시지를 해석하지 못했습니다.\n 다시 시도해 주세요.";
        private const string DeckShuffleFailedMessage = "카드를 준비하지 못했습니다.\n다시 시도해 주세요.";
        private const string ReadingSectionTag = "[점괘]";
        private const string ReadingOpeningFormat = "{0}카드가 나왔어요";
        private const string ReadingOpeningReversedFormat = "{0}카드가 역방향으로 나왔어요";
        private const string ReadingOpeningBodySeparator = "\n\n";
        /// <summary>이모지·특수 기호만 걷어낼 때 사용(한 패턴만 유지).</summary>
        private static readonly Regex SymbolAndEmojiPattern = new Regex(
            @"[\p{Cs}\p{So}\p{Cf}]",
            RegexOptions.Compiled);

        /// <summary>모델이 질문란을 반복한 줄(고민: …)을 건너뛸 때 라벨 앞부분만 비교합니다.</summary>
        /// <summary>질문란 에코 줄 제거. "의미"는 스프레드 해석 본문에 자주 쓰이므로 넣지 않습니다.</summary>
        private static readonly string[] EchoLineLabelPrefixes =
        {
            "고민", "분야", "카드", "키워드", "해석 지침", "방향", "리딩 종류", "카드 의미", "선택된 카드", "슬롯"
        };

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
            _uiManager.SetReadingResultScrollActive(false);
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
                _uiManager.SetReadingResultScrollActive(true);
                _uiManager.UpdateReadingResultUI(AiNotReadyMessage);
                return;
            }

            if (!_uiManager.TryGetSelectedCategory(out TarotConcernCategory concernCategory))
            {
                _uiManager.UpdateDefaultUIText(CategoryNotSelectedMessage);
                return;
            }

            if (!_uiManager.TryGetSelectedReadingMode(out TarotReadingMode readingMode))
            {
                _uiManager.UpdateDefaultUIText(ReadingModeNotSelectedMessage);
                return;
            }

            string userConcern = _uiManager.GetUserConcern();
            if (string.IsNullOrWhiteSpace(userConcern))
            {
                _uiManager.UpdateDefaultUIText(EmptyConcernMessage);
                return;
            }

            _isProcessing = true;
            _uiManager.SetReadingResultScrollActive(true);
            _uiManager.SetDrawButtonInteractable(false);
            _uiManager.SetResetButtonInteractable(false);
            _uiManager.HideInputUI();

            try
            {
                await ExecuteTarotReadingFlowAsync(concernCategory, readingMode, userConcern);
            }
            finally
            {
                _uiManager.SetDrawButtonInteractable(true);
                _isProcessing = false;
            }
        }

        private async Task ExecuteTarotReadingFlowAsync(
            TarotConcernCategory concernCategory,
            TarotReadingMode readingMode,
            string userConcern)
        {
            if (readingMode == TarotReadingMode.SpreadPastPresentFuture)
            {
                await ExecuteSpreadPastPresentFutureFlowAsync(concernCategory, userConcern);
                return;
            }

            await ExecuteDailySingleCardFlowAsync(concernCategory, userConcern);
        }

        private async Task ExecuteDailySingleCardFlowAsync(TarotConcernCategory concernCategory, string userConcern)
        {
            BeginReadingFlowPresentation();

            await Task.Delay(ShufflePresentationDelayMs);

            TarotCardData[] pair = _deckController.ShuffleAndDrawTwoCards();
            if (pair == null || pair.Length < 2)
            {
                _uiManager.UpdateDefaultUIText(DeckShuffleFailedMessage);
                _uiManager.RestoreConcernInputAfterInterruptedFlow();
                return;
            }

            _uiManager.UpdateDefaultUIText(PickOneOfTwoCardsMessage);
            Sprite cardBack = _resourceLoader.GetCardBackSprite();
            TarotCardData drawnCard = await _uiManager.WaitForTwoCardSelectionAsync(pair[0], pair[1], cardBack);

            TarotCardOrientation orientation = RandomCardOrientation();

            _uiManager.UpdateDefaultUIText(ReadingInProgressMessage);

            string prompt = _promptBuilder.BuildUserPrompt(concernCategory, userConcern, drawnCard, orientation);
            Debug.Log($"[TarotGameManager] 생성된 유저 프롬프트:\n{prompt}");

            string rawReply = await _aiManager.RequestTarotReadingAsync(prompt);
            string readingResult = ExtractReadingResult(rawReply, prompt);
            readingResult = StripPastFutureSectionsForDailyReading(readingResult);
            readingResult = BuildReadingDisplayText(drawnCard.NameKr, readingResult, orientation);

            Sprite frontSprite = _resourceLoader.GetCardFrontSprite(drawnCard.Id);
            _uiManager.HideDefaultUI();
            _uiManager.ShowCardUiArea();
            await _cardAnimator.ShowCardAsync(frontSprite, orientation);

            _uiManager.UpdateCardNameUI(drawnCard.NameKr, drawnCard.NameEn, orientation);

            await _uiManager.StreamReadingResultAsync(readingResult);
            _uiManager.SetResetButtonInteractable(true);
        }

        private async Task ExecuteSpreadPastPresentFutureFlowAsync(
            TarotConcernCategory concernCategory,
            string userConcern)
        {
            BeginReadingFlowPresentation();

            await Task.Delay(ShufflePresentationDelayMs);

            TarotCardData[] three = _deckController.ShuffleAndDrawThreeCards();
            if (three == null || three.Length < 3)
            {
                _uiManager.UpdateDefaultUIText(DeckShuffleFailedMessage);
                _uiManager.RestoreConcernInputAfterInterruptedFlow();
                return;
            }

            _uiManager.ShowCardUiArea();

            int[] deckIndexForSlot = await _uiManager.WaitForSpreadCenterPlacementAsync(
                _resourceLoader.GetCardBackSprite(),
                three);

            TarotCardData pastCard = three[deckIndexForSlot[0]];
            TarotCardData presentCard = three[deckIndexForSlot[1]];
            TarotCardData futureCard = three[deckIndexForSlot[2]];

            TarotCardOrientation oPast = RandomCardOrientation();
            TarotCardOrientation oPresent = RandomCardOrientation();
            TarotCardOrientation oFuture = RandomCardOrientation();

            _uiManager.UpdateDefaultUIText(ReadingInProgressMessage);

            string prompt = _promptBuilder.BuildSpreadThreeCardPrompt(
                concernCategory,
                userConcern,
                pastCard,
                oPast,
                presentCard,
                oPresent,
                futureCard,
                oFuture);
            Debug.Log($"[TarotGameManager] 스프레드 유저 프롬프트:\n{prompt}");

            string rawReply = await _aiManager.RequestTarotReadingAsync(prompt);
            string readingResult = ExtractReadingResult(rawReply, prompt);
            string[] spreadSections = TarotSpreadReadingParser.SplitSections(readingResult);

            var fronts = new Sprite[3];
            fronts[0] = _resourceLoader.GetCardFrontSprite(pastCard.Id);
            fronts[1] = _resourceLoader.GetCardFrontSprite(presentCard.Id);
            fronts[2] = _resourceLoader.GetCardFrontSprite(futureCard.Id);

            var orientations = new[] { oPast, oPresent, oFuture };
            TarotCardData[] cardsBySlot = { pastCard, presentCard, futureCard };

            _uiManager.HideDefaultUI();
            _uiManager.ShowCardUiArea();
            _uiManager.BringReadingResultAreaToFront();

            await _uiManager.RunSpreadFlipAllAndShowResultAsync(
                fronts,
                orientations,
                spreadSections,
                string.Empty,
                cardsBySlot);

            _uiManager.SetResetButtonInteractable(true);
        }

        /// <summary>
        /// 점괘 표시가 끝난 뒤 리셋 버튼으로 입력 화면으로 돌아갑니다.
        /// </summary>
        private void OnResetButtonClicked()
        {
            if (_isProcessing) return;

            _cardAnimator.HideCard();
            _uiManager.HideSpreadThreeCardUi();
            _uiManager.ClearConcernInput();
            _uiManager.ResetUI();
        }

        private void BeginReadingFlowPresentation()
        {
            _uiManager.HideCardUiArea();
            _cardAnimator.HideCard();
            _uiManager.HideTwoCardChoiceUi();
            _uiManager.HideSpreadThreeCardUi();
            _uiManager.UpdateReadingResultUI(string.Empty);
            _uiManager.UpdateDefaultUIText(ShufflingDeckMessage);
        }

        private static TarotCardOrientation RandomCardOrientation()
        {
            return Random.value < 0.5f ? TarotCardOrientation.Upright : TarotCardOrientation.Reversed;
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

            int readingIndex = result.LastIndexOf(ReadingSectionTag, StringComparison.Ordinal);
            if (readingIndex != -1)
            {
                result = result.Substring(readingIndex + ReadingSectionTag.Length).TrimStart();
            }

            result = StripStandaloneMetaTagLines(result);
            result = StripEchoLabelLines(result);

            if (string.IsNullOrWhiteSpace(result))
                return ReadingParseFailedMessage;

            result = SanitizeReadingText(result);
            result = result.Replace(". ", ".\n");

            return result;
        }

        /// <summary>단독 줄 [생각]·[점괘]만 제거. 스프레드 구역 [과거] 본문은 유지.</summary>
        private static string StripStandaloneMetaTagLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var kept = new List<string>(lines.Length);
            foreach (string line in lines)
            {
                string t = TrimLeadingStars(line.Trim());
                if (t == "[생각]" || t == "[점괘]")
                    continue;
                kept.Add(line);
            }

            return string.Join("\n", kept).Trim();
        }

        /// <summary>질문란 라벨로 시작하는 줄(고민: …) 제거 — Regex 없이 앞부분만 비교.</summary>
        private static string StripEchoLabelLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var kept = new List<string>(lines.Length);
            foreach (string line in lines)
            {
                if (IsEchoLabelLine(line))
                    continue;
                kept.Add(line);
            }

            return string.Join("\n", kept).Trim();
        }

        private static string TrimLeadingStars(string t)
        {
            while (t.Length > 0 && t[0] == '*')
                t = t.Substring(1).TrimStart();
            return t;
        }

        private static bool IsEchoLabelLine(string line)
        {
            string t = TrimLeadingStars(line.Trim());
            int sep = t.IndexOf(':');
            if (sep < 0)
                sep = t.IndexOf('：');
            if (sep <= 0)
                return false;

            string label = t.Substring(0, sep).Trim();
            foreach (string prefix in EchoLineLabelPrefixes)
            {
                if (label.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>데일리 한 장에서 과거·미래 축 줄과 본문 앞 "현재:"만 정리합니다.</summary>
        private static string StripPastFutureSectionsForDailyReading(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var kept = new List<string>(lines.Length);

            foreach (string line in lines)
            {
                string t = TrimLeadingStars(line.Trim());
                if (t.StartsWith("[과거]", StringComparison.Ordinal) || t.StartsWith("[미래]", StringComparison.Ordinal))
                    continue;
                if (IsPastOrFutureColonHeaderLine(t))
                    continue;
                kept.Add(line);
            }

            string joined = string.Join("\n", kept).Trim();
            joined = StripLeadingPresentLabel(joined);
            return joined.Trim();
        }

        private static bool IsPastOrFutureColonHeaderLine(string t)
        {
            if (t.StartsWith("과거", StringComparison.Ordinal))
                return HasColonAfterAxisLabel(t, 2);
            if (t.StartsWith("미래", StringComparison.Ordinal))
                return HasColonAfterAxisLabel(t, 2);
            return false;
        }

        private static bool HasColonAfterAxisLabel(string t, int labelCharLen)
        {
            int i = labelCharLen;
            while (i < t.Length && char.IsWhiteSpace(t[i]))
                i++;
            return i < t.Length && (t[i] == ':' || t[i] == '：');
        }

        private static string StripLeadingPresentLabel(string s)
        {
            s = s.TrimStart();
            if (!s.StartsWith("현재", StringComparison.Ordinal))
                return s;
            int i = 2;
            while (i < s.Length && char.IsWhiteSpace(s[i]))
                i++;
            if (i >= s.Length || (s[i] != ':' && s[i] != '：'))
                return s;
            i++;
            while (i < s.Length && char.IsWhiteSpace(s[i]))
                i++;
            return i >= s.Length ? string.Empty : s.Substring(i);
        }

        /// <summary>
        /// 모델이 규칙을 무시하고 섞어 넣는 마크다운·이모지·특수문자를 제거합니다.
        /// \p{Cs}(서로게이트)·\p{So}(기호)·\p{Cf}(ZWJ 등 포맷) 유니코드 카테고리로 필터링합니다.
        /// </summary>
        private static string SanitizeReadingText(string text)
        {
            foreach (char c in "*#_~`")
                text = text.Replace(c.ToString(), "");

            text = SymbolAndEmojiPattern.Replace(text, "");

            while (text.Contains("  "))
                text = text.Replace("  ", " ");

            return text.Trim();
        }

        /// <summary>
        /// 점괘 본문 앞에 고정 첫 문장(한글 카드명 + "카드가 나왔어요")과 빈 줄 두 줄(\n\n)을 붙입니다.
        /// </summary>
        private static string BuildReadingDisplayText(
            string cardNameKr,
            string readingBody,
            TarotCardOrientation orientation)
        {
            if (string.IsNullOrWhiteSpace(readingBody))
                return readingBody;

            string name = cardNameKr?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
                return readingBody;

            string opening = orientation == TarotCardOrientation.Reversed
                ? string.Format(ReadingOpeningReversedFormat, name)
                : string.Format(ReadingOpeningFormat, name);
            return opening + ReadingOpeningBodySeparator + readingBody;
        }

    }
}
