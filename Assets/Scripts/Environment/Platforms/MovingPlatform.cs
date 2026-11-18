using UnityEngine;

namespace Platformer2D.Environment.Platforms
{
    /// <summary>
    /// Sàn di chuyển (Moving Platform):
    /// - Di chuyển tuần hoàn giữa các mốc điểm (Waypoints).
    /// - Cung cấp thông tin Velocity chuẩn xác để PlayerController đồng bộ chuyển động.
    /// - Không can thiệp đổi transform.parent của Player để tránh lỗi xung đột vật lý và kẹt góc.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        public enum MovementType
        {
            PingPong,
            Loop
        }

        [Header("Movement Settings")]
        [Tooltip("Danh sách các điểm mốc sàn sẽ di chuyển qua (World position)")]
        [SerializeField] private Transform[] _waypoints;
        [Tooltip("Tốc độ di chuyển của sàn (units/s)")]
        [SerializeField] private float _speed = 3f;
        [Tooltip("Thời gian dừng nghỉ tại mỗi mốc điểm (giây)")]
        [SerializeField] private float _waitTimeAtWaypoint = 0.5f;
        [Tooltip("Chế độ di chuyển: PingPong (đi tới rồi quay lại) hoặc Loop (quay vòng về điểm đầu)")]
        [SerializeField] private MovementType _movementType = MovementType.PingPong;

        private Rigidbody2D _rb;
        private int _currentWaypointIndex = 0;
        private int _direction = 1;
        private float _waitTimer = 0f;
        private bool _isWaiting = false;

        public Vector2 Velocity { get; private set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void FixedUpdate()
        {
            if (_waypoints == null || _waypoints.Length < 2)
            {
                Velocity = Vector2.zero;
                return;
            }

            if (_isWaiting)
            {
                Velocity = Vector2.zero;
                _waitTimer -= Time.fixedDeltaTime;
                if (_waitTimer <= 0f)
                {
                    _isWaiting = false;
                }
                return;
            }

            Transform targetWaypoint = _waypoints[_currentWaypointIndex];
            if (targetWaypoint == null)
            {
                Velocity = Vector2.zero;
                return;
            }

            Vector2 currentPosition = _rb.position;
            Vector2 targetPosition = targetWaypoint.position;

            Vector2 newPosition = Vector2.MoveTowards(currentPosition, targetPosition, _speed * Time.fixedDeltaTime);
            Velocity = (newPosition - currentPosition) / Time.fixedDeltaTime;
            _rb.MovePosition(newPosition);

            if (Vector2.Distance(newPosition, targetPosition) < 0.02f)
            {
                _isWaiting = true;
                _waitTimer = _waitTimeAtWaypoint;
                AdvanceWaypoint();
            }
        }

        private void OnDisable()
        {
            Velocity = Vector2.zero;
        }

        private void AdvanceWaypoint()
        {
            if (_movementType == MovementType.PingPong)
            {
                if (_currentWaypointIndex >= _waypoints.Length - 1)
                {
                    _direction = -1;
                }
                else if (_currentWaypointIndex <= 0)
                {
                    _direction = 1;
                }
                _currentWaypointIndex += _direction;
            }
            else // Loop
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_waypoints == null || _waypoints.Length < 2) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                if (_waypoints[i] == null) continue;

                Gizmos.DrawWireSphere(_waypoints[i].position, 0.25f);

                int nextIndex = (i + 1) % _waypoints.Length;
                if (_movementType == MovementType.PingPong && nextIndex == 0) continue;

                if (_waypoints[nextIndex] != null)
                {
                    Gizmos.DrawLine(_waypoints[i].position, _waypoints[nextIndex].position);
                }
            }
        }
    }
}
