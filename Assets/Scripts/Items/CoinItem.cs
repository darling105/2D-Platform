using System.Linq;
using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Managers;
using Platformer2D.VFX;

namespace Platformer2D.Items
{
    /// <summary>
    /// Đồng xu / Vật phẩm nhặt được trong game 2D Platformer:
    /// - Phát hoạt ảnh xoay mượt theo chuỗi Sprite và nhấp nhô nhẹ.
    /// - Khi Player chạm vào: cộng điểm, phát âm thanh, tạo hiệu ứng và tự biến mất.
    /// - Kết nối trực tiếp với EventBus & GameManager.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CoinItem : MonoBehaviour
    {
        [Header("Coin Settings")]
        [Tooltip("Số điểm cộng thêm khi nhặt được")]
        [SerializeField] private int _coinValue = 10;
        [Tooltip("ID âm thanh phát ra khi nhặt")]
        [SerializeField] private string _soundId = "Coin";
        [Tooltip("Hiệu ứng bụi sáng khi nhặt đồng xu")]
        [SerializeField] private GameObject _collectVfxPrefab;

        [Header("Animation")]
        [Tooltip("Chuỗi Sprite hoạt ảnh xoay của đồng xu")]
        [SerializeField] private Sprite[] _animationFrames;
        [Tooltip("Tốc độ khung hình xoay (FPS)")]
        [SerializeField] private float _frameRate = 12f;
        [Tooltip("Bật hiệu ứng nhấp nhô lên xuống nhẹ")]
        [SerializeField] private bool _enableBobbing = true;
        [SerializeField] private float _bobAmplitude = 0.05f;
        [SerializeField] private float _bobFrequency = 3.2f;

        private SpriteRenderer _spriteRenderer;
        private Vector3 _startLocalPos;
        private float _phaseOffset;
        private bool _isCollected;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            _spriteRenderer = GetComponent<SpriteRenderer>();
            _startLocalPos = transform.localPosition;
            _phaseOffset = Mathf.Abs(transform.position.x * 0.65f + transform.position.y * 0.35f);

            EnsureAnimationFrames();
            AmbientGlowPulse.Attach(transform, new Color(1f, 0.86f, 0.25f, 0.26f), 0.85f, -1);
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

        private void EnsureAnimationFrames()
        {
            if (_animationFrames != null && _animationFrames.Length > 1) return;

#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/GameAssets/Field-Tileset/Objects_Animated/Coin.png");
            if (assets != null)
            {
                Sprite[] loaded = assets
                    .OfType<Sprite>()
                    .OrderBy(s => s.rect.x)
                    .ToArray();

                if (loaded.Length > 0)
                {
                    _animationFrames = loaded;
                }
            }
#endif
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected) return;

            // Kiểm tra xem đối tượng va chạm có phải là Player không
            if (other.CompareTag("Player") || other.name.Contains("Pink_Monster") || other.name.Contains("Player"))
            {
                Collect();
            }
        }

        public void CollectDirectly()
        {
            if (!_isCollected)
            {
                Collect();
            }
        }

        private void Collect()
        {
            _isCollected = true;

            // 1. Cộng điểm qua GameManager & EventBus
            if (GameManager.HasInstance)
            {
                GameManager.Instance.AddScore(_coinValue);
            }
            else
            {
                EventBus.Publish(new ScoreChangedEvent(_coinValue, _coinValue));
            }

            // 2. Phát âm thanh
            EventBus.Publish(new PlaySoundEvent(_soundId, SoundType.SFX, transform.position));

            // 3. Hiệu ứng VFX
            if (_collectVfxPrefab != null)
            {
                Instantiate(_collectVfxPrefab, transform.position, Quaternion.identity);
            }

            // 4. Biến mất
            Destroy(gameObject);
        }
    }
}
