using UnityEngine;
using Tarot.Core;
using Tarot.Data;
using Tarot.UI;

namespace Tarot
{
    /// <summary>
    /// 타로 게임의 전체 흐름을 제어하는 게임 매니저 클래스입니다.
    /// </summary>
    public class TarotGameManager : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private TarotAIManager _aiManager;
        [SerializeField] private TarotUIManager _uiManager;

        private TarotDeckController _deckController;
        private TarotPromptBuilder _promptBuilder;

        private void Awake()
        {
            // 의존성 주입 및 초기화
            _deckController = new TarotDeckController();
            _promptBuilder = new TarotPromptBuilder();
        }

        private void Start()
        {
            _uiManager.ResetUI();
            _uiManager.AddDrawButtonListener(OnDrawButtonClicked);
        }

        /// <summary>
        /// 사용자가 타로 뽑기 버튼을 클릭했을 때의 메인 흐름을 실행합니다.
        /// </summary>
        private void OnDrawButtonClicked()
        {
            // 1. AI 준비 상태 체크
            if (_aiManager == null || !_aiManager.IsReady)
            {
                _uiManager.UpdateReadingResultUI("AI 엔진이 아직 준비되지 않았습니다. 잠시만 기다려주세요.");
                return;
            }

            // 2. 고민 내용 가져오기 및 유효성 검사
            string userConcern = _uiManager.GetUserConcern();
            if (string.IsNullOrWhiteSpace(userConcern))
            {
                _uiManager.UpdateReadingResultUI("먼저 고민거리를 입력해 주세요!");
                return;
            }

            // UI 상태 변경 (중복 클릭 방지)
            _uiManager.SetDrawButtonInteractable(false);
            _uiManager.UpdateReadingResultUI("카드를 섞고 운명의 메시지를 읽는 중입니다...");

            // 3. 타로 카드 뽑기
            TarotCardData drawnCard = _deckController.DrawRandomCard();
            _uiManager.UpdateCardNameUI(drawnCard.NameKr, drawnCard.NameEn);

            // 4. AI에게 전달할 프롬프트 조합
            string prompt = _promptBuilder.BuildUserPrompt(userConcern, drawnCard);
            Debug.Log($"[TarotGameManager] 생성된 유저 프롬프트:\n{prompt}");

            // 5. AI 추론 요청 (스트리밍)
            _aiManager.RequestTarotReadingStreaming(
                userPrompt: prompt,
                onReplyStreaming: (replySoFar) => 
                {
                    // AI의 답변이 한 글자씩 생성될 때마다 UI 업데이트
                    // LLMUnity는 기본적으로 User Prompt를 결과 문자열에 포함시켜서 반환합니다.
                    // 따라서 우리가 보낸 prompt를 잘라내고, 순수 AI의 답변만 화면에 표시해야 합니다.
                    string pureReply = replySoFar;
                    if (pureReply.StartsWith(prompt))
                    {
                        pureReply = pureReply.Substring(prompt.Length).TrimStart();
                    }
                    
                    int readingIndex = pureReply.IndexOf("[점괘]");
                    if (readingIndex != -1)
                    {
                        pureReply = pureReply.Substring(readingIndex + "[점괘]".Length).TrimStart();
                    }
                    else
                    {
                        pureReply = "카드의 기운을 읽고 있습니다...";
                    }

                    _uiManager.UpdateReadingResultUI(pureReply);
                },
                onReplyComplete: () => 
                {
                    // 추론이 끝나면 다시 버튼 활성화
                    _uiManager.SetDrawButtonInteractable(true);
                }
            );
        }
    }
}