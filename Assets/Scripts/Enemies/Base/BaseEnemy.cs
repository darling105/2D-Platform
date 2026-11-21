using System.Collections;
using UnityEngine;
using Platformer2D.Combat;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;
using Platformer2D.Environment.Hazards;

namespace Platformer2D.Enemies.Base
{
    public enum EnemyArchetype
    {
        PatrolSlime = 0,
        FastChaser = 1,
        SpikeSummoner = 2,
        RangedMage = 3,
        HeavyBruiser = 4,
        EliteCrusher = 5
    }

    /// <summary>
    /// Bộ điều khiển AI Quái vật đa chủng loại cho game 2D Platformer (Độ khó 1 - Cân bằng):
    /// - Hỗ trợ 6 hệ quái: Tuần tra (Ooze), Truy đuổi nhanh (Red), Phóng gai đất (Tiny),
    ///   Pháp sư bắn cầu phép (Mage), Đấu sĩ cận chiến nặng (Bear) và Hộ vệ tinh anh (Yellow).
    /// - Xử lý đúng hướng quay mặt khi Sprite gốc quay sang Trái (-X).
    /// - Tự động nhận diện và né tường, mép vực, Bẫy Gai (SpikeHazard) và KillZone.
    /// - Có nhịp khựng đẩy lùi khi trúng đòn (Hurt Stun), nghỉ ngắn ở mép tuần tra và đòn đánh có cảnh báo (Windup).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class BaseEnemy : MonoBehaviour
    {
        [Header("Archetype & Orientation")]
        [Tooltip("Chủng loại hành vi AI của quái")]
        [SerializeField] private EnemyArchetype _archetype = EnemyArchetype.PatrolSlime;
        [Tooltip("Sprite gốc trong gói CraftPix mặc định quay mặt sang Trái (-X)")]
        [SerializeField] private bool _spriteFacesLeftByDefault = true;

        [Header("Movement & Patrol")]
        [Tooltip("Tốc độ di chuyển khi đi tuần")]
        [SerializeField] private float _moveSpeed = 1.8f;
        [Tooltip("Hệ số tăng tốc khi phát hiện và đuổi theo Player")]
        [SerializeField] private float _chaseSpeedMultiplier = 1.25f;
        [Tooltip("Bắt đầu di chuyển sang phải (true) hay sang trái (false)")]
        [SerializeField] private bool _startMovingRight = true;
        [Tooltip("Bán kính tuần tra tối đa tính từ vị trí xuất phát (<= 0 là không giới hạn)")]
        [SerializeField] private float _maxPatrolDistance = 4.5f;
        [Tooltip("Thời gian dừng quan sát ở mép vực/tường trước khi quay đầu")]
        [SerializeField] private float _edgePauseDuration = 0.35f;

        [Header("Edge, Wall & Hazard Detection")]
        [Tooltip("Khoảng cách kiểm tra phía trước mép collider")]
        [SerializeField] private float _forwardCheckDistance = 0.35f;
        [Tooltip("Độ sâu tia kiểm tra mép vực phía dưới chân")]
        [SerializeField] private float _ledgeCheckDepth = 0.55f;
        [Tooltip("Layer tính là mặt đất/tường")]
        [SerializeField] private LayerMask _groundLayer = ~0;

        [Header("Aggro & Combat")]
        [Tooltip("Tầm phát hiện Player để truy đuổi hoặc tấn công")]
        [SerializeField] private float _aggroRange = 5.5f;
        [Tooltip("Tầm ra đòn tấn công")]
        [SerializeField] private float _attackRange = 1.35f;
        [Tooltip("Sát thương mỗi đòn tấn công")]
        [SerializeField] private int _attackDamage = 1;
        [Tooltip("Thời gian hồi giữa 2 lần tấn công (giây)")]
        [SerializeField] private float _attackCooldown = 1.5f;
        [Tooltip("Thời gian gồng đòn trước khi tung chiêu (giúp người chơi dễ né)")]
        [SerializeField] private float _attackWindup = 0.28f;
        [Tooltip("Độ lệch tâm vùng đánh cận chiến phía trước")]
        [SerializeField] private float _meleeHitOffsetX = 0.65f;
        [Tooltip("Bán kính vùng đánh cận chiến")]
        [SerializeField] private float _meleeHitRadius = 0.65f;

        [Header("Territory & Leashing")]
        [Tooltip("Khoảng cách tối đa quái được phép truy đuổi tính từ vị trí xuất phát trước khi bỏ cuộc (Leash)")]
        [SerializeField] private float _leashDistance = 7.5f;
        [Tooltip("Thời gian đứng cảnh giới/ngó nghiêng khi mất dấu Player trước khi quay về tổ")]
        [SerializeField] private float _lostTargetDuration = 1.2f;

        [Header("Ranged / Projectile Settings")]
        [SerializeField] private Sprite[] _projectileFrames = System.Array.Empty<Sprite>();
        [SerializeField] private float _projectileSpeed = 5.2f;
        [SerializeField] private float _projectileLifetime = 2.6f;

        [Header("Animation")]
        [SerializeField] private Animator _animator;

        private Rigidbody2D _rb;
        private Collider2D _mainCollider;
        private SpriteRenderer _spriteRenderer;
        private Health _health;
        private DamageDealer _damageDealer;

        private Transform _playerTransform;
        private Health _playerHealth;
        private Vector3 _spawnPosition;

        private bool _isMovingRight;
        private bool _isGrounded;
        private bool _isAttacking;
        private bool _pendingTurn;

        private enum EnemyAIState
        {
            Patrol = 0,
            Chase = 1,
            LostTarget = 2,
            ReturnToSpawn = 3
        }

        private EnemyAIState _aiState = EnemyAIState.Patrol;
        private float _lostTargetTimer;
        private float _patrolIdleTimer;
        private float _timeUntilNextPatrolIdle = 4.5f;

        private float _attackCooldownTimer;
        private float _hurtStunTimer;
        private float _edgePauseTimer;
        private float _turnCooldownTimer;
        private float _playerLookupTimer;
        private Coroutine _attackCoroutine;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

        private bool IsRangedArchetype =>
            _archetype == EnemyArchetype.RangedMage || _archetype == EnemyArchetype.SpikeSummoner;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _mainCollider = GetComponent<Collider2D>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _health = GetComponent<Health>();
            _damageDealer = GetComponent<DamageDealer>();

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            _isMovingRight = _startMovingRight;
            UpdateFacingDirection();
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void Start()
        {
            _spawnPosition = transform.position;
            ResolvePlayerReference();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnDamaged += HandleDamaged;
                _health.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnDamaged -= HandleDamaged;
                _health.OnDeath -= HandleDeath;
            }
        }

        private void FixedUpdate()
        {
            if (_health != null && _health.IsDead) return;

            float dt = Time.fixedDeltaTime;
            DecrementTimers(dt);
            _isGrounded = CheckStandingOnGround();

            if (_animator != null)
            {
                _animator.SetBool(IsGroundedHash, _isGrounded);
            }

            // Đang bị đẩy lùi khi trúng đòn -> giữ nguyên quán tính knockback
            if (_hurtStunTimer > 0f)
            {
                UpdateAnimatorSpeed(0f);
                return;
            }

            // Đang gồng đòn tấn công -> đứng yên tung chiêu
            if (_isAttacking)
            {
                _rb.linearVelocityX = 0f;
                UpdateAnimatorSpeed(0f);
                return;
            }

            RefreshPlayerPeriodically(dt);
            ExecuteStateMachine(dt);
        }

        private void DecrementTimers(float dt)
        {
            if (_attackCooldownTimer > 0f) _attackCooldownTimer -= dt;
            if (_hurtStunTimer > 0f) _hurtStunTimer -= dt;
            if (_turnCooldownTimer > 0f) _turnCooldownTimer -= dt;

            if (_edgePauseTimer > 0f)
            {
                _edgePauseTimer -= dt;
                if (_edgePauseTimer <= 0f && _pendingTurn)
                {
                    _pendingTurn = false;
                    TurnAround();
                    _turnCooldownTimer = 0.35f;
                }
            }
        }

        private void RefreshPlayerPeriodically(float dt)
        {
            if (_playerTransform != null) return;

            _playerLookupTimer -= dt;
            if (_playerLookupTimer <= 0f)
            {
                _playerLookupTimer = 0.5f;
                ResolvePlayerReference();
            }
        }

        private void ResolvePlayerReference()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
                _playerHealth = playerObj.GetComponent<Health>();
            }
        }

        private void ExecuteStateMachine(float dt)
        {
            bool playerValid = _playerTransform != null && (_playerHealth == null || !_playerHealth.IsDead);
            float dx = playerValid ? _playerTransform.position.x - transform.position.x : 999f;
            float dy = playerValid ? Mathf.Abs(_playerTransform.position.y - transform.position.y) : 999f;
            float absDx = Mathf.Abs(dx);
            float distFromSpawn = Mathf.Abs(transform.position.x - _spawnPosition.x);

            // Kiểm tra điều kiện có thể Aggro (Người chơi trong tầm nhìn và quái chưa đi quá xa lãnh thổ)
            bool canAggro = playerValid && absDx <= _aggroRange && dy <= 2.2f && distFromSpawn <= _leashDistance;

            // Nếu đang đi tuần / đứng ngó nghiêng / về tổ mà người chơi đi vào tầm -> Truy đuổi (Aggro)
            if (_aiState != EnemyAIState.Chase && canAggro)
            {
                _aiState = EnemyAIState.Chase;
                _edgePauseTimer = 0f;
                _pendingTurn = false;
                _lostTargetTimer = 0f;
                _patrolIdleTimer = 0f;
            }

            switch (_aiState)
            {
                case EnemyAIState.Chase:
                    HandleChaseState(dx, absDx, dy, distFromSpawn);
                    break;

                case EnemyAIState.LostTarget:
                    HandleLostTargetState(dt);
                    break;

                case EnemyAIState.ReturnToSpawn:
                    HandleReturnToSpawnState(distFromSpawn);
                    break;

                case EnemyAIState.Patrol:
                default:
                    HandlePatrolState(dt);
                    break;
            }
        }

        private void HandleChaseState(float dx, float absDx, float dy, float distFromSpawn)
        {
            bool playerValid = _playerTransform != null && (_playerHealth == null || !_playerHealth.IsDead);

            // De-aggro: Quái từ bỏ truy đuổi nếu:
            // 1. Quái đã rượt đuổi vượt quá bán kính bảo vệ (Leash Distance)
            // 2. Người chơi đã chạy quá xa khỏi tầm nhìn hoặc nhảy lên tầng cao khác
            // 3. Người chơi đã chết
            if (!playerValid || distFromSpawn > _leashDistance || absDx > _aggroRange * 1.35f || dy > 2.6f)
            {
                _aiState = EnemyAIState.LostTarget;
                _lostTargetTimer = _lostTargetDuration;
                _rb.linearVelocityX = 0f;
                UpdateAnimatorSpeed(0f);
                return;
            }

            bool faceRight = Mathf.Abs(dx) > 0.05f ? dx > 0f : _isMovingRight;
            SetFacing(faceRight);

            // Đã vào tầm đánh -> Dừng chân và ra đòn
            if (absDx <= _attackRange)
            {
                _rb.linearVelocityX = 0f;
                UpdateAnimatorSpeed(0f);

                if (_attackCooldownTimer <= 0f)
                {
                    StartAttack();
                }
                return;
            }

            // Đuổi theo người chơi, nhưng nếu gặp mép vực sâu/bẫy gai/tường chặn thì dừng lại cảnh giới, không tự sát
            float chaseDir = faceRight ? 1f : -1f;
            if (HasObstacleOrHazardAhead(chaseDir))
            {
                _rb.linearVelocityX = 0f;
                UpdateAnimatorSpeed(0f);
                return;
            }

            float chaseSpeed = _moveSpeed * _chaseSpeedMultiplier;
            _rb.linearVelocityX = chaseDir * chaseSpeed;
            UpdateAnimatorSpeed(Mathf.Abs(_rb.linearVelocityX));
        }

        private void HandleLostTargetState(float dt)
        {
            // Đứng lại ngó nghiêng / gầm gừ / cảnh giới khi mất dấu mục tiêu
            _rb.linearVelocityX = 0f;
            UpdateAnimatorSpeed(0f);

            _lostTargetTimer -= dt;
            if (_lostTargetTimer <= 0f)
            {
                // Hết thời gian cảnh giới -> Quay đầu điềm tĩnh trở về vị trí tuần tra ban đầu
                _aiState = EnemyAIState.ReturnToSpawn;
            }
        }

        private void HandleReturnToSpawnState(float distFromSpawn)
        {
            // Khi đã về tới gần điểm xuất phát ban đầu (< 0.4f), tiếp tục tuần tra bình thường
            if (distFromSpawn <= 0.4f)
            {
                _aiState = EnemyAIState.Patrol;
                _isMovingRight = _startMovingRight;
                UpdateFacingDirection();
                return;
            }

            float returnDir = Mathf.Sign(_spawnPosition.x - transform.position.x);
            SetFacing(returnDir > 0f);

            // Nếu trên đường về gặp mép vực/vật cản thì chuyển luôn sang tuần tra tại chỗ
            if (HasObstacleOrHazardAhead(returnDir))
            {
                _aiState = EnemyAIState.Patrol;
                return;
            }

            // Đi bộ đều đặn quay về tổ
            _rb.linearVelocityX = returnDir * (_moveSpeed * 0.9f);
            UpdateAnimatorSpeed(Mathf.Abs(_rb.linearVelocityX));
        }

        private void HandlePatrolState(float dt)
        {
            // Nghỉ tạm thời khi tới mép hoặc nghỉ ngẫu nhiên
            if (_edgePauseTimer > 0f)
            {
                _rb.linearVelocityX = 0f;
                UpdateAnimatorSpeed(0f);
                return;
            }

            // Nghỉ quan sát định kỳ để quái có cử động sống động
            if (_patrolIdleTimer > 0f)
            {
                _patrolIdleTimer -= dt;
                _rb.linearVelocityX = 0f;
                UpdateAnimatorSpeed(0f);
                return;
            }

            _timeUntilNextPatrolIdle -= dt;
            if (_timeUntilNextPatrolIdle <= 0f)
            {
                _timeUntilNextPatrolIdle = Random.Range(5.0f, 8.0f);
                _patrolIdleTimer = 1.0f;
                _rb.linearVelocityX = 0f;
                UpdateAnimatorSpeed(0f);
                return;
            }

            float direction = _isMovingRight ? 1f : -1f;

            if (ShouldTurnAroundInPatrol(direction))
            {
                if (_edgePauseDuration > 0f)
                {
                    _rb.linearVelocityX = 0f;
                    UpdateAnimatorSpeed(0f);
                    _edgePauseTimer = _edgePauseDuration;
                    _pendingTurn = true;
                }
                else
                {
                    TurnAround();
                    _turnCooldownTimer = 0.35f;
                }
                return;
            }

            _rb.linearVelocityX = direction * _moveSpeed;
            UpdateAnimatorSpeed(Mathf.Abs(_rb.linearVelocityX));
        }

        private bool ShouldTurnAroundInPatrol(float direction)
        {
            if (_turnCooldownTimer > 0f) return false;

            if (_maxPatrolDistance > 0f)
            {
                if (direction > 0f && transform.position.x >= _spawnPosition.x + _maxPatrolDistance)
                {
                    return true;
                }
                if (direction < 0f && transform.position.x <= _spawnPosition.x - _maxPatrolDistance)
                {
                    return true;
                }
            }

            return HasObstacleOrHazardAhead(direction);
        }

        private Bounds GetColliderBounds()
        {
            return _mainCollider != null
                ? _mainCollider.bounds
                : new Bounds(transform.position, new Vector3(0.8f, 0.8f, 0f));
        }

        private bool CheckStandingOnGround()
        {
            Bounds b = GetColliderBounds();
            Vector2 footOrigin = new Vector2(b.center.x, b.min.y + 0.08f);
            RaycastHit2D[] hits = Physics2D.RaycastAll(footOrigin, Vector2.down, _ledgeCheckDepth, _groundLayer);
            for (int i = 0; i < hits.Length; i++)
            {
                if (IsWalkableGroundCollider(hits[i].collider))
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasObstacleOrHazardAhead(float direction)
        {
            Bounds b = GetColliderBounds();
            float checkX = b.center.x + direction * (b.extents.x + _forwardCheckDistance);

            // 1. Kiểm tra tường hoặc quái khác chắn phía trước (2 tia tầm ngực & ống chân)
            Vector2 chestOrigin = new Vector2(b.center.x + direction * (b.extents.x + 0.05f), b.center.y);
            Vector2 shinOrigin = new Vector2(b.center.x + direction * (b.extents.x + 0.05f), b.min.y + 0.18f);
            float wallRayDist = _forwardCheckDistance + 0.15f;

            if (HasBlockingObstacleRay(chestOrigin, direction, wallRayDist) ||
                HasBlockingObstacleRay(shinOrigin, direction, wallRayDist))
            {
                return true;
            }

            // 2. Kiểm tra Bẫy Gai (SpikeHazard) hoặc Vực Tử Thần (KillZone) phía trước mặt
            Vector2 hazardBoxCenter = new Vector2(checkX + direction * 0.2f, b.min.y + 0.35f);
            Vector2 hazardBoxSize = new Vector2(0.85f, 1.0f);
            Collider2D[] hazardHits = Physics2D.OverlapBoxAll(hazardBoxCenter, hazardBoxSize, 0f);
            for (int i = 0; i < hazardHits.Length; i++)
            {
                Collider2D col = hazardHits[i];
                if (col == null || col.gameObject == gameObject) continue;

                if (col.GetComponent<SpikeHazard>() != null ||
                    col.GetComponentInParent<SpikeHazard>() != null ||
                    col.GetComponent<KillZone>() != null ||
                    col.GetComponentInParent<KillZone>() != null)
                {
                    return true;
                }
            }

            // 3. Nếu đang đứng trên đất thì kiểm tra mép vực phía trước
            if (!_isGrounded) return false;

            Vector2 ledgeOrigin = new Vector2(checkX, b.min.y + 0.1f);
            RaycastHit2D[] groundHits = Physics2D.RaycastAll(ledgeOrigin, Vector2.down, _ledgeCheckDepth, _groundLayer);
            for (int i = 0; i < groundHits.Length; i++)
            {
                if (IsWalkableGroundCollider(groundHits[i].collider))
                {
                    return false;
                }
            }

            return true;
        }

        private bool HasBlockingObstacleRay(Vector2 origin, float direction, float distance)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.right * direction, distance, _groundLayer);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i].collider;
                if (col != null && col.gameObject != gameObject && !col.isTrigger && !col.CompareTag("Player"))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsWalkableGroundCollider(Collider2D col)
        {
            return col != null
                && col.gameObject != gameObject
                && !col.isTrigger
                && !col.CompareTag("Player")
                && col.GetComponent<BaseEnemy>() == null;
        }

        private void StartAttack()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
            }
            _attackCoroutine = StartCoroutine(PerformAttackRoutine());
        }

        private IEnumerator PerformAttackRoutine()
        {
            _isAttacking = true;
            _attackCooldownTimer = _attackCooldown;
            _rb.linearVelocityX = 0f;

            float attackDir = _isMovingRight ? 1f : -1f;
            if (_animator != null)
            {
                _animator.SetTrigger(AttackHash);
            }

            if (_attackWindup > 0f)
            {
                yield return new WaitForSeconds(_attackWindup);
            }

            if (_health == null || !_health.IsDead)
            {
                if (IsRangedArchetype && _projectileFrames != null && _projectileFrames.Length > 0)
                {
                    SpawnEnemyProjectile(attackDir);
                }
                else
                {
                    PerformMeleeStrike(attackDir);
                }
            }

            yield return new WaitForSeconds(0.18f);
            _isAttacking = false;
            _attackCoroutine = null;
        }

        private void PerformMeleeStrike(float attackDir)
        {
            EventBus.Publish(new PlaySoundEvent("Attack", SoundType.SFX, transform.position));

            Vector2 hitCenter = (Vector2)transform.position + new Vector2(attackDir * _meleeHitOffsetX, 0f);
            Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, _meleeHitRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i];
                if (col == null || !col.CompareTag("Player")) continue;

                IDamageable damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    DamageData data = new DamageData(
                        _attackDamage,
                        gameObject,
                        hitCenter,
                        new Vector2(attackDir * 4.5f, 1.0f),
                        DamageType.Physical);
                    damageable.TakeDamage(data);
                    break;
                }
            }
        }

        private void SpawnEnemyProjectile(float attackDir)
        {
            EventBus.Publish(new PlaySoundEvent("Attack", SoundType.SFX, transform.position));

            float offsetY = _archetype == EnemyArchetype.SpikeSummoner ? -0.22f : 0.06f;
            Vector3 spawnPos = transform.position + new Vector3(attackDir * 0.48f, offsetY, 0f);

            GameObject projObj = new GameObject($"{gameObject.name}_Projectile");
            projObj.transform.position = spawnPos;

            EnemyProjectile projectile = projObj.AddComponent<EnemyProjectile>();
            Material sharedMat = _spriteRenderer != null ? _spriteRenderer.sharedMaterial : null;

            projectile.Initialize(
                new Vector2(attackDir, 0f),
                _projectileSpeed,
                _attackDamage,
                _projectileLifetime,
                _projectileFrames,
                _spriteFacesLeftByDefault,
                sharedMat);
        }

        private void HandleDamaged(DamageData data)
        {
            if (_health != null && _health.IsDead) return;

            _hurtStunTimer = 0.25f;
            _edgePauseTimer = 0f;
            _pendingTurn = false;
            _patrolIdleTimer = 0f;

            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
                _isAttacking = false;
            }

            // Phản xạ khi bị tấn công: Chuyển ngay sang trạng thái truy đuổi
            if (_aiState != EnemyAIState.Chase)
            {
                _aiState = EnemyAIState.Chase;
                _lostTargetTimer = 0f;
            }

            // Quay mặt về phía người tấn công nếu bị đánh lén từ sau lưng
            float attackerX = _playerTransform != null
                && data.Source != null
                && data.Source.transform.root.CompareTag("Player")
                ? _playerTransform.position.x
                : data.HitPoint.x;

            if (Mathf.Abs(attackerX - transform.position.x) > 0.05f)
            {
                SetFacing(attackerX >= transform.position.x);
            }
        }

        private void HandleDeath()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }

            _isAttacking = false;
            _pendingTurn = false;
            _aiState = EnemyAIState.Patrol;
            _lostTargetTimer = 0f;

            if (_damageDealer != null)
            {
                _damageDealer.enabled = false;
            }

            // Hãm vận tốc ngang, để rơi tự do chạm sàn tự nhiên (Health.cs tự điều khiển rơi và khóa kinematic sau khi tiếp đất)
            if (_rb != null)
            {
                _rb.linearVelocityX = 0f;
            }

            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash, 0f);
                _animator.SetBool(IsDeadHash, true);
            }
        }

        private void TurnAround()
        {
            SetFacing(!_isMovingRight);
        }

        private void SetFacing(bool movingRight)
        {
            if (_isMovingRight == movingRight) return;
            _isMovingRight = movingRight;
            UpdateFacingDirection();
        }

        private void UpdateFacingDirection()
        {
            Vector3 scale = transform.localScale;
            float absX = Mathf.Abs(scale.x);
            bool positiveScale = _isMovingRight != _spriteFacesLeftByDefault;
            scale.x = positiveScale ? absX : -absX;
            transform.localScale = scale;
        }

        private void UpdateAnimatorSpeed(float speed)
        {
            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash, speed);
            }
        }

        private void OnDrawGizmosSelected()
        {
            float direction = Application.isPlaying
                ? (_isMovingRight ? 1f : -1f)
                : (_startMovingRight ? 1f : -1f);

            Bounds b = _mainCollider != null
                ? _mainCollider.bounds
                : new Bounds(transform.position, new Vector3(0.8f, 0.8f, 0f));

            float checkX = b.center.x + direction * (b.extents.x + _forwardCheckDistance);
            Vector2 ledgeOrigin = new Vector2(checkX, b.min.y + 0.1f);
            Vector2 chestOrigin = new Vector2(b.center.x + direction * (b.extents.x + 0.05f), b.center.y);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(ledgeOrigin, ledgeOrigin + Vector2.down * _ledgeCheckDepth);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(chestOrigin, chestOrigin + Vector2.right * direction * (_forwardCheckDistance + 0.15f));

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(new Vector2(checkX + direction * 0.2f, b.min.y + 0.35f), new Vector3(0.85f, 1.0f, 0f));

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _aggroRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);

            if (_maxPatrolDistance > 0f)
            {
                Vector3 center = Application.isPlaying ? _spawnPosition : transform.position;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(center + Vector3.left * _maxPatrolDistance, center + Vector3.right * _maxPatrolDistance);
            }
        }
    }
}
