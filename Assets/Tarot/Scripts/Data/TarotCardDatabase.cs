using System.Collections.Generic;
using UnityEngine;

namespace Tarot.Data
{
    /// <summary>
    /// 타로 카드 데이터를 관리하고 제공하는 클래스입니다.
    /// MVP 구현을 위해 메이저 아르카나 22장의 데이터를 하드코딩 형태로 제공합니다.
    /// 추후 ScriptableObject나 JSON으로 분리할 수 있습니다.
    /// </summary>
    public static class TarotCardDatabase
    {
        private static readonly List<TarotCardData> _majorArcanaCards = new List<TarotCardData>
        {
            new TarotCardData { Id = 0, NameKr = "바보", NameEn = "The Fool", Keywords = "새로운 시작, 순수함, 잠재력, 모험", Meaning = "아무것도 없는 상태에서 내딛는 새로운 발걸음을 의미합니다." },
            new TarotCardData { Id = 1, NameKr = "마법사", NameEn = "The Magician", Keywords = "창조력, 자신감, 잠재력 실현, 재능", Meaning = "당신이 가진 모든 능력과 자원을 바탕으로 무언가를 창조할 수 있는 시기입니다." },
            new TarotCardData { Id = 2, NameKr = "고위 여사제", NameEn = "The High Priestess", Keywords = "직관, 신비, 내면의 지혜, 통찰력", Meaning = "논리보다 직관과 내면의 목소리에 귀를 기울여야 할 때입니다." },
            new TarotCardData { Id = 3, NameKr = "여황제", NameEn = "The Empress", Keywords = "풍요, 모성, 자연, 창조", Meaning = "물질적, 정신적인 풍요로움과 돌봄의 힘을 나타냅니다." },
            new TarotCardData { Id = 4, NameKr = "황제", NameEn = "The Emperor", Keywords = "권위, 구조, 안정, 부성", Meaning = "규칙과 체계를 세우고, 리더십과 책임을 다해야 함을 의미합니다." },
            new TarotCardData { Id = 5, NameKr = "교황", NameEn = "The Hierophant", Keywords = "전통, 가르침, 신념, 소속감", Meaning = "기존의 체제나 전통, 멘토의 조언을 따르는 것이 이로울 수 있습니다." },
            new TarotCardData { Id = 6, NameKr = "연인", NameEn = "The Lovers", Keywords = "사랑, 조화, 관계, 선택", Meaning = "중요한 선택의 기로에 있거나, 깊고 의미 있는 관계를 상징합니다." },
            new TarotCardData { Id = 7, NameKr = "전차", NameEn = "The Chariot", Keywords = "전진, 통제, 승리, 의지", Meaning = "강한 의지와 자기 통제를 통해 목표를 향해 힘차게 나아가야 합니다." },
            new TarotCardData { Id = 8, NameKr = "힘", NameEn = "Strength", Keywords = "용기, 인내, 동정심, 내면의 힘", Meaning = "강압적인 물리력이 아닌, 부드러움과 인내를 통한 진정한 힘을 의미합니다." },
            new TarotCardData { Id = 9, NameKr = "은둔자", NameEn = "The Hermit", Keywords = "자기 성찰, 고독, 내면의 탐구, 지혜", Meaning = "외부의 소음을 차단하고 자신만의 시간을 가지며 답을 찾아야 할 때입니다." },
            new TarotCardData { Id = 10, NameKr = "운명의 수레바퀴", NameEn = "Wheel of Fortune", Keywords = "운명, 변화, 전환점, 순환", Meaning = "피할 수 없는 흐름과 긍정적인 방향으로의 변화를 상징합니다." },
            new TarotCardData { Id = 11, NameKr = "정의", NameEn = "Justice", Keywords = "공정성, 진실, 인과응보, 균형", Meaning = "행동에 대한 결과가 공정하게 나타나며, 객관적인 판단이 필요함을 의미합니다." },
            new TarotCardData { Id = 12, NameKr = "매달린 사람", NameEn = "The Hanged Man", Keywords = "희생, 관점의 전환, 기다림, 깨달음", Meaning = "상황을 다른 각도에서 바라보아야 하며, 때로는 자발적인 기다림이 필요합니다." },
            new TarotCardData { Id = 13, NameKr = "죽음", NameEn = "Death", Keywords = "끝, 새로운 시작, 변화, 마무리", Meaning = "과거의 것이 끝나고 완전히 새로운 국면으로 접어드는 필연적 변화를 의미합니다." },
            new TarotCardData { Id = 14, NameKr = "절제", NameEn = "Temperance", Keywords = "균형, 중용, 조화, 목적", Meaning = "양극단에 치우치지 않고 적절한 타협점과 균형을 찾아야 합니다." },
            new TarotCardData { Id = 15, NameKr = "악마", NameEn = "The Devil", Keywords = "구속, 집착, 물질주의, 유혹", Meaning = "보이지 않는 사슬이나 나쁜 습관에 얽매여 있을 수 있음을 경고합니다." },
            new TarotCardData { Id = 16, NameKr = "탑", NameEn = "The Tower", Keywords = "갑작스러운 변화, 파괴, 해방, 혼란", Meaning = "피할 수 없는 거대한 변화나 위기가 오지만, 이는 낡은 것을 부수고 새로 짓기 위함입니다." },
            new TarotCardData { Id = 17, NameKr = "별", NameEn = "The Star", Keywords = "희망, 영감, 치유, 긍정", Meaning = "어둠이 걷히고 밝은 희망과 치유의 에너지가 찾아옴을 의미합니다." },
            new TarotCardData { Id = 18, NameKr = "달", NameEn = "The Moon", Keywords = "환상, 두려움, 불안, 무의식", Meaning = "상황이 불분명하고 혼란스러울 수 있으니, 섣부른 판단을 경계해야 합니다." },
            new TarotCardData { Id = 19, NameKr = "태양", NameEn = "The Sun", Keywords = "성공, 활력, 기쁨, 긍정성", Meaning = "가장 긍정적인 카드 중 하나로, 기쁨과 성공, 밝은 에너지를 나타냅니다." },
            new TarotCardData { Id = 20, NameKr = "심판", NameEn = "Judgement", Keywords = "부활, 평가, 자각, 내면의 부름", Meaning = "과거를 돌아보고 결산을 내리며, 새로운 단계로 나아가기 위한 각성의 시기입니다." },
            new TarotCardData { Id = 21, NameKr = "세계", NameEn = "The World", Keywords = "완성, 통합, 성취, 목적 달성", Meaning = "하나의 긴 여정이 성공적으로 마무리되고 완전함을 이루는 최고의 상태입니다." }
        };

        /// <summary>
        /// 모든 메이저 아르카나 카드의 리스트를 반환합니다.
        /// </summary>
        public static IReadOnlyList<TarotCardData> GetAllCards()
        {
            return _majorArcanaCards.AsReadOnly();
        }

        /// <summary>
        /// 특정 ID를 가진 카드를 반환합니다.
        /// </summary>
        public static TarotCardData GetCardById(int id)
        {
            return _majorArcanaCards.Find(card => card.Id == id);
        }
    }
}