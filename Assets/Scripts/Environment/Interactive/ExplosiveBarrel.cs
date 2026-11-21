using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer2D.CameraControl;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;

namespace Platformer2D.Environment.Interactive
{
    /// <summary>
    /// Thùng Thuốc Nổ (Explosive Barrel):
    /// - Khi bị tấn công bởi đòn cận chiến, phi tiêu đá, cầu lửa hoặc vụ nổ lân cận:
    ///   nhấp nháy đỏ cảnh báo trong tích tắc rồi phát nổ dữ dội.
    /// - Vụ nổ gây sát thương diện rộng (AoE), hất tung vật thể, kích nổ dây chuyền
    ///   các thùng bên cạnh và làm rung nhẹ camera tạo cảm giác uy lực.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class ExplosiveBarrel : MonoBehaviour, IDamageable
    {
        private const string ExplosionSheetPath = "Assets/GameAssets/Player-Skills/5 Explosion/Explosion.png";
        private static readonly Dictionary<Texture2D, Sprite[]> ExplosionFrameCache = new Dictionary<Texture2D, Sprite[]>();

        [Header("Explosion Settings")]
        [Tooltip("Bán kính sát thương nổ (units)")]
        [SerializeField] private float _explosionRadius = 2.4f;
        [Tooltip("Lượng sát thương nổ gây ra cho quái vật và vật thể")]
        [SerializeField] private int _damageAmount = 12;
        [Tooltip("Lực đẩy hất tung vật thể trong bán kính nổ")]
        [SerializeField] private float _knockbackForce = 14f;
        [Tooltip("Độ trễ trước khi nổ để tạo cảm giác ngòi nổ kích hoạt")]
        [SerializeField] private float _fuseDelay = 0.18f;

        [Header("Visuals")]
        [SerializeField] private Texture2D _explosionTexture;

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private Color _originalColor;
        private bool _isTriggered = false;
        private bool _isExploded = false;

        public bool IsDead => _isExploded;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }

            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.mass = 4f;
            _rb.linearDamping = 3.5f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

#if UNITY_EDITOR
            if (_explosionTexture == null)
            {
                _explosionTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(ExplosionSheetPath);
            }
#endif
        }

        public void TakeDamage(DamageData damageData)
        {
            if (_isTriggered || _isExploded) return;
            _isTriggered = true;

            StartCoroutine(FuseAndExplodeRoutine());
        }

        private IEnumerator FuseAndExplodeRoutine()
        {
            // Hiệu ứng nhấp nháy đỏ và phình to nhẹ như ngòi nổ đang cháy
            Vector3 originalScale = transform.localScale;
            float elapsed = 0f;
            while (elapsed < _fuseDelay)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _fuseDelay;
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.color = Color.Lerp(_originalColor, new Color(1f, 0.2f, 0.2f, 1f), Mathf.PingPong(elapsed * 18f, 1f));
                }
                transform.localScale = originalScale * (1f + 0.12f * t);
                yield return null;
            }

            Explode();
        }

        private void Explode()
        {
            if (_isExploded) return;
            _isExploded = true;

            Vector3 center = transform.position;

            // 1. Âm thanh và rung chấn Camera
            EventBus.Publish(new PlaySoundEvent("HitWood", SoundType.SFX, center));
            CameraFollow2D.TriggerShake(0.022f, 0.28f);

            // 2. Sinh hiệu ứng nổ hoạt họa sống động từ sprite sheet Explosion.png
            SpawnExplosionVfx(center);

            // 3. Quét vùng sát thương diện rộng (AoE)
            Collider2D[] colliders = Physics2D.OverlapCircleAll(center, _explosionRadius);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D col = colliders[i];
                if (col == null || col.gameObject == gameObject) continue;

                // Gây sát thương cho quái vật / người chơi / thùng khác
                IDamageable damageable = col.GetComponent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    Vector2 hitDir = ((Vector2)col.transform.position - (Vector2)center).normalized;
                    if (hitDir.sqrMagnitude < 0.01f) hitDir = Vector2.up;

                    bool isTargetPlayer = col.CompareTag("Player");
                    int finalDamage = isTargetPlayer ? 1 : _damageAmount;

                    damageable.TakeDamage(new DamageData(finalDamage, gameObject, (Vector2)col.transform.position, hitDir * _knockbackForce, DamageType.Physical));
                }

                // Tác động lực đẩy nổ cho các Rigidbody lân cận
                Rigidbody2D otherRb = col.GetComponent<Rigidbody2D>();
                if (otherRb != null && otherRb.bodyType == RigidbodyType2D.Dynamic)
                {
                    Vector2 forceDir = ((Vector2)col.transform.position - (Vector2)center).normalized;
                    if (forceDir.sqrMagnitude < 0.01f) forceDir = Vector2.up;
                    otherRb.AddForce(forceDir * (_knockbackForce * 1.5f), ForceMode2D.Impulse);
                }
            }

            // Hủy đối tượng thùng
            Destroy(gameObject);
        }

        private void SpawnExplosionVfx(Vector3 position)
        {
            Sprite[] frames = GetExplosionFrames();
            if (frames == null || frames.Length == 0) return;

            GameObject vfxObj = new GameObject("Barrel_Explosion_VFX");
            vfxObj.transform.position = position;
            vfxObj.transform.localScale = Vector3.one * 1.6f;

            SpriteRenderer sr = vfxObj.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = _spriteRenderer != null ? _spriteRenderer.sortingLayerName : "Default";
            sr.sortingOrder = 15;

            var runner = vfxObj.AddComponent<ExplosionVfxRunner>();
            runner.Play(frames, 0.035f);
        }

        private Sprite[] GetExplosionFrames()
        {
            Texture2D tex = _explosionTexture;
#if UNITY_EDITOR
            if (tex == null)
            {
                tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(ExplosionSheetPath);
                _explosionTexture = tex;
            }
#endif
            if (tex == null) return null;

            if (ExplosionFrameCache.TryGetValue(tex, out Sprite[] cached) && cached != null && cached.Length > 0)
            {
                return cached;
            }

            int cellSize = tex.height;
            int cols = cellSize > 0 ? (tex.width / cellSize) : 0;
            if (cellSize <= 0 || cols <= 0) return null;

            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Sprite[] frames = new Sprite[cols];
            for (int c = 0; c < cols; c++)
            {
                Rect rect = new Rect(c * cellSize, 0f, cellSize, cellSize);
                frames[c] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 32f);
            }

            ExplosionFrameCache[tex] = frames;
            return frames;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _explosionRadius);
        }
    }

    /// <summary>
    /// Runner phụ tự động chạy hết frame của vụ nổ và tự hủy GameObject
    /// </summary>
    public class ExplosionVfxRunner : MonoBehaviour
    {
        public void Play(Sprite[] frames, float frameDuration)
        {
            StartCoroutine(RunRoutine(frames, frameDuration));
        }

        private IEnumerator RunRoutine(Sprite[] frames, float frameDuration)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            for (int i = 0; i < frames.Length; i++)
            {
                if (sr != null) sr.sprite = frames[i];
                yield return new WaitForSeconds(frameDuration);
            }
            Destroy(gameObject);
        }
    }
}
