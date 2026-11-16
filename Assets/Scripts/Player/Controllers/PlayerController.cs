using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;

namespace Platformer2D.Player.Controllers
{
    /// <summary>
    /// Bộ điều khiển nhân vật chính 2D Platformer:
    /// - Di chuyển ngang mượt mà, lật mặt (Flip).
    /// - Nhảy với Coyote Time & Jump Buffering (Game Feel chuẩn).
    /// - Kiểm tra chạm đất (Ground Check) chính xác.
    /// - Tự động đồng bộ Animator (Speed, IsGrounded, Attack, Hurt, IsDead).
    /// - Hỗ trợ nhận sát thương qua IDamageable.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        [Header("Movement Settings")]
        [Tooltip("Tốc độ di chuyển ngang (units/s)")]
        [SerializeField] private float _moveSpeed = 8f;

        [Header("Jump Settings")]
        [Tooltip("Lực bật nhảy")]
        [SerializeField] private float _jumpForce = 13f;
        [Tooltip("Gia tốc rơi khi đang rơi xuống (giúp rơi nhanh, không bị bồng bềnh)")]
        [SerializeField] private float _fallGravityMultiplier = 2.5f;
        [Tooltip("Gia tốc rơi khi nhả phím nhảy sớm (nhảy thấp/cao tùy thời gian giữ phím)")]
        [SerializeField] private float _lowJumpGravityMultiplier = 2f;
        [Tooltip("Thời gian Coyote (cho phép nhảy khi vừa rớt khỏi mép sàn)")]
        [SerializeField] private float _coyoteTime = 0.15f;
        [Tooltip("Thời gian Jump Buffer (ghi nhớ lệnh nhảy trước khi chạm đất)")]
        [SerializeField] private float _jumpBufferTime = 0.15f;

        [Header("Ground Check")]
        [Tooltip("Layer được coi là mặt đất")]
        [SerializeField] private LayerMask _groundLayer = ~0; // Mặc định nhận mọi layer trừ Player
        [Tooltip("Bán kính vòng tròn kiểm tra chân chạm đất")]
        [SerializeField] private float _groundCheckRadius = 0.2f;
        [Tooltip("Vị trí chân kiểm tra đất (offset so với tâm nhân vật)")]
        [SerializeField] private Vector2 _groundCheckOffset = new Vector2(0f, -0.6f);

        [Header("Combat & Health")]
        [SerializeField] private int _maxHealth = 3;

        private Rigidbody2D _rb;
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;

        private Vector2 _moveInput;
        private bool _isGrounded;
        private bool _isFacingRight = true;
        private int _currentHealth;
        private bool _isDead;

        private float _coyoteTimer;
        private float _jumpBufferTimer;

        // Animator Hashes (Tối ưu hiệu năng, không dùng chuỗi String)
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

        public bool IsDead => _isDead;
        public int CurrentHealth => _currentHealth;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            _currentHealth = _maxHealth;

            // Cấu hình vật lý tối ưu cho Player
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void Update()
        {
            if (_isDead) return;

            ReadInput();
            UpdateTimers();
            CheckGround();
            HandleJump();
            HandleAttack();
            UpdateAnimations();
        }

        private void FixedUpdate()
        {
            if (_isDead) return;

            ApplyMovement();
            ApplyBetterJumpGravity();
        }

        private void ReadInput()
        {
            // Đọc phím New Input System (hỗ trợ cả A/D, mũi tên, Joystick Gamepad)
            float moveX = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    moveX -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    moveX += 1f;

                // Jump buffer
                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
                {
                    _jumpBufferTimer = _jumpBufferTime;
                }

                // Attack
                if (Keyboard.current.jKey.wasPressedThisFrame || Keyboard.current.zKey.wasPressedThisFrame || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
                {
                    TriggerAttack();
                }
            }

            // Gamepad Support (nếu có cắm tay cầm)
            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                if (Mathf.Abs(stick.x) > 0.2f)
                {
                    moveX = Mathf.Sign(stick.x);
                }

                if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                {
                    _jumpBufferTimer = _jumpBufferTime;
                }

                if (Gamepad.current.buttonWest.wasPressedThisFrame)
                {
                    TriggerAttack();
                }
            }

            _moveInput = new Vector2(moveX, 0f);
        }

        private void UpdateTimers()
        {
            if (_isGrounded)
            {
                _coyoteTimer = _coyoteTime;
            }
            else
            {
                _coyoteTimer -= Time.deltaTime;
            }

            _jumpBufferTimer -= Time.deltaTime;
        }

        private void CheckGround()
        {
            Vector2 checkPosition = (Vector2)transform.position + _groundCheckOffset;
            Collider2D[] hits = Physics2D.OverlapCircleAll(checkPosition, _groundCheckRadius, _groundLayer);

            _isGrounded = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject != gameObject && !hits[i].isTrigger)
                {
                    _isGrounded = true;
                    break;
                }
            }
        }

        private void ApplyMovement()
        {
            _rb.linearVelocityX = _moveInput.x * _moveSpeed;

            // Lật mặt nhân vật (Flip) theo hướng di chuyển
            if (_moveInput.x > 0 && !_isFacingRight)
            {
                Flip();
            }
            else if (_moveInput.x < 0 && _isFacingRight)
            {
                Flip();
            }
        }

        private void Flip()
        {
            _isFacingRight = !_isFacingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;
        }

        private void HandleJump()
        {
            // Nhảy khi có Jump Buffer và còn thời gian Coyote Time
            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
            {
                _rb.linearVelocityY = _jumpForce;
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;

                // Bắn event phát âm thanh nhảy qua EventBus (tự động phát nếu có AudioManager)
                EventBus.Publish(new PlaySoundEvent("Jump", SoundType.SFX, transform.position));
            }
        }

        private void ApplyBetterJumpGravity()
        {
            // Rơi nhanh hơn khi đang rơi xuống (Game feel)
            bool isJumpingButtonHeld = (Keyboard.current != null && (Keyboard.current.spaceKey.isPressed || Keyboard.current.wKey.isPressed)) ||
                                       (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed);

            if (_rb.linearVelocityY < 0f)
            {
                _rb.linearVelocityY += Physics2D.gravity.y * (_fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
            }
            else if (_rb.linearVelocityY > 0f && !isJumpingButtonHeld)
            {
                // Nhả phím nhảy sớm -> nhảy thấp
                _rb.linearVelocityY += Physics2D.gravity.y * (_lowJumpGravityMultiplier - 1f) * Time.fixedDeltaTime;
            }
        }

        private void HandleAttack()
        {
            // Logic cooldown tấn công nếu cần
        }

        public void TriggerAttack()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(AttackHash);
            }
            EventBus.Publish(new PlaySoundEvent("Attack", SoundType.SFX, transform.position));
        }

        private void UpdateAnimations()
        {
            if (_animator == null) return;

            float horizontalSpeed = Mathf.Abs(_rb.linearVelocityX);
            _animator.SetFloat(SpeedHash, horizontalSpeed);
            _animator.SetBool(IsGroundedHash, _isGrounded);
        }

        #region IDamageable Implementation

        public void TakeDamage(DamageData damageData)
        {
            if (_isDead) return;

            _currentHealth -= damageData.Amount;
            EventBus.Publish(new PlayerHealthChangedEvent(_currentHealth, _maxHealth));

            if (_animator != null)
            {
                _animator.SetTrigger(HurtHash);
            }

            // Đẩy lùi (Knockback)
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
            _rb.bodyType = RigidbodyType2D.Kinematic; // Đứng yên tại chỗ khi chết

            if (_animator != null)
            {
                _animator.SetBool(IsDeadHash, true);
            }

            EventBus.Publish(new PlayerDiedEvent(transform.position));
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            // Vẽ vòng tròn kiểm tra mặt đất trong Scene để dễ căn chỉnh trong Inspector
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Vector2 checkPosition = (Vector2)transform.position + _groundCheckOffset;
            Gizmos.DrawWireSphere(checkPosition, _groundCheckRadius);
        }
    }
}
