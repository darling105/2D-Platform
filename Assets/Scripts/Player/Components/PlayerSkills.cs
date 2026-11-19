using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Platformer2D.CameraControl;
using Platformer2D.Combat;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Interfaces;
using Platformer2D.Managers;
using Platformer2D.Player.Input;
using Platformer2D.VFX;

namespace Platformer2D.Player.Components
{
    public enum PlayerSkillSlot
    {
        Fireball = 0,
        Lightning = 1,
        MagicShield = 2,
        EarthSpikes = 3
    }

    /// <summary>
    /// Quản lý bộ 4 kỹ năng phép thuật chủ lực của người chơi và hệ thống nâng cấp theo Cấp (Lv.0 = Khóa -> Lv.3 = Tối đa):
    /// - Slot 0 (1 / Q): Fireball - Bắn cầu lửa phát nổ sát thương vùng.
    /// - Slot 1 (2 / E): Lightning - Phóng luồng sét tầm trung trước mặt.
    /// - Slot 2 (3 / R): Magic Shield - Tạo khiên năng lượng chặn sát thương & đẩy lùi quái.
    /// - Slot 3 (4 / F): Earth Spikes - Gọi chuỗi gai đất trồi lên hất tung kẻ địch phía trước.
    /// </summary>
    public class PlayerSkills : MonoBehaviour
    {
        public const int SlotCount = 4;
        public const int MaxSkillLevel = 3;

        private struct SkillDefinition
        {
            public string DisplayName;
            public string HotkeyLabel;
            public int[] UnlockCosts;
            public float[] Cooldowns;
            public float[] PowerValues;
            public string SheetPath;
            public string IconPath;
            public Vector2 FramePivot;
        }

        private static readonly SkillDefinition[] Definitions =
        {
            new SkillDefinition
            {
                DisplayName = "FIREBALL",
                HotkeyLabel = "1/Q",
                UnlockCosts = new[] { 10, 20, 30 },
                Cooldowns = new[] { 2.5f, 2.0f, 1.5f },
                PowerValues = new[] { 2f, 3f, 4f },
                SheetPath = "Assets/GameAssets/Player-Skills/10 Fire ball/Fire-ball.png",
                IconPath = "Assets/GameAssets/Player-Skills/Icons/10-Fire-ball.png",
                FramePivot = new Vector2(0.5f, 0.29f)
            },
            new SkillDefinition
            {
                DisplayName = "LIGHTNING",
                HotkeyLabel = "2/E",
                UnlockCosts = new[] { 10, 20, 30 },
                Cooldowns = new[] { 4.0f, 3.2f, 2.4f },
                PowerValues = new[] { 3f, 4f, 6f },
                SheetPath = "Assets/GameAssets/Player-Skills/1 Lightning/Lightning.png",
                IconPath = "Assets/GameAssets/Player-Skills/Icons/1-Lightning.png",
                FramePivot = new Vector2(0.0f, 0.40f)
            },
            new SkillDefinition
            {
                DisplayName = "MAGIC SHIELD",
                HotkeyLabel = "3/R",
                UnlockCosts = new[] { 10, 20, 30 },
                Cooldowns = new[] { 7.0f, 5.8f, 4.5f },
                PowerValues = new[] { 3.0f, 4.2f, 5.5f },
                SheetPath = "Assets/GameAssets/Player-Skills/8 Shield/Shield.png",
                IconPath = "Assets/GameAssets/Player-Skills/Icons/8-Shield.png",
                FramePivot = new Vector2(0.5f, 0.47f)
            },
            new SkillDefinition
            {
                DisplayName = "EARTH SPIKES",
                HotkeyLabel = "4/F",
                UnlockCosts = new[] { 20, 30, 40 },
                Cooldowns = new[] { 5.0f, 4.0f, 3.0f },
                PowerValues = new[] { 2f, 3f, 4f },
                SheetPath = "Assets/GameAssets/Player-Skills/6 Spikes/Spikes.png",
                IconPath = "Assets/GameAssets/Player-Skills/Icons/6-Spikes.png",
                FramePivot = new Vector2(0.5f, 0.015f)
            }
        };

        private const string ExplosionSheetPath = "Assets/GameAssets/Player-Skills/5 Explosion/Explosion.png";

        [Header("Skill Assets (Optional Override for Runtime Builds)")]
        [SerializeField] private Sprite[] _skillIcons = new Sprite[SlotCount];
        [SerializeField] private Texture2D[] _skillTextures = new Texture2D[SlotCount];
        [SerializeField] private Texture2D _explosionTexture;

        private static readonly Dictionary<Texture2D, Sprite[]> GridFrameCache = new Dictionary<Texture2D, Sprite[]>();

        private readonly int[] _levels = new int[SlotCount];
        private readonly float[] _cooldownTimers = new float[SlotCount];

        private PlayerInputHandler _input;
        private PlayerMovement _movement;
        private SpriteRenderer _playerRenderer;
        private Collider2D _playerCollider;

        private bool _isShieldActive;
        private Coroutine _shieldCoroutine;
        private GameObject _shieldVfxObject;
        private float _lockedNoticeTimer;

        public bool IsShieldActive => _isShieldActive;

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            _movement = GetComponent<PlayerMovement>();
            _playerRenderer = GetComponent<SpriteRenderer>();
            _playerCollider = GetComponent<Collider2D>();
        }

        public void ProcessUpdate()
        {
            if (_lockedNoticeTimer > 0f) _lockedNoticeTimer -= Time.deltaTime;

            for (int i = 0; i < SlotCount; i++)
            {
                if (_cooldownTimers[i] > 0f)
                {
                    _cooldownTimers[i] = Mathf.Max(0f, _cooldownTimers[i] - Time.deltaTime);
                }
            }

            if (_input == null || _movement == null || _movement.IsClimbing) return;

            for (int i = 0; i < SlotCount; i++)
            {
                if (_input.IsSkillSlotTriggered(i))
                {
                    TryCastSkill(i);
                }
            }
        }

        #region Skill Query & Upgrade API (Cho HUD & Skill Panel)

        public int GetSkillLevel(int slot) => IsValidSlot(slot) ? _levels[slot] : 0;

        public string GetSkillName(int slot) => IsValidSlot(slot) ? Definitions[slot].DisplayName : string.Empty;

        public string GetHotkeyLabel(int slot) => IsValidSlot(slot) ? Definitions[slot].HotkeyLabel : string.Empty;

        public int GetNextCost(int slot)
        {
            if (!IsValidSlot(slot)) return 0;
            int level = _levels[slot];
            if (level >= MaxSkillLevel) return 0;
            return Definitions[slot].UnlockCosts[level];
        }

        public float GetCooldownTotal(int slot)
        {
            if (!IsValidSlot(slot)) return 1f;
            int idx = Mathf.Max(0, _levels[slot] - 1);
            return Definitions[slot].Cooldowns[idx];
        }

        public float GetCooldownRemaining(int slot) => IsValidSlot(slot) ? _cooldownTimers[slot] : 0f;

        public float GetCooldownNormalized(int slot)
        {
            if (!IsValidSlot(slot) || _levels[slot] <= 0) return 0f;
            float total = GetCooldownTotal(slot);
            if (total <= 0.001f) return 0f;
            return Mathf.Clamp01(_cooldownTimers[slot] / total);
        }

        public Sprite GetSkillIcon(int slot)
        {
            if (!IsValidSlot(slot)) return null;
            if (_skillIcons != null && slot < _skillIcons.Length && _skillIcons[slot] != null)
            {
                return _skillIcons[slot];
            }

#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(Definitions[slot].IconPath);
            Sprite loaded = assets != null ? assets.OfType<Sprite>().FirstOrDefault() : null;
            if (loaded != null && _skillIcons != null && slot < _skillIcons.Length)
            {
                _skillIcons[slot] = loaded;
            }
            return loaded;
#else
            return null;
#endif
        }

        public string GetStatSummary(int slot)
        {
            if (!IsValidSlot(slot)) return string.Empty;
            int lv = _levels[slot];
            SkillDefinition def = Definitions[slot];
            int idx = Mathf.Max(0, lv - 1);
            float cd = def.Cooldowns[idx];

            switch ((PlayerSkillSlot)slot)
            {
                case PlayerSkillSlot.Fireball:
                case PlayerSkillSlot.Lightning:
                    return lv <= 0
                        ? $"LOCKED  |  DMG {def.PowerValues[0]:0}  |  CD {cd:0.#}s"
                        : $"DMG {def.PowerValues[idx]:0}  |  CD {cd:0.#}s";

                case PlayerSkillSlot.MagicShield:
                    return lv <= 0
                        ? $"LOCKED  |  DUR {def.PowerValues[0]:0.#}s  |  CD {cd:0.#}s"
                        : $"DUR {def.PowerValues[idx]:0.#}s  |  CD {cd:0.#}s";

                case PlayerSkillSlot.EarthSpikes:
                    int waveCount = idx + 2;
                    return lv <= 0
                        ? $"LOCKED  |  2 WAVES  |  CD {cd:0.#}s"
                        : $"{waveCount} WAVES  |  DMG {def.PowerValues[idx]:0}  |  CD {cd:0.#}s";
            }

            return string.Empty;
        }

        public bool TryUnlockOrUpgradeSkill(int slot)
        {
            if (!IsValidSlot(slot)) return false;
            int currentLevel = _levels[slot];
            if (currentLevel >= MaxSkillLevel) return false;

            int cost = Definitions[slot].UnlockCosts[currentLevel];
            if (GameManager.HasInstance && !GameManager.Instance.TrySpendScore(cost))
            {
                EventBus.Publish(new HudToastMessageEvent($"NEED {cost} COINS!", new Color(1f, 0.45f, 0.35f, 1f)));
                return false;
            }

            _levels[slot] = currentLevel + 1;
            _cooldownTimers[slot] = 0f;

            string actionLabel = currentLevel == 0 ? "UNLOCKED" : $"LV.{_levels[slot]}";
            EventBus.Publish(new SkillStateChangedEvent(slot, _levels[slot]));
            EventBus.Publish(new PlaySoundEvent("Coin", SoundType.SFX, transform.position));
            EventBus.Publish(new HudToastMessageEvent($"{Definitions[slot].DisplayName} {actionLabel}!", new Color(0.32f, 0.92f, 0.52f, 1f)));
            return true;
        }

        #endregion

        #region Skill Casting

        public bool TryCastSkill(int slot)
        {
            if (!IsValidSlot(slot)) return false;

            int level = _levels[slot];
            if (level <= 0)
            {
                if (_lockedNoticeTimer <= 0f)
                {
                    _lockedNoticeTimer = 1.0f;
                    EventBus.Publish(new HudToastMessageEvent("SKILL LOCKED! PRESS [TAB] TO UNLOCK", new Color(1f, 0.78f, 0.25f, 1f)));
                }
                return false;
            }

            if (_cooldownTimers[slot] > 0f) return false;

            int tier = level - 1;
            _cooldownTimers[slot] = Definitions[slot].Cooldowns[tier];

            switch ((PlayerSkillSlot)slot)
            {
                case PlayerSkillSlot.Fireball:
                    CastFireball(Mathf.RoundToInt(Definitions[slot].PowerValues[tier]));
                    break;
                case PlayerSkillSlot.Lightning:
                    CastLightning(Mathf.RoundToInt(Definitions[slot].PowerValues[tier]));
                    break;
                case PlayerSkillSlot.MagicShield:
                    CastMagicShield(Definitions[slot].PowerValues[tier]);
                    break;
                case PlayerSkillSlot.EarthSpikes:
                    StartCoroutine(CastEarthSpikesRoutine(level + 1, Mathf.RoundToInt(Definitions[slot].PowerValues[tier])));
                    break;
            }

            return true;
        }

        private void CastFireball(int damage)
        {
            float facingDir = (_movement == null || _movement.IsFacingRight) ? 1f : -1f;
            Vector3 spawnPos = transform.position + new Vector3(facingDir * 0.55f, 0.05f, 0f);

            GameObject fbObj = new GameObject("Skill_Fireball");
            fbObj.transform.position = spawnPos;
            fbObj.AddComponent<SpriteRenderer>();

            CircleCollider2D col = fbObj.AddComponent<CircleCollider2D>();
            col.radius = 0.34f;
            col.isTrigger = true;

            SkillFireballProjectile projectile = fbObj.AddComponent<SkillFireballProjectile>();
            Sprite[] loopFrames = GetSkillFrames((int)PlayerSkillSlot.Fireball);
            Sprite[] explosionFrames = GetExplosionFrames();

            int layerId = _playerRenderer != null ? _playerRenderer.sortingLayerID : 0;
            int order = _playerRenderer != null ? _playerRenderer.sortingOrder + 2 : 5;
            projectile.Initialize(facingDir, damage, gameObject, loopFrames, explosionFrames, layerId, order);

            EventBus.Publish(new PlaySoundEvent("Throw", SoundType.SFX, spawnPos));
        }

        private void CastLightning(int damage)
        {
            float facingDir = (_movement == null || _movement.IsFacingRight) ? 1f : -1f;
            Vector3 origin = transform.position + new Vector3(facingDir * 0.45f, 0.08f, 0f);
            float scale = 2.35f;

            Sprite[] frames = GetSkillFrames((int)PlayerSkillSlot.Lightning);
            if (frames != null && frames.Length > 0)
            {
                GameObject vfx = new GameObject("VFX_SkillLightning");
                vfx.transform.position = origin;
                vfx.transform.localScale = new Vector3(facingDir * scale, scale, 1f);

                SpriteRenderer sr = vfx.AddComponent<SpriteRenderer>();
                ConfigureVfxRenderer(sr, 2);

                SpriteAnimationEffect anim = vfx.AddComponent<SpriteAnimationEffect>();
                anim.Initialize(frames, 22f);
            }

            Vector2 hitCenter = (Vector2)origin + new Vector2(facingDir * 2.3f, 0.08f);
            Vector2 hitSize = new Vector2(4.6f, 2.4f);
            ApplyBoxDamage(hitCenter, hitSize, damage, new Vector2(facingDir * 5.5f, 3.5f));

            CameraFollow2D.Instance?.Shake(0.16f, 0.18f);
            EventBus.Publish(new PlaySoundEvent("Attack", SoundType.SFX, origin));
        }

        private void CastMagicShield(float duration)
        {
            if (_shieldCoroutine != null)
            {
                StopCoroutine(_shieldCoroutine);
            }
            _shieldCoroutine = StartCoroutine(MagicShieldRoutine(duration));
            EventBus.Publish(new PlaySoundEvent("Checkpoint", SoundType.SFX, transform.position));
        }

        private IEnumerator MagicShieldRoutine(float duration)
        {
            _isShieldActive = true;
            Sprite[] frames = GetSkillFrames((int)PlayerSkillSlot.MagicShield);

            if (_shieldVfxObject != null) Destroy(_shieldVfxObject);
            _shieldVfxObject = new GameObject("VFX_MagicShieldAura");
            _shieldVfxObject.transform.SetParent(transform, false);
            _shieldVfxObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            _shieldVfxObject.transform.localScale = Vector3.one * 1.75f;

            SpriteRenderer shieldSr = _shieldVfxObject.AddComponent<SpriteRenderer>();
            ConfigureVfxRenderer(shieldSr, 1);
            shieldSr.color = Color.white;

            float elapsed = 0f;
            float animTimer = 0f;
            float pulseTimer = 0f;

            while (elapsed < duration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                animTimer += dt * 14f;
                pulseTimer -= dt;

                if (frames != null && frames.Length > 0 && shieldSr != null)
                {
                    shieldSr.sprite = frames[Mathf.FloorToInt(animTimer) % frames.Length];

                    // Nhấp nháy cảnh báo nhẹ trong 0.8 giây cuối trước khi hết khiên
                    if (duration - elapsed <= 0.8f)
                    {
                        float alpha = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(elapsed * 18f));
                        shieldSr.color = new Color(1f, 1f, 1f, alpha);
                    }
                }

                if (pulseTimer <= 0f)
                {
                    pulseTimer = 0.35f;
                    ApplyShieldPulse();
                }

                yield return null;
            }

            _isShieldActive = false;
            if (_shieldVfxObject != null)
            {
                Destroy(_shieldVfxObject);
                _shieldVfxObject = null;
            }
            _shieldCoroutine = null;
        }

        private void ApplyShieldPulse()
        {
            Vector2 center = transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, 0.95f);
            HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i];
                if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;
                if (col.CompareTag("Player")) continue;

                IDamageable dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                if (dmg != null && !dmg.IsDead && hitTargets.Add(dmg))
                {
                    float pushDir = col.transform.position.x >= transform.position.x ? 1f : -1f;
                    dmg.TakeDamage(new DamageData(1, gameObject, center, new Vector2(pushDir * 6f, 3.5f), DamageType.Magical));
                }
            }
        }

        private IEnumerator CastEarthSpikesRoutine(int waveCount, int damage)
        {
            float facingDir = (_movement == null || _movement.IsFacingRight) ? 1f : -1f;
            float baseX = transform.position.x;
            float probeStartY = transform.position.y + 1.0f;
            float fallbackFloorY = _playerCollider != null ? _playerCollider.bounds.min.y : transform.position.y - 0.5f;

            Sprite[] frames = GetSkillFrames((int)PlayerSkillSlot.EarthSpikes);
            HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

            for (int w = 0; w < waveCount; w++)
            {
                float spikeX = baseX + facingDir * (1.25f + w * 1.35f);
                float groundY = FindGroundSurfaceY(spikeX, probeStartY, fallbackFloorY);
                Vector3 spikePos = new Vector3(spikeX, groundY, 0f);

                if (frames != null && frames.Length > 0)
                {
                    GameObject spikeObj = new GameObject($"VFX_EarthSpike_{w}");
                    spikeObj.transform.position = spikePos;
                    spikeObj.transform.localScale = new Vector3(facingDir * 2.15f, 2.15f, 1f);

                    SpriteRenderer sr = spikeObj.AddComponent<SpriteRenderer>();
                    ConfigureVfxRenderer(sr, 2);

                    SpriteAnimationEffect effect = spikeObj.AddComponent<SpriteAnimationEffect>();
                    effect.Initialize(frames, 20f);
                }

                ApplyBoxDamage(
                    new Vector2(spikeX, groundY + 0.75f),
                    new Vector2(1.35f, 1.65f),
                    damage,
                    new Vector2(facingDir * 3.5f, 6.8f),
                    alreadyHit);

                CameraFollow2D.Instance?.Shake(0.10f, 0.12f);
                EventBus.Publish(new PlaySoundEvent("Attack", SoundType.SFX, spikePos));
                yield return new WaitForSeconds(0.085f);
            }
        }

        #endregion

        #region Helpers & Reset

        public void ResetRuntimeEffects()
        {
            StopAllCoroutines();
            _isShieldActive = false;
            _shieldCoroutine = null;

            if (_shieldVfxObject != null)
            {
                Destroy(_shieldVfxObject);
                _shieldVfxObject = null;
            }
        }

        private void ApplyBoxDamage(
            Vector2 center,
            Vector2 size,
            int damage,
            Vector2 knockback,
            HashSet<IDamageable> sharedFilter = null)
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);
            HashSet<IDamageable> hitTargets = sharedFilter ?? new HashSet<IDamageable>();

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i];
                if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;
                if (col.CompareTag("Player")) continue;

                IDamageable dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                if (dmg != null && !dmg.IsDead && hitTargets.Add(dmg))
                {
                    dmg.TakeDamage(new DamageData(damage, gameObject, center, knockback, DamageType.Magical));
                }
            }
        }

        private float FindGroundSurfaceY(float worldX, float startY, float fallbackY)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(worldX, startY), Vector2.down, 4.5f);
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit2D hit = hits[i];
                if (hit.collider == null || hit.collider.isTrigger) continue;
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.normal.y > 0.5f)
                {
                    return hit.point.y;
                }
            }

            return fallbackY;
        }

        private void ConfigureVfxRenderer(SpriteRenderer sr, int orderOffset)
        {
            if (sr == null || _playerRenderer == null) return;
            sr.sortingLayerID = _playerRenderer.sortingLayerID;
            sr.sortingOrder = _playerRenderer.sortingOrder + orderOffset;
        }

        private Sprite[] GetSkillFrames(int slot)
        {
            Texture2D tex = (_skillTextures != null && slot < _skillTextures.Length) ? _skillTextures[slot] : null;
#if UNITY_EDITOR
            if (tex == null)
            {
                tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(Definitions[slot].SheetPath);
                if (tex != null && _skillTextures != null && slot < _skillTextures.Length)
                {
                    _skillTextures[slot] = tex;
                }
            }
#endif
            return GetOrSliceGridFrames(tex, Definitions[slot].FramePivot);
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
            return GetOrSliceGridFrames(tex, new Vector2(0.5f, 0.5f));
        }

        private static Sprite[] GetOrSliceGridFrames(Texture2D tex, Vector2 pivot)
        {
            if (tex == null) return null;
            if (GridFrameCache.TryGetValue(tex, out Sprite[] cached) &&
                cached != null && cached.Length > 0 && cached[0] != null)
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
                frames[c] = Sprite.Create(tex, rect, pivot, 32f);
            }

            GridFrameCache[tex] = frames;
            return frames;
        }

        private static bool IsValidSlot(int slot) => slot >= 0 && slot < SlotCount;

        #endregion
    }
}
