using UnityEngine;
using Platformer2D.CameraControl;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;

namespace Platformer2D.Combat
{
    /// <summary>
    /// Component gây sát thương khi va chạm Trigger (Hitbox):
    /// - Gắn vào nấm đấm/vũ khí của Player.
    /// - Gắn vào thân quái vật để gây sát thương khi chạm vào Player (Contact Damage).
    /// - Gắn vào bẫy gai (Hazards).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DamageDealer : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("Lượng sát thương gây ra")]
        [SerializeField] private int _damageAmount = 1;
        [Tooltip("Loại sát thương")]
        [SerializeField] private DamageType _damageType = DamageType.Physical;

        [Header("Knockback Settings")]
        [Tooltip("Độ mạnh của lực đẩy lùi theo phương ngang")]
        [SerializeField] private float _knockbackHorizontal = 6f;
        [Tooltip("Độ mạnh của lực đẩy lùi hất tung lên trên")]
        [SerializeField] private float _knockbackVertical = 4f;

        [Header("Target Filtering")]
        [Tooltip("Tag của mục tiêu bị nhận sát thương (để trống nếu gây sát thương lên mọi đối tượng trừ chủ sở hữu)")]
        [SerializeField] private string _targetTag = "";
        [Tooltip("Chủ sở hữu của hitbox này (để không tự đánh trúng mình)")]
        [SerializeField] private GameObject _owner;

        [Header("Impact Feedback")]
        [SerializeField] private bool _shakeCameraOnHit = true;
        [SerializeField] private float _shakeIntensity = 0.2f;
        [SerializeField] private GameObject _hitVfxPrefab;
        [SerializeField] private string _hitSoundId = "Hit";

        private void Awake()
        {
            if (_owner == null)
            {
                _owner = transform.root.gameObject;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDealDamage(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryDealDamage(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryDealDamage(collision.collider);
        }

        private void TryDealDamage(Collider2D other)
        {
            if (other == null) return;

            // Bỏ qua chủ sở hữu
            if (_owner != null && (other.gameObject == _owner || other.transform.IsChildOf(_owner.transform)))
            {
                return;
            }

            // Lọc theo tag nếu có chỉ định
            if (!string.IsNullOrEmpty(_targetTag) && !other.CompareTag(_targetTag))
            {
                return;
            }

            // Tìm component nhận sát thương IDamageable
            var damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                // Tính toán hướng đẩy lùi (Knockback Direction)
                float directionX = Mathf.Sign(other.transform.position.x - (_owner != null ? _owner.transform.position.x : transform.position.x));
                Vector2 knockback = new Vector2(directionX * _knockbackHorizontal, _knockbackVertical);

                DamageData data = new DamageData(
                    amount: _damageAmount,
                    source: _owner,
                    hitPoint: other.ClosestPoint(transform.position),
                    knockbackForce: knockback,
                    type: _damageType
                );

                damageable.TakeDamage(data);

                // Feedback: Rung màn hình
                if (_shakeCameraOnHit && CameraFollow2D.Instance != null)
                {
                    CameraFollow2D.Instance.Shake(0.12f, _shakeIntensity);
                }

                // Feedback: Âm thanh qua EventBus
                if (!string.IsNullOrEmpty(_hitSoundId))
                {
                    EventBus.Publish(new PlaySoundEvent(_hitSoundId, SoundType.SFX, transform.position));
                }

                // Feedback: VFX tia lửa va chạm
                if (_hitVfxPrefab != null)
                {
                    Instantiate(_hitVfxPrefab, other.ClosestPoint(transform.position), Quaternion.identity);
                }
            }
        }
    }
}
