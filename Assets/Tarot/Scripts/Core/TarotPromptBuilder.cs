using Tarot.Data;

namespace Tarot.Core
{
    /// <summary>
    /// 소형 온디바이스 모델용 — 시스템·유저 프롬프트를 짧고 고정된 형식으로만 전달합니다.
    /// </summary>
    public class TarotPromptBuilder
    {
        private const string SystemPromptMessage =
            "타로 점술가. 해요체 한국어.\n" +
            "답: 한 줄 [점괘] 다음 본문만. 고민:/분야:/카드: 같은 질문란은 답에 다시 쓰지 않음.\n" +
            "한 장: 오늘·지금 조언만 2~3문장.\n" +
            "스프레드: 본문에 [과거] [현재] [미래] 단어를 꼭 쓰고(대괄호 포함), 각 뒤에 해석 2~3문장. 세 구간 빠지면 안 됨. 이모지·굵게 없음.";

        /// <summary>
        /// 시스템 프롬프트를 반환합니다.
        /// </summary>
        public string GetSystemPrompt()
        {
            return SystemPromptMessage;
        }

        /// <summary>
        /// 데일리(한 장) 유저 메시지 — 라벨·값만 나열하고 [점괘]로 끝냅니다.
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
                $"카드:{drawnCard.NameKr}({dir}) 키워드:{drawnCard.Keywords}\n" +
                $"의미:{drawnCard.Meaning}\n" +
                "[점괘]";
        }

        /// <summary>
        /// 3장 스프레드 유저 메시지.
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
            string L(string slot, TarotCardData c, TarotCardOrientation o) =>
                $"{slot}:{c.NameKr}({TarotCardOrientationLabels.GetShortLabelKr(o)}) 키워드:{c.Keywords} 의미:{c.Meaning}";

            return
                $"분야:{area}\n" +
                $"고민:{userConcern}\n" +
                L("과거", pastCard, pastOrientation) + "\n" +
                L("현재", presentCard, presentOrientation) + "\n" +
                L("미래", futureCard, futureOrientation) + "\n" +
                "[점괘]\n" +
                "답 형식: [과거]...(줄바꿈)...[현재]...(줄바꿈)...[미래]...";
        }
    }
}
