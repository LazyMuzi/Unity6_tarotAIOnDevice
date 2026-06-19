using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;
using Tarot.Data;
using Tarot.UI;

namespace Tarot.Core
{
    /// <summary>
    /// LLMUnity를 통해 온디바이스 AI 추론을 담당하는 매니저 클래스입니다.
    /// </summary>
    public class TarotAIManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("엔진 초기화가 끝날 때까지 게임플레이 UI를 숨깁니다.")]
        [SerializeField] private TarotUIManager _uiManager;

        [SerializeField] private LLM _llm;
        [SerializeField] private LLMAgent _llmAgent;
        private TarotPromptBuilder _promptBuilder;

        // 출력 토큰 상한(numPredict): 1B 모델의 장황함을 하드 컷으로 막습니다.
        // 한국어 기준 데일리 2~3문장 / 스프레드 3문장에 맞춘 여유값(필요 시 조절).
        private const int DailyReadingMaxTokens = 160;
        private const int SpreadReadingMaxTokens = 200;

        public bool IsReady { get; private set; } = false;

        private async void Start()
        {
            _promptBuilder = new TarotPromptBuilder();
            await InitializeAIAsync();
        }

        /// <summary>
        /// LLM 및 LLMAgent를 초기화하고 모델이 준비될 때까지 대기합니다.
        /// </summary>
        private async Task InitializeAIAsync()
        {
            Debug.Log("[TarotAIManager] AI 엔진 초기화 시작...");

            if (_uiManager == null)
                Debug.LogWarning("[TarotAIManager] TarotUIManager 참조가 없습니다. 초기화 중 UI를 숨기지 못합니다.");

            _uiManager?.ShowEnginePreparingUi();

            // 엔진 셋업 중 잘못된 호출 방지
            gameObject.SetActive(false);

            _llmAgent.systemPrompt = _promptBuilder.GetSystemPrompt(TarotReadingMode.DailySingleCard);
            
            // LLM 파라미터 튜닝: 장황함·뜬구름 완화
            // temperature: 낮출수록 담백·일관(존대/반말 섞임도 함께 줄어듦)
            _llmAgent.temperature = 0.35f;
            // repeatPenalty: 1.2는 반복을 피하려 새 주제를 끌어와 답이 길어지는 부작용이 있어 1.1로 완화
            _llmAgent.repeatPenalty = 1.1f;
            // topP: 0.9로 약간 좁혀 표현이 흩어지는 것을 줄임
            _llmAgent.topP = 0.9f;

            gameObject.SetActive(true);

            // 모델 복사/다운로드 및 서버 로딩 대기
            Debug.Log("[TarotAIManager] 모델 파일 로딩(또는 다운로드) 대기 중...");
            await LLM.WaitUntilModelSetup(progress =>
            {
                _uiManager?.UpdateModelSetupProgress(progress);
            });
            
            Debug.Log("[TarotAIManager] 내부 AI 서버 구동 대기 중...");
            await _llm.WaitUntilReady();

            IsReady = true;
            Debug.Log("[TarotAIManager] AI 엔진 초기화 완료! 타로 리딩 준비 됨.");

            _uiManager?.SetGameplayUiVisible(true);
            _uiManager?.ResetUI();
        }

        /// <summary>
        /// 완성된 사용자 프롬프트를 전송하고 AI 추론이 완료될 때까지 대기한 뒤 전체 결과를 반환합니다.
        /// </summary>
        /// <param name="userPrompt">조합이 완료된 사용자 질문 프롬프트</param>
        /// <returns>AI가 생성한 전체 답변 문자열</returns>
        public async Task<string> RequestTarotReadingAsync(string userPrompt, TarotReadingMode mode)
        {
            if (!IsReady || _llmAgent == null)
            {
                Debug.LogError("[TarotAIManager] AI가 아직 준비되지 않았습니다!");
                return string.Empty;
            }

            Debug.Log("[TarotAIManager] AI 추론 요청 전송...");

            _llmAgent.systemPrompt = _promptBuilder.GetSystemPrompt(mode);
            _llmAgent.numPredict = mode == TarotReadingMode.SpreadPastPresentFuture
                ? SpreadReadingMaxTokens
                : DailyReadingMaxTokens;
            await _llmAgent.ClearHistory();

            var tcs = new TaskCompletionSource<string>();
            string lastReply = string.Empty;

            _ = _llmAgent.Chat(
                userPrompt,
                replySoFar =>
                {
                    lastReply = replySoFar;
                },
                () =>
                {
                    Debug.Log("[TarotAIManager] AI 추론 완료.");
                    tcs.TrySetResult(lastReply);
                }
            );

            return await tcs.Task;
        }
    }
}