using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

namespace Tarot.UI
{
    /// <summary>
    /// LitMotion Yoyo 루프로 RectTransform의 anchoredPosition.y를
    /// 부드럽게 위아래 반복 이동시키는 모션. 달, 장식 요소 등에 사용합니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FloatingMotion : MonoBehaviour
    {
        [Header("Float Settings")]
        [SerializeField] private float amplitude = 15f;
        [SerializeField] private float cycleDuration = 2f;

        private RectTransform _rectTransform;
        private MotionHandle _handle;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            float originY = _rectTransform.anchoredPosition.y;

            _handle = LMotion.Create(originY - amplitude, originY + amplitude, cycleDuration)
                .WithEase(Ease.InOutSine)
                .WithLoops(-1, LoopType.Yoyo)
                .BindToAnchoredPositionY(_rectTransform);
        }

        private void OnDisable()
        {
            CancelMotion();
        }

        private void CancelMotion()
        {
            if (_handle.IsActive())
                _handle.Cancel();
        }
    }
}
