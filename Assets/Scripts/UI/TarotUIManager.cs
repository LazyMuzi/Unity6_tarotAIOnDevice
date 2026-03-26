using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Tarot.UI
{
    /// <summary>
    /// 타로 게임의 메인 UI 요소들을 관리하는 스크립트입니다.
    /// </summary>
    public class TarotUIManager : MonoBehaviour
    {
        [Header("Input UI")]
        [Tooltip("사용자가 고민을 입력하는 필드")]
        [SerializeField] private TMP_InputField _concernInputField;
        
        [Tooltip("타로 카드 뽑기를 실행하는 버튼")]
        [SerializeField] private Button _drawCardButton;

        [Header("Result UI")]
        [Tooltip("뽑힌 카드의 이름을 표시할 텍스트")]
        [SerializeField] private TextMeshProUGUI _cardNameText;
        
        [Tooltip("AI가 생성한 타로 리딩 결과를 스트리밍하여 보여줄 텍스트")]
        [SerializeField] private TextMeshProUGUI _readingResultText;

        /// <summary>
        /// 고민 입력창의 내용을 반환합니다.
        /// </summary>
        public string GetUserConcern()
        {
            return _concernInputField != null ? _concernInputField.text : string.Empty;
        }

        /// <summary>
        /// 뽑기 버튼 클릭 시 실행될 이벤트를 등록합니다.
        /// </summary>
        public void AddDrawButtonListener(UnityEngine.Events.UnityAction action)
        {
            if (_drawCardButton != null)
            {
                _drawCardButton.onClick.AddListener(action);
            }
        }

        /// <summary>
        /// 버튼의 활성화 상태를 변경합니다. (AI 추론 중 중복 클릭 방지)
        /// </summary>
        public void SetDrawButtonInteractable(bool interactable)
        {
            if (_drawCardButton != null)
            {
                _drawCardButton.interactable = interactable;
            }
        }

        /// <summary>
        /// 화면에 뽑힌 카드 이름을 업데이트합니다.
        /// </summary>
        public void UpdateCardNameUI(string nameKr, string nameEn)
        {
            if (_cardNameText != null)
            {
                _cardNameText.text = $"선택된 카드:\n{nameKr} ({nameEn})";
            }
        }

        /// <summary>
        /// 화면에 타로 리딩 결과를 업데이트합니다. (스트리밍 용도)
        /// </summary>
        public void UpdateReadingResultUI(string resultText)
        {
            if (_readingResultText != null)
            {
                _readingResultText.text = resultText;
            }
        }

        /// <summary>
        /// 초기 UI 상태로 리셋합니다.
        /// </summary>
        public void ResetUI()
        {
            UpdateCardNameUI("카드를 뽑아주세요", "...");
            UpdateReadingResultUI("당신의 고민을 입력하고, 버튼을 눌러 카드를 뽑아보세요.");
        }
    }
}