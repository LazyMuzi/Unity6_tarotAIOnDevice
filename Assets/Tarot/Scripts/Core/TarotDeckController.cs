using System.Collections.Generic;
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

        /// <summary>
        /// 전체 덱을 한 번 섞은 뒤 위에서부터 서로 다른 2장을 반환합니다.
        /// </summary>
        /// <returns>길이 2인 배열, 실패 시 null</returns>
        public TarotCardData[] ShuffleAndDrawTwoCards()
        {
            var cards = TarotCardDatabase.GetAllCards();
            if (cards == null || cards.Count < 2)
            {
                Debug.LogError("타로 카드 덱이 2장 미만입니다.");
                return null;
            }

            var deck = new List<TarotCardData>(cards);
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }

            return new[] { deck[0], deck[1] };
        }

        /// <summary>
        /// 덱을 섞은 뒤 위에서부터 서로 다른 <paramref name="count"/>장을 반환합니다.
        /// </summary>
        /// <returns>길이 count인 배열, 실패 시 null</returns>
        public TarotCardData[] ShuffleAndDrawCards(int count)
        {
            if (count < 1)
                count = 1;

            var cards = TarotCardDatabase.GetAllCards();
            if (cards == null || cards.Count < count)
            {
                Debug.LogError($"타로 카드 덱이 {count}장 미만입니다.");
                return null;
            }

            var deck = new List<TarotCardData>(cards);
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }

            var result = new TarotCardData[count];
            for (int i = 0; i < count; i++)
                result[i] = deck[i];

            return result;
        }

        /// <summary>
        /// 덱을 섞은 뒤 위에서부터 서로 다른 3장을 반환합니다 (과거·현재·미래 순).
        /// </summary>
        public TarotCardData[] ShuffleAndDrawThreeCards()
        {
            return ShuffleAndDrawCards(3);
        }
    }
}