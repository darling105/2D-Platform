using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Managers;

namespace Platformer2D.Items
{
    /// <summary>
    /// Đồng xu / Vật phẩm nhặt được trong game 2D Platformer:
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

        private bool _isCollected;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
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
