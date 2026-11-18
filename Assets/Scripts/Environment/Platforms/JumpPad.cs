using System.Collections;
using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Player.Controllers;

namespace Platformer2D.Environment.Platforms
{
    /// <summary>
    /// Bệ nhún / Đệm lò xo (Jump Pad / Trampoline):
    /// - Khi nhân vật dẫm lên, đẩy nhân vật bay vút lên trời với lực mạnh.
    /// - Hiệu ứng nén nhả lò xo (Squash & Stretch) tạo cảm giác nảy sống động.
    /// - Phát âm thanh và hồi lại số lần nhảy trên không (Double Jump) cho nhân vật.
    /// </summary>
    public class JumpPad : MonoBehaviour
    {
        [Tooltip("Lực đẩy nhân vật lên cao (units/s)")]
        [SerializeField] private float _bounceForce = 18f;
        [Tooltip("Âm thanh phát khi nhún")]
        [SerializeField] private string _bounceSound = "Bounce";
        [Tooltip("Có hồi lại lượt nhảy trên không (Air Jump) cho người chơi hay không")]
        [SerializeField] private bool _resetAirJumps = true;

        private Vector3 _originalScale;
        private Coroutine _squashCoroutine;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryBounce(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryBounce(collision.gameObject);
        }

        private void TryBounce(GameObject target)
        {
            if (!target.CompareTag("Player")) return;

            Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocityY = _bounceForce;

                PlayerController player = target.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.NotifyJumpPadLaunch(_bounceForce);
                    if (_resetAirJumps)
                    {
                        player.ResetAirJumps();
                    }
                }

                // Hiệu ứng lò xo co giãn
                if (_squashCoroutine != null) StopCoroutine(_squashCoroutine);
                _squashCoroutine = StartCoroutine(SquashRoutine());

                EventBus.Publish(new PlaySoundEvent(_bounceSound, SoundType.SFX, transform.position));
            }
        }

        private IEnumerator SquashRoutine()
        {
            // Nén xuống theo trục Y, bè ra theo trục X
            transform.localScale = new Vector3(_originalScale.x * 1.3f, _originalScale.y * 0.6f, _originalScale.z);
            yield return new WaitForSeconds(0.08f);

            // Bật dãn cao lên
            transform.localScale = new Vector3(_originalScale.x * 0.85f, _originalScale.y * 1.25f, _originalScale.z);
            yield return new WaitForSeconds(0.12f);

            // Trở về kích thước ban đầu
            transform.localScale = _originalScale;
            _squashCoroutine = null;
        }
    }
}
