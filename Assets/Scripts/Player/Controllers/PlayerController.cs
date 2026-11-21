using System.Collections;
using UnityEngine;
using Platformer2D.CameraControl;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;
using Platformer2D.Environment.Interactive;
using Platformer2D.Player.Components;
using Platformer2D.Player.Input;

namespace Platformer2D.Player.Controllers
{
    /// <summary>
    /// Bộ điều phối trung tâm của nhân vật chính (Player Controller - Facade / Mediator):
    /// - Tuân thủ nguyên tắc Đơn trách nhiệm (Single Responsibility Principle - SRP).
    /// - Điều phối các Sub-components chuyên biệt: PlayerInputHandler, PlayerMovement, PlayerCombat, PlayerAnimationSync.
    /// - Quản lý sinh mệnh (Health), nhận sát thương (IDamageable), hồi máu (IHealable) và quy trình hồi sinh (Respawn).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerCombat))]
    [RequireComponent(typeof(PlayerAnimationSync))]
    [RequireComponent(typeof(PlayerProgression))]
    public class PlayerController : MonoBehaviour, IDamageable, IHealable
    {
        [Header("Health Settings")]
        [Tooltip("Lượng máu tối đa của nhân vật")]
        [SerializeField] private int _maxHealth = 3;
        [Tooltip("Thời gian bất tử tạm (i-frames) sau khi nhận sát thương")]
        [SerializeField] private float _invulnerabilityDuration = 0.8f;
        [Tooltip("Nhịp nhấp nháy trong lúc bất tử tạm")]
        [SerializeField] private float _hurtBlinkInterval = 0.08f;
        [Tooltip("Màu nhấp nháy đỏ khi bị thương")]
        [SerializeField] private Color _hurtFlashColor = new Color(1f, 0.22f, 0.22f, 0.92f);

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private PlayerInputHandler _input;
        private PlayerMovement _movement;
        private PlayerCombat _combat;
        private PlayerAnimationSync _animSync;
        private PlayerSkills _skills;
        private PlayerProgression _progression;

        private int _currentHealth;
        private bool _isDead;
        private bool _isInvulnerable;
        private float _hurtLockTimer;
        private Color _baseSpriteColor = Color.white;
        private Coroutine _invulnerabilityCoroutine;
        private Coroutine _deathSequenceCoroutine;

        public bool IsDead => _isDead;
        public bool IsHurt => _hurtLockTimer > 0f;
        public bool IsInvulnerable => _isInvulnerable;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsAtMaxHealth => _currentHealth >= _maxHealth;
        public bool IsDashing => _movement != null && _movement.IsDashing;
        public PlayerSkills Skills => _skills;
        public PlayerProgression Progression => _progression;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _baseSpriteColor = _spriteRenderer.color;
            }
            _input = GetComponent<PlayerInputHandler>();
            _movement = GetComponent<PlayerMovement>();
            _combat = GetComponent<PlayerCombat>();
            _animSync = GetComponent<PlayerAnimationSync>();
            _skills = GetComponent<PlayerSkills>();
            if (_skills == null) _skills = gameObject.AddComponent<PlayerSkills>();
            _progression = GetComponent<PlayerProgression>();
            if (_progression == null) _progression = gameObject.AddComponent<PlayerProgression>();

            _currentHealth = _maxHealth;

            transform.SetParent(null);
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void Start()
        {
            EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));
        }

        private void Update()
        {
            if (_isDead) return;

            // Lưới an toàn chống rơi vô tận: Rơi qua vực sâu đáy màn (Y < -11f)
            if (transform.position.y < -11f)
            {
                ForceKill();
                return;
            }

            if (_hurtLockTimer > 0f)
            {
                _hurtLockTimer -= Time.deltaTime;
                _animSync.ProcessUpdate(_isDead);
                return;
            }

            _movement.ProcessUpdate();
            _combat.ProcessUpdate();
            if (_skills != null) _skills.ProcessUpdate();
            if (_progression != null) _progression.ProcessUpdate();
            _animSync.ProcessUpdate(_isDead);
        }

        private void FixedUpdate()
        {
            if (_isDead) return;

            _movement.ProcessFixedUpdate();
        }

        #region Platform & Environment Delegations

        public void ResetAirJumps()
        {
            if (_movement != null) _movement.ResetAirJumps();
        }

        public void NotifyJumpPadLaunch(float force)
        {
            if (_movement != null) _movement.NotifyJumpPadLaunch(force);
        }

        public void ApplySpeedBuff(float multiplier, float duration)
        {
            if (_movement != null) _movement.ApplySpeedBuff(multiplier, duration);
        }

        public void SetNearLadder(Ladder ladder, bool isNear)
        {
            if (_movement != null) _movement.SetNearLadder(ladder, isNear);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_movement != null) _movement.RegisterPlatformCollision(collision, true);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (_movement != null) _movement.RegisterPlatformCollision(collision, true);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (_movement != null) _movement.RegisterPlatformCollision(collision, false);
        }

        #endregion

        #region IDamageable Implementation

        public void TakeDamage(DamageData damageData)
        {
            // Sát thương rơi vực sâu (FallDamage / Lethal) là ranh giới môi trường, không thể bị chặn bởi Khiên (Skill 3) hay Lướt (Dash)
            bool isLethalFall = damageData.Type == DamageType.FallDamage || damageData.Amount >= 9999;
            if (!isLethalFall)
            {
                if (_isDead || IsDashing || _isInvulnerable || (_skills != null && _skills.IsShieldActive)) return;
            }
            else
            {
                if (_isDead) return;
            }

            _currentHealth = Mathf.Max(0, _currentHealth - damageData.Amount);
            EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));

            if (damageData.KnockbackForce != Vector2.zero && _rb != null)
            {
                _rb.linearVelocity = damageData.KnockbackForce;
            }

            if (CameraFollow2D.Instance != null)
            {
                CameraFollow2D.Instance.Shake(0.16f, 0.20f);
            }

            if (_currentHealth <= 0)
            {
                Die();
            }
            else
            {
                if (_animSync != null)
                {
                    _animSync.TriggerHurt(0.35f);
                }
                _hurtLockTimer = 0.28f;
                EventBus.Publish(new PlaySoundEvent("PlayerHurt", SoundType.SFX, transform.position));
                StartInvulnerability();
            }
        }

        /// <summary>
        /// Buộc chết ngay lập tức (dùng khi rơi khỏi vực sâu / KillZone), giải phóng toàn bộ hiệu ứng khiên và lướt.
        /// </summary>
        public void ForceKill()
        {
            if (_isDead) return;
            _currentHealth = 0;
            EventBus.Publish(new PlayerHealthChangedEvent(0, _maxHealth));
            Die();
        }

        private void StartInvulnerability()
        {
            StopInvulnerability();
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _hurtFlashColor;
            }
            if (_invulnerabilityDuration > 0f)
            {
                _invulnerabilityCoroutine = StartCoroutine(InvulnerabilityRoutine());
            }
        }

        private IEnumerator InvulnerabilityRoutine()
        {
            _isInvulnerable = true;
            float elapsed = 0f;
            float blinkStep = Mathf.Max(0.04f, _hurtBlinkInterval);

            while (elapsed < _invulnerabilityDuration)
            {
                if (_spriteRenderer != null)
                {
                    bool isFlash = Mathf.FloorToInt(elapsed / blinkStep) % 2 == 0;
                    _spriteRenderer.color = isFlash
                        ? _hurtFlashColor
                        : new Color(_baseSpriteColor.r, _baseSpriteColor.g, _baseSpriteColor.b, 0.65f);
                }

                yield return new WaitForSeconds(blinkStep);
                elapsed += blinkStep;
            }

            StopInvulnerability();
        }

        private void StopInvulnerability()
        {
            if (_invulnerabilityCoroutine != null)
            {
                StopCoroutine(_invulnerabilityCoroutine);
                _invulnerabilityCoroutine = null;
            }

            _isInvulnerable = false;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseSpriteColor;
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;
            _hurtLockTimer = 0f;
            StopInvulnerability();
            if (_skills != null) _skills.ResetRuntimeEffects();

            // Dừng vận tốc ngang, nhưng vẫn giữ rơi tự nhiên xuống mặt đất nếu đang ở trên không
            if (_rb != null)
            {
                _rb.linearVelocityX = 0f;
            }

            if (_animSync != null)
            {
                _animSync.SetDead(true);
            }

            if (CameraFollow2D.Instance != null)
            {
                CameraFollow2D.Instance.Shake(0.24f, 0.28f);
            }

            EventBus.Publish(new PlaySoundEvent("PlayerDeath", SoundType.SFX, transform.position));

            if (_deathSequenceCoroutine != null)
            {
                StopCoroutine(_deathSequenceCoroutine);
            }
            _deathSequenceCoroutine = StartCoroutine(DeathSequenceRoutine());
        }

        private IEnumerator DeathSequenceRoutine()
        {
            // Chớp đỏ ngắn lúc hết máu tạo cảm giác dính đòn chí mạng
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _hurtFlashColor;
            }
            yield return new WaitForSeconds(0.12f);
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseSpriteColor;
            }

            // Chờ phần còn lại của hoạt ảnh Pink_Monster_Death (8 frame, ~1.02s) chạy xong trọn vẹn
            yield return new WaitForSeconds(1.03f);

            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.bodyType = RigidbodyType2D.Kinematic;
            }

            // Sau khi chạy xong toàn bộ hoạt ảnh chết, MỚI phát PlayerDiedEvent để kích hoạt popup menu Game Over!
            EventBus.Publish(new PlayerDiedEvent(transform.position));
            _deathSequenceCoroutine = null;
        }

        #endregion

        #region IHealable Implementation

        public void Heal(int amount)
        {
            if (_isDead || IsAtMaxHealth) return;

            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));
        }

        public void SetMaxHealth(int newMaxHealth)
        {
            _maxHealth = Mathf.Max(1, newMaxHealth);
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));
        }

        #endregion

        #region Respawn Flow

        /// <summary>
        /// Hồi sinh nhân vật tại vị trí Checkpoint chỉ định.
        /// </summary>
        public void RespawnAt(Vector3 spawnPosition)
        {
            if (_deathSequenceCoroutine != null)
            {
                StopCoroutine(_deathSequenceCoroutine);
                _deathSequenceCoroutine = null;
            }

            _isDead = false;
            _hurtLockTimer = 0f;
            StopInvulnerability();
            _currentHealth = _maxHealth;
            transform.SetParent(null);
            transform.position = spawnPosition;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = Vector2.zero;

            if (_movement != null) _movement.ResetState();
            if (_combat != null) _combat.ResetCombat();
            if (_skills != null) _skills.ResetRuntimeEffects();
            if (_input != null) _input.ResetInputs();
            if (_animSync != null) _animSync.SetDead(false);

            EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));
            EventBus.Publish(new PlayerRespawnedEvent(spawnPosition));
        }

        #endregion
    }
}
