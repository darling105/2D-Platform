using System.Collections;
using UnityEngine;
using Platformer2D.CameraControl;
using Platformer2D.Combat;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;

namespace Platformer2D.Enemies.Bosses
{
    /// <summary>
    /// Boss Màn 3: Chúa Tể Chiêu Hồn (Necromancer Boss):
    /// - Pháp sư hắc ám cai trị hỏa ngục tro tàn.
    /// - Phase 1: Bắn chùm cầu hắc ám ma thuật định hướng và dịch chuyển hư không (Teleport).
    /// - Phase 2 (< 50% HP): Bão lửa xương quỷ rơi từ trời và vòng xoáy ma thuật dung nham.
    /// </summary>
    public class NecromancerBoss : BaseBoss
    {
        [Header("Necromancer Magic")]
        [SerializeField] private float _teleportCooldown = 7.0f;
        private float _teleportTimer = 0f;

        protected override void Awake()
        {
            _bossDisplayName = "CHÚA TỂ CHIÊU HỒN (NECROMANCER)";
            _maxHealth = 40;
            _baseMoveSpeed = 1.8f;
            _meleeRange = 2.8f;
            _meleeDamage = 1;
            _attackCooldown = 2.8f;
            _specialCooldown = 8.0f;
            _enrageSpeedMultiplier = 1.15f;
            _teleportCooldown = 7.0f;
            _spriteFacesLeftByDefault = true;

            base.Awake();
        }

        protected override void Update()
        {
            base.Update();
            if (_isDead || _state == BossAIState.Dormant) return;

            _teleportTimer -= Time.deltaTime;
            // Dịch chuyển hư không né đòn nếu người chơi áp sát quá gần
            if (_teleportTimer <= 0f && _playerTransform != null && Mathf.Abs(_playerTransform.position.x - transform.position.x) < 2.0f)
            {
                _teleportTimer = _teleportCooldown;
                StartCoroutine(TeleportRoutine());
            }
        }

        protected override void ExecuteSpecialAttack()
        {
            if (_phase == BossPhase.EnragedPhase2 && UnityEngine.Random.value > 0.45f)
            {
                // Bão lửa xương quỷ Phase 2
                StartCoroutine(HellfireRainRoutine());
            }
            else
            {
                // Bắn 3 cầu ma thuật định hướng
                StartCoroutine(DarkVolleyRoutine());
            }
        }

        private IEnumerator DarkVolleyRoutine()
        {
            _state = BossAIState.SpecialAttack;
            _rb.linearVelocityX = 0f;

            if (_animator != null)
            {
                _animator.Play("Necromancer_Attack");
            }

            yield return new WaitForSeconds(0.55f);

            // Bắn 3 viên đạn hình quạt tốc độ vừa phải để dễ né
            float baseDirX = _isFacingRight ? 1f : -1f;
            for (int i = -1; i <= 1; i++)
            {
                Vector3 spawnPos = transform.position + new Vector3(baseDirX * 1.2f, 0.5f + i * 0.4f, 0f);
                SpawnMagicMissile(spawnPos, new Vector2(baseDirX * 6.0f, i * 1.8f));
            }

            EventBus.Publish(new PlaySoundEvent("Roar", SoundType.SFX, transform.position));
            CameraFollow2D.TriggerShake(0.12f, 0.08f);

            yield return new WaitForSeconds(0.5f);
            _specialCooldownTimer = _specialCooldown;
            _state = BossAIState.Chase;
        }

        private IEnumerator HellfireRainRoutine()
        {
            _state = BossAIState.SpecialAttack;
            _rb.linearVelocityX = 0f;

            if (_animator != null)
            {
                _animator.Play("Necromancer_Attack2");
            }

            EventBus.Publish(new HudToastMessageEvent("CẢNH BÁO: BÃO LỬA ĐỊA NGỤC (HELLFIRE RAIN)!", new Color(1f, 0.2f, 0.1f, 1f)));
            CameraFollow2D.TriggerShake(0.20f, 0.16f);

            yield return new WaitForSeconds(0.6f);

            // Rơi 3 tảng lửa từ trên trời xuống khu vực quanh người chơi
            float playerX = _playerTransform != null ? _playerTransform.position.x : transform.position.x;
            for (int i = 0; i < 3; i++)
            {
                float dropX = playerX + UnityEngine.Random.Range(-4.5f, 4.5f);
                Vector3 rainPos = new Vector3(dropX, transform.position.y + 6.5f, 0f);
                SpawnFallingHellfire(rainPos);
                yield return new WaitForSeconds(0.18f);
            }

            yield return new WaitForSeconds(0.4f);
            _specialCooldownTimer = _specialCooldown * 1.3f;
            _state = BossAIState.Chase;
        }

        private IEnumerator TeleportRoutine()
        {
            _isInvulnerable = true;
            if (_animator != null) _animator.Play("Necromancer_Sneer");

            // Mờ dần
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = new Color(1f, 1f, 1f, 0.2f);
            }

            yield return new WaitForSeconds(0.25f);

            // Dịch chuyển ra phía sau người chơi ~5 units
            if (_playerTransform != null)
            {
                float offset = _isFacingRight ? -5.5f : 5.5f;
                transform.position = new Vector3(_playerTransform.position.x + offset, transform.position.y, transform.position.z);
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _isEnraged ? _enrageTint : _originalColor;
            }

            CameraFollow2D.TriggerShake(0.08f, 0.05f);
            _isInvulnerable = false;
        }

        private void SpawnMagicMissile(Vector3 spawnPos, Vector2 vel)
        {
            GameObject orb = new GameObject("Necro_MagicMissile");
            orb.transform.position = spawnPos;
            orb.transform.localScale = Vector3.one * 1.2f;

            CircleCollider2D col = orb.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            col.isTrigger = true;

            Rigidbody2D orbRb = orb.AddComponent<Rigidbody2D>();
            orbRb.bodyType = RigidbodyType2D.Kinematic;
            orbRb.linearVelocity = vel;

            var dealer = orb.AddComponent<DamageDealer>();
            dealer.Initialize(1, vel.normalized, 6f, 2f, DamageType.Magical);

            Destroy(orb, 4f);
        }

        private void SpawnFallingHellfire(Vector3 spawnPos)
        {
            GameObject fire = new GameObject("Necro_Hellfire");
            fire.transform.position = spawnPos;
            fire.transform.localScale = Vector3.one * 1.4f;

            CircleCollider2D col = fire.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.isTrigger = true;

            Rigidbody2D fireRb = fire.AddComponent<Rigidbody2D>();
            fireRb.bodyType = RigidbodyType2D.Dynamic;
            fireRb.gravityScale = 2.0f;

            var dealer = fire.AddComponent<DamageDealer>();
            dealer.Initialize(1, Vector2.down, 4f, 4f, DamageType.Magical);

            Destroy(fire, 3.5f);
        }
    }
}
