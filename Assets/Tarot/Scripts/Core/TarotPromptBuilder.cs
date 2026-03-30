using UnityEngine;
using Tarot.Data;

namespace Tarot.Core
{
    /// <summary>
    /// LLM 모델에게 전달할 타로 리딩 프롬프트를 생성하는 클래스입니다.
    /// 소형 로컬 모델에 맞춰 규칙을 최소화하고, 인라인 예시로 형식을 유도합니다.
    /// </summary>
    public class TarotPromptBuilder
    {
        private const string SystemPromptMessage =
            "당신은 타로 점술가예요. " +
            "사용자의 고민과 뽑힌 카드를 연결해서 점괘를 알려 줘요.\n" +
            "반드시 [생각]과 [점괘] 두 부분으로 나눠서 출력하세요.\n" +
            "[생각]에서 고민과 카드 키워드를 짧게 분석하고, " +
            "[점괘]에서 카드 이름을 꼭 언급하며 고민에 맞는 조언을 해요체로 2~3문장 써요.\n" +
            "한국어만, 이모지 없이, 텍스트만 써요.";

        private const string FewShotExample =
            "[예시]\n" +
            "고민: 오늘 저녁 메뉴 추천해줘\n" +
            "카드: 은둔자\n" +
            "키워드: 자기 성찰, 고독, 내면의 탐구\n" +
            "[생각]\n" +
            "저녁 메뉴 고민에 은둔자 카드이므로, 혼자 조용히 먹는 소박한 식사 쪽이 어울린다.\n" +
            "[점괘]\n" +
            "은둔자 카드가 나왔어요. " +
            "오늘은 집에서 혼자 조용히 밥 먹는 시간이 더 잘 어울릴지도 몰라요. " +
            "마음이 살짝 가벼워질 거예요.\n";

        /// <summary>
        /// 시스템 프롬프트를 반환합니다.
        /// </summary>
        public string GetSystemPrompt()
        {
            return SystemPromptMessage;
        }

        /// <summary>
        /// 사용자의 고민과 뽑힌 카드 데이터를 기반으로 AI에게 질문할 사용자 프롬프트를 생성합니다.
        /// 인라인 예시를 먼저 보여준 뒤 실제 질문을 배치하여 소형 모델의 형식 추종을 유도합니다.
        /// </summary>
        public string BuildUserPrompt(string userConcern, TarotCardData drawnCard)
        {
            string prompt =
                FewShotExample +
                "\n[실제 질문]\n" +
                $"고민: {userConcern}\n" +
                $"카드: {drawnCard.NameKr}\n" +
                $"키워드: {drawnCard.Keywords}\n" +
                $"카드 의미: {drawnCard.Meaning}\n" +
                "[생각]";

            return prompt;
        }
    }
}