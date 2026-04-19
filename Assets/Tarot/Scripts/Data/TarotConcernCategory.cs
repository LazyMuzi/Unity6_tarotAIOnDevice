namespace Tarot.Data
{
    /// <summary>
    /// 타로 고민의 주제 분류입니다. 프롬프트와 UI 라벨에 공통으로 사용합니다.
    /// </summary>
    public enum TarotConcernCategory
    {
        LoveAndRelationships,
        CareerAndStudy,
        Money,
        Health,
        Choice,
        Other
    }

    /// <summary>
    /// <see cref="TarotConcernCategory"/>의 표시·프롬프트용 한글 문구를 제공합니다.
    /// </summary>
    public static class TarotConcernCategoryLabels
    {
        /// <summary>
        /// 버튼 등 UI에 쓰이는 짧은 한글 라벨입니다.
        /// </summary>
        public static string GetShortLabelKr(TarotConcernCategory category)
        {
            return category switch
            {
                TarotConcernCategory.LoveAndRelationships => "연애·관계",
                TarotConcernCategory.CareerAndStudy => "직장·학업",
                TarotConcernCategory.Money => "금전",
                TarotConcernCategory.Health => "건강",
                TarotConcernCategory.Choice => "선택·결정",
                TarotConcernCategory.Other => "기타",
                _ => "기타"
            };
        }

        /// <summary>
        /// AI 프롬프트에 넣는 분야 설명 한 줄입니다.
        /// </summary>
        public static string GetPromptLineKr(TarotConcernCategory category)
        {
            return category switch
            {
                TarotConcernCategory.LoveAndRelationships => "연애, 애정, 가족·친구 등 사람과의 관계",
                TarotConcernCategory.CareerAndStudy => "직장, 진로, 학업, 업무",
                TarotConcernCategory.Money => "돈, 재정, 물질적 안정",
                TarotConcernCategory.Health => "몸과 마음의 건강, 컨디션",
                TarotConcernCategory.Choice => "갈림길, 선택, 결정",
                TarotConcernCategory.Other => "위에 해당하지 않는 기타 고민",
                _ => "기타 고민"
            };
        }
    }
}
