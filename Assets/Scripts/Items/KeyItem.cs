using System.Linq;
using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Managers;
using Platformer2D.Player.Controllers;
using Platformer2D.VFX;

namespace Platformer2D.Items
{
    /// <summary>
    /// Chìa khóa kho báu (Bạc / Vàng):
    /// - Chạy hoạt ảnh xoay 12 frame liên tục + nhấp nhô nhẹ.
    /// - Khi Player chạm vào: cộng chìa khóa vào GameManager, bung hiệu ứng Pickup lấp lánh và hiện thông báo HUD.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class KeyItem : MonoBehaviour
    {
        private const string SilverKeySheetPath = "Assets/GameAssets/Field-Tileset/Objects_Animated/Silver_Key.png";
        private const string GoldenKeySheetPath = "Assets/GameAssets/Field-Tileset/Objects_Animated/Golden_Key.png";
        private const string PickupVfxSheetPath = "Assets/GameAssets/Field-Tileset/Objects_Animated/Pickup.png";

        [Header("Key Settings")]
        [SerializeField] private KeyType _keyType = KeyType.Golden;
        [SerializeField] private string _soundId = "KeyPickup";

        [Header("Animation")]
        [SerializeField] private Sprite[] _animationFrames;
        [SerializeField] private float _frameRate = 12f;
        [SerializeField] private bool _enableBobbing = true;
        [SerializeField] private float _bobAmplitude = 0.05f;
        [SerializeField] private float _bobFrequency = 3.2f;
        [SerializeField] private Sprite[] _pickupVfxFrames;

        private SpriteRenderer _spriteRenderer;
        private Vector3 _startLocalPos;
        private float _phaseOffset;
        private bool _isCollected;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;

            _spriteRenderer = GetComponent<SpriteRenderer>();
            _startLocalPos = transform.localPosition;
            _phaseOffset = Mathf.Abs(transform.position.x * 0.65f + transform.position.y * 0.35f);

            EnsureFrames();

            Color glowColor = _keyType == KeyType.Golden
                ? new Color(1f, 0.85f, 0.22f, 0.34f)
                : new Color(0.65f, 0.88f, 1f, 0.32f);
            AmbientGlowPulse.Attach(transform, glowColor, 1.05f, -1);
        }

        private void Update()
        {
            if (_isCollected) return;

            if (_spriteRenderer != null && _animationFrames != null && _animationFrames.Length > 0)
            {
                float rawFrame = Time.time * Mathf.Max(1f, _frameRate) + _phaseOffset;
                int index = Mathf.FloorToInt(rawFrame) % _animationFrames.Length;
                if (index < 0) index += _animationFrames.Length;

                Sprite nextSprite = _animationFrames[index];
                if (nextSprite != null && _spriteRenderer.sprite != nextSprite)
                {
                    _spriteRenderer.sprite = nextSprite;
                }
            }

            if (_enableBobbing)
            {
                float yOffset = Mathf.Sin((Time.time + _phaseOffset * 0.25f) * _bobFrequency) * _bobAmplitude;
                transform.localPosition = _startLocalPos + new Vector3(0f, yOffset, 0f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected) return;

            bool isPlayer = other.CompareTag("Player")
                || other.name.Contains("Pink_Monster")
                || other.name.Contains("Player");
            if (!isPlayer) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null && player.IsDead) return;

            Collect();
        }

        private void Collect()
        {
            _isCollected = true;

            if (GameManager.HasInstance)
            {
                GameManager.Instance.AddKey(_keyType, 1);
            }

            SpriteAnimationEffect.SpawnOneShot(transform.position, _pickupVfxFrames, 16f, 5, 1.15f);
            EventBus.Publish(new PlaySoundEvent(_soundId, SoundType.SFX, transform.position));

            string keyName = _keyType == KeyType.Golden ? "CHÌA KHÓA VÀNG" : "CHÌA KHÓA BẠC";
            Color accent = _keyType == KeyType.Golden
                ? new Color(1f, 0.85f, 0.25f, 1f)
                : new Color(0.75f, 0.88f, 1f, 1f);
            EventBus.Publish(new HudToastMessageEvent($"ĐÃ NHẶT {keyName}!", accent));

            Destroy(gameObject);
        }

        private void EnsureFrames()
        {
#if UNITY_EDITOR
            if (_animationFrames == null || _animationFrames.Length <= 1)
            {
                string path = _keyType == KeyType.Golden ? GoldenKeySheetPath : SilverKeySheetPath;
                Sprite[] loaded = LoadSortedSprites(path);
                if (loaded.Length > 0) _animationFrames = loaded;
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
