using System.Collections;
using System.Linq;
using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Managers;
using Platformer2D.Player.Controllers;
using Platformer2D.VFX;

namespace Platformer2D.Environment.Interactive
{
    /// <summary>
    /// Điểm chiến thắng (Rương kho báu kết màn):
    /// - Yêu cầu nhặt đủ Chìa Khóa Vàng (tùy chọn) mới mở được rương.
    /// - Khi mở: phát hiệu ứng lấp lánh, hoạt ảnh bật mở nắp rương 8 frame, cộng điểm thưởng và mở màn hình Victory.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class VictoryGoal : MonoBehaviour
    {
        private const string ChestSpriteSheetPath = "Assets/GameAssets/Field-Tileset/Objects_Animated/Golden_Chest_Open.png";
        private const string PickupVfxSheetPath = "Assets/GameAssets/Field-Tileset/Objects_Animated/Pickup.png";

        [Header("Victory Settings")]
        [Tooltip("Có bắt buộc phải có Chìa Khóa Vàng mới mở được rương kết màn hay không")]
        [SerializeField] private bool _requireGoldenKey = true;
        [Tooltip("Điểm thưởng cộng thêm khi về đích")]
        [SerializeField] private int _bonusScore = 50;
        [Tooltip("ID âm thanh phát ra khi chạm điểm thắng")]
        [SerializeField] private string _victorySoundId = "Victory";
        [Tooltip("Chuỗi Sprite hoạt ảnh mở nắp rương")]
        [SerializeField] private Sprite[] _openFrames;
        [Tooltip("Hiệu ứng bung sáng khi mở khóa rương")]
        [SerializeField] private Sprite[] _pickupVfxFrames;
        [Tooltip("Tốc độ phát hoạt ảnh mở rương (FPS)")]
        [SerializeField] private float _openFrameRate = 14f;

        [Header("Next Level Transition")]
        [Tooltip("Tên scene tiếp theo sẽ tự động chuyển đến sau khi chiến thắng (để trống nếu là màn cuối)")]
        [SerializeField] private string _nextSceneName = "";

        private SpriteRenderer _spriteRenderer;
        private float _groundBottomY;
        private float _lastLockedToastTime = -10f;
        private bool _isTriggered;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;

            _spriteRenderer = GetComponent<SpriteRenderer>();
            EnsureFrames();

            if (_spriteRenderer != null && _openFrames != null && _openFrames.Length > 0 && _openFrames[0] != null)
            {
                _spriteRenderer.sprite = _openFrames[0];
            }

            float initialMinY = _spriteRenderer != null && _spriteRenderer.sprite != null
                ? _spriteRenderer.sprite.bounds.min.y
                : -0.28125f;
            _groundBottomY = transform.position.y + initialMinY * transform.lossyScale.y;

            AmbientGlowPulse.Attach(transform, new Color(1f, 0.84f, 0.22f, 0.34f), 1.65f, -1, new Vector3(0f, 0.08f, 0f));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isTriggered) return;

            bool isPlayer = other.CompareTag("Player")
                || other.name.Contains("Pink_Monster")
                || other.name.Contains("Player");

            if (!isPlayer) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null && player.IsDead) return;

            if (_requireGoldenKey && (!GameManager.HasInstance || !GameManager.Instance.TryUseKey(KeyType.Golden)))
            {
                if (Time.time - _lastLockedToastTime >= 1.2f)
                {
                    _lastLockedToastTime = Time.time;
                    EventBus.Publish(new PlaySoundEvent("Locked", SoundType.SFX, transform.position));
                    EventBus.Publish(new HudToastMessageEvent("CẦN CHÌA KHÓA VÀNG ĐỂ MỞ RƯƠNG CHIẾN THẮNG!", new Color(1f, 0.45f, 0.35f, 1f)));
                }
                return;
            }

            _isTriggered = true;
            StartCoroutine(TriggerVictoryRoutine());
        }

        private IEnumerator TriggerVictoryRoutine()
        {
            if (_bonusScore > 0)
            {
                if (GameManager.HasInstance)
                {
                    GameManager.Instance.AddScore(_bonusScore);
                }
                else
                {
                    EventBus.Publish(new ScoreChangedEvent(_bonusScore, _bonusScore));
                }
            }

            SpriteAnimationEffect.SpawnOneShot(transform.position + new Vector3(0f, 0.25f, 0f), _pickupVfxFrames, 16f, 5, 1.3f);
            EventBus.Publish(new PlaySoundEvent(_victorySoundId, SoundType.SFX, transform.position));
            EventBus.Publish(new HudToastMessageEvent($"VICTORY! +{_bonusScore} COINS", new Color(1f, 0.85f, 0.25f, 1f)));

            if (_spriteRenderer != null && _openFrames != null && _openFrames.Length > 0)
            {
                float frameDelay = 1f / Mathf.Max(1f, _openFrameRate);
                for (int i = 0; i < _openFrames.Length; i++)
                {
                    Sprite frame = _openFrames[i];
                    if (frame != null)
                    {
                        _spriteRenderer.sprite = frame;
                        PinSpriteBottomToGround(transform, frame, _groundBottomY);
                    }
                    yield return new WaitForSecondsRealtime(frameDelay);
                }
            }

            yield return new WaitForSecondsRealtime(0.25f);

            if (!string.IsNullOrEmpty(_nextSceneName))
            {
                EventBus.Publish(new HudToastMessageEvent($"CHIẾN THẮNG! ĐANG CHUYỂN TIẾP SANG {_nextSceneName.ToUpper()}...", new Color(0.4f, 1f, 0.5f, 1f)));
                yield return new WaitForSecondsRealtime(2.2f);
                if (SceneLoader.HasInstance)
                {
                    SceneLoader.Instance.LoadSceneAsync(_nextSceneName);
                }
                else
                {
                    Time.timeScale = 1f;
                    UnityEngine.SceneManagement.SceneManager.LoadScene(_nextSceneName);
                }
                yield break;
            }

            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetState(GameState.Victory);
            }
            else
            {
                EventBus.Publish(new GameStateChangedEvent(GameState.Playing, GameState.Victory));
            }
        }

        public static void PinSpriteBottomToGround(Transform targetTransform, Sprite frame, float groundBottomY)
        {
            if (targetTransform == null || frame == null) return;

            float scaleY = targetTransform.lossyScale.y;
            Vector3 pos = targetTransform.position;
            pos.y = groundBottomY - frame.bounds.min.y * scaleY;
            targetTransform.position = pos;
        }

        private void EnsureFrames()
        {
#if UNITY_EDITOR
            if (_openFrames == null || _openFrames.Length <= 1)
            {
                Sprite[] loaded = LoadSortedSprites(ChestSpriteSheetPath);
                if (loaded.Length > 0) _openFrames = loaded;
            }

            if (_pickupVfxFrames == null || _pickupVfxFrames.Length == 0)
            {
                Sprite[] loadedVfx = LoadSortedSprites(PickupVfxSheetPath);
                if (loadedVfx.Length > 0) _pickupVfxFrames = loadedVfx;
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
    }
}
