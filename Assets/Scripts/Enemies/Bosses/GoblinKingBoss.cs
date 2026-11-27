using System.Collections;
using UnityEngine;
using Platformer2D.CameraControl;
using Platformer2D.Combat;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;

namespace Platformer2D.Enemies.Bosses
{
    /// <summary>
    /// Boss Màn 2: Vua Yêu Tinh (Goblin King):
    /// - Chúa tể hầm mỏ hang động với cây búa tạ khổng lồ.
    /// - Phase 1: Nện búa tạ rung chuyển mặt đất và ném tảng đá khổng lồ.
    /// - Phase 2 (< 50% HP): Cuồng nộ hú gọi đệ tử yêu tinh hang động trợ chiến và xoay búa tạ liên hoàn.
    /// </summary>
    public class GoblinKingBoss : BaseBoss
    {
        [Header("Goblin King Minion Summon")]
        [SerializeField] private GameObject _minionPrefab;

        protected override void Awake()
        {
            _bossDisplayName = "VUA YÊU TINH (GOBLIN KING)";
            _maxHealth = 32;
            _baseMoveSpeed = 1.9f;
            _meleeRange = 2.6f;
            _meleeDamage = 1;
            _attackCooldown = 2.8f;
            _specialCooldown = 7.5f;
            _enrageSpeedMultiplier = 1.18f;
            _spriteFacesLeftByDefault = true;

            base.Awake();
        }

        protected override void ExecuteSpecialAttack()
        {
            if (_phase == BossPhase.EnragedPhase2 && UnityEngine.Random.value > 0.5f)
            {
                // Chiêu Phase 2: Hú gọi 2 đệ tử quái vật Goblin
                StartCoroutine(SummonMinionsRoutine());
            }
            else
            {
                // Ném đá tảng khổng lồ
                StartCoroutine(RockTossRoutine());
            }
        }

        private IEnumerator RockTossRoutine()
        {
            _state = BossAIState.SpecialAttack;
            _rb.linearVelocityX = 0f;

            if (_animator != null)
            {
                _animator.Play("Goblin_king_Attack2");
            }

            yield return new WaitForSeconds(0.6f);

            // Ném tảng đá lớn
            float dirX = _isFacingRight ? 1f : -1f;
            Vector3 spawnPos = transform.position + new Vector3(dirX * 1.4f, 0.6f, 0f);

            GameObject rock = new GameObject("GoblinKing_GiantBoulder");
            rock.transform.position = spawnPos;
            rock.transform.localScale = Vector3.one * 1.6f;

            CircleCollider2D col = rock.AddComponent<CircleCollider2D>();
            col.radius = 0.42f;
            col.isTrigger = false;

            Rigidbody2D rockRb = rock.AddComponent<Rigidbody2D>();
            rockRb.bodyType = RigidbodyType2D.Dynamic;
            rockRb.linearVelocity = new Vector2(dirX * 6.5f, 4.0f);
            rockRb.mass = 4f;

            var dealer = rock.AddComponent<DamageDealer>();
            dealer.Initialize(1, Vector2.right * dirX, 7f, 3f, DamageType.Physical);

            CameraFollow2D.TriggerShake(0.12f, 0.08f);
            EventBus.Publish(new PlaySoundEvent("HitWood", SoundType.SFX, spawnPos));

            Destroy(rock, 5f);

            yield return new WaitForSeconds(0.5f);
            _specialCooldownTimer = _specialCooldown;
            _state = BossAIState.Chase;
        }

        private IEnumerator SummonMinionsRoutine()
        {
            _state = BossAIState.SpecialAttack;
            _rb.linearVelocityX = 0f;

            if (_animator != null)
            {
                _animator.Play("Goblin_king_Sneer");
            }

            EventBus.Publish(new PlaySoundEvent("Roar", SoundType.SFX, transform.position));
            CameraFollow2D.TriggerShake(0.18f, 0.12f);
            EventBus.Publish(new HudToastMessageEvent("GOBLIN KING TRIỆU HỒI ĐỆ TỬ CAVE!", new Color(1f, 0.65f, 0.2f, 1f)));

            yield return new WaitForSeconds(0.65f);

            // Sinh 1 đệ tử yêu tinh trợ chiến
            SpawnMinion(transform.position + new Vector3(-3.2f, 0f, 0f));

            yield return new WaitForSeconds(0.6f);
            _specialCooldownTimer = _specialCooldown * 1.3f;
            _state = BossAIState.Chase;
        }

        private void SpawnMinion(Vector3 pos)
        {
            if (_minionPrefab != null)
            {
                Instantiate(_minionPrefab, pos, Quaternion.identity);
            }
        }
    }
}
