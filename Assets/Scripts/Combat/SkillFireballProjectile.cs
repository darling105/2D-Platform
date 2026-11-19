using System.Collections.Generic;
using UnityEngine;
using Platformer2D.CameraControl;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;
using Platformer2D.VFX;

namespace Platformer2D.Combat
{
    /// <summary>
    /// Đạn Cầu Lửa phép thuật (Fireball Skill):
    /// - Phát animation chuỗi frame lửa liên tục trong lúc bay ngang.
    /// - Khi va chạm quái vật, thùng gỗ hoặc tường cứng sẽ phát nổ gây sát thương vùng (AoE).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
    public class SkillFireballProjectile : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private SpriteRenderer _sr;

        private Sprite[] _loopFrames;
        private Sprite[] _explosionFrames;
        private float _speed = 13.5f;
        private int _damage = 2;
        private float _aoeRadius = 1.45f;
        private float _directionX = 1f;
        private GameObject _owner;
        private float _animTimer;
        private int _frameIndex;
        private bool _hasDetonated;

        private const float AnimationFps = 16f;
        private const float LifeTime = 2.5f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _sr = GetComponent<SpriteRenderer>();

            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void Start()
        {
            Destroy(gameObject, LifeTime);
        }

        public void Initialize(
            float directionX,
            int damage,
            GameObject owner,
            Sprite[] loopFrames,
            Sprite[] explosionFrames,
            int sortingLayerId,
            int sortingOrder)
        {
            _directionX = directionX >= 0f ? 1f : -1f;
            _damage = Mathf.Max(1, damage);
            _owner = owner;
            _loopFrames = loopFrames;
            _explosionFrames = explosionFrames;

            if (_sr != null)
            {
                _sr.sortingLayerID = sortingLayerId;
                _sr.sortingOrder = sortingOrder;
                if (_loopFrames != null && _loopFrames.Length > 0)
                {
                    _sr.sprite = _loopFrames[0];
                }
            }

            transform.localScale = new Vector3(_directionX * 1.85f, 1.85f, 1f);
            _rb.linearVelocity = new Vector2(_directionX * _speed, 0f);
        }

        private void Update()
        {
            if (_hasDetonated || _loopFrames == null || _loopFrames.Length == 0 || _sr == null) return;

            _animTimer += Time.deltaTime * AnimationFps;
            int nextIndex = Mathf.FloorToInt(_animTimer) % _loopFrames.Length;
            if (nextIndex != _frameIndex)
            {
                _frameIndex = nextIndex;
                _sr.sprite = _loopFrames[_frameIndex];
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasDetonated || other == null) return;
            if (_owner != null && (other.gameObject == _owner || other.transform.IsChildOf(_owner.transform))) return;
            if (other.CompareTag("Player")) return;

            IDamageable directTarget = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (directTarget != null && !directTarget.IsDead)
            {
                Detonate(directTarget);
                return;
            }

            if (!other.isTrigger)
            {
                Detonate(null);
            }
        }

        private void Detonate(IDamageable directTarget)
        {
            if (_hasDetonated) return;
            _hasDetonated = true;

            HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
            Vector2 impactPos = transform.position;
            Vector2 knockback = new Vector2(_directionX * 6f, 4f);

            if (directTarget != null && !directTarget.IsDead)
            {
                hitTargets.Add(directTarget);
                directTarget.TakeDamage(new DamageData(_damage, _owner, impactPos, knockback, DamageType.Magical));
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(impactPos, _aoeRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i];
                if (col == null) continue;
                if (_owner != null && (col.gameObject == _owner || col.transform.IsChildOf(_owner.transform))) continue;
                if (col.CompareTag("Player")) continue;

                IDamageable dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                if (dmg != null && !dmg.IsDead && hitTargets.Add(dmg))
                {
                    dmg.TakeDamage(new DamageData(_damage, _owner, impactPos, knockback, DamageType.Magical));
                }
            }

            SpawnExplosionVfx(impactPos);
            CameraFollow2D.Instance?.Shake(0.14f, 0.16f);
            EventBus.Publish(new PlaySoundEvent("Explosion", SoundType.SFX, impactPos));
            Destroy(gameObject);
        }

        private void SpawnExplosionVfx(Vector2 pos)
        {
            if (_explosionFrames == null || _explosionFrames.Length == 0) return;

            GameObject vfxObj = new GameObject("VFX_FireballExplosion");
            vfxObj.transform.position = pos;
            vfxObj.transform.localScale = new Vector3(2.1f, 2.1f, 1f);

            SpriteRenderer vfxSr = vfxObj.AddComponent<SpriteRenderer>();
            if (_sr != null)
            {
                vfxSr.sortingLayerID = _sr.sortingLayerID;
                vfxSr.sortingOrder = _sr.sortingOrder + 1;
            }

            SpriteAnimationEffect effect = vfxObj.AddComponent<SpriteAnimationEffect>();
            effect.Initialize(_explosionFrames, 24f);
        }
    }
}
