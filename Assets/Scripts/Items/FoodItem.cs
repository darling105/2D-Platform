using System.Collections;
using UnityEngine;
using Platformer2D.Combat;
using Platformer2D.Core.Architecture;
using Platformer2D.Player.Components;
using Platformer2D.Player.Controllers;

namespace Platformer2D.Items
{
    public enum FoodType
    {
        HealthMeal,     // Hồi Máu: +1 HP (Thịt nướng / Đùi gà)
        StaminaSnack,   // Hồi Thể Lực: +40 Stamina (Bánh quy / Dâu tây)
        SwiftBuffCake   // Bánh Kem Thần Tốc: Tăng 25% tốc độ chạy trong 10 giây! (Bánh kem)
    }

    /// <summary>
    /// Vật Phẩm Thức Ăn (Food & Consumables):
    /// - Rơi ra từ hòm gỗ bị đập vỡ hoặc bố trí rải rác trong màn chơi.
    /// - Tạo hoạt ảnh lơ lửng bồng bềnh thu hút người chơi.
    /// - Khi nhặt: kích hoạt hồi phục hoặc gia tăng sức mạnh, phát âm thanh và hiển thị thông báo Toast.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FoodItem : MonoBehaviour
    {
        [Header("Food Type & Values")]
        [SerializeField] private FoodType _foodType = FoodType.HealthMeal;
        [Tooltip("Số máu hồi (nếu là HealthMeal)")]
        [SerializeField] private int _healAmount = 1;
        [Tooltip("Lượng thể lực hồi (nếu là StaminaSnack)")]
        [SerializeField] private float _staminaAmount = 45f;
        [Tooltip("Hệ số tăng tốc (nếu là SwiftBuffCake)")]
        [SerializeField] private float _speedMultiplier = 1.25f;
        [Tooltip("Thời gian duy trì buff (giây)")]
        [SerializeField] private float _buffDuration = 10f;

        [Header("Toast Notification")]
        [SerializeField] private string _customToastMessage = "";

        private Collider2D _collider;
        private SpriteRenderer _spriteRenderer;
        private Vector3 _basePosition;
        private float _bobOffset;
        private bool _isCollected = false;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _basePosition = transform.position;
            _bobOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (_isCollected) return;

            // Hoạt ảnh dập dềnh nhẹ nhàng
            float hoverY = Mathf.Sin((Time.time * 3.5f) + _bobOffset) * 0.08f;
            transform.position = _basePosition + Vector3.up * hoverY;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected || !other.CompareTag("Player")) return;
            _isCollected = true;

            GameObject playerObj = other.gameObject;
            ApplyFoodEffect(playerObj);

            // Âm thanh nhặt đồ
            EventBus.Publish(new PlaySoundEvent("Coin", SoundType.SFX, transform.position));

            StartCoroutine(CollectRoutine());
        }

        private void ApplyFoodEffect(GameObject playerObj)
        {
            Color toastColor = new Color(0.28f, 0.85f, 0.45f, 1f);
            string message = _customToastMessage;

            switch (_foodType)
            {
                case FoodType.HealthMeal:
                    Health health = playerObj.GetComponent<Health>();
                    if (health != null) health.Heal(_healAmount);
                    if (string.IsNullOrEmpty(message)) message = $"+{_healAmount} MÁU (ĐÃ DÙNG BỮA)";
                    toastColor = new Color(0.35f, 0.9f, 0.45f, 1f);
                    break;

                case FoodType.StaminaSnack:
                    PlayerProgression prog = playerObj.GetComponent<PlayerProgression>();
                    if (prog != null) prog.RestoreStamina(_staminaAmount);
                    if (string.IsNullOrEmpty(message)) message = $"+{Mathf.RoundToInt(_staminaAmount)} THỂ LỰC (BÁNH NGỌT)";
                    toastColor = new Color(0.35f, 0.75f, 1f, 1f);
                    break;

                case FoodType.SwiftBuffCake:
                    PlayerController controller = playerObj.GetComponent<PlayerController>();
                    if (controller != null) controller.ApplySpeedBuff(_speedMultiplier, _buffDuration);
                    if (string.IsNullOrEmpty(message)) message = $"TĂNG TỐC ĐỘ CHẠY +25% ({Mathf.RoundToInt(_buffDuration)}s)!";
                    toastColor = new Color(1f, 0.85f, 0.25f, 1f);
                    break;
            }

            EventBus.Publish(new HudToastMessageEvent(message, toastColor));
        }

        private IEnumerator CollectRoutine()
        {
            if (_collider != null) _collider.enabled = false;

            float elapsed = 0f;
            const float duration = 0.25f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + Vector3.up * 0.45f;
            Color startColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(startPos, targetPos, t);

                if (_spriteRenderer != null)
                {
                    Color c = startColor;
                    c.a = Mathf.Lerp(startColor.a, 0f, t);
                    _spriteRenderer.color = c;
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
