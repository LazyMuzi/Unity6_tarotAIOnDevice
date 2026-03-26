using UnityEngine;
using Tarot.Data;

namespace Tarot.Core
{
    /// <summary>
    /// 타로 카드 뽑기와 관련된 핵심 로직을 담당하는 클래스입니다.
    /// </summary>
    public class TarotDeckController
    {
        /// <summary>
        /// 22장의 메이저 아르카나 중 1장의 카드를 랜덤으로 뽑습니다.
        /// </summary>
        /// <returns>선택된 타로 카드 데이터</returns>
        public TarotCardData DrawRandomCard()
        {
            var cards = TarotCardDatabase.GetAllCards();
            
            if (cards == null || cards.Count == 0)
            {
                Debug.LogError("타로 카드 데이터가 존재하지 않습니다!");
                return default;
            }

            // 0부터 카드 개수-1 사이의 난수 생성 (유니티 Random.Range는 int일 경우 max값은 포함하지 않음)
            int randomIndex = Random.Range(0, cards.Count);
            
            return cards[randomIndex];
        }
    }
}