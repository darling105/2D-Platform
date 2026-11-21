using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;

namespace Platformer2D.Combat
{
    /// <summary>
    /// Đạn ném tầm xa của người chơi (Player Projectile / Rock / Dart):
    /// - Bay theo hướng nhân vật nhìn với tốc độ cấu hình.
    /// - Gây sát thương và đẩy lùi (Knockback) khi trúng mục tiêu có IDamageable.
    /// - Tự hủy khi va chạm vào tường, đất hoặc sau thời gian tồn tại tối đa.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerProjectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [Tooltip("Tốc độ bay của đạn (units/s)")]
        [SerializeField] private float _speed = 12f;
        [Tooltip("Lực sát thương")]
        [SerializeField] private int _damage = 1;
        [Tooltip("Lực hất văng mục tiêu khi trúng")]
        [SerializeField] private Vector2 _knockbackForce = new Vector2(5f, 3f);
        [Tooltip("Thời gian tồn tại tối đa trước khi tự hủy (giây)")]
        [SerializeField] private float _lifeTime = 3f;
        [Tooltip("Âm thanh phát khi trúng đích")]
        [SerializeField] private string _hitSound = "HitImpact";
        [Tooltip("Hiệu ứng nổ/vỡ khi chạm tường hoặc quái")]
        [SerializeField] private GameObject _hitVfxPrefab;

        private Rigidbody2D _rb;
        private Vector2 _direction = Vector2.right;
        private GameObject _owner;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0.5f; // Rơi theo đường vòng cung tự nhiên nhẹ
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true; // Dùng trigger để va chạm mượt mà
        }

        private void Start()
        {
            Destroy(gameObject, _lifeTime);
        }

        public void Initialize(Vector2 direction, GameObject owner)
        {
            _owner = owner;
            _direction = direction.normalized;
            _rb.linearVelocity = _direction * _speed;

            // Xoay đầu đạn theo hướng bay
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void SetDamage(int dmg)
        {
            _damage = Mathf.Max(1, dmg);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Bỏ qua nếu chạm chính người ném hoặc collider con của người ném (hitbox, v.v.)
            if (_owner != null && (other.gameObject == _owner || other.transform.IsChildOf(_owner.transform))) return;
            if (other.CompareTag("Player")) return;

            IDamageable damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                Vector2 knockback = new Vector2(Mathf.Sign(_direction.x) * _knockbackForce.x, _knockbackForce.y);
                DamageData data = new DamageData(_damage, gameObject, transform.position, knockback, DamageType.Physical);
                damageable.TakeDamage(data);

                OnHit();
                return;
            }

            // Chạm đất hoặc tường cứng
            if (!other.isTrigger)
            {
                OnHit();
            }
        }

        private void OnHit()
        {
            if (_hitVfxPrefab != null)
            {
                Instantiate(_hitVfxPrefab, transform.position, Quaternion.identity);
            }

            EventBus.Publish(new PlaySoundEvent(_hitSound, SoundType.SFX, transform.position));
            Destroy(gameObject);
        }
    }
}
