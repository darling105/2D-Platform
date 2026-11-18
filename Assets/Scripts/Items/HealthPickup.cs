using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;

namespace Platformer2D.Items
{
    /// <summary>
    /// Vật phẩm bình máu / Trái tim hồi máu (Health Pickup):
    /// - Hồi máu cho đối tượng thực thi IHealable (Player).
    /// - Chỉ nhặt được khi người chơi bị thương (máu chưa đầy).
    /// - Hiệu ứng bồng bềnh nhẹ (Bobbing) tạo cảm giác vật phẩm trong game 2D.
    /// - Tự động phát âm thanh và hiệu ứng hồi phục khi nhặt.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HealthPickup : MonoBehaviour
    {
        [Header("Heal Settings")]
        [Tooltip("Lượng máu hồi phục")]
        [SerializeField] private int _healAmount = 1;
        [Tooltip("Âm thanh khi nhặt máu")]
        [SerializeField] private string _pickupSound = "HealthPickup";
        [Tooltip("Hiệu ứng VFX khi nhặt (nếu có)")]
        [SerializeField] private GameObject _pickupVfxPrefab;

        [Header("Visual Floating (Bobbing)")]
        [Tooltip("Tự động bồng bềnh lên xuống nhẹ nhàng")]
        [SerializeField] private bool _enableBobbing = true;
        [Tooltip("Tốc độ bồng bềnh")]
        [SerializeField] private float _bobbingSpeed = 3f;
        [Tooltip("Biên độ bồng bềnh (khoảng cách lên xuống)")]
        [SerializeField] private float _bobbingAmplitude = 0.12f;

        private Vector3 _basePosition;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void Start()
        {
            _basePosition = transform.position;
        }

        private void Update()
        {
            if (_enableBobbing)
            {
                float newY = _basePosition.y + Mathf.Sin(Time.time * _bobbingSpeed) * _bobbingAmplitude;
                transform.position = new Vector3(_basePosition.x, newY, _basePosition.z);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            IHealable healable = other.GetComponent<IHealable>();
            // Chỉ cho phép nhặt nếu thực thể có thể hồi máu và máu hiện tại chưa đạt tối đa
            if (healable != null && !healable.IsAtMaxHealth)
            {
                healable.Heal(_healAmount);

                if (_pickupVfxPrefab != null)
                {
                    Instantiate(_pickupVfxPrefab, transform.position, Quaternion.identity);
                }

                EventBus.Publish(new PlaySoundEvent(_pickupSound, SoundType.SFX, transform.position));
                Destroy(gameObject);
            }
        }

        private void OnDrawGizmos()
        {
            // Vẽ biểu tượng hình trái tim đỏ trong Scene để dễ nhận diện vị trí
            Gizmos.color = new Color(1f, 0.2f, 0.4f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.35f);
        }
    }
}
