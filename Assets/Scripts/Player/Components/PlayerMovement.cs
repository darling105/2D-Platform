using System.Collections;
using System.Linq;
using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Environment.Platforms;
using Platformer2D.Environment.Interactive;
using Platformer2D.Player.Input;
using Platformer2D.VFX;

namespace Platformer2D.Player.Components
{
    /// <summary>
    /// Quản lý toàn bộ vật lý và kỹ năng di chuyển của nhân vật (Player Locomotion):
    /// - Di chuyển ngang, lật mặt (Flip).
    /// - Nhảy (Coyote Time, Jump Buffering, Variable Jump Height, Double Jump).
    /// - Trượt tường (Wall Slide) & Bật tường (Wall Jump).
    /// - Lướt nhanh (Dash) với bóng mờ & bất tử.
    /// - Trượt đất (Ground Slide) thu nhỏ Collider luồn qua khe hẹp.
    /// - Leo thang / Dây leo (Climbing) tự dừng hình khi buông tay.
    /// - Tương tác sàn di chuyển (Moving Platform) và rơi xuyên sàn 1 chiều (One-Way Platform).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Tốc độ di chuyển ngang (units/s)")]
        [SerializeField] private float _moveSpeed = 8f;

        [Header("Jump Settings")]
        [Tooltip("Lực bật nhảy")]
        [SerializeField] private float _jumpForce = 13f;
        [Tooltip("Gia tốc rơi khi đang rơi xuống (Game Feel)")]
        [SerializeField] private float _fallGravityMultiplier = 2.5f;
        [Tooltip("Gia tốc rơi khi nhả phím nhảy sớm")]
        [SerializeField] private float _lowJumpGravityMultiplier = 2f;
        [Tooltip("Thời gian Coyote")]
        [SerializeField] private float _coyoteTime = 0.15f;

        [Header("Double Jump")]
        [Tooltip("Số lần nhảy trên không tối đa (1 = Double Jump)")]
        [SerializeField] private int _maxAirJumps = 1;
        [Tooltip("Prefab hiệu ứng khói khi Double Jump (tùy chọn, nếu trống tự nạp Double_Jump_Dust_5)")]
        [SerializeField] private GameObject _doubleJumpDustPrefab;
        [Tooltip("Tọa độ lệch của bụi Double Jump so với tâm nhân vật (X, Y)")]
        [SerializeField] private Vector2 _doubleJumpDustOffset = new Vector2(0f, 0f);

        [Header("Footstep & Push Dust Effects")]
        [Tooltip("Bật / tắt hiệu ứng bụi chân")]
        [SerializeField] private bool _enableFootDust = true;
        [Tooltip("Prefab hiệu ứng bụi chân khi chạy/đẩy/tiếp đất (tùy chọn, nếu trống tự nạp Walk_Run_Push_Dust_6)")]
        [SerializeField] private GameObject _footDustPrefab;
        [Tooltip("Tọa độ lệch của bụi chân so với tâm nhân vật (X = lùi sau gót, Y = cao độ tiếp đất)")]
        [SerializeField] private Vector2 _footDustOffset = new Vector2(-0.25f, 0f);
        [Tooltip("Thời gian giữa 2 nhịp sinh bụi khi chạy (giây)")]
        [SerializeField] private float _runDustInterval = 0.25f;
        [Tooltip("Thời gian giữa 2 nhịp sinh bụi khi đẩy thùng (giây)")]
        [SerializeField] private float _pushDustInterval = 0.28f;
        [Tooltip("Tốc độ khung hình của animation bụi (Frames per second)")]
        [SerializeField] private float _dustFrameRate = 14f;
        [Tooltip("Kích thước to nhỏ của hiệu ứng bụi (Scale)")]
        [SerializeField] private float _dustScale = 1f;

        [Header("Dash Settings")]
        [Tooltip("Tốc độ khi lướt")]
        [SerializeField] private float _dashSpeed = 16f;
        [Tooltip("Thời gian lướt (giây)")]
        [SerializeField] private float _dashDuration = 0.16f;
        [Tooltip("Thời gian hồi chiêu Dash (giây)")]
        [SerializeField] private float _dashCooldown = 0.6f;

        [Header("Wall Slide & Wall Jump")]
        [Tooltip("Tốc độ trượt xuống tối đa khi bám tường")]
        [SerializeField] private float _wallSlideSpeed = 2.5f;
        [Tooltip("Lực bật ra khi nhảy từ tường")]
        [SerializeField] private Vector2 _wallJumpForce = new Vector2(10f, 13f);
        [Tooltip("Layer được coi là tường")]
        [SerializeField] private LayerMask _wallLayer = ~0;
        [Tooltip("Bán kính kiểm tra tường phía trước")]
        [SerializeField] private float _wallCheckRadius = 0.18f;
        [Tooltip("Offset kiểm tra tường")]
        [SerializeField] private Vector2 _wallCheckOffset = new Vector2(0.35f, 0f);
        [Tooltip("Khóa điều khiển ngang tạm thời sau khi nhảy tường (giây)")]
        [SerializeField] private float _wallJumpLockoutTime = 0.15f;

        [Header("Ladder Climbing")]
        [Tooltip("Tốc độ leo thang lên/xuống (units/s)")]
        [SerializeField] private float _climbSpeed = 5f;

        [Header("Ground Slide")]
        [Tooltip("Tốc độ trượt đất (units/s)")]
        [SerializeField] private float _slideSpeed = 12f;
        [Tooltip("Thời gian trượt đất (giây)")]
        [SerializeField] private float _slideDuration = 0.35f;
        [Tooltip("Thời gian hồi chiêu trượt (giây)")]
        [SerializeField] private float _slideCooldown = 0.5f;

        [Header("Collision Checks")]
        [Tooltip("Layer được coi là mặt đất")]
        [SerializeField] private LayerMask _groundLayer = ~0;
        [Tooltip("Bán kính vòng tròn kiểm tra chân chạm đất")]
        [SerializeField] private float _groundCheckRadius = 0.1f;
        [Tooltip("Offset chân kiểm tra đất")]
        [SerializeField] private Vector2 _groundCheckOffset = new Vector2(0f, -0.48f);

        private Rigidbody2D _rb;
        private Collider2D _mainCollider;
        private BoxCollider2D _boxCollider;
        private GhostTrail _ghostTrail;
        private PlayerInputHandler _input;

        private bool _isFacingRight = true;
        private bool _isGrounded;
        private bool _isTouchingWall;
        private bool _isWallSliding;
        private bool _isDashing;
        private bool _isSliding;
        private bool _isClimbing;
        private bool _canClimb;
        private bool _isPushing;

        private int _airJumpsRemaining;
        private float _coyoteTimer;
        private float _wallJumpLockoutTimer;
        private float _dashCooldownTimer;
        private float _slideCooldownTimer;
        private float _originalGravityScale;

        private Vector2 _originalColliderSize;
        private Vector2 _originalColliderOffset;

        private Ladder _currentLadder;
        private OneWayPlatform _currentOneWayPlatform;
        private MovingPlatform _currentMovingPlatform;

        private float _jumpPadAllowanceTimer = 0f;
        private float _maxAllowedUpwardSpeed;

        private float _stepDustTimer;
        private bool _wasGrounded;
        private float _lastVerticalVelocity;
#if UNITY_EDITOR
        private Sprite[] _cachedStepDustSprites;
#endif

        // Public Properties for External / Animator Access
        public bool IsFacingRight => _isFacingRight;
        public bool IsGrounded => _isGrounded;
        public bool IsWallSliding => _isWallSliding;
        public bool IsDashing => _isDashing;
        public bool IsSliding => _isSliding;
        public bool IsClimbing => _isClimbing;
        public bool IsPushing => _isPushing;
        public float RelativeHorizontalSpeed { get; private set; }
        public float VerticalClimbSpeed => _input != null ? _input.VerticalInput : 0f;
        public float DustFrameRate => _dustFrameRate;
        public float DustScale => _dustScale;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _mainCollider = GetComponent<Collider2D>();
            _boxCollider = _mainCollider as BoxCollider2D;
            _ghostTrail = GetComponent<GhostTrail>();
            _input = GetComponent<PlayerInputHandler>();

            if (_boxCollider != null)
            {
                _originalColliderSize = _boxCollider.size;
                _originalColliderOffset = _boxCollider.offset;
            }

            // Gán PhysicsMaterial2D không ma sát cho Collider và Rigidbody2D để chống dính tường / dính thùng
            if (_mainCollider != null && _mainCollider.sharedMaterial == null)
            {
#if UNITY_EDITOR
                _mainCollider.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/Frictionless2D.physicsMaterial2D");
#endif
                if (_mainCollider.sharedMaterial == null)
                {
                    PhysicsMaterial2D mat = new PhysicsMaterial2D("PlayerColliderFrictionless") { friction = 0f, bounciness = 0f };
                    mat.hideFlags = HideFlags.DontSave;
                    _mainCollider.sharedMaterial = mat;
                }
            }
            if (_rb != null && _rb.sharedMaterial == null)
            {
#if UNITY_EDITOR
                _rb.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/Frictionless2D.physicsMaterial2D");
#endif
                if (_rb.sharedMaterial == null)
                {
                    PhysicsMaterial2D mat = new PhysicsMaterial2D("PlayerRbFrictionless") { friction = 0f, bounciness = 0f };
                    mat.hideFlags = HideFlags.DontSave;
                    _rb.sharedMaterial = mat;
                }
            }

            _originalGravityScale = _rb.gravityScale;
            _airJumpsRemaining = _maxAirJumps;
            _maxAllowedUpwardSpeed = _jumpForce + 1.5f;
        }

        public void ProcessUpdate()
        {
            UpdateTimers();
            CheckGround();
            CheckWall();
            CheckPushing();
            HandleClimbing();
            HandleWallSlide();
            HandleJump();
            HandleAbilitiesInput();
            CalculateRelativeSpeed();
            HandleMovementDustEffects();
        }

        public void ProcessFixedUpdate()
        {
            if (_jumpPadAllowanceTimer > 0f)
            {
                _jumpPadAllowanceTimer -= Time.fixedDeltaTime;
            }
            else
            {
                _maxAllowedUpwardSpeed = _jumpForce + 1.5f;
            }

            // Chốt chặn 1: Triệt tiêu mọi xung lực va chạm đẩy vọt người chơi lên trời
            if (_rb.linearVelocityY > _maxAllowedUpwardSpeed)
            {
                _rb.linearVelocityY = _maxAllowedUpwardSpeed;
            }

            if (_isDashing) return;

            if (_isClimbing)
            {
                ApplyClimbMovement();
                return;
            }

            if (_isSliding) return;

            ApplyMovement();
            ApplyBetterJumpGravity();

            // Chốt chặn 2: Đảm bảo sau khi di chuyển và tính toán trọng lực vận tốc Y vẫn trong ngưỡng an toàn
            if (_rb.linearVelocityY > _maxAllowedUpwardSpeed)
            {
                _rb.linearVelocityY = _maxAllowedUpwardSpeed;
            }
        }

        public void NotifyJumpPadLaunch(float force)
        {
            _jumpPadAllowanceTimer = 0.6f;
            _maxAllowedUpwardSpeed = force + 2f;
        }

        private void UpdateTimers()
        {
            if (_isGrounded)
            {
                _coyoteTimer = _coyoteTime;
                _airJumpsRemaining = _maxAirJumps;
            }
            else
            {
                _coyoteTimer -= Time.deltaTime;
            }

            if (_dashCooldownTimer > 0f) _dashCooldownTimer -= Time.deltaTime;
            if (_slideCooldownTimer > 0f) _slideCooldownTimer -= Time.deltaTime;
            if (_wallJumpLockoutTimer > 0f) _wallJumpLockoutTimer -= Time.deltaTime;
        }

        #region Collision & Environment Sensors

        private void CheckGround()
        {
            _isGrounded = false;
            _currentMovingPlatform = null;

            // Bắn 3 tia thẳng đứng xuống dưới: Trái, Giữa, Phải để kiểm tra mặt đất
            // Độ rộng tia hẹp hơn bề ngang collider để không bao giờ bị chạm vào cạnh tường/thùng hai bên hông
            float halfWidth = 0.22f;
            float castDistance = _groundCheckRadius + 0.12f;
            Vector2 footPos = (Vector2)transform.position + _groundCheckOffset;

            Vector2[] rayOrigins = new Vector2[]
            {
                footPos + new Vector2(-halfWidth, 0f),
                footPos,
                footPos + new Vector2(halfWidth, 0f)
            };

            for (int i = 0; i < rayOrigins.Length; i++)
            {
                RaycastHit2D hit = Physics2D.Raycast(rayOrigins[i], Vector2.down, castDistance, _groundLayer);
                if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger)
                {
                    // Chỉ công nhận là mặt đất nếu bề mặt tiếp xúc có phương vuông góc hướng lên trên (normal.y > 0.6)
                    // Tuyệt đối không coi mặt phẳng dựng đứng bên hông của tường hoặc thùng là đất
                    if (hit.normal.y > 0.6f)
                    {
                        _isGrounded = true;

                        MovingPlatform mp = hit.collider.GetComponentInParent<MovingPlatform>();
                        if (mp != null)
                        {
                            _currentMovingPlatform = mp;
                        }
                        break;
                    }
                }
            }
        }

        private void CheckWall()
        {
            float direction = _isFacingRight ? 1f : -1f;
            Vector2 checkPosition = (Vector2)transform.position + new Vector2(direction * _wallCheckOffset.x, _wallCheckOffset.y);
            Collider2D[] hits = Physics2D.OverlapCircleAll(checkPosition, _wallCheckRadius, _wallLayer);

            _isTouchingWall = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject != gameObject && !hits[i].isTrigger)
                {
                    // Bỏ qua thùng gỗ đẩy được (thùng là vật thể tương tác di động, không phải tường kiến trúc để bám trượt)
                    if (hits[i].GetComponentInParent<PushableBox>() != null) continue;

                    _isTouchingWall = true;
                    break;
                }
            }
        }

        private void CheckPushing()
        {
            if (!_isGrounded || _input == null || Mathf.Abs(_input.MoveInput.x) < 0.1f)
            {
                _isPushing = false;
                return;
            }

            float direction = _isFacingRight ? 1f : -1f;
            // Người chơi phải nhấn phím di chuyển theo hướng đang nhìn vào thùng
            if (Mathf.Sign(_input.MoveInput.x) != direction)
            {
                _isPushing = false;
                return;
            }

            Vector2 rayDir = Vector2.right * direction;
            Vector2[] origins = new Vector2[]
            {
                (Vector2)transform.position + new Vector2(direction * 0.35f, -0.1f),
                (Vector2)transform.position + new Vector2(direction * 0.35f, 0.15f)
            };

            _isPushing = false;
            for (int o = 0; o < origins.Length; o++)
            {
                // Bắn tia quét mọi layer (không giới hạn groundLayer) để bắt trúng PushableBox
                RaycastHit2D[] hits = Physics2D.RaycastAll(origins[o], rayDir, 0.35f);
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider.gameObject == gameObject || hits[i].collider.isTrigger) continue;

                    if (hits[i].collider.GetComponentInParent<PushableBox>() != null)
                    {
                        _isPushing = true;
                        return;
                    }
                }
            }
        }

        private void CalculateRelativeSpeed()
        {
            float platformSpeedX = _currentMovingPlatform != null ? _currentMovingPlatform.Velocity.x : 0f;
            RelativeHorizontalSpeed = Mathf.Abs(_rb.linearVelocityX - platformSpeedX);
        }

        #endregion

        #region Locomotion Physics

        private void ApplyMovement()
        {
            if (_input == null) return;

            Vector2 platformVelocity = _currentMovingPlatform != null ? _currentMovingPlatform.Velocity : Vector2.zero;
            float currentMoveSpeed = _isPushing ? (_moveSpeed * 0.6f) : _moveSpeed;

            if (_wallJumpLockoutTimer <= 0f)
            {
                _rb.linearVelocityX = (_input.MoveInput.x * currentMoveSpeed) + platformVelocity.x;
            }

            if (_isGrounded && _currentMovingPlatform != null)
            {
                if (Mathf.Abs(platformVelocity.y) > 0.01f && _rb.linearVelocityY <= 0.1f)
                {
                    _rb.linearVelocityY = platformVelocity.y;
                }
            }

            if (_rb.linearVelocityY > _maxAllowedUpwardSpeed)
            {
                _rb.linearVelocityY = _maxAllowedUpwardSpeed;
            }

            if (_input.MoveInput.x > 0 && !_isFacingRight)
            {
                Flip();
            }
            else if (_input.MoveInput.x < 0 && _isFacingRight)
            {
                Flip();
            }
        }

        public void Flip()
        {
            _isFacingRight = !_isFacingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;
        }

        private void ApplyBetterJumpGravity()
        {
            if (_isClimbing || _input == null) return;

            if (_rb.linearVelocityY < 0f)
            {
                _rb.linearVelocityY += Physics2D.gravity.y * (_fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
            }
            else if (_rb.linearVelocityY > 0f && !_input.JumpHeld)
            {
                _rb.linearVelocityY += Physics2D.gravity.y * (_lowJumpGravityMultiplier - 1f) * Time.fixedDeltaTime;
            }
        }

        #endregion

        #region Jumping & Wall Mechanics

        private void HandleJump()
        {
            if (_input == null || !_input.JumpTriggered) return;

            // Nhảy khỏi thang khi đang leo
            if (_isClimbing)
            {
                StopClimbing();
                float jumpDir = _input.MoveInput.x != 0 ? Mathf.Sign(_input.MoveInput.x) : (_isFacingRight ? -1f : 1f);
                _rb.linearVelocity = new Vector2(jumpDir * 6f, _jumpForce * 0.85f);
                _input.ConsumeJump();
                EventBus.Publish(new PlaySoundEvent("Jump", SoundType.SFX, transform.position));
                return;
            }

            // 1. Rơi xuyên sàn một chiều khi ấn Giữ Phím Xuống + Nhảy
            if (_input.IsDownHeld && _currentOneWayPlatform != null && _mainCollider != null)
            {
                _currentOneWayPlatform.DropThrough(_mainCollider);
                _input.ConsumeJump();
                return;
            }

            // 2. Nhảy từ mặt đất (hoặc trong khoảng Coyote Time)
            if (_coyoteTimer > 0f)
            {
                float upwardBoost = _currentMovingPlatform != null ? Mathf.Max(0f, _currentMovingPlatform.Velocity.y) : 0f;
                _rb.linearVelocityY = _jumpForce + upwardBoost;
                _currentMovingPlatform = null;
                _coyoteTimer = 0f;
                _isGrounded = false;
                _input.ConsumeJump();
                EventBus.Publish(new PlaySoundEvent("Jump", SoundType.SFX, transform.position));
                return;
            }

            // 3. Nhảy từ tường (Wall Jump)
            if (_isWallSliding)
            {
                float jumpDir = _isFacingRight ? -1f : 1f;
                _rb.linearVelocity = new Vector2(jumpDir * _wallJumpForce.x, _wallJumpForce.y);
                _wallJumpLockoutTimer = _wallJumpLockoutTime;
                _airJumpsRemaining = _maxAirJumps;
                _coyoteTimer = 0f;
                _isWallSliding = false;

                if ((jumpDir > 0 && !_isFacingRight) || (jumpDir < 0 && _isFacingRight))
                {
                    Flip();
                }

                _input.ConsumeJump();
                EventBus.Publish(new PlaySoundEvent("Jump", SoundType.SFX, transform.position));
                return;
            }

            // 4. Nhảy 2 lần trên không (Double Jump)
            if (_airJumpsRemaining > 0)
            {
                _airJumpsRemaining--;
                _rb.linearVelocityY = _jumpForce;
                _coyoteTimer = 0f;
                _input.ConsumeJump();

                SpawnDoubleJumpDust();
                EventBus.Publish(new PlaySoundEvent("Jump", SoundType.SFX, transform.position));
            }
        }

        private void HandleWallSlide()
        {
            if (_isClimbing)
            {
                _isWallSliding = false;
                return;
            }

            if (_isTouchingWall && !_isGrounded && _rb.linearVelocityY < 0f)
            {
                _isWallSliding = true;
                _rb.linearVelocityY = Mathf.Max(_rb.linearVelocityY, -_wallSlideSpeed);
            }
            else
            {
                _isWallSliding = false;
            }
        }

        public void ResetAirJumps()
        {
            _airJumpsRemaining = _maxAirJumps;
        }

        private void SpawnDoubleJumpDust()
        {
            Vector3 spawnPos = transform.position + new Vector3(_doubleJumpDustOffset.x, _doubleJumpDustOffset.y, 0f);

            if (_doubleJumpDustPrefab != null)
            {
                Instantiate(_doubleJumpDustPrefab, spawnPos, Quaternion.identity);
                return;
            }

#if UNITY_EDITOR
            var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/GameAssets/Player/1 Pink_Monster/Double_Jump_Dust_5.png");
            var dustSprites = allAssets != null ? System.Linq.Enumerable.OfType<Sprite>(allAssets).OrderBy(s => s.name).ToArray() : null;

            if (dustSprites != null && dustSprites.Length > 0)
            {
                GameObject dustObj = new GameObject("VFX_DoubleJumpDust");
                dustObj.transform.position = spawnPos;
                dustObj.transform.localScale = Vector3.one * _dustScale;

                SpriteRenderer sr = dustObj.AddComponent<SpriteRenderer>();
                var myRenderer = GetComponent<SpriteRenderer>();
                if (myRenderer != null)
                {
                    sr.sortingLayerID = myRenderer.sortingLayerID;
                    sr.sortingOrder = myRenderer.sortingOrder - 1;
                }

                SpriteAnimationEffect effect = dustObj.AddComponent<SpriteAnimationEffect>();
                effect.Initialize(dustSprites, _dustFrameRate);
            }
#endif
        }

        private void HandleMovementDustEffects()
        {
            if (!_enableFootDust) return;

            // 1. Kiểm tra tiếp đất sau cú rơi để tạo hiệu ứng bụi đập đất (Landing Dust)
            bool justLanded = !_wasGrounded && _isGrounded;
            if (justLanded && _lastVerticalVelocity < -3.5f)
            {
                SpawnFootDust(false);
            }
            _wasGrounded = _isGrounded;
            _lastVerticalVelocity = _rb.linearVelocityY;

            if (!_isGrounded) return;

            // 2. Tạo bụi chân định kỳ khi đẩy thùng, trượt đất hoặc chạy nhanh
            if (_isPushing)
            {
                _stepDustTimer -= Time.deltaTime;
                if (_stepDustTimer <= 0f)
                {
                    _stepDustTimer = _pushDustInterval;
                    SpawnFootDust(true);
                }
            }
            else if (_isSliding)
            {
                _stepDustTimer -= Time.deltaTime;
                if (_stepDustTimer <= 0f)
                {
                    _stepDustTimer = _runDustInterval * 0.6f;
                    SpawnFootDust(true);
                }
            }
            else if (_input != null && Mathf.Abs(_input.MoveInput.x) > 0.1f && RelativeHorizontalSpeed > 0.5f)
            {
                _stepDustTimer -= Time.deltaTime;
                if (_stepDustTimer <= 0f)
                {
                    _stepDustTimer = _runDustInterval;
                    SpawnFootDust(true);
                }
            }
            else
            {
                _stepDustTimer = 0.05f;
            }
        }

        private void SpawnFootDust(bool behind)
        {
            if (!_enableFootDust) return;

            float dir = _isFacingRight ? 1f : -1f;
            float xOffset = behind ? (dir * _footDustOffset.x) : 0f;
            Vector3 spawnPos = transform.position + new Vector3(xOffset, _footDustOffset.y, 0f);

            if (_footDustPrefab != null)
            {
                GameObject dust = Instantiate(_footDustPrefab, spawnPos, Quaternion.identity);
                dust.transform.localScale = new Vector3(_isFacingRight ? _dustScale : -_dustScale, _dustScale, 1f);
                return;
            }

#if UNITY_EDITOR
            if (_cachedStepDustSprites == null || _cachedStepDustSprites.Length == 0)
            {
                var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/GameAssets/Player/1 Pink_Monster/Walk_Run_Push_Dust_6.png");
                _cachedStepDustSprites = allAssets != null ? System.Linq.Enumerable.OfType<Sprite>(allAssets).OrderBy(s => s.name).ToArray() : null;
            }

            if (_cachedStepDustSprites != null && _cachedStepDustSprites.Length > 0)
            {
                GameObject dustObj = new GameObject("VFX_FootDust");
                dustObj.transform.position = spawnPos;
                dustObj.transform.localScale = new Vector3(_isFacingRight ? _dustScale : -_dustScale, _dustScale, 1f);

                SpriteRenderer sr = dustObj.AddComponent<SpriteRenderer>();
                var myRenderer = GetComponent<SpriteRenderer>();
                if (myRenderer != null)
                {
                    sr.sortingLayerID = myRenderer.sortingLayerID;
                    sr.sortingOrder = myRenderer.sortingOrder - 1;
                }

                SpriteAnimationEffect effect = dustObj.AddComponent<SpriteAnimationEffect>();
                effect.Initialize(_cachedStepDustSprites, _dustFrameRate);
            }
#endif
        }

        #endregion

        #region Climbing (Thang leo)

        public void SetNearLadder(Ladder ladder, bool isNear)
        {
            _canClimb = isNear;
            if (isNear)
            {
                _currentLadder = ladder;
            }
            else
            {
                if (_isClimbing) StopClimbing();
                _currentLadder = null;
            }
        }

        private void HandleClimbing()
        {
            if (_canClimb && !_isClimbing && _input != null && Mathf.Abs(_input.VerticalInput) > 0.2f)
            {
                StartClimbing();
            }
        }

        private void StartClimbing()
        {
            _isClimbing = true;
            _rb.gravityScale = 0f;
            _rb.linearVelocity = Vector2.zero;

            if (_currentLadder != null && _currentLadder.ShouldSnapToCenter())
            {
                transform.position = new Vector3(_currentLadder.CenterX, transform.position.y, transform.position.z);
            }
        }

        public void StopClimbing()
        {
            _isClimbing = false;
            _rb.gravityScale = _originalGravityScale;
        }

        private void ApplyClimbMovement()
        {
            if (_input == null) return;
            _rb.linearVelocity = new Vector2(_input.MoveInput.x * (_moveSpeed * 0.4f), _input.VerticalInput * _climbSpeed);
        }

        #endregion

        #region Dash & Ground Slide Abilities

        private void HandleAbilitiesInput()
        {
            if (_input == null) return;

            if (_input.DashTriggered && _dashCooldownTimer <= 0f && !_isDashing && !_isClimbing)
            {
                StartCoroutine(DashRoutine());
            }

            if (_input.SlideTriggered && _slideCooldownTimer <= 0f && !_isSliding && !_isDashing && !_isClimbing && _isGrounded)
            {
                StartCoroutine(SlideRoutine());
            }
        }

        private IEnumerator DashRoutine()
        {
            _isDashing = true;
            _dashCooldownTimer = _dashCooldown;

            float dashDir = (_input != null && _input.MoveInput.x != 0) ? Mathf.Sign(_input.MoveInput.x) : (_isFacingRight ? 1f : -1f);
            _rb.gravityScale = 0f;
            _rb.linearVelocity = new Vector2(dashDir * _dashSpeed, 0f);

            if (_ghostTrail != null) _ghostTrail.StartTrail(_dashDuration);
            EventBus.Publish(new PlaySoundEvent("Dash", SoundType.SFX, transform.position));

            yield return new WaitForSeconds(_dashDuration);

            _rb.gravityScale = _originalGravityScale;
            _rb.linearVelocityX = 0f;
            _isDashing = false;
        }

        private IEnumerator SlideRoutine()
        {
            _isSliding = true;
            _slideCooldownTimer = _slideCooldown;

            if (_boxCollider != null)
            {
                _boxCollider.size = new Vector2(_originalColliderSize.x, _originalColliderSize.y * 0.5f);
                _boxCollider.offset = new Vector2(_originalColliderOffset.x, _originalColliderOffset.y - _originalColliderSize.y * 0.25f);
            }

            float slideDir = _isFacingRight ? 1f : -1f;
            _rb.linearVelocityX = slideDir * _slideSpeed;

            if (_ghostTrail != null) _ghostTrail.StartTrail(_slideDuration);
            EventBus.Publish(new PlaySoundEvent("Slide", SoundType.SFX, transform.position));

            float elapsed = 0f;
            while (elapsed < _slideDuration)
            {
                elapsed += Time.deltaTime;
                _rb.linearVelocityX = Mathf.Lerp(slideDir * _slideSpeed, slideDir * _moveSpeed, elapsed / _slideDuration);
                yield return null;
            }

            while (IsCeilingBlocked())
            {
                _rb.linearVelocityX = slideDir * (_moveSpeed * 0.8f);
                yield return null;
            }

            if (_boxCollider != null)
            {
                _boxCollider.size = _originalColliderSize;
                _boxCollider.offset = _originalColliderOffset;
            }

            _isSliding = false;
        }

        private bool IsCeilingBlocked()
        {
            Vector2 checkPos = (Vector2)transform.position + new Vector2(0f, 0.4f);
            Collider2D hit = Physics2D.OverlapCircle(checkPos, 0.2f, _groundLayer);
            return hit != null && hit.gameObject != gameObject && !hit.isTrigger;
        }

        #endregion

        #region Platform Collisions

        public void RegisterPlatformCollision(Collision2D collision, bool isEntering)
        {
            if (collision.gameObject.TryGetComponent<OneWayPlatform>(out var platform))
            {
                _currentOneWayPlatform = isEntering ? platform : null;
            }
        }

        #endregion

        public void ResetState()
        {
            StopAllCoroutines();
            _isDashing = false;
            _isClimbing = false;
            _isSliding = false;
            _isPushing = false;
            _isWallSliding = false;
            _currentMovingPlatform = null;
            _currentOneWayPlatform = null;
            _rb.gravityScale = _originalGravityScale;
            _airJumpsRemaining = _maxAirJumps;

            if (_boxCollider != null)
            {
                _boxCollider.size = _originalColliderSize;
                _boxCollider.offset = _originalColliderOffset;
            }
        }

        private void OnDrawGizmosSelected()
        {
            float direction = Application.isPlaying ? (_isFacingRight ? 1f : -1f) : (transform.localScale.x >= 0 ? 1f : -1f);

            // 1. Ground Check (Vòng tròn tại đế giày và 3 tia Raycast tiếp đất)
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Vector2 footPos = (Vector2)transform.position + _groundCheckOffset;
            Gizmos.DrawWireSphere(footPos, _groundCheckRadius);

            float halfWidth = 0.22f;
            float castDist = _groundCheckRadius + 0.12f;
            Gizmos.DrawLine(footPos + new Vector2(-halfWidth, 0f), footPos + new Vector2(-halfWidth, -castDist));
            Gizmos.DrawLine(footPos, footPos + new Vector2(0f, -castDist));
            Gizmos.DrawLine(footPos + new Vector2(halfWidth, 0f), footPos + new Vector2(halfWidth, -castDist));

            // 2. Wall Check (Vòng tròn kiểm tra tường phía trước mặt)
            Gizmos.color = _isTouchingWall ? Color.yellow : Color.blue;
            Vector2 wallPos = (Vector2)transform.position + new Vector2(direction * _wallCheckOffset.x, _wallCheckOffset.y);
            Gizmos.DrawWireSphere(wallPos, _wallCheckRadius);

            // 3. Ceiling Check (Vòng tròn kiểm tra trần phía trên đầu)
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere((Vector2)transform.position + new Vector2(0f, 0.45f), 0.15f);

            // 4. Push Check (2 tia quét đẩy thùng phía trước)
            Gizmos.color = _isPushing ? Color.green : new Color(1f, 0.6f, 0.2f);
            Vector2 p1 = (Vector2)transform.position + new Vector2(direction * 0.35f, -0.1f);
            Vector2 p2 = (Vector2)transform.position + new Vector2(direction * 0.35f, 0.15f);
            Gizmos.DrawLine(p1, p1 + new Vector2(direction * 0.35f, 0f));
            Gizmos.DrawLine(p2, p2 + new Vector2(direction * 0.35f, 0f));

            // 5. Foot Dust Spawn Position (Điểm xuất hiện bụi chân sau gót)
            if (_enableFootDust)
            {
                Gizmos.color = Color.cyan;
                Vector2 dustPos = (Vector2)transform.position + new Vector2(direction * _footDustOffset.x, _footDustOffset.y);
                Gizmos.DrawWireSphere(dustPos, 0.05f);
            }
        }
    }
}
