using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Combat;
using Platformer2D.Player.Input;

namespace Platformer2D.Player.Components
{
    /// <summary>
    /// Xử lý toàn bộ logic chiến đấu của người chơi (Player Combat System):
    /// - Đòn đánh cận chiến Combo 2 nhịp (Attack 1 -> Attack 2).
    /// - Vừa đi vừa đánh (Walk Attack).
    /// - Quản lý bật/tắt Hitbox đánh cận chiến.
    /// - Đòn đánh tầm xa (Throw) ném đá/đạn.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Melee Combat")]
        [Tooltip("Collider Hitbox gây sát thương khi đấm")]
        [SerializeField] private Collider2D _attackHitbox;
        [Tooltip("Thời gian hồi sau đòn đánh 1 (giây)")]
        [SerializeField] private float _attackCooldown = 0.22f;
        [Tooltip("Thời gian hồi sau đòn liên hoàn 2 phát Attack 2 (giây)")]
        [SerializeField] private float _attack2Cooldown = 0.40f;
        [Tooltip("Thời gian hồi sau đòn vừa chạy vừa đánh Walk Attack (giây)")]
        [SerializeField] private float _walkAttackCooldown = 0.28f;
        [Tooltip("Thời gian Hitbox mỗi cú đấm tồn tại (giây)")]
        [SerializeField] private float _hitboxActiveDuration = 0.09f;
        [Tooltip("Khoảng thời gian cho phép nối combo sang đòn thứ 2 (giây)")]
        [SerializeField] private float _comboWindow = 0.65f;
        [Tooltip("Thời gian ghi nhớ lệnh bấm đánh khi đang ra đòn để bấm đúp nhanh luôn nối sang Attack 2 (giây)")]
        [SerializeField] private float _attackBufferTime = 0.25f;
        [Tooltip("Thời gian tối thiểu sau Attack 1 trước khi nối ngay sang Attack 2 khi bấm đúp nhanh (giây)")]
        [SerializeField] private float _comboChainDelay = 0.13f;
        [Tooltip("Thời gian tối thiểu sau Walk Attack trước khi nối ngay sang Attack 2 khi bấm đúp nhanh (giây)")]
        [SerializeField] private float _walkComboChainDelay = 0.16f;

        [Header("Attack 2 (Double Punch)")]
        [Tooltip("Sát thương mỗi phát của Attack 2 (-1 = dùng mặc định của DamageDealer)")]
        [SerializeField] private int _attack2HitDamage = 1;
        [Tooltip("Hệ số đẩy lùi phát đấm 1 của Attack 2 (nhẹ để giữ mục tiêu trong tầm cho phát 2)")]
        [SerializeField] private float _attack2FirstHitKnockbackScale = 0.25f;
        [Tooltip("Hệ số đẩy lùi phát đấm 2 kết liễu của Attack 2")]
        [SerializeField] private float _attack2SecondHitKnockbackScale = 1.25f;

        [Header("Ranged Throw")]
        [Tooltip("Prefab của viên đạn ném (tùy chọn, nếu trống sẽ tự tạo đạn đá Rock1)")]
        [SerializeField] private GameObject _projectilePrefab;
        [Tooltip("Số đá có sẵn khi bắt đầu màn chơi (0 = phải nhặt đá mới ném được)")]
        [SerializeField] private int _startingRocks = 0;
        [Tooltip("Số đá tối đa mang theo")]
        [SerializeField] private int _maxRocks = 15;
        [Tooltip("Thời gian hồi giữa 2 lần ném (giây)")]
        [SerializeField] private float _throwCooldown = 0.45f;
        [Tooltip("Độ trễ sinh đá khớp khung hình vung tay ném của Pink_Monster_Throw (giây)")]
        [SerializeField] private float _throwReleaseDelay = 0.17f;
        [Tooltip("Vị trí ném so với tâm nhân vật")]
        [SerializeField] private Vector2 _throwOffset = new Vector2(0.4f, 0.1f);

        private PlayerInputHandler _input;
        private PlayerMovement _movement;
        private DamageDealer _damageDealer;

        private int _currentRocks;
        private int _comboStep = 0;
        private float _comboTimer = 0f;
        private float _attackCooldownTimer;
        private float _throwCooldownTimer;
        private float _attackAnimLockTimer;
        private float _attackBufferTimer;
        private float _comboChainTimer;
        private Coroutine _hitboxCoroutine;
        private Coroutine _throwCoroutine;

        public bool IsAttacking => _attackAnimLockTimer > 0f;
        public int CurrentRocks => _currentRocks;
        public int MaxRocks => _maxRocks;

        // Events để PlayerAnimationSync lắng nghe kích hoạt hoạt ảnh
        public event Action OnAttack1Triggered;
        public event Action OnAttack2Triggered;
        public event Action OnWalkAttackTriggered;
        public event Action OnThrowTriggered;

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            _movement = GetComponent<PlayerMovement>();
            _currentRocks = Mathf.Clamp(_startingRocks, 0, _maxRocks);

            if (_attackHitbox != null)
            {
                _attackHitbox.enabled = false;
                _damageDealer = _attackHitbox.GetComponent<DamageDealer>();
            }
        }

        private void Start()
        {
            EventBus.Publish(new RockCountChangedEvent(_currentRocks, _maxRocks));
        }

        public bool AddRocks(int amount)
        {
            if (amount <= 0 || _currentRocks >= _maxRocks) return false;

            _currentRocks = Mathf.Clamp(_currentRocks + amount, 0, _maxRocks);
            EventBus.Publish(new RockCountChangedEvent(_currentRocks, _maxRocks));
            return true;
        }

        public void ProcessUpdate()
        {
            UpdateTimers();
            HandleCombatInput();
        }

        private void UpdateTimers()
        {
            if (_attackCooldownTimer > 0f) _attackCooldownTimer -= Time.deltaTime;
            if (_throwCooldownTimer > 0f) _throwCooldownTimer -= Time.deltaTime;
            if (_attackAnimLockTimer > 0f) _attackAnimLockTimer -= Time.deltaTime;
            if (_attackBufferTimer > 0f) _attackBufferTimer -= Time.deltaTime;
            if (_comboChainTimer > 0f) _comboChainTimer -= Time.deltaTime;

            if (_comboTimer > 0f)
            {
                _comboTimer -= Time.deltaTime;
                if (_comboTimer <= 0f)
                {
                    _comboStep = 0;
                }
            }
        }

        private void HandleCombatInput()
        {
            if (_input == null || _movement == null) return;
            if (_movement.IsDashing || _movement.IsClimbing) return;

            if (_input.AttackTriggered)
            {
                _attackBufferTimer = _attackBufferTime;
            }

            bool canChainToAttack2 = _comboStep == 1 && _comboTimer > 0f && _comboChainTimer <= 0f;
            bool canNormalAttack = _attackCooldownTimer <= 0f;

            if (_attackBufferTimer > 0f && _throwCoroutine == null && (canChainToAttack2 || canNormalAttack))
            {
                _attackBufferTimer = 0f;
                ExecuteAttack();
            }

            if (_input.ThrowTriggered && _throwCooldownTimer <= 0f && _hitboxCoroutine == null)
            {
                if (_currentRocks <= 0)
                {
                    _throwCooldownTimer = 0.25f;
                    EventBus.Publish(new HudToastMessageEvent("NO ROCKS! PICK UP ROCKS OR SMASH CRATES", new Color(1f, 0.45f, 0.35f, 1f)));
                }
                else
                {
                    _currentRocks--;
                    EventBus.Publish(new RockCountChangedEvent(_currentRocks, _maxRocks));
                    ExecuteThrow();
                }
            }
        }

        private void ExecuteAttack()
        {
            // 1. Nối sang Attack 2 (đấm liên hoàn 2 phát) bất kể đang đứng yên, chạy hay trên không
            if (_comboStep == 1 && _comboTimer > 0f)
            {
                _attackCooldownTimer = _attack2Cooldown;
                _comboChainTimer = 0f;
                _attackAnimLockTimer = 0.36f;
                _comboStep = 0;
                _comboTimer = 0f;
                OnAttack2Triggered?.Invoke();
                StartHitboxCoroutine(ActivateDoubleHitRoutine());
                return;
            }

            // 2. Vừa đi vừa đánh khi đang di chuyển trên mặt đất (vẫn cho phép bấm đúp nối sang Attack 2)
            if (_movement.IsGrounded && Mathf.Abs(_input.MoveInput.x) > 0.1f)
            {
                _attackCooldownTimer = _walkAttackCooldown;
                _comboChainTimer = _walkComboChainDelay;
                _attackAnimLockTimer = 0.34f;
                _comboStep = 1;
                _comboTimer = _comboWindow;
                OnWalkAttackTriggered?.Invoke();
                StartHitboxCoroutine(ActivateSingleHitRoutine(0.05f, _hitboxActiveDuration, -1, 0.9f, 1f));
                return;
            }

            // 3. Đòn đánh mở đầu Attack 1
            _attackCooldownTimer = _attackCooldown;
            _comboChainTimer = _comboChainDelay;
            _attackAnimLockTimer = 0.16f;
            _comboStep = 1;
            _comboTimer = _comboWindow;
            OnAttack1Triggered?.Invoke();
            StartHitboxCoroutine(ActivateSingleHitRoutine(0.03f, _hitboxActiveDuration, -1, 0.7f, 0.9f));
        }

        private void StartHitboxCoroutine(IEnumerator routine)
        {
            if (_hitboxCoroutine != null)
            {
                StopCoroutine(_hitboxCoroutine);
            }
            if (_attackHitbox != null)
            {
                _attackHitbox.enabled = false;
            }
            _damageDealer?.EndSwing();
            _hitboxCoroutine = StartCoroutine(routine);
        }

        private IEnumerator ActivateSingleHitRoutine(float delay, float duration, int damageOverride, float knockbackScale, float shakeScale)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            yield return TriggerSwingPulse(duration, damageOverride, knockbackScale, shakeScale);
            _hitboxCoroutine = null;
        }

        private IEnumerator ActivateDoubleHitRoutine()
        {
            // Khớp nhịp đấm 1 (~0.133s) và đấm 2 (~0.267s) của Pink_Monster_Attack_2 ở tốc độ 1.5x
            yield return new WaitForSeconds(0.11f);
            yield return TriggerSwingPulse(0.08f, _attack2HitDamage, _attack2FirstHitKnockbackScale, 0.6f);

            // Khoảng hở >= 3 physics frames để đảm bảo OnTriggerEnter2D kích hoạt lại cho phát 2
            yield return new WaitForSeconds(0.06f);
            yield return TriggerSwingPulse(0.09f, _attack2HitDamage, _attack2SecondHitKnockbackScale, 1.4f);

            _hitboxCoroutine = null;
        }

        private IEnumerator TriggerSwingPulse(float duration, int damageOverride, float knockbackScale, float shakeScale)
        {
            EventBus.Publish(new PlaySoundEvent("Attack", SoundType.SFX, transform.position));

            if (_attackHitbox == null) yield break;

            _damageDealer?.BeginSwing(damageOverride, knockbackScale, shakeScale);
            _attackHitbox.enabled = true;
            yield return new WaitForSeconds(duration);
            _attackHitbox.enabled = false;
            _damageDealer?.EndSwing();
        }

        private void ExecuteThrow()
        {
            _throwCooldownTimer = _throwCooldown;
            _attackAnimLockTimer = 0.29f;
            OnThrowTriggered?.Invoke();

            if (_throwCoroutine != null)
            {
                StopCoroutine(_throwCoroutine);
            }
            _throwCoroutine = StartCoroutine(SpawnProjectileRoutine(_throwReleaseDelay));
        }

        private IEnumerator SpawnProjectileRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            bool facingRight = _movement != null ? _movement.IsFacingRight : true;
            Vector3 spawnPos = transform.position + new Vector3((facingRight ? 1f : -1f) * _throwOffset.x, _throwOffset.y, 0f);

            GameObject projObj;
            if (_projectilePrefab != null)
            {
                projObj = Instantiate(_projectilePrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                projObj = CreateDefaultRockProjectile(spawnPos);
            }

            PlayerProjectile proj = projObj.GetComponent<PlayerProjectile>();
            if (proj != null)
            {
                proj.Initialize(new Vector2(facingRight ? 1f : -1f, 0.15f), gameObject);
            }

            EventBus.Publish(new PlaySoundEvent("Throw", SoundType.SFX, transform.position));
            _throwCoroutine = null;
        }

        private GameObject CreateDefaultRockProjectile(Vector3 position)
        {
            GameObject rock = new GameObject("Player_Rock_Projectile");
            rock.transform.position = position;

            SpriteRenderer sr = rock.AddComponent<SpriteRenderer>();
            SpriteRenderer myRenderer = GetComponent<SpriteRenderer>();
            if (myRenderer != null)
            {
                sr.sortingLayerID = myRenderer.sortingLayerID;
                sr.sortingOrder = myRenderer.sortingOrder + 1;
            }

            Sprite rockSprite = Resources.Load<Sprite>("Rock1");
            if (rockSprite == null)
            {
#if UNITY_EDITOR
                var allSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/GameAssets/Player/1 Pink_Monster/Rock1.png");
                rockSprite = allSprites != null ? System.Linq.Enumerable.OfType<Sprite>(allSprites).FirstOrDefault() : null;
#endif
            }
            if (rockSprite != null) sr.sprite = rockSprite;

            CircleCollider2D col = rock.AddComponent<CircleCollider2D>();
            col.radius = 0.2f;
            col.isTrigger = true;

            rock.AddComponent<PlayerProjectile>();
            return rock;
        }

        public void ResetCombat()
        {
            StopAllCoroutines();
            if (_attackHitbox != null) _attackHitbox.enabled = false;
            _damageDealer?.EndSwing();
            _comboStep = 0;
            _comboTimer = 0f;
            _attackCooldownTimer = 0f;
            _throwCooldownTimer = 0f;
            _attackAnimLockTimer = 0f;
            _attackBufferTimer = 0f;
            _comboChainTimer = 0f;
            _hitboxCoroutine = null;
            _throwCoroutine = null;
            EventBus.Publish(new RockCountChangedEvent(_currentRocks, _maxRocks));
        }
    }
}
