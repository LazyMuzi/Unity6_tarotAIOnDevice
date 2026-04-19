using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

namespace Tarot.UI
{
    /// <summary>
    /// LitMotion 트윈으로 구름을 오른쪽에서 왼쪽으로 천천히 이동시키는 모션.
    /// 화면 밖으로 나가면 반대쪽에서 다시 나타나며, 각 구름마다 다른 속도를 설정할 수 있습니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CloudDriftMotion : MonoBehaviour
    {
        [Header("Drift Settings")]
        [Tooltip("이동 속도 (px/s). 값이 클수록 빠르게 이동합니다.")]
        [SerializeField] private float speed = 20f;

        [Tooltip("화면 오른쪽 밖 리스폰 여유 거리")]
        [SerializeField] private float respawnMargin = 100f;

        private RectTransform _rectTransform;
        private MotionHandle _handle;
        private float _rightX;
        private float _leftX;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            var canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            float halfCanvasWidth = canvasRect.rect.width * 0.5f;
            float halfObjectWidth = _rectTransform.rect.width * _rectTransform.lossyScale.x * 0.5f;

            _rightX = halfCanvasWidth + halfObjectWidth + respawnMargin;
            _leftX = -halfCanvasWidth - halfObjectWidth - respawnMargin;

            StartFirstSegment();
        }

        private void OnDisable()
        {
            CancelMotion();
        }

        /// <summary>
        /// 현재 위치에서 왼쪽 끝까지 이동 후 무한 루프로 전환합니다.
        /// </summary>
        private void StartFirstSegment()
        {
            float currentX = _rectTransform.anchoredPosition.x;
            float distance = currentX - _leftX;

            if (distance <= 0f)
            {
                StartLoop();
                return;
            }

            float duration = distance / speed;

            _handle = LMotion.Create(currentX, _leftX, duration)
                .WithEase(Ease.Linear)
                .WithOnComplete(StartLoop)
                .BindToAnchoredPositionX(_rectTransform);
        }

        private void StartLoop()
        {
            float duration = (_rightX - _leftX) / speed;

            _handle = LMotion.Create(_rightX, _leftX, duration)
                .WithEase(Ease.Linear)
                .WithLoops(-1, LoopType.Restart)
                .BindToAnchoredPositionX(_rectTransform);
        }

        private void CancelMotion()
        {
            if (_handle.IsActive())
                _handle.Cancel();
        }
    }
}
