using System.Collections;
using UnityEngine;
using Platformer2D.CameraControl;
using Platformer2D.Combat;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;

namespace Platformer2D.Enemies.Bosses
{
    /// <summary>
    /// Boss Màn 1: Chúa Quỷ Đồng Cỏ (Demon Boss):
    /// - Quái thú hung tợn thống trị thung lũng cổ thụ.
    /// - Phase 1: Chém quét liên hoàn 2 nhịp và lao húc hất văng người chơi.
    /// - Phase 2 (< 50% HP): Cuồng nộ phủ khói đỏ, nhảy dậm đất gây chấn động mạnh và bắn cầu năng lượng hắc ám.
    /// </summary>
    public class DemonBoss : BaseBoss
    {
        [Header("Demon Boss Specific")]
        [SerializeField] private float _darkOrbSpeed = 5.5f;

        protected override void Awake()
        {
            _bossDisplayName = "CHÚA QUỶ ĐỒNG CỎ (DEMON)";
            _maxHealth = 24;
            _baseMoveSpeed = 2.0f;
            _meleeRange = 2.4f;
            _meleeDamage = 1;
            _attackCooldown = 2.8f;
            _specialCooldown = 7.5f;
            _enrageSpeedMultiplier = 1.18f;
            _darkOrbSpeed = 5.5f;
            _spriteFacesLeftByDefault = true;

            base.Awake();
        }

        protected override void ExecuteSpecialAttack()
        {
            if (_phase == BossPhase.EnragedPhase2 && UnityEngine.Random.value > 0.45f)
            {
                // Chiêu Phase 2: Dậm đất phát tán sóng năng lượng và bắn cầu hắc ám
                StartCoroutine(DemonDarkSlamRoutine());
            }
            else
            {
                base.ExecuteSpecialAttack();
            }
        }

        private IEnumerator DemonDarkSlamRoutine()
        {
            _state = BossAIState.SpecialAttack;
            _rb.linearVelocityX = 0f;

            if (_animator != null)
            {
                _animator.Play("Demon_Sneer");
            }

            yield return new WaitForSeconds(0.55f);

            // Nhảy lên nhẹ và dậm sầm xuống đất
            _rb.linearVelocityY = 6.5f;
            yield return new WaitForSeconds(0.35f);

            _rb.linearVelocityY = -12f;
            while (transform.position.y > -1.4f)
            {
                yield return null;
            }

            CameraFollow2D.TriggerShake(0.18f, 0.15f);
            EventBus.Publish(new PlaySoundEvent("EnemyHurt", SoundType.SFX, transform.position));

            // Sát thương chấn động xung quanh
            DealHitboxDamage(3.6f, _meleeDamage);

            // Bắn cầu hắc ám về phía người chơi
            if (_playerTransform != null)
            {
                SpawnDarkOrb();
            }

            yield return new WaitForSeconds(0.55f);
            _specialCooldownTimer = _specialCooldown;
            _state = BossAIState.Chase;
        }

        private void SpawnDarkOrb()
        {
            float dirX = _isFacingRight ? 1f : -1f;
            Vector3 spawnPos = transform.position + new Vector3(dirX * 1.2f, 0.4f, 0f);

            GameObject orb = new GameObject("Demon_DarkOrb");
            orb.transform.position = spawnPos;
            orb.transform.localScale = Vector3.one * 1.3f;

            SpriteRenderer sr = orb.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.85f, 0.15f, 0.95f, 1f);

            // Dùng sprite đạn nếu có, hoặc hình tròn đỏ tím
            CircleCollider2D col = orb.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            Rigidbody2D orbRb = orb.AddComponent<Rigidbody2D>();
            orbRb.bodyType = RigidbodyType2D.Kinematic;
            orbRb.linearVelocity = new Vector2(dirX * _darkOrbSpeed, 0f);

            var dealer = orb.AddComponent<DamageDealer>();
            dealer.Initialize(1, Vector2.right * dirX, 6f, 2f, DamageType.Magical);

            Destroy(orb, 3.5f);
        }
    }
}
