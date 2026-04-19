namespace Tarot.Data
{
    /// <summary>
    /// 뽑힌 타로 카드의 방향(정방향 / 역방향)입니다. 해석 규칙과 UI·프롬프트에 공통으로 씁니다.
    /// </summary>
    public enum TarotCardOrientation
    {
        Upright,
        Reversed
    }

    /// <summary>
    /// <see cref="TarotCardOrientation"/> 표시용·프롬프트용 문구입니다.
    /// </summary>
    public static class TarotCardOrientationLabels
    {
        /// <summary>
        /// UI에 쓰는 짧은 한글 라벨입니다.
        /// </summary>
        public static string GetShortLabelKr(TarotCardOrientation orientation)
        {
            return orientation == TarotCardOrientation.Upright ? "정방향" : "역방향";
        }

        /// <summary>
        /// 사용자 프롬프트에 넣는 방향별 해석 지침 한 줄입니다.
        /// </summary>
        public static string GetPromptInstructionKr(TarotCardOrientation orientation)
        {
            if (orientation == TarotCardOrientation.Upright)
            {
                return "정방향: 아래 키워드와 카드 의미를 통상적인 긍정·전진 쪽으로 읽어요.";
            }

            return "역방향: 같은 카드이지만 막힘·지연·과잉·내면화·과거 미해결·에너지 역전 등으로 나타날 수 있어요. 키워드와 의미를 역방향에 맞게 조정해요.";
        }
    }
}
