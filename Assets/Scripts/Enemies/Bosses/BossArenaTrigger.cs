using System.Collections;
using UnityEngine;
using Platformer2D.CameraControl;
using Platformer2D.Core.Architecture;

namespace Platformer2D.Enemies.Bosses
{
    /// <summary>
    /// Vùng kích hoạt Đấu trường Boss (Boss Arena Trigger):
    /// - Khi người chơi bước vào: phong tỏa 2 đầu đấu trường (hạ cửa đá chặn đường lùi và đường tiến).
    /// - Kích hoạt Boss gầm rú, hiển thị Thanh Máu Boss trên HUD và khóa góc nhìn Camera.
    /// - Khi Boss bị hạ gục: giải tỏa rào chắn, sinh Chìa khóa Vàng / mở Rương Chiến Thắng sang màn kế tiếp.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BossArenaTrigger : MonoBehaviour
    {
        [Header("Arena References")]
        [SerializeField] private BaseBoss _boss;
        [SerializeField] private GameObject _entryBarrier;
        [SerializeField] private GameObject _exitBarrier;
        [SerializeField] private GameObject _victoryRewardPrefab;
        [SerializeField] private Vector3 _rewardSpawnOffset = new Vector3(3f, 0f, 0f);

        [Header("Arena Camera Bounds (Tùy chọn)")]
        [SerializeField] private bool _clampCamera = true;
        [SerializeField] private float _arenaMinX = 100f;
        [SerializeField] private float _arenaMaxX = 135f;

        [Header("Doorstep Checkpoint (Trước cửa phòng Boss)")]
        [SerializeField] private Vector3 _doorstepCheckpointPos;
        [SerializeField] private bool _autoRegisterDoorstepCheckpoint = true;

        private bool _isTriggered = false;
        private bool _isBossDefeated = false;

        public static bool IsBossEncounterActive { get; private set; } = false;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;

            // Ban đầu chưa vào đấu trường thì rào chắn vào mở, rào chắn ra đóng
            if (_entryBarrier != null) _entryBarrier.SetActive(false);
            if (_exitBarrier != null) _exitBarrier.SetActive(true);
        }

        private void Start()
        {
            EventBus.Subscribe<BossDefeatedEvent>(OnBossDefeated);
            EventBus.Subscribe<PlayerRespawnedEvent>(OnPlayerRespawned);
        }

        private void OnDestroy()
        {
            IsBossEncounterActive = false;
            EventBus.Unsubscribe<BossDefeatedEvent>(OnBossDefeated);
            EventBus.Unsubscribe<PlayerRespawnedEvent>(OnPlayerRespawned);
        }

        private void OnPlayerRespawned(PlayerRespawnedEvent evt)
        {
            IsBossEncounterActive = false;
            if (_isBossDefeated) return;

            _isTriggered = false;
            StopAllCoroutines();

            if (_entryBarrier != null) _entryBarrier.SetActive(false);
            if (_exitBarrier != null) _exitBarrier.SetActive(true);

            if (_boss != null)
            {
                _boss.ResetBoss();
            }

            EventBus.Publish(new BossHealthBarHideEvent());
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isTriggered || !other.CompareTag("Player")) return;
            _isTriggered = true;

            // Đảm bảo lưu Checkpoint ngay trước cửa phòng Boss
            if (_autoRegisterDoorstepCheckpoint && Platformer2D.Managers.CheckpointManager.HasInstance)
            {
                Vector3 cpPos = _doorstepCheckpointPos != Vector3.zero 
                    ? _doorstepCheckpointPos 
                    : (_entryBarrier != null ? _entryBarrier.transform.position + Vector3.left * 4.0f : transform.position + Vector3.left * 8f);
                Platformer2D.Managers.CheckpointManager.Instance.SetRespawnPosition(cpPos);
            }

            StartCoroutine(LockdownArenaRoutine());
        }

        private IEnumerator LockdownArenaRoutine()
        {
            IsBossEncounterActive = true;

            // Rung chấn và đóng sập cửa vào
            CameraFollow2D.TriggerShake(0.18f, 0.12f);
            EventBus.Publish(new PlaySoundEvent("HitWood", SoundType.SFX, transform.position));

            if (_entryBarrier != null)
            {
                _entryBarrier.SetActive(true);
            }

            EventBus.Publish(new HudToastMessageEvent("CẢNH BÁO: ĐẤU TRƯỜNG BOSS BỊ PHONG TỎA!", new Color(1f, 0.25f, 0.25f, 1f)));

            yield return new WaitForSeconds(0.4f);

            // Đánh thức Boss
            if (_boss != null)
            {
                _boss.ActivateBoss();
            }
        }

        private void OnBossDefeated(BossDefeatedEvent evt)
        {
            IsBossEncounterActive = false;
            if (_isBossDefeated) return;
            _isBossDefeated = true;

            StartCoroutine(UnlockArenaRoutine(evt.Position));
        }

        private IEnumerator UnlockArenaRoutine(Vector3 bossDeathPos)
        {
            yield return new WaitForSeconds(1.8f);

            // Mở rào chắn lối ra
            if (_exitBarrier != null)
            {
                _exitBarrier.SetActive(false);
            }
            if (_entryBarrier != null)
            {
                _entryBarrier.SetActive(false);
            }

            CameraFollow2D.TriggerShake(0.15f, 0.10f);
            EventBus.Publish(new PlaySoundEvent("Coin", SoundType.SFX, bossDeathPos));
            EventBus.Publish(new HudToastMessageEvent("CHIẾN THẮNG BOSS! ĐƯỜNG ĐI ĐÃ MỞ!", new Color(0.3f, 0.95f, 0.45f, 1f)));

            // Rơi quà tặng chiến thắng (Golden Key hoặc Rương)
            if (_victoryRewardPrefab != null)
            {
                Instantiate(_victoryRewardPrefab, bossDeathPos + _rewardSpawnOffset, Quaternion.identity);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}
