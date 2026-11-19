using System.Collections;
using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Singletons;
using Platformer2D.Player.Controllers;
using Platformer2D.Environment.Platforms;

namespace Platformer2D.Managers
{
    /// <summary>
    /// Quản lý điểm hồi sinh (Checkpoint & Respawn Manager):
    /// - Lưu trữ vị trí Checkpoint gần nhất mà người chơi chạm vào.
    /// - Khi nhận PlayerDiedEvent, tự động xử lý thời gian chờ và hồi sinh nhân vật tại Checkpoint.
    /// - Khôi phục lại trạng thái của các bệ sập (Falling Platforms) trong màn chơi.
    /// </summary>
    public class CheckpointManager : Singleton<CheckpointManager>
    {
        [Tooltip("Tự động hồi sinh ngay sau khi chết mà không chờ người chơi bấm nút trên bảng Game Over")]
        [SerializeField] private bool _autoRespawnOnDeath = false;
        [Tooltip("Thời gian chờ trước khi hồi sinh sau khi chết (giây)")]
        [SerializeField] private float _respawnDelay = 1.2f;
        [Tooltip("Điểm xuất phát mặc định (nếu chưa chạm Checkpoint nào)")]
        [SerializeField] private Transform _defaultSpawnPoint;

        private Vector3 _currentRespawnPosition;
        private PlayerController _playerController;

        private void Start()
        {
            _playerController = FindAnyObjectByType<PlayerController>();

            if (_defaultSpawnPoint != null)
            {
                _currentRespawnPosition = _defaultSpawnPoint.position;
            }
            else if (_playerController != null)
            {
                _currentRespawnPosition = _playerController.transform.position;
            }

            EventBus.Subscribe<CheckpointActivatedEvent>(OnCheckpointActivated);
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventBus.Unsubscribe<CheckpointActivatedEvent>(OnCheckpointActivated);
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        }

        private void OnCheckpointActivated(CheckpointActivatedEvent evt)
        {
            _currentRespawnPosition = evt.CheckpointPosition;
        }

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            if (_autoRespawnOnDeath)
            {
                StartCoroutine(RespawnRoutine());
            }
        }

        /// <summary>
        /// Hồi sinh nhân vật ngay lập tức tại Checkpoint gần nhất (dùng khi bấm nút Checkpoint trên Game Over UI).
        /// </summary>
        public void RespawnNow()
        {
            StopAllCoroutines();
            ExecuteRespawn();
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(_respawnDelay);
            ExecuteRespawn();
        }

        private void ExecuteRespawn()
        {
            if (_playerController == null)
            {
                _playerController = FindAnyObjectByType<PlayerController>();
            }

            if (_playerController != null)
            {
                _playerController.RespawnAt(_currentRespawnPosition);
            }

            // Đặt lại các bệ sụp lún trong màn chơi khi hồi sinh
            FallingPlatform[] fallingPlatforms = FindObjectsByType<FallingPlatform>();
            for (int i = 0; i < fallingPlatforms.Length; i++)
            {
                fallingPlatforms[i].ResetPlatform();
            }
        }

        /// <summary>
        /// Cho phép thiết lập vị trí hồi sinh thủ công bằng Code.
        /// </summary>
        public void SetRespawnPosition(Vector3 newPosition)
        {
            _currentRespawnPosition = newPosition;
        }
    }
}
