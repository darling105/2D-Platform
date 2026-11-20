using System.Linq;
using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Player.Components;

namespace Platformer2D.Items
{
    /// <summary>
    /// Vật phẩm Đá nhặt được trên bản đồ hoặc rơi ra khi đập vỡ thùng gỗ:
    /// - Cộng số lượng đá vào túi đồ chiến đấu (PlayerCombat) để người chơi ném tầm xa.
    /// - Hiển thị thông báo nhanh lên HUD khi nhặt hoặc khi túi đá đã đầy.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class RockPickup : MonoBehaviour
    {
        [Header("Rock Pickup Settings")]
        [Tooltip("Số viên đá nhận được khi nhặt")]
        [SerializeField] private int _rockAmount = 3;
        [Tooltip("Âm thanh phát khi nhặt đá")]
        [SerializeField] private string _soundId = "Coin";
        [Tooltip("Biên độ nhấp nhô lên xuống nhẹ cho dễ nhận biết")]
        [SerializeField] private float _bobAmplitude = 0.06f;
        [Tooltip("Tần số nhấp nhô")]
        [SerializeField] private float _bobFrequency = 3.5f;

        private Vector3 _startPos;
        private bool _isCollected;
        private float _fullNoticeTimer;

        public int RockAmount => _rockAmount;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
            _startPos = transform.position;
        }

        public void ResetStartPosition(Vector3 newPos)
        {
            _startPos = newPos;
            transform.position = newPos;
        }

        private void Update()
        {
            if (_fullNoticeTimer > 0f)
            {
                _fullNoticeTimer -= Time.deltaTime;
            }

            if (_bobAmplitude > 0f)
            {
                float offsetY = Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude;
                transform.position = new Vector3(_startPos.x, _startPos.y + offsetY, _startPos.z);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected) return;

            PlayerCombat combat = other.GetComponent<PlayerCombat>() ?? other.GetComponentInParent<PlayerCombat>();
            if (combat == null) return;

            if (combat.AddRocks(_rockAmount))
            {
                _isCollected = true;
                EventBus.Publish(new PlaySoundEvent(_soundId, SoundType.SFX, transform.position));
                EventBus.Publish(new HudToastMessageEvent($"+{_rockAmount} ROCKS", new Color(0.45f, 0.88f, 1f, 1f)));
                Destroy(gameObject);
            }
            else if (_fullNoticeTimer <= 0f)
            {
                _fullNoticeTimer = 1.5f;
                EventBus.Publish(new HudToastMessageEvent("ROCKS FULL!", new Color(1f, 0.78f, 0.25f, 1f)));
            }
        }

        /// <summary>
        /// Thu nhận trực tiếp vật phẩm (dùng khi vật phẩm bay từ rương ném thẳng vào người chơi).
        /// </summary>
        public bool CollectDirectly(PlayerCombat combat)
        {
            if (_isCollected || combat == null) return false;

            if (combat.AddRocks(_rockAmount))
            {
                _isCollected = true;
                EventBus.Publish(new PlaySoundEvent(_soundId, SoundType.SFX, transform.position));
                EventBus.Publish(new HudToastMessageEvent($"+{_rockAmount} ROCKS", new Color(0.45f, 0.88f, 1f, 1f)));
                Destroy(gameObject);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tạo nhanh vật phẩm nhặt Đá lúc runtime khi đập vỡ thùng chưa gắn sẵn Prefab rơi đồ.
        /// </summary>
        public static GameObject SpawnRuntimePickup(Vector3 position, int rockAmount = 3)
        {
            GameObject obj = new GameObject("RockPickup_Drop");
            obj.transform.position = position;

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;

            Sprite rockSprite = Resources.Load<Sprite>("Rock2") ?? Resources.Load<Sprite>("Rock1");
#if UNITY_EDITOR
            if (rockSprite == null)
            {
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/GameAssets/Player/1 Pink_Monster/Rock2.png");
                rockSprite = assets != null ? System.Linq.Enumerable.OfType<Sprite>(assets).FirstOrDefault() : null;
            }
#endif
            if (rockSprite != null)
            {
                sr.sprite = rockSprite;
            }

            CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
            col.radius = 0.24f;
            col.isTrigger = true;

            RockPickup pickup = obj.AddComponent<RockPickup>();
            pickup._rockAmount = Mathf.Max(1, rockAmount);
            return obj;
        }
    }
}
