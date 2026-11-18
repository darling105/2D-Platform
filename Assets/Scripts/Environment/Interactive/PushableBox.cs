using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;

namespace Platformer2D.Environment.Interactive
{
    /// <summary>
    /// Khối đá / Thùng gỗ có thể đẩy được (Pushable Box / Crate):
    /// - Cho phép nhân vật áp sát đẩy di chuyển để giải đố, kê chân leo cao hoặc đè công tắc.
    /// - Tự động thiết lập cấu hình vật lý chống lật và có độ ma sát đầm chắc.
    /// - Hỗ trợ tùy chọn đập vỡ khi bị đấm nếu có gắn máu.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PushableBox : MonoBehaviour, IDamageable
    {
        [Header("Push Settings")]
        [Tooltip("Khối lượng thùng (kg). Nên từ 3-8 để tạo cảm giác nặng đầm")]
        [SerializeField] private float _mass = 5f;
        [Tooltip("Lực cản trượt (Linear Drag) giúp thùng dừng lại khi thôi đẩy")]
        [SerializeField] private float _linearDrag = 4f;

        [Header("Breakable Option (Tùy chọn)")]
        [Tooltip("Thùng có thể bị đấm vỡ hay không")]
        [SerializeField] private bool _isBreakable = false;
        [Tooltip("Số đòn đấm để vỡ thùng")]
        [SerializeField] private int _hitsToBreak = 3;
        [Tooltip("Vật phẩm rơi ra khi vỡ (Coin, Heart - tùy chọn)")]
        [SerializeField] private GameObject _dropPrefab;

        private Rigidbody2D _rb;
        private int _currentHits = 0;
        private bool _isBroken = false;

        public bool IsDead => _isBroken;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.mass = _mass;
            _rb.linearDamping = _linearDrag;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Gán PhysicsMaterial2D không ma sát ở các cạnh để Player không bị bám dính khi nhảy cọ xát
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && col.sharedMaterial == null)
            {
#if UNITY_EDITOR
                col.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/Frictionless2D.physicsMaterial2D");
#endif
                if (col.sharedMaterial == null)
                {
                    PhysicsMaterial2D mat = new PhysicsMaterial2D("PushableBoxFrictionless") { friction = 0f, bounciness = 0f };
                    mat.hideFlags = HideFlags.DontSave;
                    col.sharedMaterial = mat;
                }
            }
        }

        private void FixedUpdate()
        {
            // Thùng chỉ rơi xuống theo trọng lực, không bao giờ tự nảy vọt lên trên truyền xung lực lỗi cho người chơi
            if (_rb != null && _rb.linearVelocityY > 0.05f)
            {
                _rb.linearVelocityY = 0f;
            }
        }

        public void TakeDamage(DamageData damageData)
        {
            if (!_isBreakable || _isBroken) return;

            _currentHits += damageData.Amount;
            EventBus.Publish(new PlaySoundEvent("HitWood", SoundType.SFX, transform.position));

            if (_currentHits >= _hitsToBreak)
            {
                BreakBox();
            }
        }

        private void BreakBox()
        {
            _isBroken = true;

            if (_dropPrefab != null)
            {
                Instantiate(_dropPrefab, transform.position, Quaternion.identity);
            }

            EventBus.Publish(new PlaySoundEvent("BoxBreak", SoundType.SFX, transform.position));
            Destroy(gameObject);
        }
    }
}
