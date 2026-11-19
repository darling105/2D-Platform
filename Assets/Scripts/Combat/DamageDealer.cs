using System.Collections.Generic;
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

        private readonly HashSet<IDamageable> _hitTargetsThisSwing = new HashSet<IDamageable>();
        private bool _useSwingSession;
        private int _swingDamageOverride = -1;
        private float _swingKnockbackScale = 1f;
        private float _swingShakeScale = 1f;

        private void Awake()
        {
            if (_owner == null)
            {
                _owner = transform.root.gameObject;
            }
        }

        /// <summary>
        /// Bắt đầu một lượt vung đòn cận chiến mới (reset danh sách mục tiêu đã trúng trong lượt).
        /// </summary>
        public void BeginSwing(int damageOverride = -1, float knockbackScale = 1f, float shakeScale = 1f)
        {
            _useSwingSession = true;
            _swingDamageOverride = damageOverride;
            _swingKnockbackScale = knockbackScale;
            _swingShakeScale = shakeScale;
            _hitTargetsThisSwing.Clear();
        }

        /// <summary>
        /// Kết thúc lượt vung đòn cận chiến và xóa trạng thái ghi đè.
        /// </summary>
        public void EndSwing()
        {
            _useSwingSession = false;
            _swingDamageOverride = -1;
            _swingKnockbackScale = 1f;
            _swingShakeScale = 1f;
            _hitTargetsThisSwing.Clear();
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
                if (_useSwingSession && !_hitTargetsThisSwing.Add(damageable))
                {
                    return;
                }

                int finalDamage = (_useSwingSession && _swingDamageOverride > 0) ? _swingDamageOverride : _damageAmount;
                float kbScale = _useSwingSession ? _swingKnockbackScale : 1f;
                float shakeScale = _useSwingSession ? _swingShakeScale : 1f;

                // Tính toán hướng đẩy lùi (Knockback Direction)
                float directionX = Mathf.Sign(other.transform.position.x - (_owner != null ? _owner.transform.position.x : transform.position.x));
                Vector2 knockback = new Vector2(directionX * _knockbackHorizontal * kbScale, _knockbackVertical * kbScale);

                DamageData data = new DamageData(
                    amount: finalDamage,
                    source: _owner,
                    hitPoint: other.ClosestPoint(transform.position),
                    knockbackForce: knockback,
                    type: _damageType
                );

                damageable.TakeDamage(data);

                // Feedback: Rung màn hình
                if (_shakeCameraOnHit && CameraFollow2D.Instance != null)
                {
                    CameraFollow2D.Instance.Shake(0.12f, _shakeIntensity * shakeScale);
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
