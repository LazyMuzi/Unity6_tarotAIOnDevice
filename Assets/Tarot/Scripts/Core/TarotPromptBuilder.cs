using Tarot.Data;

namespace Tarot.Core
{
    /// <summary>
    /// 소형 온디바이스 모델(Gemma 3 1B/4B)용 프롬프트.
    /// 원칙: 금지문("~하지 마세요")은 오히려 그 단어를 유도하므로 쓰지 않고,
    /// 원하는 출력 형식을 라벨 템플릿 + 예시 1개로 그대로 보여 준다. 라벨은 코드에서 벗긴다.
    /// 고민은 유저 메시지 맨 끝에 두고 마지막 줄에서 한 번 더 인용해, 모델이 카드 뜻이나
    /// 분야 설명("선택, 결정")으로 흘러가지 않고 그 고민 하나만 다루게 한다.
    /// </summary>
    public class TarotPromptBuilder
    {
        // 유저 프롬프트에 주입할 카드 키워드 최대 개수(쉼표 구분 기준).
        private const int MaxKeywordCount = 3;

        private const string PersonaLines =
            "당신은 다정한 타로 점술가예요. 모든 문장은 '~요'로 끝나는 한국어 해요체로 써요.\n" +
            "역방향 카드는 그 뜻이 약해지거나 늦어진다고 봐요.\n";

        private const string DailySingleCardSystemPromptMessage =
            PersonaLines +
            "아래 형식으로 정확히 두 줄만 써요. 각 줄은 짧은 한 문장이에요.\n" +
            "지금: 카드로 본 지금 상황\n" +
            "조언: 오늘 해 보면 좋은 일\n" +
            "두 줄 모두 고민에 나온 말을 그대로 써서, 그 고민 하나만 이야기해요.\n" +
            "예시 (고민이 '이직할까 고민돼요'일 때)\n" +
            "지금: 이직 쪽으로 마음이 기울어 있지만 확신은 아직 없어요.\n" +
            "조언: 오늘은 회사 조건을 하나만 비교해 보세요.";

        // "답:" 줄을 먼저 쓰게 하면 모델이 고민을 먼저 붙잡아 뒤 세 줄도 그 고민을 벗어나지 않는다.
        private const string SpreadPastPresentFutureSystemPromptMessage =
            PersonaLines +
            "아래 형식으로 정확히 네 줄만 써요. 각 줄은 짧은 한 문장이에요.\n" +
            "답: 고민에 대한 직접적인 한 줄 답\n" +
            "과거: 카드1로 본 지난 흐름\n" +
            "현재: 카드2로 본 지금 상황\n" +
            "미래: 카드3으로 본 앞으로의 조언\n" +
            "네 줄 모두 고민에 나온 말을 그대로 써서, 그 고민 하나만 이야기해요.\n" +
            "예시 (고민이 '이직할까 고민돼요'일 때)\n" +
            "답: 지금은 이직 준비를 계속하되 결정은 조금 미뤄도 좋아요.\n" +
            "과거: 익숙한 회사에 머물며 이직을 미뤄 왔어요.\n" +
            "현재: 지금은 이직 쪽으로 마음이 기울고 있어요.\n" +
            "미래: 서두르지 말고 회사 조건을 비교해 보면 좋아요.";

        /// <summary>
        /// 시스템 프롬프트를 반환합니다.
        /// </summary>
        public string GetSystemPrompt(TarotReadingMode mode)
        {
            return mode switch
            {
                TarotReadingMode.SpreadPastPresentFuture => SpreadPastPresentFutureSystemPromptMessage,
                _ => DailySingleCardSystemPromptMessage
            };
        }

        /// <summary>
        /// 데일리(한 장) 유저 메시지. 분야 설명은 넣지 않고(추상어 유도), 고민을 끝에 두고 한 번 더 인용합니다.
        /// </summary>
        public string BuildUserPrompt(
            TarotConcernCategory category,
            string userConcern,
            TarotCardData drawnCard,
            TarotCardOrientation orientation)
        {
            return
                $"카드 = {CardLine(drawnCard, orientation)}\n" +
                $"고민 = {userConcern}\n" +
                $"'{userConcern}' 이 고민에 대해 형식대로 두 줄, 해요체로 답해요.";
        }

        /// <summary>
        /// 3장 스프레드 유저 메시지. 카드 줄은 "라벨 = 값" 꼴로 써서
        /// 모델이 답 형식("과거: …")과 혼동해 그대로 베끼지 않게 합니다.
        /// </summary>
        public string BuildSpreadThreeCardPrompt(
            TarotConcernCategory category,
            string userConcern,
            TarotCardData pastCard,
            TarotCardOrientation pastOrientation,
            TarotCardData presentCard,
            TarotCardOrientation presentOrientation,
            TarotCardData futureCard,
            TarotCardOrientation futureOrientation)
        {
            return
                $"카드1 과거 = {CardLine(pastCard, pastOrientation)}\n" +
                $"카드2 현재 = {CardLine(presentCard, presentOrientation)}\n" +
                $"카드3 미래 = {CardLine(futureCard, futureOrientation)}\n" +
                $"고민 = {userConcern}\n" +
                $"'{userConcern}' 이 고민에 대해 형식대로 네 줄, 해요체로 답해요.";
        }

        private static string CardLine(TarotCardData card, TarotCardOrientation orientation)
        {
            string dir = TarotCardOrientationLabels.GetShortLabelKr(orientation);
            string kw = ShortKeywords(card.Keywords);
            return string.IsNullOrEmpty(kw)
                ? $"{card.NameKr} ({dir})"
                : $"{card.NameKr} ({dir}) {kw}";
        }

        /// <summary>
        /// 카드 Keywords에서 앞쪽 핵심 몇 개만 잘라 짧게 전달합니다(토큰 절약).
        /// 쉼표 구분을 우선 사용하고, 쉼표가 없으면 문자열 전체를 그대로 사용합니다.
        /// </summary>
        private static string ShortKeywords(string keywords)
        {
            if (string.IsNullOrWhiteSpace(keywords))
                return string.Empty;

            string[] parts = keywords.Split(',');
            int take = parts.Length < MaxKeywordCount ? parts.Length : MaxKeywordCount;

            var head = new string[take];
            for (int i = 0; i < take; i++)
                head[i] = parts[i].Trim();

            return string.Join(", ", head);
        }
    }
}
