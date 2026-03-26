using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;

/// <summary>
/// On-Device AI(LLMUnity)를 사용하여 모델 추론을 실행하는 샘플 클래스입니다.
/// </summary>
public class Solution : MonoBehaviour
{
    // 매직 스트링 방지를 위한 상수 정의
    private const string ModelFileName = "gemma-3-1b-it-q4_0.gguf";
    private const string SystemPromptMessage = "You are a helpful and clever AI assistant. Answer the questions properly.";
    
    // 로깅용 역할 이름
    private const string AssistantRoleName = "AI";
    private const string UserRoleName = "User";

    private LLM _llm;
    private LLMAgent _llmAgent;

    private async void Start()
    {
        // 1. LLM 및 Agent 초기화
        InitializeAI();

        // 2. 모델 로드 대기 (앱 시작 시 모델 복사/로딩이 필요할 수 있음)
        await WaitUntilModelReadyAsync();

        // LLM 객체가 완전히 준비될 때까지 대기
        await _llm.WaitUntilReady();

        // 3. 추론 테스트 (비동기 전체 응답)
        await TestInferenceAsync("안녕! 네 이름은 뭐고 어떤 일을 할 수 있어?");
        
        // 4. (참고용) 스트리밍 방식 추론 테스트
        // TestInferenceStreaming("On-Device AI의 장점이 뭐야?");
    }

    /// <summary>
    /// LLM 및 LLMAgent 컴포넌트를 코드로 생성하고 초기화합니다.
    /// </summary>
    private void InitializeAI()
    {
        // Awake()가 즉시 호출되어 잘못 초기화되는 것을 방지하기 위해 일시적으로 비활성화
        gameObject.SetActive(false);

        // 1. LLM 컴포넌트 추가 및 설정 (LLM 코어 엔진 역할)
        _llm = gameObject.AddComponent<LLM>();
        _llm.model = ModelFileName; // StreamingAssets 폴더 내의 파일명
        _llm.numThreads = -1;       // 가용한 모든 CPU 스레드 사용
        _llm.numGPULayers = 32;     // 1B 모델이므로 대부분의 레이어를 GPU에 올려 가속 (환경에 맞게 조절)

        // 2. LLMAgent 컴포넌트 추가 및 설정 (프롬프트 및 채팅 관리 역할)
        _llmAgent = gameObject.AddComponent<LLMAgent>();
        _llmAgent.llm = _llm;
        _llmAgent.systemPrompt = SystemPromptMessage;
        
        // 채팅 히스토리 자동 저장 설정 (Application.persistentDataPath에 저장됨)
        _llmAgent.save = "MyAISaveData.json";

        // 설정 완료 후 오브젝트 활성화하여 엔진 초기화 진행
        gameObject.SetActive(true);
    }

    /// <summary>
    /// LLM 모델 셋업이 완료될 때까지 안전하게 대기합니다.
    /// </summary>
    private async Task WaitUntilModelReadyAsync()
    {
        Debug.Log("AI 모델 로딩 중...");
        await LLM.WaitUntilModelSetup();
        Debug.Log("AI 모델 로딩 완료!");
    }

    /// <summary>
    /// AI 모델에게 질문을 던지고 완성된 전체 응답을 비동기로 받아옵니다.
    /// </summary>
    /// <param name="message">AI에게 보낼 질문</param>
    private async Task TestInferenceAsync(string message)
    {
        Debug.Log($"[{UserRoleName}] {message}");
        
        // Chat 메서드는 전체 답변이 생성될 때까지 대기합니다.
        string reply = await _llmAgent.Chat(message);
        
        Debug.Log($"[{AssistantRoleName}] {reply}");
    }

    /// <summary>
    /// 답변이 한 글자씩 스트리밍 되어 생성될 때마다 이벤트를 받아 처리하는 방식입니다.
    /// </summary>
    /// <param name="message">AI에게 보낼 질문</param>
    private void TestInferenceStreaming(string message)
    {
        Debug.Log($"[{UserRoleName}] {message}");
        
        // C# Discards(_)를 사용하여 비동기 대기 없이 바로 실행시키고 콜백으로 처리합니다.
        _ = _llmAgent.Chat(
            message, 
            replySoFar => 
            {
                // 글자가 생성될 때마다 호출됨 (UI 텍스트 업데이트 등에 유용)
                Debug.Log($"[{AssistantRoleName} - Streaming] {replySoFar}");
            }, 
            () => 
            {
                // 답변 생성이 완전히 종료되었을 때 호출됨
                Debug.Log($"[{AssistantRoleName}] 스트리밍 응답 완료.");
            }
        );
    }
}