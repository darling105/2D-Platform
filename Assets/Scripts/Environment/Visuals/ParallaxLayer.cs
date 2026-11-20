using UnityEngine;

namespace Platformer2D.Environment.Visuals
{
    /// <summary>
    /// Lớp nền Parallax 2D cuộn ngang vô tận:
    /// - Tạo chiều sâu tách lớp theo chuyển động của Camera chính.
    /// - Hỗ trợ trôi ngang tự động nhẹ (dùng cho lớp Mây trên bầu trời).
    /// - Tự động lặp lại mượt mà qua chuỗi panel con không bao giờ bị hụt mép màn hình.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        public const float DefaultPanelScale = 1.05f;
        public const float DefaultWorldBottomY = -2.30f;
        public const float UnifiedVerticalParallaxFactor = 0.70f;
        private const float MaxVerticalShift = 1.8f;

        [Tooltip("0 = đứng yên theo camera (rất xa), 1 = bám cố định trong thế giới game (rất gần)")]
        [SerializeField, Range(0f, 1.2f)] private float _parallaxFactorX = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _parallaxFactorY = UnifiedVerticalParallaxFactor;
        [Tooltip("Tốc độ trôi tự động theo trục X (units/s), thích hợp cho tầng Mây")]
        [SerializeField] private float _autoDriftSpeedX = 0f;
        [SerializeField] private float _panelWidth = 26f;

        [Header("Bottom Alignment")]
        [Tooltip("Tự động căn mép đáy của lớp nền xuống dưới mặt đất và đồng bộ scale/tốc độ dọc lúc Start")]
        [SerializeField] private bool _alignBottomOnStart = true;
        [SerializeField] private float _worldBottomY = DefaultWorldBottomY;

        private Transform _cameraTransform;
        private Vector3 _startLayerPos;
        private Vector3 _startCameraPos;
        private float _accumulatedDriftX;
        private bool _initialized;

        public void Configure(
            float factorX,
            float factorY,
            float panelWidth,
            float autoDriftSpeedX = 0f,
            bool alignBottomOnStart = true,
            float worldBottomY = DefaultWorldBottomY)
        {
            _parallaxFactorX = factorX;
            _parallaxFactorY = factorY;
            _panelWidth = Mathf.Max(1f, panelWidth);
            _autoDriftSpeedX = autoDriftSpeedX;
            _alignBottomOnStart = alignBottomOnStart;
            _worldBottomY = worldBottomY;
        }

        private void Start()
        {
            TryInitialize();
        }

        private void TryInitialize()
        {
            if (_initialized) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            if (_alignBottomOnStart)
            {
                AlignPanelsAndBottomEdge();
            }

            _cameraTransform = cam.transform;
            _startLayerPos = transform.position;
            _startCameraPos = _cameraTransform.position;
            _initialized = true;
        }

        /// <summary>
        /// Vì auto-slicer cắt các lớp 1.png..8.png với chiều cao khác nhau và pivot ở giữa,
        /// cần căn lại theo mép đáy chung (_worldBottomY) và đồng bộ scale/parallaxY để 8 lớp không tách rời khi Camera lên cao.
        /// </summary>
        private void AlignPanelsAndBottomEdge()
        {
            Sprite primarySprite = null;
            int panelCount = 0;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (!child.TryGetComponent<SpriteRenderer>(out var sr) || sr.sprite == null) continue;
                if (child.name.Contains("BottomSkirt")) continue;

                if (primarySprite == null) primarySprite = sr.sprite;
                panelCount++;
            }

            if (primarySprite == null) return;

            _parallaxFactorY = UnifiedVerticalParallaxFactor;
            _panelWidth = Mathf.Max(1f, primarySprite.bounds.size.x * DefaultPanelScale - 0.02f);

            int panelIdx = 0;
            int halfCount = panelCount / 2;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (!child.TryGetComponent<SpriteRenderer>(out var sr) || sr.sprite == null) continue;
                if (child.name.Contains("BottomSkirt")) continue;

                child.localScale = new Vector3(DefaultPanelScale, DefaultPanelScale, 1f);
                child.localPosition = new Vector3((panelIdx - halfCount) * _panelWidth, 0f, 0f);
                sr.color = Color.white;
                panelIdx++;
            }

            float alignedCenterY = _worldBottomY - primarySprite.bounds.min.y * DefaultPanelScale;
            transform.position = new Vector3(transform.position.x, alignedCenterY, transform.position.z);
        }

        private void LateUpdate()
        {
            if (!_initialized)
            {
                TryInitialize();
                if (!_initialized) return;
            }

            _accumulatedDriftX += _autoDriftSpeedX * Time.deltaTime;

            float camDeltaX = _cameraTransform.position.x - _startCameraPos.x;
            float camDeltaY = _cameraTransform.position.y - _startCameraPos.y;

            float parallaxShiftX = camDeltaX * (1f - _parallaxFactorX) + _accumulatedDriftX;
            float targetX = _startLayerPos.x + parallaxShiftX;

            if (_panelWidth > 0.1f)
            {
                float relToCam = targetX - _cameraTransform.position.x;
                float wrappedRel = Mathf.Repeat(relToCam + _panelWidth * 0.5f, _panelWidth) - _panelWidth * 0.5f;
                targetX = _cameraTransform.position.x + wrappedRel;
            }

            float verticalShiftY = Mathf.Clamp(camDeltaY * (1f - _parallaxFactorY), -MaxVerticalShift, MaxVerticalShift);
            float targetY = _startLayerPos.y + verticalShiftY;
            transform.position = new Vector3(targetX, targetY, _startLayerPos.z);
        }
    }
}
