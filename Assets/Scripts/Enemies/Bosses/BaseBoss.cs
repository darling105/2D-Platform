using System;
using System.Collections;
using UnityEngine;
using Platformer2D.CameraControl;
using Platformer2D.Combat;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;

namespace Platformer2D.Enemies.Bosses
{
    public enum BossAIState
    {
        Dormant,
        Intro,
        Chase,
        Attack1,
        Attack2,
        SpecialAttack,
        Sneer,
        Hurt,
        Dead
    }

    public enum BossPhase
    {
        Phase1,
        EnragedPhase2
    }

    /// <summary>
    /// Kiến trúc lớp cơ sở cho toàn bộ Boss trong game (Demon, Goblin King, Necromancer):
    /// - Quản lý Thanh Máu Boss, Đổi Giai đoạn Cuồng nộ (Enrage Phase 2 dưới 50% HP).
    /// - Điều khiển đòn đánh liên hoàn, lướt áp sát và chiêu thức tất sát.
    /// - Xử lý hoạt ảnh tiếp đất mượt mà khi chết, mở khóa đấu trường và thưởng quà chiến thắng.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class BaseBoss : MonoBehaviour, IDamageable
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int Attack2Hash = Animator.StringToHash("Attack2");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

        [Header("Boss Identity")]
        [SerializeField] protected string _bossDisplayName = "CHÚA QUỶ ĐỒNG CỎ";
        [SerializeField] protected int _maxHealth = 25;
        [SerializeField] protected float _baseMoveSpeed = 2.0f;
        [SerializeField] protected float _chaseRange = 16f;
        [SerializeField] protected float _meleeRange = 2.4f;

        [Header("Combat Settings")]
        [SerializeField] protected int _meleeDamage = 1;
        [SerializeField] protected float _attackCooldown = 2.8f;
        [SerializeField] protected float _specialCooldown = 7.5f;
        [SerializeField] protected float _enrageSpeedMultiplier = 1.18f;

        [Header("Visuals & Facing")]
        [SerializeField] protected bool _spriteFacesLeftByDefault = true;
        [SerializeField] protected Color _enrageTint = new Color(1f, 0.45f, 0.45f, 1f);

        protected Rigidbody2D _rb;
        protected BoxCollider2D _collider;
        protected Animator _animator;
        protected SpriteRenderer _spriteRenderer;
        protected Color _originalColor;

        protected int _currentHealth;
        protected BossAIState _state = BossAIState.Dormant;
        protected BossPhase _phase = BossPhase.Phase1;
        protected Transform _playerTransform;
        protected Health _playerHealth;
        protected Vector3 _initialSpawnPosition;

        protected float _stateTimer = 0f;
        protected float _attackCooldownTimer = 0f;
        protected float _specialCooldownTimer = 0f;
        protected bool _isFacingRight = false;
        protected bool _isEnraged = false;
        protected bool _isInvulnerable = false;
        protected bool _isDead = false;

        public string BossDisplayName => _bossDisplayName;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsDead => _isDead;
        public BossPhase CurrentPhase => _phase;

        public event Action<int, int> OnBossHealthChanged;
        public event Action OnBossDeath;

        protected virtual void Awake()
        {
            _initialSpawnPosition = transform.position;
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }

            _currentHealth = _maxHealth;

            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.mass = 12f;
            _rb.linearDamping = 1.5f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        protected virtual void Start()
        {
            ResolvePlayer();
            SetFacing(_isFacingRight);
        }

        protected virtual void Update()
        {
            if (_isDead || _state == BossAIState.Dormant) return;

            DecrementTimers(Time.deltaTime);
            ResolvePlayer();

            switch (_state)
            {
                case BossAIState.Intro:
                    UpdateIntroState();
                    break;
                case BossAIState.Chase:
                    UpdateChaseState();
                    break;
                case BossAIState.Sneer:
                    UpdateSneerState();
                    break;
            }
        }

        private void DecrementTimers(float dt)
        {
            if (_attackCooldownTimer > 0f) _attackCooldownTimer -= dt;
            if (_specialCooldownTimer > 0f) _specialCooldownTimer -= dt;
            if (_stateTimer > 0f) _stateTimer -= dt;
        }

        private void ResolvePlayer()
        {
            if (_playerTransform != null && _playerHealth != null && !_playerHealth.IsDead) return;

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
                _playerHealth = playerObj.GetComponent<Health>();
            }
        }

        /// <summary>
        /// Kích hoạt trận chiến Boss (được gọi từ BossArenaTrigger khi người chơi bước vào sàn đấu).
        /// </summary>
        public virtual void ActivateBoss()
        {
            if (_state != BossAIState.Dormant) return;

            _state = BossAIState.Intro;
            _stateTimer = 1.6f;
            _rb.linearVelocityX = 0f;

            EventBus.Publish(new BossSpawnedEvent(_bossDisplayName, _currentHealth, _maxHealth));
            EventBus.Publish(new PlaySoundEvent("Roar", SoundType.SFX, transform.position));
            CameraFollow2D.TriggerShake(0.12f, 0.08f);

            if (_animator != null)
            {
                _animator.Play("Demon_Sneer");
            }
        }

        /// <summary>
        /// Khôi phục trạng thái Boss về ban đầu khi Người chơi bị tiêu diệt và chọn Đánh lại Boss (Retry):
        /// - Đưa Boss về vị trí xuất phát ban đầu.
        /// - Khôi phục 100% Máu và hủy trạng thái Cuồng nộ (Enrage Phase 2).
        /// - Dừng mọi chiêu thức đang dở dang và đưa về trạng thái Dormant/Idle.
        /// </summary>
        public virtual void ResetBoss()
        {
            if (_isDead) return;

            StopAllCoroutines();
            _state = BossAIState.Dormant;
            _phase = BossPhase.Phase1;
            _isEnraged = false;
            _isInvulnerable = false;
            _currentHealth = _maxHealth;
            _stateTimer = 0f;
            _attackCooldownTimer = 0f;
            _specialCooldownTimer = 0f;

            transform.position = _initialSpawnPosition;

            if (_rb != null)
            {
                _rb.simulated = true;
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }

            if (_collider != null)
            {
                _collider.enabled = true;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalColor;
            }

            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash, 0f);
                _animator.SetBool(IsGroundedHash, true);
                _animator.Play("Demon_Idle");
            }

            SetFacing(!_spriteFacesLeftByDefault);
            OnBossHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        protected virtual void UpdateIntroState()
        {
            _rb.linearVelocityX = 0f;
            if (_animator != null) _animator.SetFloat(SpeedHash, 0f);

            if (_stateTimer <= 0f)
            {
                _state = BossAIState.Chase;
            }
        }

        protected virtual void UpdateChaseState()
        {
            if (_playerTransform == null)
            {
                _rb.linearVelocityX = 0f;
                if (_animator != null) _animator.SetFloat(SpeedHash, 0f);
                return;
            }

            float dx = _playerTransform.position.x - transform.position.x;
            float absDx = Mathf.Abs(dx);

            // Quay mặt về phía người chơi
            SetFacing(dx > 0f);

            // Kiểm tra tung chiêu đặc biệt (Special Attack / Dash / AoE)
            if (_specialCooldownTimer <= 0f && absDx <= 9f && absDx >= 3.0f)
            {
                ExecuteSpecialAttack();
                return;
            }

            // Kiểm tra đòn cận chiến
            if (absDx <= _meleeRange)
            {
                _rb.linearVelocityX = 0f;
                if (_animator != null) _animator.SetFloat(SpeedHash, 0f);

                if (_attackCooldownTimer <= 0f)
                {
                    ExecuteMeleeAttack();
                }
                return;
            }

            // Di chuyển áp sát người chơi
            float speed = _baseMoveSpeed * (_isEnraged ? _enrageSpeedMultiplier : 1.0f);
            float moveDir = Mathf.Sign(dx);
            _rb.linearVelocityX = moveDir * speed;

            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash, Mathf.Abs(_rb.linearVelocityX));
                _animator.SetBool(IsGroundedHash, true);
            }
        }

        protected virtual void ExecuteMeleeAttack()
        {
            _state = BossAIState.Attack1;
            _rb.linearVelocityX = 0f;
            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash, 0f);
                // Đổi xen kẽ giữa Attack 1 và Attack 2
                bool useCombo2 = (_phase == BossPhase.EnragedPhase2 && UnityEngine.Random.value > 0.4f);
                _animator.SetTrigger(useCombo2 ? Attack2Hash : AttackHash);
            }

            float animDuration = _isEnraged ? 0.85f : 1.05f;
            StartCoroutine(MeleeAttackRoutine(animDuration));
        }

        protected virtual IEnumerator MeleeAttackRoutine(float duration)
        {
            yield return new WaitForSeconds(duration * 0.45f);

            // Gây sát thương tại vùng trước mặt
            DealHitboxDamage(_meleeRange * 0.85f, _meleeDamage);

            yield return new WaitForSeconds(duration * 0.55f);

            _attackCooldownTimer = _isEnraged ? (_attackCooldown * 0.8f) : _attackCooldown;
            _state = BossAIState.Chase;
        }

        protected virtual void ExecuteSpecialAttack()
        {
            _state = BossAIState.SpecialAttack;
            _rb.linearVelocityX = 0f;
            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash, 0f);
                _animator.SetTrigger(Attack2Hash);
            }

            StartCoroutine(SpecialAttackRoutine());
        }

        protected virtual IEnumerator SpecialAttackRoutine()
        {
            // Lao húc hoặc dậm đất
            CameraFollow2D.TriggerShake(0.08f, 0.06f);
            float chargeDir = _isFacingRight ? 1f : -1f;
            float chargeSpeed = _baseMoveSpeed * 2.0f;

            float chargeTime = 0.42f;
            while (chargeTime > 0f)
            {
                chargeTime -= Time.deltaTime;
                _rb.linearVelocityX = chargeDir * chargeSpeed;
                DealHitboxDamage(_meleeRange * 1.0f, _meleeDamage);
                yield return null;
            }

            _rb.linearVelocityX = 0f;
            _specialCooldownTimer = _isEnraged ? (_specialCooldown * 0.8f) : _specialCooldown;
            _attackCooldownTimer = 1.4f;
            _state = BossAIState.Chase;
        }

        protected void DealHitboxDamage(float hitRadius, int damage)
        {
            Vector2 hitCenter = (Vector2)transform.position + new Vector2((_isFacingRight ? 1f : -1f) * (hitRadius * 0.6f), 0.2f);
            Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, hitRadius);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i];
                if (col.CompareTag("Player"))
                {
                    IDamageable damageable = col.GetComponent<IDamageable>();
                    if (damageable != null && !damageable.IsDead)
                    {
                        Vector2 kb = new Vector2((_isFacingRight ? 1f : -1f) * 8f, 3.5f);
                        damageable.TakeDamage(new DamageData(damage, gameObject, col.transform.position, kb, DamageType.Physical));
                        CameraFollow2D.TriggerShake(0.14f, 0.12f);
                        EventBus.Publish(new PlaySoundEvent("EnemyHurt", SoundType.SFX, hitCenter));
                    }
                    break;
                }
            }
        }

        public virtual void TakeDamage(DamageData damageData)
        {
            if (_isDead || _isInvulnerable) return;

            _currentHealth = Mathf.Max(0, _currentHealth - damageData.Amount);
            OnBossHealthChanged?.Invoke(_currentHealth, _maxHealth);
            EventBus.Publish(new BossHealthChangedEvent(_currentHealth, _maxHealth));

            StartCoroutine(BossDamageFlashRoutine());

            // Kiểm tra Cuồng nộ (Enrage Phase 2 dưới 50% HP)
            if (!_isEnraged && _currentHealth <= _maxHealth * 0.5f)
            {
                EnterEnragePhase();
            }

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void EnterEnragePhase()
        {
            _isEnraged = true;
            _phase = BossPhase.EnragedPhase2;

            _state = BossAIState.Sneer;
            _stateTimer = 1.4f;
            _rb.linearVelocityX = 0f;

            if (_animator != null)
            {
                _animator.Play("Demon_Sneer");
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _enrageTint;
            }

            EventBus.Publish(new PlaySoundEvent("Roar", SoundType.SFX, transform.position));
            CameraFollow2D.TriggerShake(0.18f, 0.14f);
            EventBus.Publish(new HudToastMessageEvent("CẢNH BÁO: BOSS ĐÃ CUỒNG NỘ (PHASE 2)!", new Color(1f, 0.2f, 0.2f, 1f)));
        }

        protected virtual void UpdateSneerState()
        {
            _rb.linearVelocityX = 0f;
            if (_stateTimer <= 0f)
            {
                _state = BossAIState.Chase;
            }
        }

        protected virtual IEnumerator BossDamageFlashRoutine()
        {
            if (_spriteRenderer == null) yield break;
            Color baseColor = _isEnraged ? _enrageTint : _originalColor;
            _spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            if (_spriteRenderer != null && !_isDead)
            {
                _spriteRenderer.color = baseColor;
            }
        }

        protected virtual void Die()
        {
            if (_isDead) return;
            _isDead = true;
            _state = BossAIState.Dead;

            StopAllCoroutines();

            if (_collider != null) _collider.enabled = false;
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _rb.simulated = false;
            }

            if (_animator != null)
            {
                _animator.SetBool(IsDeadHash, true);
                _animator.Play("Demon_Death");
            }

            EventBus.Publish(new PlaySoundEvent("EnemyHurt", SoundType.SFX, transform.position));
            EventBus.Publish(new BossDefeatedEvent(_bossDisplayName, transform.position));
            OnBossDeath?.Invoke();

            CameraFollow2D.TriggerShake(0.24f, 0.18f);

            StartCoroutine(BossDeathAlignAndLootRoutine());
        }

        protected virtual IEnumerator BossDeathAlignAndLootRoutine()
        {
            // Chờ 1 frame để Animator chuyển sang frame đầu của hoạt ảnh tử vong
            yield return null;

            // Dò tìm mặt sàn bằng raycast và căn chỉnh đáy sprite khít sàn
            float groundY = _collider != null ? _collider.bounds.min.y : transform.position.y - 1.0f;
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position + Vector3.up * 0.5f, Vector2.down, 14f);
            float bestRayY = float.MinValue;
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.collider == null || h.collider == _collider || h.collider.transform.IsChildOf(transform)) continue;
                if (h.collider.isTrigger) continue;
                if (h.normal.y > 0.45f && h.point.y <= transform.position.y + 0.5f && h.point.y > bestRayY)
                {
                    bestRayY = h.point.y;
                }
            }

            if (bestRayY > float.MinValue + 1f)
            {
                groundY = bestRayY;
            }

            float timer = 0f;
            while (timer < 2.5f)
            {
                timer += Time.deltaTime;
                if (_spriteRenderer != null)
                {
                    float bottom = _spriteRenderer.bounds.min.y;
                    float deltaY = bottom - groundY;
                    if (Mathf.Abs(deltaY) > 0.005f)
                    {
                        transform.position += Vector3.down * deltaY;
                    }

                    // 0.8s cuối mờ dần
                    if (timer > 1.7f)
                    {
                        float alpha = Mathf.Clamp01((2.5f - timer) / 0.8f);
                        Color c = _spriteRenderer.color;
                        c.a = alpha;
                        _spriteRenderer.color = c;
                    }
                }
                yield return null;
            }

            Destroy(gameObject);
        }

        protected void SetFacing(bool faceRight)
        {
            _isFacingRight = faceRight;
            Vector3 scale = transform.localScale;
            float absX = Mathf.Abs(scale.x);
            bool positive = _isFacingRight != _spriteFacesLeftByDefault;
            scale.x = positive ? absX : -absX;
            transform.localScale = scale;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector2 hitCenter = (Vector2)transform.position + new Vector2((_isFacingRight ? 1f : -1f) * (_meleeRange * 0.6f), 0.2f);
            Gizmos.DrawWireSphere(hitCenter, _meleeRange);
        }
    }
}
