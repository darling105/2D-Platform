using System.Collections;
using UnityEngine;

namespace Platformer2D.Environment.Platforms
{
    /// <summary>
    /// Sàn một chiều (One-Way Platform):
    /// - Cho phép nhảy từ dưới lên xuyên qua sàn và đứng lên trên.
    /// - Khi người chơi ấn phím xuống (S / Down Arrow) + Nhảy (Space), tạm thời tắt va chạm để rơi xuống.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class OneWayPlatform : MonoBehaviour
    {
        [Tooltip("Thời gian vô hiệu hóa va chạm khi người chơi rơi xuyên qua (giây)")]
        [SerializeField] private float _dropThroughDuration = 0.35f;

        private Collider2D _platformCollider;

        private void Awake()
        {
            _platformCollider = GetComponent<Collider2D>();
        }

        /// <summary>
        /// Cho phép người chơi rơi xuyên qua sàn này trong một khoảng thời gian ngắn.
        /// </summary>
        /// <param name="playerCollider">Collider của nhân vật</param>
        public void DropThrough(Collider2D playerCollider)
        {
            if (playerCollider != null && _platformCollider != null)
            {
                StartCoroutine(DropThroughRoutine(playerCollider));
            }
        }

        private IEnumerator DropThroughRoutine(Collider2D playerCollider)
        {
            Physics2D.IgnoreCollision(_platformCollider, playerCollider, true);
            yield return new WaitForSeconds(_dropThroughDuration);

            if (_platformCollider != null && playerCollider != null)
            {
                Physics2D.IgnoreCollision(_platformCollider, playerCollider, false);
            }
        }
    }
}
