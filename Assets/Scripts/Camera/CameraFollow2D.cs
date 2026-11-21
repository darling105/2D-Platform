using System.Collections;
using UnityEngine;

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
        [SerializeField] private float _smoothTime = 0.14f;

        [Header("Lookahead (Nhìn trước)")]
        [Tooltip("Khoảng cách Camera nhìn trước theo hướng di chuyển thực tế")]
        [SerializeField] private float _lookaheadDistance = 0.85f;
        [Tooltip("Tốc độ chuyển hướng Lookahead")]
        [SerializeField] private float _lookaheadSpeed = 2.2f;

        [Header("Camera Bounds (Tùy chọn)")]
        [SerializeField] private bool _useBounds = false;
        [SerializeField] private Vector2 _minBounds = new Vector2(-100f, -10f);
        [SerializeField] private Vector2 _maxBounds = new Vector2(100f, 50f);

        private Vector3 _currentVelocity = Vector3.zero;
        private float _currentLookaheadX = 0f;
        private float _targetLookaheadX = 0f;
        private float _lookaheadVelocity = 0f;
        private Rigidbody2D _targetRb;

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

            if (_target != null)
            {
                _targetRb = _target.GetComponent<Rigidbody2D>();
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // 1. Lookahead theo vận tốc thực tế giúp camera lia mượt, không bị giật khi đổi hướng nhanh
            if (_targetRb != null)
            {
                float vx = _targetRb.linearVelocityX;
                if (Mathf.Abs(vx) > 1.8f)
                {
                    _targetLookaheadX = Mathf.Sign(vx) * _lookaheadDistance;
                }
                else
                {
                    _targetLookaheadX = Mathf.Sign(_target.localScale.x) * (_lookaheadDistance * 0.25f);
                }
            }
            else
            {
                _targetLookaheadX = Mathf.Sign(_target.localScale.x) * _lookaheadDistance;
            }

            float lookaheadSmoothTime = 1f / Mathf.Max(0.5f, _lookaheadSpeed);
            _currentLookaheadX = Mathf.SmoothDamp(_currentLookaheadX, _targetLookaheadX, ref _lookaheadVelocity, lookaheadSmoothTime);

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
        /// Độ mạnh rung lắc được cân bằng mượt mà, tự suy giảm dần (decay) để không gây chóng mặt.
        /// </summary>
        /// <param name="duration">Thời gian rung (giây, thường 0.06 - 0.12s)</param>
        /// <param name="magnitude">Độ mạnh rung lắc (thường 0.03 - 0.08)</param>
        public void Shake(float duration = 0.09f, float magnitude = 0.05f)
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }
            // Giới hạn biên độ rung tối đa không vượt quá 0.08f trong không gian 2D để camera luôn êm ái
            float clampedMagnitude = Mathf.Clamp(magnitude, 0.015f, 0.08f);
            float clampedDuration = Mathf.Clamp(duration, 0.05f, 0.16f);
            _shakeCoroutine = StartCoroutine(ShakeRoutine(clampedDuration, clampedMagnitude));
        }

        public static void TriggerShake(float duration = 0.09f, float magnitude = 0.05f)
        {
            if (Instance != null)
            {
                Instance.Shake(duration, magnitude);
            }
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // Suy giảm độ rung mượt mà từ 1 về 0 (Damper)
                float damper = 1f - (elapsed / duration);
                float x = Random.Range(-1f, 1f) * magnitude * damper;
                float y = Random.Range(-1f, 1f) * magnitude * damper;

                _shakeOffset = new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            _shakeOffset = Vector3.zero;
            _shakeCoroutine = null;
        }
    }
}
