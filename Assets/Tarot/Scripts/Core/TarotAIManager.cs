using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;
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

            _llmAgent.systemPrompt = _promptBuilder.GetSystemPrompt();
            
            // LLM 파라미터 튜닝: 반복 현상 방지 및 더 자연스러운 생성 유도
            // 너무 높으면 존대어/반말이 섞이므로 0.7에서 0.5로 하향 안정화
            _llmAgent.temperature = 0.5f;
            // repeatPenalty: 1.2로 유지 (반복 방지)
            _llmAgent.repeatPenalty = 1.2f;
            // topP: 0.95 유지
            _llmAgent.topP = 0.95f;

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
        public async Task<string> RequestTarotReadingAsync(string userPrompt)
        {
            if (!IsReady || _llmAgent == null)
            {
                Debug.LogError("[TarotAIManager] AI가 아직 준비되지 않았습니다!");
                return string.Empty;
            }

            Debug.Log("[TarotAIManager] AI 추론 요청 전송...");

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