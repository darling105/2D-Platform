using UnityEngine;
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
    public class PlayerController : MonoBehaviour, IDamageable, IHealable
    {
        [Header("Health Settings")]
        [Tooltip("Lượng máu tối đa của nhân vật")]
        [SerializeField] private int _maxHealth = 3;

        private Rigidbody2D _rb;
        private PlayerInputHandler _input;
        private PlayerMovement _movement;
        private PlayerCombat _combat;
        private PlayerAnimationSync _animSync;

        private int _currentHealth;
        private bool _isDead;

        public bool IsDead => _isDead;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsAtMaxHealth => _currentHealth >= _maxHealth;
        public bool IsDashing => _movement != null && _movement.IsDashing;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _input = GetComponent<PlayerInputHandler>();
            _movement = GetComponent<PlayerMovement>();
            _combat = GetComponent<PlayerCombat>();
            _animSync = GetComponent<PlayerAnimationSync>();

            _currentHealth = _maxHealth;

            transform.SetParent(null);
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void Update()
        {
            if (_isDead) return;

            _movement.ProcessUpdate();
            _combat.ProcessUpdate();
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
            if (_isDead || IsDashing) return;

            _currentHealth -= damageData.Amount;
            EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));

            if (_animSync != null)
            {
                _animSync.TriggerHurt();
            }

            if (damageData.KnockbackForce != Vector2.zero)
            {
                _rb.linearVelocity = damageData.KnockbackForce;
            }

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            _isDead = true;
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Kinematic;

            if (_animSync != null)
            {
                _animSync.SetDead(true);
            }

            EventBus.Publish(new PlayerDiedEvent(transform.position));
        }

        #endregion

        #region IHealable Implementation

        public void Heal(int amount)
        {
            if (_isDead || IsAtMaxHealth) return;

            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));
        }

        #endregion

        #region Respawn Flow

        /// <summary>
        /// Hồi sinh nhân vật tại vị trí Checkpoint chỉ định.
        /// </summary>
        public void RespawnAt(Vector3 spawnPosition)
        {
            _isDead = false;
            _currentHealth = _maxHealth;
            transform.SetParent(null);
            transform.position = spawnPosition;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = Vector2.zero;

            if (_movement != null) _movement.ResetState();
            if (_combat != null) _combat.ResetCombat();
            if (_input != null) _input.ResetInputs();
            if (_animSync != null) _animSync.SetDead(false);

            EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));
            EventBus.Publish(new PlayerRespawnedEvent(spawnPosition));
        }

        #endregion
    }
}
