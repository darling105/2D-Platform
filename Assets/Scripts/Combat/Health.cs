using System;
using System.Collections;
using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;

namespace Platformer2D.Combat
{
    /// <summary>
    /// Component quản lý Máu & Nhận sát thương đa năng:
    /// - Dùng chung được cho cả Player, Quái vật (Enemies), Boss hoặc Thùng gỗ phá hủy được.
    /// - Kế thừa IDamageable, IHealable.
    /// - Hỗ trợ thời gian bất tử (i-frames) và hiệu ứng nhấp nháy đỏ khi bị trúng đòn.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable, IHealable
    {
        [Header("Health Settings")]
        [SerializeField] private int _maxHealth = 3;
        [SerializeField] private bool _isPlayer = false;

        [Header("Invulnerability (i-Frames)")]
        [Tooltip("Thời gian bất tử sau khi nhận sát thương (giây)")]
        [SerializeField] private float _invulnerabilityDuration = 0.8f;
        [Tooltip("Thời gian bất tử cho Quái/Vật thể không phải Player (ngắn để nhận đủ combo nhiều đòn)")]
        [SerializeField] private float _enemyInvulnerabilityDuration = 0.08f;
        [Tooltip("Thời gian nháy màu khi trúng đòn đối với Quái/Vật thể")]
        [SerializeField] private float _hitFlashDuration = 0.12f;
        [Tooltip("Màu nhấp nháy khi bị thương")]
        [SerializeField] private Color _flashColor = new Color(1f, 0.3f, 0.3f, 0.7f);

        [Header("Feedback Effects")]
        [SerializeField] private GameObject _deathVfxPrefab;

        private int _currentHealth;
        private bool _isInvulnerable;
        private bool _isDead;
        private Coroutine _invulnerabilityCoroutine;
        private Coroutine _hitFlashCoroutine;

        private SpriteRenderer _spriteRenderer;
        private Color _originalColor;
        private Rigidbody2D _rb;
        private Animator _animator;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsDead => _isDead;
        public bool IsAtMaxHealth => _currentHealth >= _maxHealth;

        public event Action<int, int> OnHealthChanged;
        public event Action<DamageData> OnDamaged;
        public event Action OnDeath;

        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

        private void Awake()
        {
            _currentHealth = _maxHealth;
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();

            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }
        }

        public void TakeDamage(DamageData damageData)
        {
            bool isLethalFall = damageData.Type == DamageType.FallDamage || damageData.Amount >= 9999;
            if (!isLethalFall)
            {
                if (_isDead || _isInvulnerable) return;
            }
            else
            {
                if (_isDead) return;
            }

            _currentHealth = Mathf.Max(0, _currentHealth - damageData.Amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnDamaged?.Invoke(damageData);

            // Bắn event toàn cục nếu là Player để UI tự cập nhật thanh máu
            if (_isPlayer)
            {
                EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));
            }

            // Kích hoạt animation Hurt nếu có Animator
            if (_animator != null)
            {
                _animator.SetTrigger(HurtHash);
            }

            // Đẩy lùi (Knockback) - Giới hạn lực hất tung thẳng đứng để quái không bị hất bổng lên trời
            if (_rb != null && damageData.KnockbackForce != Vector2.zero)
            {
                Vector2 kb = damageData.KnockbackForce;
                kb.y = Mathf.Clamp(kb.y, -3f, 1.4f);
                _rb.linearVelocity = kb;
            }

            // Kiểm tra tử vong
            if (_currentHealth <= 0)
            {
                Die();
            }
            else
            {
                float invulnDuration = _isPlayer ? _invulnerabilityDuration : _enemyInvulnerabilityDuration;
                if (invulnDuration > 0f)
                {
                    if (_invulnerabilityCoroutine != null) StopCoroutine(_invulnerabilityCoroutine);
                    _invulnerabilityCoroutine = StartCoroutine(InvulnerabilityRoutine(invulnDuration));
                }

                if (!_isPlayer && _hitFlashDuration > 0f)
                {
                    if (_hitFlashCoroutine != null) StopCoroutine(_hitFlashCoroutine);
                    _hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
                }
            }
        }

        public void Heal(int amount)
        {
            if (_isDead || amount <= 0 || IsAtMaxHealth) return;

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_isPlayer)
            {
                EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));
            }
        }

        public void IncreaseMaxHealth(int amount)
        {
            if (amount <= 0) return;
            _maxHealth += amount;
            _currentHealth += amount;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            if (_isPlayer)
            {
                EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));
            }
        }

        public void SetMaxHealth(int newMaxHealth)
        {
            _maxHealth = Mathf.Max(1, newMaxHealth);
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            if (_isPlayer)
            {
                EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            if (_invulnerabilityCoroutine != null)
            {
                StopCoroutine(_invulnerabilityCoroutine);
                _invulnerabilityCoroutine = null;
            }
            if (_hitFlashCoroutine != null)
            {
                StopCoroutine(_hitFlashCoroutine);
                _hitFlashCoroutine = null;
            }
            _isInvulnerable = false;
            if (_spriteRenderer != null) _spriteRenderer.color = _originalColor;

            OnDeath?.Invoke();

            if (_animator != null)
            {
                _animator.SetBool(IsDeadHash, true);
            }

            if (_isPlayer)
            {
                StartCoroutine(PlayerDeathDelayRoutine());
            }
            else
            {
                // Thưởng EXP và điểm khi quái vật bị tiêu diệt
                int expReward = Mathf.Max(25, _maxHealth * 18);
                int coinReward = Mathf.Max(1, _maxHealth * 2);
                EventBus.Publish(new EnemyDefeatedEvent(transform.position, expReward, coinReward));

                // Quái vật khi chết: Cho phép rơi tự nhiên chạm sàn nếu đang ở trên không, không để lơ lửng
                var collider = GetComponent<Collider2D>();
                StartCoroutine(EnemyDeathPhysicsRoutine(collider));

                if (_deathVfxPrefab != null)
                {
                    Instantiate(_deathVfxPrefab, transform.position, Quaternion.identity);
                }

                Destroy(gameObject, 1.4f);
            }
        }

        private IEnumerator EnemyDeathPhysicsRoutine(Collider2D myCollider)
        {
            // 1. Tắt va chạm vật lý ngay lập tức để không cản trở người chơi hay vật thể khác
            if (myCollider != null)
            {
                myCollider.enabled = false;
            }

            // 2. Chuyển sang Kinematic để script tự điều khiển căn chỉnh vị trí tuyệt đối, không bị vật lý rung giật hay đẩy ngược
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.bodyType = RigidbodyType2D.Kinematic;
            }

            // 3. Quét tia dò tìm mặt sàn vững chắc gần nhất bên dưới (bỏ qua Trigger và các vật thể con)
            float groundSurfaceY = float.MinValue;
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position + Vector3.up * 0.25f, Vector2.down, 15f);
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit2D h = hits[i];
                if (h.collider == null || h.collider == myCollider || h.collider.transform.IsChildOf(transform)) continue;
                if (h.collider.isTrigger) continue; // Bỏ qua nhặt đá, coin, checkpoint, vùng trigger
                if (h.normal.y > 0.55f) // Bề mặt sàn hướng lên
                {
                    groundSurfaceY = h.point.y;
                    break;
                }
            }

            // 4. Nếu tìm thấy sàn: Giả lập rơi tự do mượt mà nếu đang ở trên không, và trên TỪNG FRAME căn chỉnh
            // đáy SpriteRenderer bám khít chính xác mặt sàn (kể cả khi animation chết chuyển từ đứng sang nằm bẹp)
            float fallSpeed = 0f;
            float deathTimer = 0f;
            const float totalDeathDuration = 1.35f;

            while (deathTimer < totalDeathDuration)
            {
                deathTimer += Time.deltaTime;

                if (groundSurfaceY > float.MinValue + 1f && _spriteRenderer != null)
                {
                    float currentBottom = _spriteRenderer.bounds.min.y;
                    float desiredBottom = groundSurfaceY + 0.015f;

                    // Nếu xác còn ở trên không (ví dụ bị đánh văng lên trời) -> rơi xuống theo gia tốc trọng lực
                    if (currentBottom > desiredBottom + 0.02f)
                    {
                        fallSpeed += 24f * Time.deltaTime;
                        float nextBottom = currentBottom - fallSpeed * Time.deltaTime;
                        if (nextBottom <= desiredBottom)
                        {
                            transform.position += Vector3.down * (currentBottom - desiredBottom);
                            fallSpeed = 0f;
                        }
                        else
                        {
                            transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
                        }
                    }
                    else
                    {
                        // Đã tiếp đất: Giữ đáy sprite dính khít mặt cỏ trên TẤT CẢ các frame của clip chết
                        float deltaY = currentBottom - desiredBottom;
                        if (Mathf.Abs(deltaY) > 0.002f)
                        {
                            transform.position += Vector3.down * deltaY;
                        }
                    }
                }
                else if (groundSurfaceY <= float.MinValue + 1f)
                {
                    // Rơi xuống vực sâu (không có sàn)
                    transform.position += Vector3.down * (8f * Time.deltaTime);
                }

                // 0.35 giây cuối cùng: Mờ dần (Fade out alpha) trước khi biến mất sạch sẽ
                if (deathTimer > 0.95f && _spriteRenderer != null)
                {
                    float fadeProgress = (deathTimer - 0.95f) / 0.4f;
                    Color c = _spriteRenderer.color;
                    c.a = Mathf.Lerp(_originalColor.a, 0f, fadeProgress);
                    _spriteRenderer.color = c;
                }

                yield return null;
            }
        }

        private IEnumerator PlayerDeathDelayRoutine()
        {
            yield return new WaitForSeconds(1.15f);
            EventBus.Publish(new PlayerDiedEvent(transform.position));
        }

        private IEnumerator InvulnerabilityRoutine(float duration)
        {
            _isInvulnerable = true;

            if (_isPlayer)
            {
                float elapsed = 0f;
                float blinkInterval = 0.1f;

                while (elapsed < duration)
                {
                    if (_spriteRenderer != null)
                    {
                        _spriteRenderer.color = (_spriteRenderer.color == _originalColor) ? _flashColor : _originalColor;
                    }
                    yield return new WaitForSeconds(blinkInterval);
                    elapsed += blinkInterval;
                }

                if (_spriteRenderer != null && !_isDead)
                {
                    _spriteRenderer.color = _originalColor;
                }
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }

            _isInvulnerable = false;
            _invulnerabilityCoroutine = null;
        }

        private IEnumerator HitFlashRoutine()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _flashColor;
                yield return new WaitForSeconds(_hitFlashDuration);
                if (_spriteRenderer != null && !_isDead)
                {
                    _spriteRenderer.color = _originalColor;
                }
            }
            _hitFlashCoroutine = null;
        }
    }
}
