using UnityEngine;

namespace Platformer2D.Player.Components
{
    /// <summary>
    /// Đồng bộ dữ liệu chuyển động và chiến đấu sang Animator (Presentation Layer):
    /// - Quản lý toàn bộ Animator Parameter Hashes (Tối ưu hiệu năng, không tạo chuỗi rác GC).
    /// - Lắng nghe sự kiện từ PlayerMovement và PlayerCombat để đổi hoạt ảnh chính xác.
    /// - Xử lý đóng băng hoạt ảnh (freeze frame) khi buông tay trên thang leo.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationSync : MonoBehaviour
    {
        private Animator _animator;
        private PlayerMovement _movement;
        private PlayerCombat _combat;
        private float _hurtTimer;

        public bool IsHurt => _hurtTimer > 0f;

        // Animator Hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int Attack2Hash = Animator.StringToHash("Attack2");
        private static readonly int WalkAttackHash = Animator.StringToHash("WalkAttack");
        private static readonly int ThrowHash = Animator.StringToHash("Throw");
        private static readonly int IsClimbingHash = Animator.StringToHash("IsClimbing");
        private static readonly int IsPushingHash = Animator.StringToHash("IsPushing");
        private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _movement = GetComponent<PlayerMovement>();
            _combat = GetComponent<PlayerCombat>();
        }

        private void OnEnable()
        {
            if (_combat != null)
            {
                _combat.OnAttack1Triggered += HandleAttack1;
                _combat.OnAttack2Triggered += HandleAttack2;
                _combat.OnWalkAttackTriggered += HandleWalkAttack;
                _combat.OnThrowTriggered += HandleThrow;
            }
        }

        private void OnDisable()
        {
            if (_combat != null)
            {
                _combat.OnAttack1Triggered -= HandleAttack1;
                _combat.OnAttack2Triggered -= HandleAttack2;
                _combat.OnWalkAttackTriggered -= HandleWalkAttack;
                _combat.OnThrowTriggered -= HandleThrow;
            }
        }

        public void ProcessUpdate(bool isDead)
        {
            if (_animator == null || isDead) return;

            if (_hurtTimer > 0f)
            {
                _hurtTimer -= Time.deltaTime;
                return;
            }

            bool isAttacking = _combat != null && _combat.IsAttacking;
            _animator.SetBool(IsAttackingHash, isAttacking);

            if (_movement != null)
            {
                // Xử lý leo thang
                if (_movement.IsClimbing)
                {
                    _animator.SetBool(IsClimbingHash, true);
                    _animator.SetBool(IsGroundedHash, _movement.IsGrounded);
                    _animator.SetBool(IsPushingHash, false);
                    _animator.SetBool(IsAttackingHash, false);
                    // Chỉ đóng băng frame khi Animator đã thật sự chuyển vào state Pink_Monster_Climb và người chơi buông phím
                    bool inClimbState = _animator.GetCurrentAnimatorStateInfo(0).IsName("Pink_Monster_Climb");
                    _animator.speed = (!inClimbState || Mathf.Abs(_movement.VerticalClimbSpeed) > 0.1f) ? 1f : 0f;
                    return;
                }
                else
                {
                    _animator.SetBool(IsClimbingHash, false);
                    _animator.speed = 1f;
                }

                _animator.SetBool(IsPushingHash, _movement.IsPushing);
                _animator.SetFloat(SpeedHash, _movement.RelativeHorizontalSpeed);
                _animator.SetBool(IsGroundedHash, _movement.IsGrounded);
            }
        }

        private void HandleAttack1()
        {
            if (_animator != null) _animator.SetTrigger(AttackHash);
        }

        private void HandleAttack2()
        {
            if (_animator != null) _animator.SetTrigger(Attack2Hash);
        }

        private void HandleWalkAttack()
        {
            if (_animator != null) _animator.SetTrigger(WalkAttackHash);
        }

        private void HandleThrow()
        {
            if (_animator != null) _animator.SetTrigger(ThrowHash);
        }

        public void TriggerHurt(float duration = 0.35f)
        {
            _hurtTimer = duration;
            if (_animator != null)
            {
                _animator.speed = 1f;
                _animator.ResetTrigger(AttackHash);
                _animator.ResetTrigger(Attack2Hash);
                _animator.ResetTrigger(WalkAttackHash);
                _animator.ResetTrigger(ThrowHash);
                _animator.SetTrigger(HurtHash);
                _animator.Play("Pink_Monster_Hurt", 0, 0f);
            }
        }

        public void SetDead(bool isDead)
        {
            _hurtTimer = 0f;
            if (_animator != null)
            {
                _animator.speed = 1f;
                _animator.SetBool(IsDeadHash, isDead);
                _animator.SetBool(IsClimbingHash, false);
                _animator.SetBool(IsPushingHash, false);
                _animator.SetBool(IsAttackingHash, false);

                if (isDead)
                {
                    _animator.Play("Pink_Monster_Death", 0, 0f);
                }
                else
                {
                    _animator.Play("Pink_Monster_Idle", 0, 0f);
                }
            }
        }
    }
}
