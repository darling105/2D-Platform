using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;
using Platformer2D.Enemies.Base;

namespace Platformer2D.Combat
{
    /// <summary>
    /// Đạn phép thuật / sóng gai tầm xa do Quái vật (Mage, Tiny...) bắn ra:
    /// - Phát hoạt ảnh chuỗi Sprite ở 12 FPS trong lúc bay.
    /// - Tự động lật hướng hiển thị theo chiều bay và hướng gốc của Sprite.
    /// - Chỉ gây sát thương cho Player hoặc vỡ khi đụng tường/đất cứng sau grace time.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public class EnemyProjectile : MonoBehaviour
    {
        private const float DefaultFrameRate = 12f;

        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private CircleCollider2D _col;

        private Sprite[] _frames;
        private Vector2 _direction = Vector2.left;
        private float _speed = 5.5f;
        private int _damage = 1;
        private float _animTimer;
        private int _frameIndex;
        private float _spawnGraceTimer = 0.06f;
        private bool _hasImpacted;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _sr = GetComponent<SpriteRenderer>();
            _col = GetComponent<CircleCollider2D>();

            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _col.isTrigger = true;
            _col.radius = 0.16f;
        }

        /// <summary>
        /// Khởi tạo đạn ngay sau khi Instantiate.
        /// </summary>
        public void Initialize(
            Vector2 direction,
            float speed,
            int damage,
            float lifetime,
            Sprite[] frames,
            bool spriteFacesLeft = true,
            Material sharedMaterial = null)
        {
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.left;
            _speed = Mathf.Max(0.5f, speed);
            _damage = Mathf.Max(1, damage);
            _frames = frames;
            _spawnGraceTimer = 0.06f;
            _hasImpacted = false;

            if (_sr != null)
            {
                if (sharedMaterial != null) _sr.sharedMaterial = sharedMaterial;
                _sr.sortingOrder = 2;
                if (_frames != null && _frames.Length > 0)
                {
                    _sr.sprite = _frames[0];
                }
            }

            // Phóng nhẹ 1.35x để đạn pixel dễ nhìn (độ khó 1 dễ né) và lật đúng hướng bay
            bool movingRight = _direction.x >= 0f;
            float signX = (movingRight == spriteFacesLeft) ? -1f : 1f;
            transform.localScale = new Vector3(signX * 1.35f, 1.35f, 1f);

            _rb.linearVelocity = _direction * _speed;
            Destroy(gameObject, Mathf.Max(0.25f, lifetime));
        }

        private void Update()
        {
            if (_spawnGraceTimer > 0f)
            {
                _spawnGraceTimer -= Time.deltaTime;
            }

            if (_hasImpacted || _frames == null || _frames.Length <= 1 || _sr == null) return;

            _animTimer += Time.deltaTime * DefaultFrameRate;
            int nextIndex = Mathf.FloorToInt(_animTimer) % _frames.Length;
            if (nextIndex != _frameIndex)
            {
                _frameIndex = nextIndex;
                _sr.sprite = _frames[_frameIndex];
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasImpacted || other == null) return;

            // Bỏ qua quái vật và các trigger phụ (Checkpoint, Ladder, Coin, Spike...)
            if (other.GetComponent<BaseEnemy>() != null || other.GetComponentInParent<BaseEnemy>() != null) return;

            if (other.CompareTag("Player"))
            {
                IDamageable damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    float dirX = Mathf.Sign(_direction.x);
                    if (Mathf.Approximately(dirX, 0f)) dirX = 1f;
                    DamageData data = new DamageData(
                        _damage,
                        gameObject,
                        transform.position,
                        new Vector2(dirX * 4.5f, 3.5f),
                        DamageType.Physical);
                    damageable.TakeDamage(data);
                }

                ImpactAndDestroy();
                return;
            }

            // Bỏ qua các collider Trigger khác
            if (other.isTrigger) return;

            // Trong khoảnh khắc đầu vừa bắn ra, tránh tự vỡ vào sàn sát chân
            if (_spawnGraceTimer > 0f) return;

            ImpactAndDestroy();
        }

        private void ImpactAndDestroy()
        {
            if (_hasImpacted) return;
            _hasImpacted = true;
            EventBus.Publish(new PlaySoundEvent("HitImpact", SoundType.SFX, transform.position));
            Destroy(gameObject);
        }
    }
}
