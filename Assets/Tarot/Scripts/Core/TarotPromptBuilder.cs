using Tarot.Data;

namespace Tarot.Core
{
    /// <summary>
    /// 소형 온디바이스 모델(Gemma 3 1B)용 — 시스템·유저 프롬프트를 짧고 단순하게 전달합니다.
    /// 스프레드도 1장과 동일하게 "한 덩어리 본문"을 받도록 형식 제약을 최소화했습니다.
    /// </summary>
    public class TarotPromptBuilder
    {
        // 유저 프롬프트에 주입할 카드 키워드 최대 개수(쉼표 구분 기준).
        private const int MaxKeywordCount = 3;

        private const string DailySingleCardSystemPromptMessage =
            "타로 점술가. 해요체 한국어.\n" +
            "답: 한 줄 [점괘] 다음 본문만. 고민:/분야:/카드: 같은 질문란은 답에 다시 쓰지 않음.\n" +
            "카드가 (역)이면 그 의미가 약해지거나 지연·내면으로 흐른다는 뜻.\n" +
            "한 장: 오늘·지금 조언만 2~3문장. 이모지·굵게 없음.";

        // 1B 모델 대응: 규칙 7개 + 예시 블록을 제거하고, 1장 프롬프트 수준으로 단순화.
        // 과거/현재/미래는 "강제 라벨"이 아니라 "흐름 순서"로만 안내한다.
        private const string SpreadPastPresentFutureSystemPromptMessage =
            "타로 점술가. 해요체 한국어로만 답해요.\n" +
            "[점괘] 뒤에 해석만 써요. 분야·고민·카드처럼 입력으로 받은 줄은 답에 다시 적지 않아요.\n" +
            "카드 뒤 (역)은 그 키워드가 약해지거나 지연·내면으로 흐른다는 뜻이에요.\n" +
            "과거 흐름 한 문장, 지금 상황 한 문장, 앞으로의 조언 한 문장. 딱 3문장만 써요.\n" +
            "막연한 비유나 신비로운 표현은 피하고, 고민에 바로 와닿는 담백하고 구체적인 말로 써요.\n" +
            "고민을 다시 설명하지 말고 바로 시작해요. '본문:', '조언:', '결론:' 같은 머리말이나 라벨, 이모지·굵게·목록 없이 짧은 문장으로만 써요.";

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
        /// 데일리(한 장) 유저 메시지. (기존 동작 유지)
        /// </summary>
        public string BuildUserPrompt(
            TarotConcernCategory category,
            string userConcern,
            TarotCardData drawnCard,
            TarotCardOrientation orientation)
        {
            string area = TarotConcernCategoryLabels.GetPromptLineKr(category);
            string dir = TarotCardOrientationLabels.GetShortLabelKr(orientation);

            return
                $"분야:{area}\n" +
                $"고민:{userConcern}\n" +
                $"카드:{drawnCard.NameKr}({dir})\n" +
                "[점괘]";
        }

        /// <summary>
        /// 3장 스프레드 유저 메시지. 카드명·방향에 더해 핵심 키워드를 짧게 주입해
        /// 1B 모델이 실제 뽑힌 카드에 근거해 해석하도록 돕습니다.
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
            string area = TarotConcernCategoryLabels.GetPromptLineKr(category);

            string L(string slot, TarotCardData c, TarotCardOrientation o)
            {
                string dir = TarotCardOrientationLabels.GetShortLabelKr(o);
                string kw = ShortKeywords(c.Keywords);
                return string.IsNullOrEmpty(kw)
                    ? $"{slot}:{c.NameKr}({dir})"
                    : $"{slot}:{c.NameKr}({dir}) {kw}";
            }

            return
                $"분야:{area}\n" +
                $"고민:{userConcern}\n" +
                L("카드1", pastCard, pastOrientation) + "\n" +
                L("카드2", presentCard, presentOrientation) + "\n" +
                L("카드3", futureCard, futureOrientation) + "\n" +
                "[점괘]";
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