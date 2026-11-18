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
        [Tooltip("Thời gian hồi giữa 2 đòn đánh (giây)")]
        [SerializeField] private float _attackCooldown = 0.3f;
        [Tooltip("Thời gian Hitbox đấm tồn tại (giây)")]
        [SerializeField] private float _hitboxActiveDuration = 0.15f;
        [Tooltip("Khoảng thời gian cho phép nối combo đòn thứ 2 (giây)")]
        [SerializeField] private float _comboWindow = 0.45f;

        [Header("Ranged Throw")]
        [Tooltip("Prefab của viên đạn ném (tùy chọn, nếu trống sẽ tự tạo đạn đá Rock1)")]
        [SerializeField] private GameObject _projectilePrefab;
        [Tooltip("Thời gian hồi giữa 2 lần ném (giây)")]
        [SerializeField] private float _throwCooldown = 0.45f;
        [Tooltip("Vị trí ném so với tâm nhân vật")]
        [SerializeField] private Vector2 _throwOffset = new Vector2(0.4f, 0.1f);

        private PlayerInputHandler _input;
        private PlayerMovement _movement;

        private int _comboStep = 0;
        private float _comboTimer = 0f;
        private float _attackCooldownTimer;
        private float _throwCooldownTimer;
        private Coroutine _hitboxCoroutine;

        // Events để PlayerAnimationSync lắng nghe kích hoạt hoạt ảnh
        public event Action OnAttack1Triggered;
        public event Action OnAttack2Triggered;
        public event Action OnWalkAttackTriggered;
        public event Action OnThrowTriggered;

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            _movement = GetComponent<PlayerMovement>();

            if (_attackHitbox != null)
            {
                _attackHitbox.enabled = false;
            }
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

            if (_input.AttackTriggered && _attackCooldownTimer <= 0f)
            {
                ExecuteAttack();
            }

            if (_input.ThrowTriggered && _throwCooldownTimer <= 0f)
            {
                ExecuteThrow();
            }
        }

        private void ExecuteAttack()
        {
            _attackCooldownTimer = _attackCooldown;

            // 1. Vừa đi vừa đánh khi đang di chuyển trên mặt đất
            if (_movement.IsGrounded && Mathf.Abs(_input.MoveInput.x) > 0.1f)
            {
                OnWalkAttackTriggered?.Invoke();
                EventBus.Publish(new PlaySoundEvent("Attack", SoundType.SFX, transform.position));
                ActivateHitbox();
                return;
            }

            // 2. Combo đòn 1 -> đòn 2 khi đứng yên hoặc trên không
            if (_comboStep == 0)
            {
                OnAttack1Triggered?.Invoke();
                _comboStep = 1;
                _comboTimer = _comboWindow;
            }
            else if (_comboStep == 1 && _comboTimer > 0f)
            {
                OnAttack2Triggered?.Invoke();
                _comboStep = 0;
                _comboTimer = 0f;
            }

            EventBus.Publish(new PlaySoundEvent("Attack", SoundType.SFX, transform.position));
            ActivateHitbox();
        }

        private void ActivateHitbox()
        {
            if (_attackHitbox != null)
            {
                if (_hitboxCoroutine != null) StopCoroutine(_hitboxCoroutine);
                _hitboxCoroutine = StartCoroutine(ActivateHitboxRoutine());
            }
        }

        private IEnumerator ActivateHitboxRoutine()
        {
            _attackHitbox.enabled = true;
            yield return new WaitForSeconds(_hitboxActiveDuration);
            _attackHitbox.enabled = false;
            _hitboxCoroutine = null;
        }

        private void ExecuteThrow()
        {
            _throwCooldownTimer = _throwCooldown;
            OnThrowTriggered?.Invoke();

            StartCoroutine(SpawnProjectileRoutine(0.12f));
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
        }

        private GameObject CreateDefaultRockProjectile(Vector3 position)
        {
            GameObject rock = new GameObject("Player_Rock_Projectile");
            rock.transform.position = position;

            SpriteRenderer sr = rock.AddComponent<SpriteRenderer>();
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
            _comboStep = 0;
            _comboTimer = 0f;
            _attackCooldownTimer = 0f;
            _throwCooldownTimer = 0f;
            _hitboxCoroutine = null;
        }
    }
}
