using System;
using UnityEngine;
using UnityEngine.UI;
using Platformer2D.Core.Architecture;
using Platformer2D.Managers;
using Platformer2D.Player.Components;
using Platformer2D.Player.Controllers;

namespace Platformer2D.UI
{
    /// <summary>
    /// Quản lý giao diện Kỹ năng trên HUD & Bảng Phát triển Kỹ năng (Skill Upgrade Panel):
    /// - Thanh Hotbar 4 ô ở đáy màn hình hiển thị icon, phím tắt, cấp độ và vòng hồi chiêu (Cooldown).
    /// - Bảng Nâng cấp (mở bằng phím Tab hoặc nút SKILLS) dùng Xu (Coins) để Mở khóa & Lên cấp từng Skill.
    /// </summary>
    public class SkillTreePanelUI : MonoBehaviour
    {
        [Header("Hotbar (4 Slots)")]
        [SerializeField] private Button[] _hotbarButtons = new Button[PlayerSkills.SlotCount];
        [SerializeField] private Image[] _hotbarIcons = new Image[PlayerSkills.SlotCount];
        [SerializeField] private Image[] _hotbarCooldownFills = new Image[PlayerSkills.SlotCount];
        [SerializeField] private Text[] _hotbarCooldownTexts = new Text[PlayerSkills.SlotCount];
        [SerializeField] private Text[] _hotbarLevelTexts = new Text[PlayerSkills.SlotCount];

        [Header("Skill Upgrade Modal")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Text _coinsText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _doneButton;

        [Header("Upgrade Rows (4 Slots)")]
        [SerializeField] private Text[] _rowLevelTexts = new Text[PlayerSkills.SlotCount];
        [SerializeField] private Text[] _rowStatTexts = new Text[PlayerSkills.SlotCount];
        [SerializeField] private Button[] _rowUpgradeButtons = new Button[PlayerSkills.SlotCount];
        [SerializeField] private Text[] _rowUpgradeButtonTexts = new Text[PlayerSkills.SlotCount];

        private static readonly Color LockedTint = new Color(0.72f, 0.76f, 0.84f, 1f);
        private static readonly Color UnlockedTint = Color.white;
        private static readonly Color LockBadgeColor = new Color(0.75f, 0.78f, 0.86f, 1f);
        private static readonly Color ActiveBadgeColor = new Color(1f, 0.84f, 0.28f, 1f);
        private static readonly Color MaxBadgeColor = new Color(0.32f, 0.92f, 0.52f, 1f);

        private PlayerSkills _playerSkills;
        private Action _onClosed;

        public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

        private void Awake()
        {
            BindButtons();
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }

        private void Start()
        {
            ResolvePlayerSkills();
            RefreshUI();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Subscribe<SkillStateChangedEvent>(OnSkillStateChanged);
            EventBus.Subscribe<PlayerRespawnedEvent>(OnPlayerRespawned);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Unsubscribe<SkillStateChangedEvent>(OnSkillStateChanged);
            EventBus.Unsubscribe<PlayerRespawnedEvent>(OnPlayerRespawned);
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void Update()
        {
            ResolvePlayerSkills();
            UpdateHotbarCooldowns();
        }

        public void Open(Action onClosed = null)
        {
            _onClosed = onClosed;
            ResolvePlayerSkills();
            RefreshUI();

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }

            Time.timeScale = 0f;
        }

        public void Close()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            if (GameManager.HasInstance && GameManager.Instance.CurrentState == GameState.Paused)
            {
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = 1f;
            }

            Action callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        }

        public void RefreshUI()
        {
            ResolvePlayerSkills();
            int coins = GameManager.HasInstance ? GameManager.Instance.CurrentScore : 0;

            if (_coinsText != null)
            {
                _coinsText.text = $"COINS: {Mathf.Max(0, coins)}";
            }

            for (int i = 0; i < PlayerSkills.SlotCount; i++)
            {
                int level = _playerSkills != null ? _playerSkills.GetSkillLevel(i) : 0;
                bool unlocked = level > 0;
                bool isMax = level >= PlayerSkills.MaxSkillLevel;

                if (_hotbarIcons != null && i < _hotbarIcons.Length && _hotbarIcons[i] != null)
                {
                    if (_playerSkills != null && _hotbarIcons[i].sprite == null)
                    {
                        _hotbarIcons[i].sprite = _playerSkills.GetSkillIcon(i);
                    }
                    _hotbarIcons[i].color = unlocked ? UnlockedTint : LockedTint;
                }

                if (_hotbarLevelTexts != null && i < _hotbarLevelTexts.Length && _hotbarLevelTexts[i] != null)
                {
                    _hotbarLevelTexts[i].text = !unlocked ? "LOCK" : (isMax ? "MAX" : $"LV.{level}");
                    _hotbarLevelTexts[i].color = !unlocked ? LockBadgeColor : (isMax ? MaxBadgeColor : ActiveBadgeColor);
                }

                if (_rowLevelTexts != null && i < _rowLevelTexts.Length && _rowLevelTexts[i] != null)
                {
                    _rowLevelTexts[i].text = !unlocked ? "LOCKED" : (isMax ? "MAX LV.3" : $"LV.{level}");
                    _rowLevelTexts[i].color = !unlocked ? LockBadgeColor : (isMax ? MaxBadgeColor : ActiveBadgeColor);
                }

                if (_rowStatTexts != null && i < _rowStatTexts.Length && _rowStatTexts[i] != null)
                {
                    _rowStatTexts[i].text = _playerSkills != null ? _playerSkills.GetStatSummary(i) : string.Empty;
                }

                if (_rowUpgradeButtons != null && i < _rowUpgradeButtons.Length && _rowUpgradeButtons[i] != null)
                {
                    int cost = _playerSkills != null ? _playerSkills.GetNextCost(i) : 0;
                    bool canUpgrade = !isMax && _playerSkills != null && coins >= cost;
                    _rowUpgradeButtons[i].interactable = canUpgrade;

                    if (_rowUpgradeButtonTexts != null && i < _rowUpgradeButtonTexts.Length && _rowUpgradeButtonTexts[i] != null)
                    {
                        _rowUpgradeButtonTexts[i].text = isMax
                            ? "MAXED"
                            : (unlocked ? $"UPGRADE ({cost})" : $"UNLOCK ({cost})");
                    }
                }
            }

            UpdateHotbarCooldowns();
        }

        private void UpdateHotbarCooldowns()
        {
            for (int i = 0; i < PlayerSkills.SlotCount; i++)
            {
                int level = _playerSkills != null ? _playerSkills.GetSkillLevel(i) : 0;
                float cdRemaining = _playerSkills != null ? _playerSkills.GetCooldownRemaining(i) : 0f;
                float cdNorm = _playerSkills != null ? _playerSkills.GetCooldownNormalized(i) : 0f;

                if (_hotbarCooldownFills != null && i < _hotbarCooldownFills.Length && _hotbarCooldownFills[i] != null)
                {
                    _hotbarCooldownFills[i].fillAmount = level > 0 ? cdNorm : 0f;
                }

                if (_hotbarCooldownTexts != null && i < _hotbarCooldownTexts.Length && _hotbarCooldownTexts[i] != null)
                {
                    bool showCd = level > 0 && cdRemaining > 0.05f;
                    _hotbarCooldownTexts[i].gameObject.SetActive(showCd);
                    if (showCd)
                    {
                        _hotbarCooldownTexts[i].text = $"{cdRemaining:0.0}";
                    }
                }
            }
        }

        private void BindButtons()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
            if (_doneButton != null) _doneButton.onClick.AddListener(Close);

            for (int i = 0; i < PlayerSkills.SlotCount; i++)
            {
                int slotIndex = i;
                if (_hotbarButtons != null && i < _hotbarButtons.Length && _hotbarButtons[i] != null)
                {
                    _hotbarButtons[i].onClick.AddListener(() => OnHotbarSlotClicked(slotIndex));
                }
                if (_rowUpgradeButtons != null && i < _rowUpgradeButtons.Length && _rowUpgradeButtons[i] != null)
                {
                    _rowUpgradeButtons[i].onClick.AddListener(() => OnUpgradeSlotClicked(slotIndex));
                }
            }
        }

        private void UnbindButtons()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
            if (_doneButton != null) _doneButton.onClick.RemoveListener(Close);

            for (int i = 0; i < PlayerSkills.SlotCount; i++)
            {
                if (_hotbarButtons != null && i < _hotbarButtons.Length && _hotbarButtons[i] != null)
                {
                    _hotbarButtons[i].onClick.RemoveAllListeners();
                }
                if (_rowUpgradeButtons != null && i < _rowUpgradeButtons.Length && _rowUpgradeButtons[i] != null)
                {
                    _rowUpgradeButtons[i].onClick.RemoveAllListeners();
                }
            }
        }

        private void OnHotbarSlotClicked(int slot)
        {
            if (IsOpen || (GameManager.HasInstance && GameManager.Instance.CurrentState != GameState.Playing)) return;

            ResolvePlayerSkills();
            if (_playerSkills != null)
            {
                _playerSkills.TryCastSkill(slot);
            }
        }

        private void OnUpgradeSlotClicked(int slot)
        {
            ResolvePlayerSkills();
            if (_playerSkills != null && _playerSkills.TryUnlockOrUpgradeSkill(slot))
            {
                RefreshUI();
            }
        }

        private void OnScoreChanged(ScoreChangedEvent evt)
        {
            RefreshUI();
        }

        private void OnSkillStateChanged(SkillStateChangedEvent evt)
        {
            RefreshUI();
        }

        private void OnPlayerRespawned(PlayerRespawnedEvent evt)
        {
            RefreshUI();
        }

        private void ResolvePlayerSkills()
        {
            if (_playerSkills != null) return;

            PlayerController controller = FindAnyObjectByType<PlayerController>();
            if (controller != null)
            {
                _playerSkills = controller.Skills != null ? controller.Skills : controller.GetComponent<PlayerSkills>();
            }
            if (_playerSkills == null)
            {
                _playerSkills = FindAnyObjectByType<PlayerSkills>();
            }
        }
    }
}
