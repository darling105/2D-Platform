using UnityEngine;
using Platformer2D.Player.Controllers;

namespace Platformer2D.Environment.Interactive
{
    /// <summary>
    /// Thang leo / Dây leo (Ladder / Vine):
    /// - Đặt Trigger Collider bao phủ vùng thang.
    /// - Khi nhân vật đứng trong vùng thang và ấn phím W/S (Lên/Xuống), nhân vật sẽ bám thang để leo.
    /// - Khi ấn Jump (Space), nhân vật sẽ nhảy bật ra khỏi thang.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Ladder : MonoBehaviour
    {
        [Tooltip("Có tự động căn tâm nhân vật vào giữa trục thang khi bắt đầu leo không")]
        [SerializeField] private bool _snapToCenter = true;

        private Collider2D _ladderCollider;

        public float CenterX => _ladderCollider != null ? _ladderCollider.bounds.center.x : transform.position.x;

        private void Awake()
        {
            _ladderCollider = GetComponent<Collider2D>();
            _ladderCollider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.TryGetComponent<PlayerController>(out _))
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.SetNearLadder(this, true);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.TryGetComponent<PlayerController>(out _))
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.SetNearLadder(this, false);
                }
            }
        }

        public bool ShouldSnapToCenter() => _snapToCenter;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}
