using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using LitMotion;
using LitMotion.Extensions;

namespace Tarot.UI
{
    /// <summary>
    /// LitMotion을 사용하여 타로 카드의 등장/퇴장 애니메이션을 제어합니다.
    /// 슬라이드 업 + X축 스케일 플립(뒷면→앞면) 연출을 담당합니다.
    /// </summary>
    public class TarotCardAnimator : MonoBehaviour
    {
        [Header("Card UI")]
        [SerializeField] private RectTransform _cardRect;
        [SerializeField] private Image _cardImage;

        [Header("Animation Settings")]
        [SerializeField] private float _slideDistance = 600f;
        [SerializeField] private float _slideDuration = 0.6f;
        [SerializeField] private float _flipDuration = 0.5f;
        [SerializeField] private Ease _slideEase = Ease.OutCubic;
        [SerializeField] private Ease _flipEase = Ease.InOutQuad;

        private Sprite _backSprite;
        private Sprite _frontSprite;
        private Vector2 _originalAnchoredPosition;

        private void Awake()
        {
            if (_cardRect != null)
            {
                _originalAnchoredPosition = _cardRect.anchoredPosition;
            }
        }

        /// <summary>
        /// 카드를 초기 상태(숨김)로 설정합니다.
        /// </summary>
        public void Initialize(Sprite backSprite)
        {
            _backSprite = backSprite;
            SetCardVisible(false);
        }

        /// <summary>
        /// 카드 등장 애니메이션을 실행합니다.
        /// 아래에서 슬라이드 업 → Y축 플립(뒷면→앞면) 순서로 진행됩니다.
        /// </summary>
        public async Task ShowCardAsync(Sprite frontSprite)
        {
            if (_cardRect == null || _cardImage == null)
                return;

            _frontSprite = frontSprite;

            _cardImage.sprite = _backSprite;
            _cardRect.localScale = Vector3.one;

            var startPos = _originalAnchoredPosition + Vector2.down * _slideDistance;
            _cardRect.anchoredPosition = startPos;

            SetCardVisible(true);

            await PlaySlideUpAsync();
            await PlayFlipAsync();
        }

        /// <summary>
        /// 카드를 숨기고 초기 상태로 리셋합니다.
        /// </summary>
        public void HideCard()
        {
            if (_cardRect == null)
                return;

            SetCardVisible(false);
            _cardRect.anchoredPosition = _originalAnchoredPosition;
            _cardRect.localScale = Vector3.one;
        }

        private async Task PlaySlideUpAsync()
        {
            await LMotion.Create(
                    _cardRect.anchoredPosition.y,
                    _originalAnchoredPosition.y,
                    _slideDuration)
                .WithEase(_slideEase)
                .BindToAnchoredPositionY(_cardRect);
        }

        /// <summary>
        /// X 스케일을 1→0(뒷면 사라짐) → 0→1(앞면 등장)으로 플립합니다.
        /// 스케일이 0이 되는 시점에 스프라이트를 교체합니다.
        /// </summary>
        private async Task PlayFlipAsync()
        {
            float halfDuration = _flipDuration * 0.5f;

            await LMotion.Create(1f, 0f, halfDuration)
                .WithEase(_flipEase)
                .BindToLocalScaleX(_cardRect);

            _cardImage.sprite = _frontSprite;

            await LMotion.Create(0f, 1f, halfDuration)
                .WithEase(_flipEase)
                .BindToLocalScaleX(_cardRect);
        }

        private void SetCardVisible(bool visible)
        {
            if (_cardImage != null)
            {
                _cardImage.enabled = visible;
            }
        }
    }
}
