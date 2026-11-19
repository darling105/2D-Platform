using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Platformer2D.Core.Architecture;

namespace Platformer2D.UI
{
    /// <summary>
    /// Hiệu ứng phản hồi nút bấm UI (Hover / Click scale mượt bằng unscaled time + phát âm thanh click).
    /// Hoạt động bình thường ngay cả khi game đang Pause (Time.timeScale == 0).
    /// </summary>
    public class UIButtonFeedback : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler
    {
        [SerializeField] private float _hoverScale = 1.06f;
        [SerializeField] private float _pressedScale = 0.95f;
        [SerializeField] private float _lerpSpeed = 18f;
        [SerializeField] private string _clickSoundId = "UI_Click";

        private Button _button;
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _targetScale = Vector3.one;
        private bool _isHovered;
        private bool _isPressed;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _baseScale = transform.localScale;
            _targetScale = _baseScale;
        }

        private void OnDisable()
        {
            _isHovered = false;
            _isPressed = false;
            _targetScale = _baseScale;
            transform.localScale = _baseScale;
        }

        private void Update()
        {
            if ((transform.localScale - _targetScale).sqrMagnitude < 0.0001f)
            {
                if (transform.localScale != _targetScale)
                {
                    transform.localScale = _targetScale;
                }
                return;
            }

            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.unscaledDeltaTime * _lerpSpeed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractable()) return;
            _isHovered = true;
            RefreshTargetScale();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _isPressed = false;
            RefreshTargetScale();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable()) return;
            _isPressed = true;
            RefreshTargetScale();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            RefreshTargetScale();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsInteractable()) return;

            if (!string.IsNullOrEmpty(_clickSoundId))
            {
                EventBus.Publish(new PlaySoundEvent(_clickSoundId, SoundType.SFX));
            }
        }

        private void RefreshTargetScale()
        {
            if (!IsInteractable())
            {
                _targetScale = _baseScale;
                return;
            }

            if (_isPressed)
            {
                _targetScale = _baseScale * _pressedScale;
            }
            else if (_isHovered)
            {
                _targetScale = _baseScale * _hoverScale;
            }
            else
            {
                _targetScale = _baseScale;
            }
        }

        private bool IsInteractable()
        {
            return _button == null || _button.interactable;
        }
    }
}
