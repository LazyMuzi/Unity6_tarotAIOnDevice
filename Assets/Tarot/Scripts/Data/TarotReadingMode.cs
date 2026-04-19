namespace Tarot.Data
{
    /// <summary>
    /// 타로 리딩 종류입니다. 데일리(1장 선택)와 과거·현재·미래 3장 스프레드를 구분합니다.
    /// </summary>
    public enum TarotReadingMode
    {
        DailySingleCard,
        SpreadPastPresentFuture
    }

    /// <summary>
    /// <see cref="TarotReadingMode"/> UI·표시용 라벨입니다.
    /// </summary>
    public static class TarotReadingModeLabels
    {
        public static string GetButtonLabelKr(TarotReadingMode mode)
        {
            return mode switch
            {
                TarotReadingMode.DailySingleCard => "데일리 운세 (카드 1장)",
                TarotReadingMode.SpreadPastPresentFuture => "스프레드 (과거·현재·미래, 3장)",
                _ => "데일리 운세 (카드 1장)"
            };
        }
    }
}
