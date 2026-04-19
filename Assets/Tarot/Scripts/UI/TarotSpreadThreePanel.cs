using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Tarot.UI
{
    /// <summary>
    /// 3장 스프레드 UI 프리팹(또는 씬 오브젝트) 루트에 붙여, 가운데 선택 카드·슬롯 홀더·이름 텍스트 참조를 인스펙터에서 연결합니다.
    /// </summary>
    public sealed class TarotSpreadThreePanel : MonoBehaviour
    {
        [Tooltip("상단(또는 가운데)에서 세 장을 고르는 행. 배치가 끝나면 비활성화할 수 있습니다.")]
        [SerializeField] private GameObject _centerRow;

        [Tooltip("인덱스 0~2: 가운데 열 루트(카드 리셋 시 부모).")]
        [SerializeField] private RectTransform[] _centerPickColumnRoots = new RectTransform[3];

        [Tooltip("인덱스 0~2: 가운데 카드 Image.")]
        [SerializeField] private Image[] _centerPickCardImages = new Image[3];

        [Tooltip("인덱스 0~2: 가운데 카드 Button.")]
        [SerializeField] private Button[] _centerPickButtons = new Button[3];

        [Tooltip("인덱스 0~2: 과거·현재·미래 슬롯에 카드가 들어갈 RectTransform(SlotCardHolder).")]
        [SerializeField] private RectTransform[] _slotCardHolders = new RectTransform[3];

        [Tooltip("인덱스 0~2: 각 슬롯 아래 카드 이름 TMP.")]
        [SerializeField] private TextMeshProUGUI[] _cardNameUnderTexts = new TextMeshProUGUI[3];

        public GameObject CenterRow => _centerRow;

        public RectTransform[] CenterPickColumnRoots => _centerPickColumnRoots;

        public Image[] CenterPickCardImages => _centerPickCardImages;

        public Button[] CenterPickButtons => _centerPickButtons;

        public RectTransform[] SlotCardHolders => _slotCardHolders;

        public TextMeshProUGUI[] CardNameUnderTexts => _cardNameUnderTexts;

        /// <summary>
        /// 인스펙터 연결이 3슬롯 모두 채워졌는지 검사합니다.
        /// </summary>
        public bool ValidateBindings()
        {
            if (_centerRow == null)
                return false;

            if (_centerPickColumnRoots == null || _centerPickColumnRoots.Length != 3
                || _centerPickCardImages == null || _centerPickCardImages.Length != 3
                || _centerPickButtons == null || _centerPickButtons.Length != 3
                || _slotCardHolders == null || _slotCardHolders.Length != 3
                || _cardNameUnderTexts == null || _cardNameUnderTexts.Length != 3)
                return false;

            for (int i = 0; i < 3; i++)
            {
                if (_centerPickColumnRoots[i] == null || _centerPickCardImages[i] == null
                    || _centerPickButtons[i] == null || _slotCardHolders[i] == null
                    || _cardNameUnderTexts[i] == null)
                    return false;
            }

            return true;
        }
    }
}
