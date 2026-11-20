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
    /// - Lướt nhanh (Dash) với bóng mờ & bất tử.
    /// - Leo thang / Dây leo (Climbing) tự dừng hình khi buông tay.
    /// - Tương tác sàn di chuyển (Moving Platform) và rơi xuyên sàn 1 chiều (One-Way Platform).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Tốc độ di chuyển ngang tối đa (units/s)")]
        [SerializeField] private float _moveSpeed = 6.2f;
        [Tooltip("Gia tốc tăng tốc trên mặt đất")]
        [SerializeField] private float _groundAcceleration = 65f;
        [Tooltip("Gia tốc phanh dừng trên mặt đất")]
        [SerializeField] private float _groundDeceleration = 75f;
        [Tooltip("Gia tốc đổi hướng nhanh trên mặt đất")]
        [SerializeField] private float _groundTurnAcceleration = 110f;
        [Tooltip("Gia tốc di chuyển trên không")]
        [SerializeField] private float _airAcceleration = 42f;
        [Tooltip("Gia tốc giảm đà trên không khi thả phím")]
        [SerializeField] private float _airDeceleration = 24f;
        [Tooltip("Gia tốc đổi hướng trên không")]
        [SerializeField] private float _airTurnAcceleration = 55f;

        [Header("Jump Settings")]
        [Tooltip("Lực bật nhảy")]
        [SerializeField] private float _jumpForce = 11.2f;
        [Tooltip("Gia tốc rơi khi đang rơi xuống (Game Feel)")]
        [SerializeField] private float _fallGravityMultiplier = 1.55f;
        [Tooltip("Gia tốc rơi khi nhả phím nhảy sớm")]
        [SerializeField] private float _lowJumpGravityMultiplier = 2.1f;
        [Tooltip("Tốc độ rơi tối đa (units/s)")]
        [SerializeField] private float _maxFallSpeed = 16f;
        [Tooltip("Ngưỡng vận tốc dọc quanh đỉnh cú nhảy để lướt mượt")]
        [SerializeField] private float _apexVelocityThreshold = 1.8f;
        [Tooltip("Hệ số giảm trọng lực nhẹ tại đỉnh nhảy")]
        [SerializeField] private float _apexGravityMultiplier = 0.65f;
        [Tooltip("Tăng nhẹ tốc độ ngang khi ở đỉnh nhảy")]
        [SerializeField] private float _apexSpeedBonus = 1.12f;
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
        [Tooltip("Bật hiệu ứng bụi khi tiếp đất mạnh (mặc định tắt để không sinh bụi chạy ngang khi nhảy rơi thẳng đứng)")]
        [SerializeField] private bool _enableLandingDust = false;
        [Tooltip("Prefab hiệu ứng bụi chân khi chạy/đẩy (tùy chọn, nếu trống tự nạp Walk_Run_Push_Dust_6)")]
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

        [Header("Ladder Climbing")]
        [Tooltip("Tốc độ leo thang lên/xuống (units/s)")]
        [SerializeField] private float _climbSpeed = 5f;

        [Header("Collision Checks")]
        [Tooltip("Layer được coi là mặt đất")]
        [SerializeField] private LayerMask _groundLayer = ~0;
        [Tooltip("Bán kính vòng tròn kiểm tra chân chạm đất")]
        [SerializeField] private float _groundCheckRadius = 0.1f;
        [Tooltip("Offset chân kiểm tra đất")]
        [SerializeField] private Vector2 _groundCheckOffset = new Vector2(0f, -0.48f);

        private Rigidbody2D _rb;
        private Collider2D _mainCollider;
        private GhostTrail _ghostTrail;
        private PlayerInputHandler _input;

        private bool _isFacingRight = true;
        private bool _isGrounded;
        private bool _isDashing;
        private bool _isClimbing;
        private bool _canClimb;
        private bool _isPushing;

        private int _airJumpsRemaining;
        private float _coyoteTimer;
        private float _groundCheckIgnoreTimer;
        private float _ladderRegrabTimer;
        private float _dashCooldownTimer;
        private float _originalGravityScale;

        private float BaseMaxUpwardSpeed => _jumpForce + 2f;

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
        public bool IsDashing => _isDashing;
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
            _ghostTrail = GetComponent<GhostTrail>();
            _input = GetComponent<PlayerInputHandler>();

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
            _maxAllowedUpwardSpeed = BaseMaxUpwardSpeed;
        }

        public void ProcessUpdate()
        {
            UpdateTimers();
            CheckGround();
            CheckPushing();
            HandleClimbing();
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
                _maxAllowedUpwardSpeed = BaseMaxUpwardSpeed;
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
            _groundCheckIgnoreTimer = 0.08f;
            _maxAllowedUpwardSpeed = force + 2f;
        }

        private void UpdateTimers()
        {
            if (_groundCheckIgnoreTimer > 0f) _groundCheckIgnoreTimer -= Time.deltaTime;

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
            if (_ladderRegrabTimer > 0f) _ladderRegrabTimer -= Time.deltaTime;
        }

        #region Collision & Environment Sensors

        private void CheckGround()
        {
            _isGrounded = false;
            _currentMovingPlatform = null;
            _currentOneWayPlatform = null;

            if (_groundCheckIgnoreTimer > 0f) return;

            // Bắn 3 tia từ cao hơn gót 0.15f xuống qua đế giày, dùng RaycastAll để không bị Trigger (Ladder, Coin...) che khuất mặt đất
            float halfWidth = 0.22f;
            float startLift = 0.15f;
            float castDistance = startLift + _groundCheckRadius + 0.12f;
            Vector2 footPos = (Vector2)transform.position + _groundCheckOffset;
            Vector2 rayBase = footPos + new Vector2(0f, startLift);

            Vector2[] rayOrigins = new Vector2[]
            {
                rayBase + new Vector2(-halfWidth, 0f),
                rayBase,
                rayBase + new Vector2(halfWidth, 0f)
            };

            for (int i = 0; i < rayOrigins.Length; i++)
            {
                RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigins[i], Vector2.down, castDistance, _groundLayer);
                for (int h = 0; h < hits.Length; h++)
                {
                    RaycastHit2D hit = hits[h];
                    if (hit.collider == null || hit.collider.gameObject == gameObject || hit.collider.isTrigger) continue;

                    // Bỏ qua collider đang bị tắt va chạm tạm thời (như OneWayPlatform lúc DropThrough)
                    if (_mainCollider != null && Physics2D.GetIgnoreCollision(hit.collider, _mainCollider)) continue;

                    // Với sàn 1 chiều (PlatformEffector2D), chỉ coi là đứng trên sàn khi tia đánh từ trên mặt sàn xuống
                    // VÀ đáy collider nhân vật đã vượt lên sát mặt sàn (khi đang leo thang phải vượt hẳn qua mặt sàn mới thoát)
                    if (hit.collider.usedByEffector)
                    {
                        if (hit.distance <= 0.001f) continue;
                        float minFootClearance = _isClimbing ? 0.015f : -0.015f;
                        if (_mainCollider != null && _mainCollider.bounds.min.y < hit.point.y + minFootClearance) continue;
                    }

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

                        OneWayPlatform owp = hit.collider.GetComponentInParent<OneWayPlatform>();
                        if (owp != null)
                        {
                            _currentOneWayPlatform = owp;
                        }
                        break;
                    }
                }

                if (_isGrounded) break;
            }
        }

        private void CheckPushing()
        {
            if (!_isGrounded || _isClimbing || _input == null || Mathf.Abs(_input.MoveInput.x) < 0.1f)
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
            float inputX = _input.MoveInput.x;

            float targetRelX = inputX * currentMoveSpeed;

            if (!_isGrounded && Mathf.Abs(_rb.linearVelocityY) < _apexVelocityThreshold)
            {
                targetRelX *= _apexSpeedBonus;
            }

            float currentRelX = _rb.linearVelocityX - platformVelocity.x;
            float accel = SelectHorizontalAcceleration(inputX, currentRelX);
            float newRelX = Mathf.MoveTowards(currentRelX, targetRelX, accel * Time.fixedDeltaTime);
            _rb.linearVelocityX = newRelX + platformVelocity.x;

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

            if (inputX > 0.05f)
            {
                SetFacing(true);
            }
            else if (inputX < -0.05f)
            {
                SetFacing(false);
            }
        }

        private float SelectHorizontalAcceleration(float inputX, float currentRelX)
        {
            bool hasInput = Mathf.Abs(inputX) > 0.05f;
            if (!hasInput)
            {
                return _isGrounded ? _groundDeceleration : _airDeceleration;
            }

            bool isTurning = Mathf.Abs(currentRelX) > 0.1f && Mathf.Sign(inputX) != Mathf.Sign(currentRelX);
            if (_isGrounded)
            {
                return isTurning ? _groundTurnAcceleration : _groundAcceleration;
            }
            return isTurning ? _airTurnAcceleration : _airAcceleration;
        }

        private void SetFacing(bool faceRight)
        {
            if (_isFacingRight != faceRight)
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

            float baseGravity = Physics2D.gravity.y * _originalGravityScale;

            if (!_isGrounded && Mathf.Abs(_rb.linearVelocityY) < _apexVelocityThreshold && _input.JumpHeld)
            {
                _rb.linearVelocityY += baseGravity * (_apexGravityMultiplier - 1f) * Time.fixedDeltaTime;
            }
            else if (_rb.linearVelocityY < 0f)
            {
                _rb.linearVelocityY += baseGravity * (_fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
            }
            else if (_rb.linearVelocityY > 0f && !_input.JumpHeld)
            {
                _rb.linearVelocityY += baseGravity * (_lowJumpGravityMultiplier - 1f) * Time.fixedDeltaTime;
            }

            if (_rb.linearVelocityY < -_maxFallSpeed)
            {
                _rb.linearVelocityY = -_maxFallSpeed;
            }
        }

        #endregion

        #region Jumping & Dust VFX

        private void HandleJump()
        {
            if (_input == null) return;

            // Nhảy khỏi thang khi đang leo (chỉ nhận phím Space / Gamepad South, tránh xung đột với W / Lên khi leo thang)
            if (_isClimbing)
            {
                if (!_input.ExplicitJumpTriggered) return;

                StopClimbing();
                _ladderRegrabTimer = 0.22f;
                _groundCheckIgnoreTimer = 0.08f;
                float jumpX = Mathf.Abs(_input.MoveInput.x) > 0.1f ? Mathf.Sign(_input.MoveInput.x) * _moveSpeed : 0f;
                _rb.linearVelocity = new Vector2(jumpX, _jumpForce * 0.9f);
                _input.ConsumeJump();
                EventBus.Publish(new PlaySoundEvent("Jump", SoundType.SFX, transform.position));
                return;
            }

            if (!_input.JumpTriggered) return;

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
                _groundCheckIgnoreTimer = 0.08f;
                _input.ConsumeJump();
                EventBus.Publish(new PlaySoundEvent("Jump", SoundType.SFX, transform.position));
                return;
            }

            // 3. Nhảy 2 lần trên không (Double Jump)
            if (_airJumpsRemaining > 0)
            {
                _airJumpsRemaining--;
                _rb.linearVelocityY = _jumpForce;
                _coyoteTimer = 0f;
                _groundCheckIgnoreTimer = 0.08f;
                _input.ConsumeJump();

                SpawnDoubleJumpDust();
                EventBus.Publish(new PlaySoundEvent("Jump", SoundType.SFX, transform.position));
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

            // 1. Chỉ tạo bụi tiếp đất khi người chơi chủ động bật _enableLandingDust (mặc định tắt để nhảy rơi thẳng đứng không sinh bụi chạy ngang)
            bool justLanded = !_wasGrounded && _isGrounded;
            if (_enableLandingDust && justLanded && _lastVerticalVelocity < -3.5f)
            {
                SpawnFootDust(false);
            }
            _wasGrounded = _isGrounded;
            _lastVerticalVelocity = _rb.linearVelocityY;

            if (!_isGrounded || _isClimbing)
            {
                _stepDustTimer = 0.05f;
                return;
            }

            // 2. Tạo bụi chân định kỳ khi đẩy thùng hoặc chạy nhanh
            if (_isPushing)
            {
                _stepDustTimer -= Time.deltaTime;
                if (_stepDustTimer <= 0f)
                {
                    _stepDustTimer = _pushDustInterval;
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
            if (isNear)
            {
                _canClimb = true;
                _currentLadder = ladder;
            }
            else if (_currentLadder == ladder)
            {
                _canClimb = false;
                if (_isClimbing) StopClimbing();
                _currentLadder = null;
            }
        }

        private void HandleClimbing()
        {
            if (_input == null) return;

            if (_canClimb && !_isClimbing && _ladderRegrabTimer <= 0f && _currentLadder != null)
            {
                bool wantsClimbUp = _input.VerticalInput > 0.2f && (!_isGrounded || transform.position.y < _currentLadder.TopY - 0.1f);
                bool wantsClimbDown = _input.VerticalInput < -0.2f && (!_isGrounded || _currentOneWayPlatform != null || transform.position.y > _currentLadder.BottomY + 0.7f);

                if (wantsClimbUp || wantsClimbDown)
                {
                    if (wantsClimbDown && _currentOneWayPlatform != null && _mainCollider != null)
                    {
                        _currentOneWayPlatform.DropThrough(_mainCollider);
                    }
                    StartClimbing();
                }
            }
            else if (_isClimbing && _currentLadder != null)
            {
                if (_input.VerticalInput < -0.1f)
                {
                    if (_currentOneWayPlatform != null && _mainCollider != null)
                    {
                        _currentOneWayPlatform.DropThrough(_mainCollider);
                    }
                    else if (_isGrounded && transform.position.y <= _currentLadder.BottomY + 0.7f)
                    {
                        StopClimbing();
                    }
                }
                else if (_isGrounded && transform.position.y > _currentLadder.BottomY + 0.8f)
                {
                    // Đã leo lên đứng vững trên sàn đỉnh thang (dù đang giữ hay vừa thả phím W) -> tự thoát leo thang gọn gàng
                    StopClimbing();
                    _ladderRegrabTimer = 0.2f;
                    if (_rb.linearVelocityY > 0f) _rb.linearVelocityY = 0f;
                }
            }
        }

        private void StartClimbing()
        {
            _isClimbing = true;
            _isPushing = false;
            _airJumpsRemaining = _maxAirJumps;
            _rb.gravityScale = 0f;
            _rb.linearVelocity = Vector2.zero;
            _input?.ConsumeJump();

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

            float verticalVel = _input.VerticalInput * _climbSpeed;
            float halfHeight = _mainCollider != null ? _mainCollider.bounds.extents.y : 0.46f;
            if (_currentLadder != null && verticalVel > 0f && transform.position.y >= _currentLadder.TopY + halfHeight - 0.01f)
            {
                verticalVel = 0f;
            }

            _rb.linearVelocity = new Vector2(_input.MoveInput.x * (_moveSpeed * 0.4f), verticalVel);
        }

        #endregion

        #region Dash Ability

        private void HandleAbilitiesInput()
        {
            if (_input == null) return;

            if (_input.DashTriggered && _dashCooldownTimer <= 0f && !_isDashing && !_isClimbing)
            {
                StartCoroutine(DashRoutine());
            }
        }

        private IEnumerator DashRoutine()
        {
            _isDashing = true;
            _dashCooldownTimer = _dashCooldown;

            float dashDir = (_input != null && _input.MoveInput.x != 0) ? Mathf.Sign(_input.MoveInput.x) : (_isFacingRight ? 1f : -1f);
            SetFacing(dashDir > 0f);
            _rb.gravityScale = 0f;
            _rb.linearVelocity = new Vector2(dashDir * _dashSpeed, 0f);

            if (_ghostTrail != null) _ghostTrail.StartTrail(_dashDuration);
            EventBus.Publish(new PlaySoundEvent("Dash", SoundType.SFX, transform.position));

            yield return new WaitForSeconds(_dashDuration);

            _rb.gravityScale = _originalGravityScale;
            float endInput = _input != null ? _input.MoveInput.x : 0f;
            _rb.linearVelocityX = (Mathf.Abs(endInput) > 0.1f && Mathf.Sign(endInput) == dashDir)
                ? dashDir * _moveSpeed
                : dashDir * (_moveSpeed * 0.45f);
            _isDashing = false;
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
            _canClimb = false;
            _isPushing = false;
            _groundCheckIgnoreTimer = 0f;
            _currentLadder = null;
            _ladderRegrabTimer = 0f;
            _currentMovingPlatform = null;
            _currentOneWayPlatform = null;
            _rb.gravityScale = _originalGravityScale;
            _airJumpsRemaining = _maxAirJumps;
        }

        private void OnDrawGizmosSelected()
        {
            float direction = Application.isPlaying ? (_isFacingRight ? 1f : -1f) : (transform.localScale.x >= 0 ? 1f : -1f);

            // 1. Ground Check (Vòng tròn tại đế giày và 3 tia RaycastAll tiếp đất)
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Vector2 footPos = (Vector2)transform.position + _groundCheckOffset;
            Gizmos.DrawWireSphere(footPos, _groundCheckRadius);

            float halfWidth = 0.22f;
            float startLift = 0.15f;
            float castDist = startLift + _groundCheckRadius + 0.12f;
            Vector2 rayBase = footPos + new Vector2(0f, startLift);
            Gizmos.DrawLine(rayBase + new Vector2(-halfWidth, 0f), rayBase + new Vector2(-halfWidth, -castDist));
            Gizmos.DrawLine(rayBase, rayBase + new Vector2(0f, -castDist));
            Gizmos.DrawLine(rayBase + new Vector2(halfWidth, 0f), rayBase + new Vector2(halfWidth, -castDist));

            // 2. Push Check (2 tia quét đẩy thùng phía trước)
            Gizmos.color = _isPushing ? Color.green : new Color(1f, 0.6f, 0.2f);
            Vector2 p1 = (Vector2)transform.position + new Vector2(direction * 0.35f, -0.1f);
            Vector2 p2 = (Vector2)transform.position + new Vector2(direction * 0.35f, 0.15f);
            Gizmos.DrawLine(p1, p1 + new Vector2(direction * 0.35f, 0f));
            Gizmos.DrawLine(p2, p2 + new Vector2(direction * 0.35f, 0f));

            // 3. Foot Dust Spawn Position (Điểm xuất hiện bụi chân sau gót)
            if (_enableFootDust)
            {
                Gizmos.color = Color.cyan;
                Vector2 dustPos = (Vector2)transform.position + new Vector2(direction * _footDustOffset.x, _footDustOffset.y);
                Gizmos.DrawWireSphere(dustPos, 0.05f);
            }
        }
    }
}
