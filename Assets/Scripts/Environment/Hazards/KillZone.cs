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
            ProcessKill(other.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            ProcessKill(other.gameObject);
        }

        private void ProcessKill(GameObject target)
        {
            if (target == null) return;

            // Xử lý Người chơi: Tuyệt đối không để bất kỳ chiêu thức nào (Kể cả Khiên Skill 3 hay Dash) chặn chết rơi vực
            if (target.CompareTag("Player") || target.name.Contains("Player") || target.name.Contains("Pink_Monster"))
            {
                var player = target.GetComponent<Platformer2D.Player.Controllers.PlayerController>();
                if (player != null && !player.IsDead)
                {
                    DamageData lethalDamage = new DamageData(9999, gameObject, target.transform.position, Vector2.zero, DamageType.FallDamage);
                    player.TakeDamage(lethalDamage);
                    if (!player.IsDead)
                    {
                        player.ForceKill();
                    }
                    EventBus.Publish(new PlaySoundEvent("FallDeath", SoundType.SFX, target.transform.position));
                    return;
                }

                IDamageable damageable = target.GetComponent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    DamageData lethalDamage = new DamageData(9999, gameObject, target.transform.position, Vector2.zero, DamageType.FallDamage);
                    damageable.TakeDamage(lethalDamage);
                    EventBus.Publish(new PlaySoundEvent("FallDeath", SoundType.SFX, target.transform.position));
                }
            }
            else
            {
                // Tiêu diệt quái vật hoặc dọn sạch vật thể bị rơi xuống vực sâu
                IDamageable enemyDamageable = target.GetComponent<IDamageable>();
                if (enemyDamageable != null && !enemyDamageable.IsDead)
                {
                    DamageData lethalDamage = new DamageData(9999, gameObject, target.transform.position, Vector2.zero, DamageType.FallDamage);
                    enemyDamageable.TakeDamage(lethalDamage);
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
