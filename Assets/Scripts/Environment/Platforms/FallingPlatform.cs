using System.Collections;
using UnityEngine;

namespace Platformer2D.Environment.Platforms
{
    /// <summary>
    /// Sàn sụp lún (Falling / Crumbling Platform):
    /// - Khi nhân vật bước lên, sàn sẽ rung lắc cảnh báo trong thời gian ngắn.
    /// - Sau đó sàn sẽ rơi xuống hoặc sập tạm thời.
    /// - Tự động tái tạo (Respawn) lại vị trí cũ sau khoảng thời gian cấu hình.
    /// </summary>
    public class FallingPlatform : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("Thời gian rung lắc trước khi sàn rơi (giây)")]
        [SerializeField] private float _fallDelay = 0.8f;
        [Tooltip("Thời gian chờ để sàn hồi sinh trở lại (giây)")]
        [SerializeField] private float _respawnDelay = 3f;
        [Tooltip("Biên độ rung lắc cảnh báo")]
        [SerializeField] private float _shakeMagnitude = 0.05f;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Rigidbody2D _rb;
        private Collider2D _collider;
        private SpriteRenderer _spriteRenderer;
        private bool _isTriggered = false;

        private void Awake()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isTriggered) return;

            if (collision.gameObject.CompareTag("Player"))
            {
                for (int i = 0; i < collision.contactCount; i++)
                {
                    // Chạm từ phía trên sàn
                    if (collision.GetContact(i).normal.y < -0.5f)
                    {
                        StartCoroutine(FallAndRespawnRoutine());
                        break;
                    }
                }
            }
        }

        private IEnumerator FallAndRespawnRoutine()
        {
            _isTriggered = true;

            // Giai đoạn 1: Rung lắc cảnh báo
            float elapsed = 0f;
            while (elapsed < _fallDelay)
            {
                float offsetX = Random.Range(-_shakeMagnitude, _shakeMagnitude);
                transform.position = _initialPosition + new Vector3(offsetX, 0f, 0f);
                yield return null;
                elapsed += Time.deltaTime;
            }

            transform.position = _initialPosition;

            // Giai đoạn 2: Sàn rơi tự do
            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.linearVelocity = Vector2.zero;
            }
            else
            {
                if (_collider != null) _collider.enabled = false;
                if (_spriteRenderer != null) _spriteRenderer.enabled = false;
            }

            // Giai đoạn 3: Chờ để hồi phục lại
            yield return new WaitForSeconds(_respawnDelay);

            ResetPlatform();
        }

        public void ResetPlatform()
        {
            StopAllCoroutines();

            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _rb.bodyType = RigidbodyType2D.Kinematic;
            }

            transform.position = _initialPosition;
            transform.rotation = _initialRotation;

            if (_collider != null) _collider.enabled = true;
            if (_spriteRenderer != null) _spriteRenderer.enabled = true;

            _isTriggered = false;
        }
    }
}
