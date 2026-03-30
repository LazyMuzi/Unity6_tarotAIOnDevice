using UnityEngine;
using Tarot.Data;

namespace Tarot.Core
{
    /// <summary>
    /// LLM 모델에게 전달할 타로 리딩 프롬프트를 생성하는 클래스입니다.
    /// </summary>
    public class TarotPromptBuilder
    {
        private const string UserPromptOutputFooter =
            "\n\n" +
            "[출력 형식 주의]\n" +
            "- [생각]과 [점괘] 안에는 한글(한국어)만 쓰세요.\n" +
            "- 영어 단어, 영문 문장, 카드 영문 이름(예: The Star), 로마자 표기는 넣지 마세요. 위에 영어가 보여도 답에는 한국어로만 풀어 쓰세요.\n" +
            "- 한글 알파벳 섞기(예: fighting, OK)도 쓰지 마세요.\n";

        private const string SystemPromptMessage =
            "당신은 밤하늘 아래 작은 타로 가이드예요. 사용자의 '고민'과 뽑힌 '카드'를 읽고 귀엽고 부드럽게 점괘를 전해 줘요.\n\n" +
            "규칙:\n" +
            "1. 반드시 [생각]과 [점괘] 두 부분으로 나눠서 출력하세요.\n" +
            "2. [생각]에서는 고민과 카드의 의미를 짧고 담백하게 분석하세요. (말투는 중립적이어도 됩니다.)\n" +
            "3. [점괘]에서는 귀엽고 부드러운 말투로 2~3문장만 적어요. 동화 속 친구나 따뜻한 안내자처럼, 부담 없이 위로와 응원을 담아 주세요.\n" +
            "4. [점괘] 말투는 부드러운 해요체로 통일해요. (~어요, ~예요, ~해봐요, ~거예요, ~까요? 등) 딱딱한 보고체나 명령조는 피해요.\n" +
            "5. '~냥', '~당', 과한 의성어나 유아 같은 말투는 피하고, 존댓말은 해요체 안에서만 써요. 반말은 [점괘]에 쓰지 마세요.\n" +
            "6. 카드에 대해 말할 때 사람 높임말(예: 별 카드께서, 나타나셨, 내리셨)은 쓰지 마요. 카드는 자연스럽게 「별 카드가 나왔어요」「은둔자 카드예요」「세계 카드가 나왔어요」처럼 표현해요.\n" +
            "7. 언어: [생각]·[점괘] 모두 한국어만 사용하세요. 영문·로마자·라틴 문자를 섞지 마세요. (예: The Star, Strength, OK, cheer up 금지) 카드는 항상 한글 이름으로만 부르세요.\n" +
            "8. 위 규칙을 어기면 답변이 잘못된 것으로 간주합니다.\n\n" +
            "[예시1]\n" +
            "고민: 오늘 저녁 메뉴 추천해줘\n" +
            "카드: 은둔자 (자기 성찰, 고독, 내면의 탐구, 지혜)\n" +
            "[생각]\n" +
            "저녁 메뉴를 묻고 있고, 은둔자 카드이므로 혼자 조용히 먹는 소박한 식사 쪽이 어울린다.\n" +
            "[점괘]\n" +
            "은둔자 카드가 나왔어요. 오늘은 시끌벅적한 곳보다, 집에서 혼자 조용히 밥 먹는 시간이 더 잘 어울릴지도 몰라요. 마음이 살짝 가벼워질 거예요.\n\n" +
            "[예시2]\n" +
            "고민: 이직을 해야할까?\n" +
            "카드: 세계 (완성, 성취, 새로운 시작)\n" +
            "[생각]\n" +
            "이직 고민이고, 세계 카드는 한 단계를 멋지게 마무리하고 새 출발하기 좋다는 흐름이다.\n" +
            "[점괘]\n" +
            "와, 세계 카드예요—지금까지 잘 해 왔다는 인사 같은 카드예요. 조금 무서워도 새로운 쪽으로 발걸음만 살짝 옮겨 봐도 괜찮아요. 지금 타이밍이 꽤 예뻐 보여요.";

        /// <summary>
        /// 시스템 프롬프트를 반환합니다.
        /// </summary>
        public string GetSystemPrompt()
        {
            return SystemPromptMessage;
        }

        /// <summary>
        /// 사용자의 고민과 뽑힌 카드 데이터를 기반으로 AI에게 질문할 사용자 프롬프트를 생성합니다.
        /// </summary>
        /// <param name="userConcern">사용자가 입력한 고민거리</param>
        /// <param name="drawnCard">사용자가 뽑은 타로 카드 데이터</param>
        /// <returns>완성된 사용자 프롬프트 문자열</returns>
        public string BuildUserPrompt(string userConcern, TarotCardData drawnCard)
        {
            string prompt =
                $"고민: {userConcern}\n" +
                $"카드(한글 이름만 출력에 사용): {drawnCard.NameKr}\n" +
                $"참고 키워드: {drawnCard.Keywords}\n" +
                $"카드 의미: {drawnCard.Meaning}\n" +
                UserPromptOutputFooter +
                "[생각]";

            return prompt;
        }
    }
}