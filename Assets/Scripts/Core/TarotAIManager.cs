using System;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;

namespace Tarot.Core
{
    /// <summary>
    /// LLMUnity를 통해 온디바이스 AI 추론을 담당하는 매니저 클래스입니다.
    /// </summary>
    public class TarotAIManager : MonoBehaviour
    {
        private LLM _llm;
        private LLMAgent _llmAgent;
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
            
            // 엔진 셋업 중 잘못된 호출 방지
            gameObject.SetActive(false);

            _llm = GetComponent<LLM>();
            _llmAgent = GetComponent<LLMAgent>();
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
                // progress는 0.0 ~ 1.0 사이의 값입니다.
                string progressPercent = ((int)(progress * 100)).ToString() + "%";
                Debug.Log($"[TarotAIManager] 모델 다운로드 진행률: {progressPercent}");
                // TODO: 필요하다면 UI 프로그레스 바 등에 progress 값을 전달하세요.
            });
            
            Debug.Log("[TarotAIManager] 내부 AI 서버 구동 대기 중...");
            await _llm.WaitUntilReady();

            IsReady = true;
            Debug.Log("[TarotAIManager] AI 엔진 초기화 완료! 타로 리딩 준비 됨.");
        }

        /// <summary>
        /// 완성된 사용자 프롬프트를 전송하고 AI의 답변을 스트리밍 방식으로 받아옵니다.
        /// </summary>
        /// <param name="userPrompt">조합이 완료된 사용자 질문 프롬프트</param>
        /// <param name="onReplyStreaming">글자가 생성될 때마다 호출될 콜백 (UI 텍스트 업데이트용)</param>
        /// <param name="onReplyComplete">답변 생성이 모두 끝났을 때 호출될 콜백</param>
        public async void RequestTarotReadingStreaming(string userPrompt, Action<string> onReplyStreaming, Action onReplyComplete)
        {
            if (!IsReady || _llmAgent == null)
            {
                Debug.LogError("[TarotAIManager] AI가 아직 준비되지 않았습니다!");
                onReplyComplete?.Invoke();
                return;
            }

            Debug.Log("[TarotAIManager] AI 추론 요청 전송...");
            
            // 이전 컨텍스트가 섞이지 않도록(매번 새로운 타로점) 히스토리 초기화 (await로 완벽히 비움)
            await _llmAgent.ClearHistory();

            // 추론 실행
            _ = _llmAgent.Chat(
                userPrompt, 
                replySoFar => 
                {
                    // 스트리밍 업데이트
                    onReplyStreaming?.Invoke(replySoFar);
                }, 
                () => 
                {
                    // 추론 완료
                    Debug.Log("[TarotAIManager] AI 추론 완료.");
                    onReplyComplete?.Invoke();
                }
            );
        }
    }
}