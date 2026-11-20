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
            if (_isDead || _isInvulnerable) return;

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

            // Đẩy lùi (Knockback)
            if (_rb != null && damageData.KnockbackForce != Vector2.zero)
            {
                _rb.linearVelocity = damageData.KnockbackForce;
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
                // Nếu là quái vật: tắt va chạm và tự hủy sau khi diễn xong anim
                var collider = GetComponent<Collider2D>();
                if (collider != null) collider.enabled = false;
                if (_rb != null) _rb.linearVelocity = Vector2.zero;

                if (_deathVfxPrefab != null)
                {
                    Instantiate(_deathVfxPrefab, transform.position, Quaternion.identity);
                }

                Destroy(gameObject, 1.2f);
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
