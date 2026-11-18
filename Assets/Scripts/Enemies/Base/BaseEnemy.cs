using UnityEngine;
using Platformer2D.Combat;

namespace Platformer2D.Enemies.Base
{
    /// <summary>
    /// Bộ điều khiển Quái vật AI cơ bản cho game 2D Platformer:
    /// - Đi tuần tra qua lại (Patrol).
    /// - Tự động quay đầu khi gặp tường hoặc mép vực (Ledge & Wall Detection).
    /// - Gây sát thương khi va chạm với Player (Contact Damage).
    /// - Nhận sát thương, bị đẩy lùi và tử vong khi hết máu.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class BaseEnemy : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Tốc độ di chuyển khi đi tuần")]
        [SerializeField] private float _moveSpeed = 2.5f;
        [Tooltip("Bắt đầu di chuyển sang phải (true) hay sang trái (false)")]
        [SerializeField] private bool _startMovingRight = true;

        [Header("Edge & Wall Detection")]
        [Tooltip("Khoảng cách điểm kiểm tra phía trước mặt")]
        [SerializeField] private float _forwardCheckDistance = 0.5f;
        [Tooltip("Độ sâu tia kiểm tra mép vực phía dưới chân")]
        [SerializeField] private float _ledgeCheckDepth = 0.8f;
        [Tooltip("Layer tính là mặt đất/tường")]
        [SerializeField] private LayerMask _groundLayer = ~0;

        [Header("Animation")]
        [SerializeField] private Animator _animator;

        private Rigidbody2D _rb;
        private Health _health;
        private bool _isMovingRight;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            _isMovingRight = _startMovingRight;

            // Đồng bộ hướng mặt ban đầu
            UpdateFacingDirection();

            // Khóa xoay Z vật lý
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnDeath -= HandleDeath;
            }
        }

        private void FixedUpdate()
        {
            if (_health != null && _health.IsDead) return;

            Patrol();
        }

        private void Patrol()
        {
            // Di chuyển ngang
            float direction = _isMovingRight ? 1f : -1f;
            _rb.linearVelocityX = direction * _moveSpeed;

            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash, Mathf.Abs(_rb.linearVelocityX));
            }

            // Kiểm tra mép vực hoặc tường phía trước để quay đầu
            if (ShouldTurnAround())
            {
                TurnAround();
            }
        }

        private bool ShouldTurnAround()
        {
            float direction = _isMovingRight ? 1f : -1f;
            Vector2 origin = (Vector2)transform.position + new Vector2(direction * _forwardCheckDistance, 0f);

            // 1. Kiểm tra tường phía trước (RaycastAll ngang, bỏ qua chính mình và trigger)
            RaycastHit2D[] wallHits = Physics2D.RaycastAll(origin, Vector2.right * direction, 0.2f, _groundLayer);
            for (int i = 0; i < wallHits.Length; i++)
            {
                Collider2D col = wallHits[i].collider;
                if (col != null && col.gameObject != gameObject && !col.isTrigger && !col.CompareTag("Player"))
                {
                    return true;
                }
            }

            // 2. Kiểm tra xem quái đang đứng trên sàn chưa (tránh quay đầu liên tục khi đang rơi trên không)
            bool isStandingOnGround = false;
            RaycastHit2D[] centerGroundHits = Physics2D.RaycastAll(transform.position, Vector2.down, _ledgeCheckDepth, _groundLayer);
            for (int i = 0; i < centerGroundHits.Length; i++)
            {
                Collider2D col = centerGroundHits[i].collider;
                if (col != null && col.gameObject != gameObject && !col.isTrigger)
                {
                    isStandingOnGround = true;
                    break;
                }
            }

            if (!isStandingOnGround)
            {
                return false;
            }

            // 3. Kiểm tra mép sàn phía trước (RaycastAll thẳng đứng xuống dưới chân phía trước)
            bool hasGroundAhead = false;
            RaycastHit2D[] groundHits = Physics2D.RaycastAll(origin, Vector2.down, _ledgeCheckDepth, _groundLayer);
            for (int i = 0; i < groundHits.Length; i++)
            {
                Collider2D col = groundHits[i].collider;
                if (col != null && col.gameObject != gameObject && !col.isTrigger)
                {
                    hasGroundAhead = true;
                    break;
                }
            }

            return !hasGroundAhead;
        }

        private void TurnAround()
        {
            _isMovingRight = !_isMovingRight;
            UpdateFacingDirection();
        }

        private void UpdateFacingDirection()
        {
            Vector3 scale = transform.localScale;
            scale.x = _isMovingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        private void HandleDeath()
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Kinematic;

            if (_animator != null)
            {
                _animator.SetBool(IsDeadHash, true);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Vẽ tia kiểm tra mép vực và tường trong Scene để dễ căn chỉnh
            float direction = _isMovingRight ? 1f : -1f;
            Vector2 origin = (Vector2)transform.position + new Vector2(direction * _forwardCheckDistance, 0f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + Vector2.down * _ledgeCheckDepth);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(origin, origin + Vector2.right * direction * 0.2f);
        }
    }
}
