using System.Collections;
using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Singletons;

namespace Platformer2D.CameraControl
{
    /// <summary>
    /// Camera bám theo Player 2D mượt mà:
    /// - Smooth Damping (chống giật hình).
    /// - Lookahead (nhìn trước theo hướng nhân vật di chuyển).
    /// - Rung lắc màn hình (Screen Shake) khi đấm trúng quái hoặc nhận sát thương.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        public static CameraFollow2D Instance { get; private set; }

        [Header("Target Tracking")]
        [Tooltip("Đối tượng Camera bám theo (tự động tìm Player nếu để trống)")]
        [SerializeField] private Transform _target;
        [Tooltip("Khoảng lệch vị trí so với nhân vật (Z luôn giữ -10)")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 1f, -10f);
        [Tooltip("Độ mượt mà khi bám theo (giá trị càng nhỏ bám càng nhanh)")]
        [SerializeField] private float _smoothTime = 0.2f;

        [Header("Lookahead (Nhìn trước)")]
        [Tooltip("Khoảng cách Camera nhìn trước theo hướng di chuyển")]
        [SerializeField] private float _lookaheadDistance = 1.5f;
        [Tooltip("Tốc độ chuyển hướng Lookahead")]
        [SerializeField] private float _lookaheadSpeed = 3f;

        [Header("Camera Bounds (Tùy chọn)")]
        [SerializeField] private bool _useBounds = false;
        [SerializeField] private Vector2 _minBounds = new Vector2(-100f, -10f);
        [SerializeField] private Vector2 _maxBounds = new Vector2(100f, 50f);

        private Vector3 _currentVelocity = Vector3.zero;
        private float _currentLookaheadX = 0f;
        private float _targetLookaheadX = 0f;

        // Screen Shake
        private Vector3 _shakeOffset = Vector3.zero;
        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            if (_target == null)
            {
                // Tự động tìm Player nếu chưa kéo thả
                var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Pink_Monster_0");
                if (player != null)
                {
                    _target = player.transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // 1. Tính toán Lookahead theo hướng quay mặt / di chuyển
            float facingDirection = Mathf.Sign(_target.localScale.x);
            _targetLookaheadX = facingDirection * _lookaheadDistance;
            _currentLookaheadX = Mathf.MoveTowards(_currentLookaheadX, _targetLookaheadX, _lookaheadSpeed * Time.deltaTime);

            // 2. Vị trí đích đến của Camera
            Vector3 targetPosition = _target.position + _offset;
            targetPosition.x += _currentLookaheadX;

            // 3. Giới hạn biên bản đồ nếu bật
            if (_useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, _minBounds.x, _maxBounds.x);
                targetPosition.y = Mathf.Clamp(targetPosition.y, _minBounds.y, _maxBounds.y);
            }

            // 4. Di chuyển mượt mà (SmoothDamp)
            Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentVelocity, _smoothTime);

            // 5. Cộng thêm độ lệch rung lắc màn hình (Screen Shake)
            transform.position = smoothedPosition + _shakeOffset;
        }

        /// <summary>
        /// Kích hoạt rung màn hình khi đánh trúng, trúng đòn hoặc nổ bom.
        /// </summary>
        /// <param name="duration">Thời gian rung (giây, thường 0.1 - 0.25s)</param>
        /// <param name="magnitude">Độ mạnh rung lắc (thường 0.1 - 0.3)</param>
        public void Shake(float duration = 0.15f, float magnitude = 0.2f)
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }
            _shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                _shakeOffset = new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            _shakeOffset = Vector3.zero;
            _shakeCoroutine = null;
        }
    }
}
