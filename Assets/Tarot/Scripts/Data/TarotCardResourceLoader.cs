using UnityEngine;

namespace Tarot.Data
{
    /// <summary>
    /// 타로 카드 ID를 기반으로 스프라이트 에셋을 로드하는 유틸리티입니다.
    /// Inspector에서 카드 스프라이트 배열과 뒷면 스프라이트를 할당하여 사용합니다.
    /// </summary>
    public class TarotCardResourceLoader : MonoBehaviour
    {
        [Header("Card Sprites")]
        [Tooltip("메이저 아르카나 0~21번 카드의 앞면 스프라이트 (인덱스 = 카드 ID)")]
        [SerializeField] private Sprite[] _cardFrontSprites;

        [Tooltip("카드 뒷면 스프라이트")]
        [SerializeField] private Sprite _cardBackSprite;

        /// <summary>
        /// 카드 ID에 해당하는 앞면 스프라이트를 반환합니다.
        /// </summary>
        public Sprite GetCardFrontSprite(int cardId)
        {
            if (_cardFrontSprites == null || cardId < 0 || cardId >= _cardFrontSprites.Length)
            {
                Debug.LogError($"[TarotCardResourceLoader] 유효하지 않은 카드 ID: {cardId}");
                return null;
            }

            return _cardFrontSprites[cardId];
        }

        /// <summary>
        /// 카드 뒷면 스프라이트를 반환합니다.
        /// </summary>
        public Sprite GetCardBackSprite()
        {
            if (_cardBackSprite == null)
            {
                Debug.LogError("[TarotCardResourceLoader] 카드 뒷면 스프라이트가 할당되지 않았습니다.");
            }

            return _cardBackSprite;
        }
    }
}
