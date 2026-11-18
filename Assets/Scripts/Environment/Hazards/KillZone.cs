using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;

namespace Platformer2D.Environment.Hazards
{
    /// <summary>
    /// Vực sâu tử thần (Bottomless Pit / Fall Kill Zone):
    /// - Đặt ở đáy màn chơi dưới dạng một Trigger Collider lớn.
    /// - Khi người chơi rơi xuống vực, lập tức gây chết và kích hoạt luồng hồi sinh tại Checkpoint.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                IDamageable damageable = other.GetComponent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    DamageData lethalDamage = new DamageData(9999, gameObject, Vector2.zero, Vector2.zero, DamageType.FallDamage);
                    damageable.TakeDamage(lethalDamage);
                    EventBus.Publish(new PlaySoundEvent("FallDeath", SoundType.SFX, other.transform.position));
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}
