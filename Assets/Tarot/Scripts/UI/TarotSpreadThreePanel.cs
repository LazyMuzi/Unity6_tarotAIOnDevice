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
        [Tooltip("상단(또는 가운데)에서 카드를 고르는 행. 배치가 끝나면 비활성화할 수 있습니다.")]
        [SerializeField] private GameObject _centerRow;

        [Tooltip("픽 카드 개수(N)만큼: 가운데 열 루트(카드 리셋 시 부모). 3개 이상, 아래 세 배열과 길이가 같아야 합니다.")]
        [SerializeField] private RectTransform[] _centerPickColumnRoots = new RectTransform[5];

        [Tooltip("픽 카드 개수(N)만큼: 가운데 카드 Image.")]
        [SerializeField] private Image[] _centerPickCardImages = new Image[5];

        [Tooltip("픽 카드 개수(N)만큼: 가운데 카드 Button.")]
        [SerializeField] private Button[] _centerPickButtons = new Button[5];

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
        /// 인스펙터 연결을 검사합니다. 픽 카드는 3장 이상이며 세 배열(컬럼·이미지·버튼) 길이가 같아야 하고,
        /// 슬롯·이름은 과거·현재·미래 3개로 고정입니다.
        /// </summary>
        public bool ValidateBindings()
        {
            const int SlotCount = 3;

            if (_centerRow == null)
                return false;

            if (_slotCardHolders == null || _slotCardHolders.Length != SlotCount
                || _cardNameUnderTexts == null || _cardNameUnderTexts.Length != SlotCount)
                return false;

            if (_centerPickCardImages == null || _centerPickCardImages.Length < SlotCount)
                return false;

            int pickCount = _centerPickCardImages.Length;
            if (_centerPickColumnRoots == null || _centerPickColumnRoots.Length != pickCount
                || _centerPickButtons == null || _centerPickButtons.Length != pickCount)
                return false;

            for (int i = 0; i < pickCount; i++)
            {
                if (_centerPickColumnRoots[i] == null || _centerPickCardImages[i] == null
                    || _centerPickButtons[i] == null)
                    return false;
            }

            for (int i = 0; i < SlotCount; i++)
            {
                if (_slotCardHolders[i] == null || _cardNameUnderTexts[i] == null)
                    return false;
            }

            return true;
        }
    }
}