using UnityEngine;

namespace Tarot.Data
{
    /// <summary>
    /// 개별 타로 카드의 데이터를 정의하는 구조체입니다.
    /// </summary>
    [System.Serializable]
    public struct TarotCardData
    {
        [Tooltip("카드의 고유 번호 (0~21)")]
        public int Id;

        [Tooltip("카드의 한글 이름 (예: 바보)")]
        public string NameKr;

        [Tooltip("카드의 영문 이름 (예: The Fool)")]
        public string NameEn;

        [Tooltip("AI 프롬프트 생성용 핵심 키워드")]
        public string Keywords;

        [Tooltip("카드의 기본 의미 (정방향 기준)")]
        [TextArea(2, 4)]
        public string Meaning;
    }
}