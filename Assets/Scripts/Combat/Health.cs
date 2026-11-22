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

            // Tắt các component gây sát thương nếu có
            var damageDealer = GetComponent<DamageDealer>();
            if (damageDealer != null) damageDealer.enabled = false;

            // 2. Tắt hoàn toàn physics simulation để script toàn quyền điều khiển transform mà không bị Rigidbody2D khóa/ghi đè vị trí
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _rb.simulated = false;
            }

            // 3. Xác định cao độ mặt sàn bên dưới:
            // Khởi tạo mặt sàn mặc định từ đáy collider hiện tại (nếu đang đứng trên sàn)
            float groundSurfaceY = float.MinValue;
            if (myCollider != null)
            {
                groundSurfaceY = myCollider.bounds.min.y;
            }

            // Quét tia dò tìm mặt sàn vững chắc bên dưới (bỏ qua Trigger, người chơi và quái khác)
            Vector3 rayStart = transform.position + Vector3.up * 0.4f;
            RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, Vector2.down, 16f);
            float bestRayY = float.MinValue;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit2D h = hits[i];
                if (h.collider == null || h.collider == myCollider || h.collider.transform.IsChildOf(transform)) continue;
                if (h.collider.isTrigger) continue;
                if (h.collider.CompareTag("Player") || h.collider.GetComponent<Health>() != null) continue;
                if (h.normal.y > 0.45f) // Bề mặt sàn hướng lên
                {
                    if (h.point.y <= transform.position.y + 0.25f && h.point.y > bestRayY)
                    {
                        bestRayY = h.point.y;
                    }
                }
            }

            if (bestRayY > float.MinValue + 1f)
            {
                groundSurfaceY = bestRayY;
            }

            // Chờ 1 frame để Animator chuyển sang clip Death và cập nhật SpriteRenderer.sprite
            yield return null;

            // 4. Giả lập rơi nếu ở trên không, và trên TỪNG FRAME căn chỉnh đáy SpriteRenderer bám khít chính xác mặt sàn
            float fallSpeed = 0f;
            float deathTimer = 0f;
            const float totalDeathDuration = 1.35f;

            while (deathTimer < totalDeathDuration)
            {
                deathTimer += Time.deltaTime;

                if (groundSurfaceY > float.MinValue + 1f && _spriteRenderer != null && _spriteRenderer.sprite != null)
                {
                    float currentBottom = _spriteRenderer.bounds.min.y;
                    float desiredBottom = groundSurfaceY + 0.015f;

                    // Nếu xác còn ở trên không (ví dụ bị đánh văng lên trời) -> rơi xuống theo gia tốc trọng lực
                    if (currentBottom > desiredBottom + 0.05f)
                    {
                        fallSpeed += 22f * Time.deltaTime;
                        float step = fallSpeed * Time.deltaTime;
                        if (currentBottom - step <= desiredBottom)
                        {
                            transform.position += Vector3.down * (currentBottom - desiredBottom);
                            fallSpeed = 0f;
                        }
                        else
                        {
                            transform.position += Vector3.down * step;
                        }
                    }
                    else
                    {
                        // Đã tiếp đất: Giữ đáy sprite dính khít mặt cỏ trên TẤT CẢ các frame của clip chết
                        float deltaY = currentBottom - desiredBottom;
                        if (Mathf.Abs(deltaY) > 0.001f)
                        {
                            transform.position += Vector3.down * deltaY;
                        }
                    }
                }
                else if (groundSurfaceY <= float.MinValue + 1f)
                {
                    // Rơi xuống vực sâu (không có sàn)
                    transform.position += Vector3.down * (7f * Time.deltaTime);
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
