using UnityEngine;
using Platformer2D.Core.Architecture;

namespace Platformer2D.Environment.Interactive
{
    /// <summary>
    /// Điểm lưu vị trí (Checkpoint):
    /// - Khi nhân vật đi qua cờ / trụ mốc, vị trí sẽ được lưu lại làm điểm hồi sinh.
    /// - Tự động đổi màu hoặc Sprite để thông báo cho người chơi điểm đã được kích hoạt.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [Tooltip("Sprite khi đã kích hoạt (nếu có)")]
        [SerializeField] private Sprite _activeSprite;
        [Tooltip("Màu phát sáng khi kích hoạt")]
        [SerializeField] private Color _activeColor = new Color(0.3f, 1f, 0.4f, 1f);
        [Tooltip("Offset vị trí hồi sinh so với Checkpoint (ví dụ: nhích lên 0.5 unit để không dính đất)")]
        [SerializeField] private Vector2 _respawnOffset = new Vector2(0f, 0.5f);

        private SpriteRenderer _spriteRenderer;
        private bool _isActivated = false;

        public bool IsActivated => _isActivated;
        public Vector3 RespawnPosition => transform.position + (Vector3)_respawnOffset;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isActivated) return;

            if (other.CompareTag("Player"))
            {
                ActivateCheckpoint();
            }
        }

        public void ActivateCheckpoint()
        {
            _isActivated = true;

            if (_activeSprite != null && _spriteRenderer != null)
            {
                _spriteRenderer.sprite = _activeSprite;
            }
            else if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _activeColor;
            }

            EventBus.Publish(new CheckpointActivatedEvent(RespawnPosition));
            EventBus.Publish(new PlaySoundEvent("Checkpoint", SoundType.SFX, transform.position));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _isActivated ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(RespawnPosition, 0.3f);
        }
    }
}
