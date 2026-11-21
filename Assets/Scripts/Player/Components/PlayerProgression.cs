using System;
using System.Collections.Generic;
using UnityEngine;
using Platformer2D.Combat;
using Platformer2D.Core.Architecture;
using Platformer2D.Managers;
using Platformer2D.Player.Controllers;

namespace Platformer2D.Player.Components
{
    public enum SkillNodeId
    {
        None = -1,
        // Nhánh 1: Chiến Binh / Thể Lực (Combat & Agility)
        Combat_StaminaBoost = 0,   // Bền Bỉ: Tăng Max Stamina (+15/cấp) & tốc độ hồi (+6/s)
        Combat_Strength = 1,       // Cường Lực: Tăng sát thương cận chiến & ném đá (+1 DMG/cấp)
        Combat_SwiftDash = 2,      // Lướt Thần Tốc: Giảm stamina Dash (-5/cấp) & tăng tốc độ lướt

        // Nhánh 2: Pháp Thuật Chủ Lực (Magic Mastery)
        Magic_Fireball = 3,        // Cầu Lửa: Mở khóa & tăng sát thương/bán kính nổ (Slot 0)
        Magic_Lightning = 4,       // Tia Sét: Mở khóa & tăng sát thương tầm xa (Slot 1)
        Magic_Shield = 5,          // Khiên Năng Lượng: Mở khóa & tăng thời gian bảo vệ (Slot 2)
        Magic_EarthSpikes = 6,     // Gai Địa Chấn: Mở khóa & tăng số đợt sóng gai (Slot 3)

        // Nhánh 3: Sinh Tồn & May Mắn (Survival & Fortune)
        Survival_Vitality = 7,     // Thể Trạng Thép: Tăng Máu Tối Đa (+1 HP mỗi cấp: 3 -> 4 -> 5 -> 6)
        Survival_BottomlessBag = 8,// Túi Đạn Vô Đáy: Tăng sức chứa đá ném (+5 viên mỗi cấp: 15 -> 20 -> 25 -> 30)
        Survival_Fortune = 9       // Phú Quý / Hút Hồn: Tăng +25% EXP & Xu rơi từ Quái và Rương
    }

    [System.Serializable]
    public class SkillNode
    {
        public SkillNodeId Id;
        public string Name;
        public string BranchName;
        public string Description;
        public SkillNodeId Prerequisite = SkillNodeId.None;
        public int MaxLevel = 3;
        public int CurrentLevel = 0;

        public bool IsMaxed => CurrentLevel >= MaxLevel;
    }

    /// <summary>
    /// Quản lý toàn bộ tiến trình phát triển của người chơi (Player Progression):
    /// - Thanh Thể Lực (Stamina): Tiêu hao khi Dash, Air Jump, leo thang; tự động hồi phục sau độ trễ.
    /// - Cấp Độ & Kinh Nghiệm (Level & EXP): Đánh quái nhận EXP, thăng cấp nhận Skill Points (SP) & hồi phục 100%.
    /// - Cây Kỹ Năng 3 Nhánh (Skill Tree): Chiến Binh, Pháp Thuật, Sinh Tồn; kiểm tra điều kiện tiên quyết và hỗ trợ Tẩy Điểm.
    /// </summary>
    public class PlayerProgression : MonoBehaviour
    {
        public const float BaseMaxStamina = 100f;
        public const float BaseStaminaRegenRate = 30f;
        public const float StaminaRegenDelay = 0.75f;

        [Header("Stamina State")]
        [SerializeField] private float _currentStamina = BaseMaxStamina;

        [Header("Level & EXP State")]
        [SerializeField] private int _currentLevel = 1;
        [SerializeField] private int _currentExp = 0;
        [SerializeField] private int _availableSkillPoints = 1;

        private float _staminaRegenDelayTimer;
        private readonly Dictionary<SkillNodeId, SkillNode> _nodes = new Dictionary<SkillNodeId, SkillNode>();

        private PlayerController _playerController;
        private PlayerMovement _playerMovement;
        private PlayerCombat _playerCombat;
        private PlayerSkills _playerSkills;
        private Health _playerHealth;

        #region Properties

        public float CurrentStamina => _currentStamina;
        public float MaxStamina => BaseMaxStamina + GetNodeLevel(SkillNodeId.Combat_StaminaBoost) * 15f;
        public float StaminaRegenRate => BaseStaminaRegenRate + GetNodeLevel(SkillNodeId.Combat_StaminaBoost) * 6f;
        public float StaminaNormalized => MaxStamina > 0.001f ? Mathf.Clamp01(_currentStamina / MaxStamina) : 0f;

        public int CurrentLevel => _currentLevel;
        public int CurrentExp => _currentExp;
        public int ExpToNextLevel => GetRequiredExpForLevel(_currentLevel);
        public float ExpNormalized => ExpToNextLevel > 0 ? Mathf.Clamp01((float)_currentExp / ExpToNextLevel) : 0f;

        public int AvailableSkillPoints => _availableSkillPoints;
        public IReadOnlyDictionary<SkillNodeId, SkillNode> Nodes
        {
            get
            {
                if (_nodes.Count == 0) InitializeSkillTree();
                return _nodes;
            }
        }

        #endregion

        private void Awake()
        {
            InitializeSkillTree();
            ResolveComponents();
            _currentStamina = MaxStamina;
        }

        private void Start()
        {
            PublishAllStates();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Subscribe<PlayerRespawnedEvent>(OnPlayerRespawned);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Unsubscribe<PlayerRespawnedEvent>(OnPlayerRespawned);
        }

        public void ProcessUpdate()
        {
            RegenerateStamina(Time.deltaTime);
        }

        private void ResolveComponents()
        {
            _playerController = GetComponent<PlayerController>();
            _playerMovement = GetComponent<PlayerMovement>();
            _playerCombat = GetComponent<PlayerCombat>();
            _playerSkills = GetComponent<PlayerSkills>();
            _playerHealth = GetComponent<Health>();
        }

        #region Stamina Logic

        public bool HasStamina(float amount)
        {
            return _currentStamina >= amount;
        }

        public bool ConsumeStamina(float amount)
        {
            if (amount <= 0f) return true;

            if (_currentStamina < amount)
            {
                // Không đủ thể lực
                return false;
            }

            _currentStamina = Mathf.Max(0f, _currentStamina - amount);
            _staminaRegenDelayTimer = StaminaRegenDelay;
            EventBus.Publish(new PlayerStaminaChangedEvent(_currentStamina, MaxStamina));
            return true;
        }

        public void PauseStaminaRegen()
        {
            _staminaRegenDelayTimer = Mathf.Max(_staminaRegenDelayTimer, 0.4f);
        }

        public void RestoreStamina(float amount)
        {
            float max = MaxStamina;
            _currentStamina = Mathf.Min(max, _currentStamina + amount);
            EventBus.Publish(new PlayerStaminaChangedEvent(_currentStamina, max));
        }

        private void RegenerateStamina(float dt)
        {
            if (_staminaRegenDelayTimer > 0f)
            {
                _staminaRegenDelayTimer -= dt;
                return;
            }

            float max = MaxStamina;
            if (_currentStamina < max)
            {
                _currentStamina = Mathf.Min(max, _currentStamina + StaminaRegenRate * dt);
                EventBus.Publish(new PlayerStaminaChangedEvent(_currentStamina, max));
            }
        }

        public float GetDashStaminaCost()
        {
            int dashPerk = GetNodeLevel(SkillNodeId.Combat_SwiftDash);
            return Mathf.Max(10f, 25f - dashPerk * 5f);
        }

        public float GetDashSpeedBonus()
        {
            return GetNodeLevel(SkillNodeId.Combat_SwiftDash) * 1.5f;
        }

        #endregion

        #region Level & EXP Logic

        public static int GetRequiredExpForLevel(int level)
        {
            if (level <= 1) return 100;
            return Mathf.RoundToInt(100f * Mathf.Pow(level, 1.38f));
        }

        public void AddExp(int rawAmount)
        {
            if (rawAmount <= 0) return;

            float fortuneMultiplier = 1f + GetFortuneExpBonus();
            int finalExp = Mathf.RoundToInt(rawAmount * fortuneMultiplier);

            _currentExp += finalExp;

            bool leveledUp = false;
            while (_currentExp >= ExpToNextLevel)
            {
                _currentExp -= ExpToNextLevel;
                _currentLevel++;
                _availableSkillPoints++;
                leveledUp = true;
            }

            if (leveledUp)
            {
                OnLeveledUp();
            }

            EventBus.Publish(new PlayerExpChangedEvent(_currentLevel, _currentExp, ExpToNextLevel));
        }

        private void OnLeveledUp()
        {
            // Hồi phục toàn bộ Máu và Thể lực khi lên cấp
            if (_playerController != null)
            {
                _playerController.Heal(99);
            }
            _currentStamina = MaxStamina;
            EventBus.Publish(new PlayerStaminaChangedEvent(_currentStamina, MaxStamina));

            EventBus.Publish(new PlayerLeveledUpEvent(_currentLevel, _availableSkillPoints));
            EventBus.Publish(new SkillPointsChangedEvent(_availableSkillPoints));
            EventBus.Publish(new PlaySoundEvent("Checkpoint", SoundType.SFX, transform.position));
            EventBus.Publish(new HudToastMessageEvent($"LEVEL UP! YOU ARE NOW LV.{_currentLevel}! (+1 SP)", new Color(1f, 0.84f, 0.28f, 1f)));
        }

        public float GetFortuneExpBonus()
        {
            return GetNodeLevel(SkillNodeId.Survival_Fortune) * 0.25f;
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent e)
        {
            AddExp(e.BaseExp);

            if (e.BaseCoins > 0 && GameManager.HasInstance)
            {
                int bonusCoins = Mathf.RoundToInt(e.BaseCoins * (1f + GetFortuneExpBonus()));
                GameManager.Instance.AddScore(bonusCoins);
            }
        }

        private void OnPlayerRespawned(PlayerRespawnedEvent e)
        {
            _currentStamina = MaxStamina;
            PublishAllStates();
        }

        #endregion

        #region Skill Tree Logic

        private void InitializeSkillTree()
        {
            _nodes.Clear();

            // === Nhánh 1: Chiến Binh (Combat) ===
            RegisterNode(SkillNodeId.Combat_StaminaBoost, "BỀN BỈ", "Chiến Binh",
                "+15 Thể lực tối đa & hồi nhanh hơn +6/s mỗi cấp.", SkillNodeId.None);
            RegisterNode(SkillNodeId.Combat_Strength, "CƯỜNG LỰC", "Chiến Binh",
                "+1 Sát thương cận chiến & ném đá mỗi cấp.", SkillNodeId.Combat_StaminaBoost);
            RegisterNode(SkillNodeId.Combat_SwiftDash, "LƯỚT THẦN TỐC", "Chiến Binh",
                "Giảm 5 Thể lực khi lướt (Dash) & tăng tầm lướt mỗi cấp.", SkillNodeId.Combat_Strength);

            // === Nhánh 2: Pháp Thuật (Magic) ===
            RegisterNode(SkillNodeId.Magic_Fireball, "CẦU LỬA (1/Q)", "Pháp Thuật",
                "Bắn cầu lửa phát nổ sát thương vùng (AoE). Tăng cấp tăng sát thương & giảm CD.", SkillNodeId.None);
            RegisterNode(SkillNodeId.Magic_Lightning, "TIA SÉT (2/E)", "Pháp Thuật",
                "Phóng luồng điện giật diện rộng phía trước. Tăng cấp tăng phạm vi & sát thương.", SkillNodeId.None);
            RegisterNode(SkillNodeId.Magic_Shield, "KHIÊN NĂNG LƯỢNG (3/R)", "Pháp Thuật",
                "Tạo vòng bảo vệ chặn đòn đánh & đẩy lùi quái. Cần mở Cầu Lửa trước.", SkillNodeId.Magic_Fireball);
            RegisterNode(SkillNodeId.Magic_EarthSpikes, "ĐẠI ĐỊA GAI (4/F)", "Pháp Thuật",
                "Gọi các đợt sóng gai đá nhấp nhô hất tung kẻ địch. Cần mở Tia Sét trước.", SkillNodeId.Magic_Lightning);

            // === Nhánh 3: Sinh Tồn & May Mắn (Survival) ===
            RegisterNode(SkillNodeId.Survival_Vitality, "THỂ TRẠNG THÉP", "Sinh Tồn",
                "+1 Máu tối đa mỗi cấp (Tối đa nâng lên 6 Tim).", SkillNodeId.None);
            RegisterNode(SkillNodeId.Survival_BottomlessBag, "TÚI ĐẠN VÔ ĐÁY", "Sinh Tồn",
                "+5 Sức chứa Đá ném mỗi cấp (15 -> 20 -> 25 -> 30 viên).", SkillNodeId.Survival_Vitality);
            RegisterNode(SkillNodeId.Survival_Fortune, "HÚT HỒN PHÚ QUÝ", "Sinh Tồn",
                "+25% EXP & Tiền Xu rơi ra từ Quái vật & Rương báu mỗi cấp.", SkillNodeId.Survival_BottomlessBag);

            // Mở sẵn Cầu Lửa (Fireball) ở Cấp 1 để người chơi bắn phép ngay lập tức khi vào màn
            if (_nodes.TryGetValue(SkillNodeId.Magic_Fireball, out var fireballNode))
            {
                fireballNode.CurrentLevel = 1;
            }
        }

        private void RegisterNode(SkillNodeId id, string name, string branch, string desc, SkillNodeId prereq = SkillNodeId.None)
        {
            _nodes[id] = new SkillNode
            {
                Id = id,
                Name = name,
                BranchName = branch,
                Description = desc,
                Prerequisite = prereq,
                MaxLevel = 3,
                CurrentLevel = 0
            };
        }

        public int GetNodeLevel(SkillNodeId id)
        {
            return _nodes.TryGetValue(id, out var node) ? node.CurrentLevel : 0;
        }

        public bool CanUpgradeNode(SkillNodeId id)
        {
            if (_availableSkillPoints < 1) return false;
            if (!_nodes.TryGetValue(id, out var node)) return false;
            if (node.IsMaxed) return false;

            if (node.Prerequisite != SkillNodeId.None)
            {
                int prereqLevel = GetNodeLevel(node.Prerequisite);
                if (prereqLevel < 1) return false;
            }

            return true;
        }

        public bool TryUpgradeNode(SkillNodeId id)
        {
            if (!CanUpgradeNode(id)) return false;

            var node = _nodes[id];
            node.CurrentLevel++;
            _availableSkillPoints--;

            ApplyPerkEffects(id);

            EventBus.Publish(new SkillPointsChangedEvent(_availableSkillPoints));
            EventBus.Publish(new PlaySoundEvent("Coin", SoundType.SFX, transform.position));
            EventBus.Publish(new HudToastMessageEvent($"NÂNG CẤP: {node.Name} (CẤP {node.CurrentLevel})!", new Color(0.35f, 0.95f, 0.55f, 1f)));
            return true;
        }

        public void ResetSkillTree()
        {
            int totalRefunded = 0;
            foreach (var kvp in _nodes)
            {
                totalRefunded += kvp.Value.CurrentLevel;
                kvp.Value.CurrentLevel = 0;
            }

            _availableSkillPoints += totalRefunded;

            // Đưa các chỉ số về ban đầu
            SyncAllPerksToComponents();

            _currentStamina = MaxStamina;
            EventBus.Publish(new PlayerStaminaChangedEvent(_currentStamina, MaxStamina));
            EventBus.Publish(new SkillPointsChangedEvent(_availableSkillPoints));
            EventBus.Publish(new PlaySoundEvent("Checkpoint", SoundType.SFX, transform.position));
            EventBus.Publish(new HudToastMessageEvent($"ĐÃ TẨY ĐIỂM! HOÀN LẠI {totalRefunded} ĐIỂM KỸ NĂNG.", new Color(1f, 0.85f, 0.35f, 1f)));
        }

        private void ApplyPerkEffects(SkillNodeId id)
        {
            ResolveComponents();
            int lv = GetNodeLevel(id);

            switch (id)
            {
                case SkillNodeId.Combat_StaminaBoost:
                    // Tự động cập nhật qua MaxStamina property
                    EventBus.Publish(new PlayerStaminaChangedEvent(_currentStamina, MaxStamina));
                    break;

                case SkillNodeId.Combat_Strength:
                    if (_playerCombat != null)
                    {
                        _playerCombat.BonusAttackDamage = lv;
                    }
                    break;

                case SkillNodeId.Combat_SwiftDash:
                    // Được truy vấn động qua GetDashStaminaCost() & GetDashSpeedBonus()
                    break;

                case SkillNodeId.Magic_Fireball:
                    SyncMagicSlot(PlayerSkillSlot.Fireball, lv);
                    break;

                case SkillNodeId.Magic_Lightning:
                    SyncMagicSlot(PlayerSkillSlot.Lightning, lv);
                    break;

                case SkillNodeId.Magic_Shield:
                    SyncMagicSlot(PlayerSkillSlot.MagicShield, lv);
                    break;

                case SkillNodeId.Magic_EarthSpikes:
                    SyncMagicSlot(PlayerSkillSlot.EarthSpikes, lv);
                    break;

                case SkillNodeId.Survival_Vitality:
                    int newMaxHp = 3 + lv;
                    if (_playerHealth != null)
                    {
                        _playerHealth.SetMaxHealth(newMaxHp);
                    }
                    if (_playerController != null)
                    {
                        _playerController.SetMaxHealth(newMaxHp);
                    }
                    break;

                case SkillNodeId.Survival_BottomlessBag:
                    int newMaxRocks = 15 + lv * 5;
                    if (_playerCombat != null)
                    {
                        _playerCombat.SetMaxRocks(newMaxRocks);
                    }
                    break;

                case SkillNodeId.Survival_Fortune:
                    // Được truy vấn động qua GetFortuneExpBonus()
                    break;
            }
        }

        private void SyncMagicSlot(PlayerSkillSlot slot, int level)
        {
            if (_playerSkills != null)
            {
                _playerSkills.SetSkillLevelExplicit((int)slot, level);
            }
            EventBus.Publish(new SkillStateChangedEvent((int)slot, level));
        }

        public void SyncAllPerksToComponents()
        {
            ResolveComponents();
            foreach (var kvp in _nodes)
            {
                ApplyPerkEffects(kvp.Key);
            }
        }

        #endregion

        public void PublishAllStates()
        {
            EventBus.Publish(new PlayerStaminaChangedEvent(_currentStamina, MaxStamina));
            EventBus.Publish(new PlayerExpChangedEvent(_currentLevel, _currentExp, ExpToNextLevel));
            EventBus.Publish(new SkillPointsChangedEvent(_availableSkillPoints));
        }
    }
}
