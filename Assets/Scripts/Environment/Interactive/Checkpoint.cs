using System.Collections;
using System.Linq;
using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Player.Controllers;
using Platformer2D.VFX;

namespace Platformer2D.Environment.Interactive
{
    /// <summary>
    /// Điểm lưu vị trí (Checkpoint):
    /// - Khi chưa kích hoạt: cờ đứng yên, màu trầm dịu nhẹ.
    /// - Khi nhân vật chạm vào: cờ sáng rực đúng màu gốc, nổ hiệu ứng lấp lánh và tung bay 8 frame.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        private const string FlagSpriteSheetPath = "Assets/GameAssets/Field-Tileset/Objects_Animated/Flag.png";
        private const string PickupVfxSheetPath = "Assets/GameAssets/Field-Tileset/Objects_Animated/Pickup.png";

        [Header("Visual Feedback")]
        [Tooltip("Màu nhẹ khi chưa kích hoạt (không làm xỉn màu gốc khi đã kích hoạt)")]
        [SerializeField] private Color _inactiveColor = new Color(0.65f, 0.65f, 0.72f, 0.92f);
        [Tooltip("Chuỗi Sprite 8 frame cờ tung bay khi đã kích hoạt")]
        [SerializeField] private Sprite[] _waveFrames;
        [Tooltip("Tốc độ hoạt ảnh cờ bay (FPS)")]
        [SerializeField] private float _waveFrameRate = 12f;
        [Tooltip("Hiệu ứng bung sáng khi chạm Checkpoint")]
        [SerializeField] private Sprite[] _activateVfxFrames;
        [Tooltip("Offset vị trí hồi sinh so với Checkpoint (ví dụ: nhích lên 0.5 unit để không dính đất)")]
        [SerializeField] private Vector2 _respawnOffset = new Vector2(0f, 0.5f);

        private SpriteRenderer _spriteRenderer;
        private Vector3 _baseScale = Vector3.one;
        private AmbientGlowPulse _activeGlow;
        private bool _isActivated = false;

        public bool IsActivated => _isActivated;
        public Vector3 RespawnPosition => transform.position + (Vector3)_respawnOffset;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;

            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;

            EnsureFrames();

            if (_spriteRenderer != null)
            {
                if (_waveFrames != null && _waveFrames.Length > 0 && _waveFrames[0] != null)
                {
                    _spriteRenderer.sprite = _waveFrames[0];
                }
                _spriteRenderer.color = _inactiveColor;
            }

            _activeGlow = AmbientGlowPulse.Attach(transform, new Color(1f, 0.55f, 0.25f, 0.30f), 1.35f, -1);
            if (_activeGlow != null)
            {
                _activeGlow.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!_isActivated || _spriteRenderer == null || _waveFrames == null || _waveFrames.Length == 0) return;

            int index = Mathf.FloorToInt(Time.time * Mathf.Max(1f, _waveFrameRate)) % _waveFrames.Length;
            Sprite nextFrame = _waveFrames[index];
            if (nextFrame != null && _spriteRenderer.sprite != nextFrame)
            {
                _spriteRenderer.sprite = nextFrame;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isActivated) return;

            bool isPlayer = other.CompareTag("Player")
                || other.name.Contains("Pink_Monster")
                || other.name.Contains("Player");
            if (!isPlayer) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null && player.IsDead) return;

            ActivateCheckpoint();
        }

        public void ActivateCheckpoint()
        {
            if (_isActivated) return;
            _isActivated = true;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.white;
            }
            if (_activeGlow != null)
            {
                _activeGlow.gameObject.SetActive(true);
            }

            SpriteAnimationEffect.SpawnOneShot(transform.position + new Vector3(0.05f, 0.15f, 0f), _activateVfxFrames, 16f, 5, 1.15f);
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(PopScaleRoutine());
            }

            EventBus.Publish(new CheckpointActivatedEvent(RespawnPosition));
            EventBus.Publish(new PlaySoundEvent("Checkpoint", SoundType.SFX, transform.position));
        }

        private IEnumerator PopScaleRoutine()
        {
            const float duration = 0.18f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float bump = 1f + Mathf.Sin(t * Mathf.PI) * 0.18f;
                transform.localScale = _baseScale * bump;
                yield return null;
            }

            transform.localScale = _baseScale;
        }

        private void EnsureFrames()
        {
#if UNITY_EDITOR
            if (_waveFrames == null || _waveFrames.Length <= 1)
            {
                Sprite[] loaded = LoadSortedSprites(FlagSpriteSheetPath);
                if (loaded.Length > 0) _waveFrames = loaded;
            }

            if (_activateVfxFrames == null || _activateVfxFrames.Length == 0)
            {
                Sprite[] loadedVfx = LoadSortedSprites(PickupVfxSheetPath);
                if (loadedVfx.Length > 0) _activateVfxFrames = loadedVfx;
            }
#endif
        }

#if UNITY_EDITOR
        private static Sprite[] LoadSortedSprites(string assetPath)
        {
            Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null) return System.Array.Empty<Sprite>();

            return assets.OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
        }
#endif

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isActivated ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(RespawnPosition, 0.3f);
        }
    }
}
