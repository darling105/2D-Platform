using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;

namespace Platformer2D.Environment.Hazards
{
    /// <summary>
    /// Bẫy chông / Bẫy gai (Spike Hazard):
    /// - Gây sát thương khi nhân vật chạm phải.
    /// - Hỗ trợ tùy chọn gây chết ngay (Instant Kill) hoặc trừ máu kèm lực hất ngược (Knockback).
    /// </summary>
    public class SpikeHazard : MonoBehaviour
    {
        [Tooltip("Lượng sát thương gây ra")]
        [SerializeField] private int _damage = 1;
        [Tooltip("Chết ngay lập tức khi chạm phải (bỏ qua lượng máu còn lại)")]
        [SerializeField] private bool _instantKill = false;
        [Tooltip("Lực hất văng khi dẫm phải gai")]
        [SerializeField] private Vector2 _knockbackForce = new Vector2(0f, 10f);
        [Tooltip("Chỉ gây sát thương cho Player (tránh quái tự dẫm gai chết)")]
        [SerializeField] private bool _damagePlayerOnly = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            ApplyHazard(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            ApplyHazard(collision.gameObject);
        }

        private void ApplyHazard(GameObject target)
        {
            if (target == null) return;
            if (_damagePlayerOnly && !target.CompareTag("Player")) return;

            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                int finalDamage = _instantKill ? 9999 : _damage;

                // Tính hướng hất ngược ra xa khỏi gai
                Vector2 direction = (target.transform.position - transform.position).normalized;
                Vector2 knockback = new Vector2(Mathf.Sign(direction.x) * Mathf.Abs(_knockbackForce.x), Mathf.Abs(_knockbackForce.y));

                DamageData damageData = new DamageData(finalDamage, gameObject, (Vector2)target.transform.position, knockback, DamageType.Trap);
                damageable.TakeDamage(damageData);

                EventBus.Publish(new PlaySoundEvent("SpikeHit", SoundType.SFX, transform.position));
            }
        }
    }
}
