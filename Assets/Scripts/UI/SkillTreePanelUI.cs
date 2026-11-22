using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Platformer2D.Core.Architecture;
using Platformer2D.Managers;
using Platformer2D.Player.Components;
using Platformer2D.Player.Controllers;

namespace Platformer2D.UI
{
    public enum SkillBranchTab
    {
        Combat = 0,
        Magic = 1,
        Survival = 2
    }

    /// <summary>
    /// Quản lý giao diện Kỹ năng trên HUD & Bảng Cây Kỹ Năng Phân Nhánh (3-Branch Skill Tree):
    /// - Thanh Hotbar 4 ô ở đáy màn hình hiển thị icon, phím tắt, cấp độ và vòng hồi chiêu (Cooldown).
    /// - Cây Kỹ Năng 3 Nhánh (⚔️ Chiến Binh, 🔮 Pháp Thuật, 🛡️ Sinh Tồn) mở bằng Tab hoặc nút SKILLS.
    /// - Dùng Điểm Kỹ Năng (SP) nhận được khi Thăng Cấp (Level Up) để mở khóa & nâng cấp 10 Node kỹ năng.
    /// - Hỗ trợ Tẩy Điểm (Reset Tree) hoàn lại 100% SP.
    /// </summary>
    public class SkillTreePanelUI : MonoBehaviour
    {
        [Header("Hotbar (4 Active Magic Slots)")]
        [SerializeField] private Button[] _hotbarButtons = new Button[PlayerSkills.SlotCount];
        [SerializeField] private Image[] _hotbarIcons = new Image[PlayerSkills.SlotCount];
        [SerializeField] private Image[] _hotbarCooldownFills = new Image[PlayerSkills.SlotCount];
        [SerializeField] private Text[] _hotbarCooldownTexts = new Text[PlayerSkills.SlotCount];
        [SerializeField] private Text[] _hotbarLevelTexts = new Text[PlayerSkills.SlotCount];

        [Header("Skill Tree Modal Root")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Text _coinsText; // Dùng làm text hiển thị SP & Level
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _doneButton;

        [Header("Branch Tabs & Containers")]
        [SerializeField] private Button _tabCombatButton;
        [SerializeField] private Button _tabMagicButton;
        [SerializeField] private Button _tabSurvivalButton;
        [SerializeField] private GameObject _branchCombatContainer;
        [SerializeField] private GameObject _branchMagicContainer;
        [SerializeField] private GameObject _branchSurvivalContainer;
        [SerializeField] private Button _resetTreeButton;

        [Header("Legacy Rows (For Backwards Compatibility)")]
        [SerializeField] private Text[] _rowLevelTexts = new Text[PlayerSkills.SlotCount];
        [SerializeField] private Text[] _rowStatTexts = new Text[PlayerSkills.SlotCount];
        [SerializeField] private Button[] _rowUpgradeButtons = new Button[PlayerSkills.SlotCount];
        [SerializeField] private Text[] _rowUpgradeButtonTexts = new Text[PlayerSkills.SlotCount];

        [System.Serializable]
        public struct SkillNodeIconEntry
        {
            public SkillNodeId NodeId;
            public Sprite Icon;
        }

        [Header("Skill Node Visuals & Icons")]
        [SerializeField] private SkillNodeIconEntry[] _nodeIcons = Array.Empty<SkillNodeIconEntry>();
        [SerializeField] private Sprite _iconFrameSprite;

        private static readonly Color LockedTint = new Color(0.72f, 0.76f, 0.84f, 1f);
        private static readonly Color UnlockedTint = Color.white;
        private static readonly Color LockBadgeColor = new Color(0.75f, 0.78f, 0.86f, 1f);
        private static readonly Color ActiveBadgeColor = new Color(1f, 0.84f, 0.28f, 1f);
        private static readonly Color MaxBadgeColor = new Color(0.32f, 0.92f, 0.52f, 1f);

        private static readonly Color TabNormalColor = new Color(0.12f, 0.15f, 0.24f, 0.95f);
        private static readonly Color TabActiveColor = new Color(0.24f, 0.32f, 0.52f, 1f);

        private PlayerSkills _playerSkills;
        private PlayerProgression _playerProgression;
        private Action _onClosed;
        private SkillBranchTab _currentTab = SkillBranchTab.Combat;

        private readonly Dictionary<SkillNodeId, NodeRowUIElements> _nodeElementMap = new Dictionary<SkillNodeId, NodeRowUIElements>();
        private bool _dynamicTreeBuilt;

        private class NodeRowUIElements
        {
            public Image Icon;
            public Image IconFrame;
            public Text LevelBadge;
            public Text StatSummary;
            public Button UpgradeButton;
            public Text UpgradeButtonText;
            public Image UpgradeButtonImage;
        }

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
            ResolveProgression();
            RefreshUI();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Subscribe<SkillStateChangedEvent>(OnSkillStateChanged);
            EventBus.Subscribe<SkillPointsChangedEvent>(OnSkillPointsChanged);
            EventBus.Subscribe<PlayerExpChangedEvent>(OnPlayerExpChanged);
            EventBus.Subscribe<PlayerLeveledUpEvent>(OnPlayerLeveledUp);
            EventBus.Subscribe<PlayerRespawnedEvent>(OnPlayerRespawned);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Unsubscribe<SkillStateChangedEvent>(OnSkillStateChanged);
            EventBus.Unsubscribe<SkillPointsChangedEvent>(OnSkillPointsChanged);
            EventBus.Unsubscribe<PlayerExpChangedEvent>(OnPlayerExpChanged);
            EventBus.Unsubscribe<PlayerLeveledUpEvent>(OnPlayerLeveledUp);
            EventBus.Unsubscribe<PlayerRespawnedEvent>(OnPlayerRespawned);
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void Update()
        {
            ResolveProgression();
            UpdateHotbarCooldowns();
        }

        public void Open(Action onClosed = null)
        {
            _onClosed = onClosed;

            if (_panelRoot == null)
            {
                Transform found = transform.Find("SkillTreePanel")
                               ?? transform.root.Find("SkillTreePanel")
                               ?? transform.root.GetComponentInChildren<RectTransform>(true)?.transform.Find("SkillTreePanel");
                if (found != null) _panelRoot = found.gameObject;
            }

            // Đảm bảo hiển thị Modal & đưa lên lớp trên cùng
            if (_panelRoot != null)
            {
                _panelRoot.transform.SetAsLastSibling();
                _panelRoot.SetActive(true);
            }

            // Bật Backdrop làm mờ màn hình nếu có
            Transform backdrop = transform.Find("DimBackdrop")
                              ?? transform.root.Find("DimBackdrop")
                              ?? transform.root.GetComponentInChildren<RectTransform>(true)?.transform.Find("DimBackdrop");
            if (backdrop != null)
            {
                backdrop.gameObject.SetActive(true);
            }

            ResolveProgression();
            BuildDynamicSkillTreeIfMissing();
            SelectTab(_currentTab);
            RefreshUI();

            Time.timeScale = 0f;
        }

        public void OpenWithBranch(SkillBranchTab tab, Action onClosed = null)
        {
            _currentTab = tab;
            Open(onClosed);
        }

        public void Close()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            Transform backdrop = transform.Find("DimBackdrop")
                              ?? transform.root.Find("DimBackdrop")
                              ?? transform.root.GetComponentInChildren<RectTransform>(true)?.transform.Find("DimBackdrop");
            if (backdrop != null && (!GameManager.HasInstance || GameManager.Instance.CurrentState == GameState.Playing))
            {
                backdrop.gameObject.SetActive(false);
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

        public void SelectTab(SkillBranchTab tab)
        {
            _currentTab = tab;

            if (_branchCombatContainer != null) _branchCombatContainer.SetActive(tab == SkillBranchTab.Combat);
            if (_branchMagicContainer != null) _branchMagicContainer.SetActive(tab == SkillBranchTab.Magic);
            if (_branchSurvivalContainer != null) _branchSurvivalContainer.SetActive(tab == SkillBranchTab.Survival);

            UpdateTabButtonVisual(_tabCombatButton, tab == SkillBranchTab.Combat);
            UpdateTabButtonVisual(_tabMagicButton, tab == SkillBranchTab.Magic);
            UpdateTabButtonVisual(_tabSurvivalButton, tab == SkillBranchTab.Survival);

            RefreshUI();
        }

        private void UpdateTabButtonVisual(Button btn, bool isActive)
        {
            if (btn == null) return;
            Image bg = btn.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = isActive ? TabActiveColor : TabNormalColor;
            }
        }

        public void RefreshUI()
        {
            ResolveProgression();

            // 1. Cập nhật Header: Điểm SP & Level của người chơi
            if (_coinsText != null)
            {
                if (_playerProgression != null)
                {
                    _coinsText.text = $"SP: {_playerProgression.AvailableSkillPoints}  |  LV.{_playerProgression.CurrentLevel}  (EXP: {_playerProgression.CurrentExp}/{_playerProgression.ExpToNextLevel})";
                }
                else
                {
                    int coins = GameManager.HasInstance ? GameManager.Instance.CurrentScore : 0;
                    _coinsText.text = $"COINS: {Mathf.Max(0, coins)}";
                }
            }

            // 2. Cập nhật Hotbar Active Magic Slots
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

                // Cập nhật các dòng legacy nếu còn dùng
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
                    SkillNodeId magicNodeId = (SkillNodeId)(3 + i);
                    bool canUpgrade = _playerProgression != null ? _playerProgression.CanUpgradeNode(magicNodeId) : false;
                    int curLv = _playerProgression != null ? _playerProgression.GetNodeLevel(magicNodeId) : level;
                    string btnLabel = curLv >= 3 ? "TỐI ĐA" : (canUpgrade ? "NÂNG CẤP (1 SP)" : "THIẾU ĐIỂM SP");

                    _rowUpgradeButtons[i].interactable = canUpgrade;
                    if (_rowUpgradeButtonTexts != null && i < _rowUpgradeButtonTexts.Length && _rowUpgradeButtonTexts[i] != null)
                    {
                        _rowUpgradeButtonTexts[i].text = btnLabel;
                    }
                }
            }

            // 3. Cập nhật 10 Node Cards
            if (_playerProgression != null)
            {
                foreach (var kvp in _nodeElementMap)
                {
                    SkillNodeId nodeId = kvp.Key;
                    NodeRowUIElements ui = kvp.Value;
                    if (ui == null) continue;

                    int curLv = _playerProgression.GetNodeLevel(nodeId);
                    bool isMax = curLv >= 3;
                    bool canUpgrade = _playerProgression.CanUpgradeNode(nodeId);

                    // Trực quan hóa Icon và Khung viền theo cấp độ
                    if (ui.Icon != null)
                    {
                        ui.Icon.color = curLv == 0 ? new Color(0.40f, 0.44f, 0.54f, 0.85f) : Color.white;
                    }
                    if (ui.IconFrame != null)
                    {
                        if (isMax)
                        {
                            ui.IconFrame.color = new Color(1f, 0.84f, 0.28f, 1f); // Viền vàng kim rực rỡ khi Max
                        }
                        else if (curLv > 0)
                        {
                            ui.IconFrame.color = Color.white;
                        }
                    }

                    if (ui.LevelBadge != null)
                    {
                        ui.LevelBadge.text = curLv == 0 ? "CHƯA HỌC" : (isMax ? "TỐI ĐA (MAX)" : $"CẤP {curLv}/3");
                        ui.LevelBadge.color = curLv == 0 ? LockBadgeColor : (isMax ? MaxBadgeColor : ActiveBadgeColor);
                    }

                    if (ui.UpgradeButton != null)
                    {
                        ui.UpgradeButton.interactable = canUpgrade;
                    }

                    if (ui.UpgradeButtonImage != null)
                    {
                        if (isMax)
                        {
                            ui.UpgradeButtonImage.color = new Color(0.28f, 0.25f, 0.16f, 0.9f);
                        }
                        else if (canUpgrade)
                        {
                            ui.UpgradeButtonImage.color = new Color(0.22f, 0.68f, 0.36f, 1f);
                        }
                        else if (_playerProgression.AvailableSkillPoints < 1)
                        {
                            ui.UpgradeButtonImage.color = new Color(0.20f, 0.22f, 0.30f, 0.85f);
                        }
                        else
                        {
                            ui.UpgradeButtonImage.color = new Color(0.35f, 0.18f, 0.18f, 0.85f);
                        }
                    }

                    if (ui.UpgradeButtonText != null)
                    {
                        if (isMax)
                        {
                            ui.UpgradeButtonText.text = "ĐÃ ĐẠT MAX";
                            ui.UpgradeButtonText.color = new Color(0.85f, 0.78f, 0.40f, 1f);
                        }
                        else if (canUpgrade)
                        {
                            ui.UpgradeButtonText.text = "NÂNG CẤP (1 SP)";
                            ui.UpgradeButtonText.color = Color.white;
                        }
                        else if (_playerProgression.AvailableSkillPoints < 1)
                        {
                            ui.UpgradeButtonText.text = "CẦN 1 SP";
                            ui.UpgradeButtonText.color = new Color(0.70f, 0.74f, 0.82f, 1f);
                        }
                        else
                        {
                            ui.UpgradeButtonText.text = "CHƯA ĐỦ ĐK";
                            ui.UpgradeButtonText.color = new Color(1f, 0.60f, 0.60f, 1f);
                        }
                    }
                }
            }

            UpdateHotbarCooldowns();
        }

        private void BuildDynamicSkillTreeIfMissing()
        {
            if (_dynamicTreeBuilt) return;

            ResolveProgression();
            if (_panelRoot == null)
            {
                Transform found = transform.Find("SkillTreePanel")
                               ?? transform.root.Find("SkillTreePanel")
                               ?? transform.root.GetComponentInChildren<RectTransform>(true)?.transform.Find("SkillTreePanel");
                if (found != null) _panelRoot = found.gameObject;
            }

            if (_panelRoot == null || _playerProgression == null) return;

            Transform contentTr = _panelRoot.transform.Find("ContentArea") ??
                                  _panelRoot.transform.Find("Window_5/ContentArea") ??
                                  _panelRoot.transform.Find("Content") ??
                                  _panelRoot.transform.Find("Body");
            Transform root = contentTr != null ? contentTr : _panelRoot.transform;

            // 1. Tìm hoặc dựng Tab Bar nếu chưa có
            Transform existingTabBar = root.Find("BranchTabBar");
            if (existingTabBar == null && _tabCombatButton == null)
            {
                GameObject tabBar = CreateUIObject("BranchTabBar", root);
                tabBar.transform.SetSiblingIndex(1); // Ngay dưới CoinsBalanceText

                HorizontalLayoutGroup hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 12f;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;

                RectTransform tabRt = GetOrAddRectTransform(tabBar);
                tabRt.sizeDelta = new Vector2(760f, 52f);

                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                _tabCombatButton = CreateTabButton(tabBar.transform, "TabCombat", "⚔️ CHIẾN BINH", font, () => SelectTab(SkillBranchTab.Combat));
                _tabMagicButton = CreateTabButton(tabBar.transform, "TabMagic", "🔮 PHÁP THUẬT", font, () => SelectTab(SkillBranchTab.Magic));
                _tabSurvivalButton = CreateTabButton(tabBar.transform, "TabSurvival", "🛡️ SINH TỒN", font, () => SelectTab(SkillBranchTab.Survival));
            }

            // 2. Tìm hoặc dựng 3 containers cho 3 nhánh
            if (_branchCombatContainer == null)
            {
                _branchCombatContainer = EnsureBranchContainer(root, "Branch_Combat");
                BuildNodeRowsForBranch(_branchCombatContainer.transform, new[]
                {
                    SkillNodeId.Combat_StaminaBoost,
                    SkillNodeId.Combat_Strength,
                    SkillNodeId.Combat_SwiftDash
                });
            }

            if (_branchMagicContainer == null)
            {
                _branchMagicContainer = EnsureBranchContainer(root, "Branch_Magic");
                BuildNodeRowsForBranch(_branchMagicContainer.transform, new[]
                {
                    SkillNodeId.Magic_Fireball,
                    SkillNodeId.Magic_Lightning,
                    SkillNodeId.Magic_Shield,
                    SkillNodeId.Magic_EarthSpikes
                });
            }

            if (_branchSurvivalContainer == null)
            {
                _branchSurvivalContainer = EnsureBranchContainer(root, "Branch_Survival");
                BuildNodeRowsForBranch(_branchSurvivalContainer.transform, new[]
                {
                    SkillNodeId.Survival_Vitality,
                    SkillNodeId.Survival_BottomlessBag,
                    SkillNodeId.Survival_Fortune
                });
            }

            // 3. Ẩn các dòng Legacy nếu containers mới đã tồn tại
            if (_rowUpgradeButtons != null && _rowUpgradeButtons.Length > 0 && _rowUpgradeButtons[0] != null)
            {
                for (int i = 0; i < _rowUpgradeButtons.Length; i++)
                {
                    if (_rowUpgradeButtons[i] != null)
                    {
                        Transform rowParent = _rowUpgradeButtons[i].transform.parent;
                        if (rowParent != null && rowParent.name.StartsWith("SkillRow_") && !rowParent.name.Contains("Branch"))
                        {
                            rowParent.gameObject.SetActive(false);
                        }
                    }
                }
            }

            // 4. Dựng nút Reset & Done ở đáy nếu chưa có
            Transform existingBottom = root.Find("BottomActionRow");
            if (existingBottom == null && _resetTreeButton == null)
            {
                GameObject btmRow = CreateUIObject("BottomActionRow", root);
                HorizontalLayoutGroup btmHlg = btmRow.AddComponent<HorizontalLayoutGroup>();
                btmHlg.spacing = 18f;
                btmHlg.childAlignment = TextAnchor.MiddleCenter;
                btmHlg.childControlWidth = false;
                btmHlg.childControlHeight = false;

                RectTransform btmRt = GetOrAddRectTransform(btmRow);
                btmRt.sizeDelta = new Vector2(760f, 54f);

                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                _resetTreeButton = CreateActionButton(btmRow.transform, "ResetButton", "TẨY ĐIỂM (RESET)", new Color(0.85f, 0.32f, 0.28f, 1f), font, new Vector2(300f, 48f));
                _resetTreeButton.onClick.AddListener(OnResetTreeClicked);
            }

            _dynamicTreeBuilt = true;
        }

        private GameObject EnsureBranchContainer(Transform root, string name)
        {
            Transform existing = root.Find(name);
            if (existing != null) return existing.gameObject;

            GameObject container = CreateUIObject(name, root);

            VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            RectTransform rt = GetOrAddRectTransform(container);
            rt.sizeDelta = new Vector2(760f, 380f);
            return container;
        }

        private static GameObject CreateUIObject(string name, Transform parent = null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform GetOrAddRectTransform(GameObject go)
        {
            if (go == null) return null;
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                rt = go.AddComponent<RectTransform>();
            }
            return rt;
        }

        private void BuildNodeRowsForBranch(Transform container, SkillNodeId[] nodes)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite frameSprite = GetIconFrameSprite();

            foreach (var nodeId in nodes)
            {
                if (!_playerProgression.Nodes.TryGetValue(nodeId, out var nodeData)) continue;

                GameObject rowObj = CreateUIObject($"NodeRow_{nodeId}", container);

                Image rowBg = rowObj.AddComponent<Image>();
                rowBg.color = new Color(0.08f, 0.11f, 0.20f, 0.94f);

                RectTransform rowRt = GetOrAddRectTransform(rowObj);
                rowRt.sizeDelta = new Vector2(760f, 92f);

                Color branchColor = GetBranchColor(nodeData.BranchName);

                // 1. Icon Box bên trái (68x68 px)
                GameObject iconBoxObj = CreateUIObject("IconBox", rowObj.transform);
                RectTransform iconBoxRt = GetOrAddRectTransform(iconBoxObj);
                iconBoxRt.anchorMin = new Vector2(0f, 0.5f);
                iconBoxRt.anchorMax = new Vector2(0f, 0.5f);
                iconBoxRt.pivot = new Vector2(0f, 0.5f);
                iconBoxRt.sizeDelta = new Vector2(68f, 68f);
                iconBoxRt.anchoredPosition = new Vector2(14f, 0f);

                Image iconBoxBg = iconBoxObj.AddComponent<Image>();
                iconBoxBg.color = new Color(0.04f, 0.06f, 0.12f, 1f);

                // Hình ảnh Skill Icon bên trong
                GameObject iconImgObj = CreateUIObject("IconImage", iconBoxObj.transform);
                Image iconImg = iconImgObj.AddComponent<Image>();
                iconImg.sprite = GetNodeIcon(nodeId);
                iconImg.preserveAspect = true;
                iconImg.color = LockedTint;
                RectTransform iconImgRt = GetOrAddRectTransform(iconImgObj);
                iconImgRt.anchorMin = new Vector2(0.5f, 0.5f);
                iconImgRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconImgRt.sizeDelta = new Vector2(52f, 52f);

                // Khung viền pixel frame bao quanh icon
                GameObject frameObj = CreateUIObject("IconFrame", iconBoxObj.transform);
                Image frameImg = frameObj.AddComponent<Image>();
                if (frameSprite != null)
                {
                    frameImg.sprite = frameSprite;
                    frameImg.color = branchColor;
                }
                else
                {
                    frameImg.color = new Color(branchColor.r, branchColor.g, branchColor.b, 0.35f);
                }
                RectTransform frameRt = GetOrAddRectTransform(frameObj);
                frameRt.anchorMin = Vector2.zero;
                frameRt.anchorMax = Vector2.one;
                frameRt.offsetMin = Vector2.zero;
                frameRt.offsetMax = Vector2.zero;

                // 2. Tên & Chi nhánh
                GameObject titleObj = CreateUIObject("Title", rowObj.transform);
                Text titleTxt = titleObj.AddComponent<Text>();
                titleTxt.text = $"{nodeData.Name}";
                titleTxt.font = font;
                titleTxt.fontSize = 20;
                titleTxt.fontStyle = FontStyle.Bold;
                titleTxt.color = branchColor;
                titleTxt.alignment = TextAnchor.UpperLeft;
                RectTransform titleRt = GetOrAddRectTransform(titleObj);
                titleRt.anchorMin = new Vector2(0f, 0.5f);
                titleRt.anchorMax = new Vector2(1f, 1f);
                titleRt.offsetMin = new Vector2(96f, 4f);
                titleRt.offsetMax = new Vector2(-330f, -8f);

                // 3. Mô tả tác dụng
                GameObject descObj = CreateUIObject("Desc", rowObj.transform);
                Text descTxt = descObj.AddComponent<Text>();
                descTxt.text = nodeData.Description;
                descTxt.font = font;
                descTxt.fontSize = 14;
                descTxt.color = new Color(0.78f, 0.82f, 0.92f, 1f);
                descTxt.alignment = TextAnchor.LowerLeft;
                RectTransform descRt = GetOrAddRectTransform(descObj);
                descRt.anchorMin = new Vector2(0f, 0f);
                descRt.anchorMax = new Vector2(1f, 0.5f);
                descRt.offsetMin = new Vector2(96f, 8f);
                descRt.offsetMax = new Vector2(-330f, -2f);

                // 4. Cấp độ Badge
                GameObject badgeObj = CreateUIObject("LevelBadge", rowObj.transform);
                RectTransform badgeRt = GetOrAddRectTransform(badgeObj);
                badgeRt.anchorMin = new Vector2(1f, 0.5f);
                badgeRt.anchorMax = new Vector2(1f, 0.5f);
                badgeRt.pivot = new Vector2(1f, 0.5f);
                badgeRt.sizeDelta = new Vector2(115f, 40f);
                badgeRt.anchoredPosition = new Vector2(-195f, 0f);

                Image badgeBg = badgeObj.AddComponent<Image>();
                badgeBg.color = new Color(0.05f, 0.07f, 0.14f, 0.95f);

                GameObject badgeTxtObj = CreateUIObject("Text", badgeObj.transform);
                Text badgeTxt = badgeTxtObj.AddComponent<Text>();
                badgeTxt.text = "CHƯA HỌC";
                badgeTxt.font = font;
                badgeTxt.fontSize = 16;
                badgeTxt.fontStyle = FontStyle.Bold;
                badgeTxt.color = LockBadgeColor;
                badgeTxt.alignment = TextAnchor.MiddleCenter;
                RectTransform badgeTxtRt = GetOrAddRectTransform(badgeTxtObj);
                badgeTxtRt.anchorMin = Vector2.zero;
                badgeTxtRt.anchorMax = Vector2.one;
                badgeTxtRt.offsetMin = Vector2.zero;
                badgeTxtRt.offsetMax = Vector2.zero;

                // 5. Nút nâng cấp
                SkillNodeId capturedId = nodeId;
                Button upgBtn = CreateActionButton(rowObj.transform, "UpgradeButton", "NÂNG CẤP (1 SP)", new Color(0.24f, 0.65f, 0.38f, 1f), font, new Vector2(175f, 52f));
                RectTransform upgRt = GetOrAddRectTransform(upgBtn.gameObject);
                upgRt.anchorMin = new Vector2(1f, 0.5f);
                upgRt.anchorMax = new Vector2(1f, 0.5f);
                upgRt.pivot = new Vector2(1f, 0.5f);
                upgRt.anchoredPosition = new Vector2(-12f, 0f);
                upgBtn.onClick.AddListener(() => OnNodeUpgradeClicked(capturedId));

                Text upgTxt = upgBtn.transform.Find("Label")?.GetComponent<Text>();
                Image upgImg = upgBtn.GetComponent<Image>();

                _nodeElementMap[nodeId] = new NodeRowUIElements
                {
                    Icon = iconImg,
                    IconFrame = frameImg,
                    LevelBadge = badgeTxt,
                    StatSummary = descTxt,
                    UpgradeButton = upgBtn,
                    UpgradeButtonText = upgTxt,
                    UpgradeButtonImage = upgImg
                };
            }
        }

        public Sprite GetNodeIcon(SkillNodeId nodeId)
        {
            if (_nodeIcons != null)
            {
                for (int i = 0; i < _nodeIcons.Length; i++)
                {
                    if (_nodeIcons[i].NodeId == nodeId && _nodeIcons[i].Icon != null)
                    {
                        return _nodeIcons[i].Icon;
                    }
                }
            }

#if UNITY_EDITOR
            string assetPath = GetIconAssetPath(nodeId);
            if (!string.IsNullOrEmpty(assetPath))
            {
                Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sp != null) return sp;
            }
#endif

            // Fallback sang hotbar icons nếu là skill phép
            int magicSlot = (int)nodeId - 3;
            if (magicSlot >= 0 && magicSlot < _hotbarIcons.Length && _hotbarIcons[magicSlot] != null)
            {
                return _hotbarIcons[magicSlot].sprite;
            }

            return null;
        }

        public Sprite GetIconFrameSprite()
        {
            if (_iconFrameSprite != null) return _iconFrameSprite;

#if UNITY_EDITOR
            _iconFrameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/UI/1 Frames/Portrait_frame.png");
            if (_iconFrameSprite != null) return _iconFrameSprite;
#endif
            return null;
        }

        private static string GetIconAssetPath(SkillNodeId nodeId)
        {
            switch (nodeId)
            {
                case SkillNodeId.Combat_StaminaBoost:
                    return "Assets/GameAssets/Player-Skills/Icons/2-Lightning-bolt.png";
                case SkillNodeId.Combat_Strength:
                    return "Assets/GameAssets/Player-Skills/Icons/4-Sun-strike.png";
                case SkillNodeId.Combat_SwiftDash:
                    return "Assets/GameAssets/Player-Skills/Icons/9-Black-hole.png";
                case SkillNodeId.Magic_Fireball:
                    return "Assets/GameAssets/Player-Skills/Icons/10-Fire-ball.png";
                case SkillNodeId.Magic_Lightning:
                    return "Assets/GameAssets/Player-Skills/Icons/1-Lightning.png";
                case SkillNodeId.Magic_Shield:
                    return "Assets/GameAssets/Player-Skills/Icons/8-Shield.png";
                case SkillNodeId.Magic_EarthSpikes:
                    return "Assets/GameAssets/Player-Skills/Icons/6-Spikes.png";
                case SkillNodeId.Survival_Vitality:
                    return "Assets/GameAssets/UI/3 Icons/Icons_43.png";
                case SkillNodeId.Survival_BottomlessBag:
                    return "Assets/GameAssets/UI/3 Icons/Icons_28.png";
                case SkillNodeId.Survival_Fortune:
                    return "Assets/GameAssets/Player-Skills/Icons/3-Midas-touch.png";
                default:
                    return null;
            }
        }

        private Color GetBranchColor(string branch)
        {
            if (branch.Contains("Chiến")) return new Color(1f, 0.45f, 0.35f, 1f);
            if (branch.Contains("Pháp")) return new Color(0.42f, 0.82f, 1f, 1f);
            return new Color(0.42f, 0.91f, 0.48f, 1f);
        }

        private Button CreateTabButton(Transform parent, string name, string label, Font font, UnityEngine.Events.UnityAction onClick)
        {
            Button btn = CreateActionButton(parent, name, label, TabNormalColor, font, new Vector2(236f, 44f));
            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }

        private Button CreateActionButton(Transform parent, string name, string label, Color bgColor, Font font, Vector2 size)
        {
            GameObject obj = CreateUIObject(name, parent);

            Image img = obj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            RectTransform rt = GetOrAddRectTransform(obj);
            rt.sizeDelta = size;

            GameObject lblObj = CreateUIObject("Label", obj.transform);
            Text txt = lblObj.AddComponent<Text>();
            txt.text = label;
            txt.font = font;
            txt.fontSize = 17;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            RectTransform lblRt = GetOrAddRectTransform(lblObj);
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;

            return btn;
        }

        private void OnNodeUpgradeClicked(SkillNodeId nodeId)
        {
            ResolveProgression();
            if (_playerProgression != null && _playerProgression.TryUpgradeNode(nodeId))
            {
                RefreshUI();
            }
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
            if (_resetTreeButton != null) _resetTreeButton.onClick.AddListener(OnResetTreeClicked);

            if (_tabCombatButton != null) _tabCombatButton.onClick.AddListener(() => SelectTab(SkillBranchTab.Combat));
            if (_tabMagicButton != null) _tabMagicButton.onClick.AddListener(() => SelectTab(SkillBranchTab.Magic));
            if (_tabSurvivalButton != null) _tabSurvivalButton.onClick.AddListener(() => SelectTab(SkillBranchTab.Survival));

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
            if (_resetTreeButton != null) _resetTreeButton.onClick.RemoveListener(OnResetTreeClicked);

            if (_tabCombatButton != null) _tabCombatButton.onClick.RemoveAllListeners();
            if (_tabMagicButton != null) _tabMagicButton.onClick.RemoveAllListeners();
            if (_tabSurvivalButton != null) _tabSurvivalButton.onClick.RemoveAllListeners();

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
            if (IsOpen) return;
            if (GameManager.HasInstance && GameManager.Instance.CurrentState != GameState.Playing) return;

            ResolveProgression();

            // Nếu kỹ năng đã mở (cấp >= 1): Thi triển chiêu thức ngay
            int skillLevel = _playerSkills != null ? _playerSkills.GetSkillLevel(slot) : 0;
            if (skillLevel > 0)
            {
                _playerSkills.TryCastSkill(slot);
                return;
            }

            // Nếu kỹ năng đang KHÓA (Cấp 0): Tự động mở Cây Kỹ Năng ở tab Pháp Thuật để người chơi nâng cấp ngay!
            OpenWithBranch(SkillBranchTab.Magic);
            EventBus.Publish(new HudToastMessageEvent("KỸ NĂNG ĐANG KHÓA! HÃY DÙNG ĐIỂM SP ĐỂ MỞ KHÓA.", new Color(1f, 0.84f, 0.28f, 1f)));
        }

        private void OnUpgradeSlotClicked(int slot)
        {
            ResolveProgression();
            if (_playerProgression != null)
            {
                SkillNodeId nodeId = (SkillNodeId)(3 + slot);
                if (_playerProgression.TryUpgradeNode(nodeId))
                {
                    RefreshUI();
                    return;
                }
            }

            if (_playerSkills != null && _playerSkills.TryUnlockOrUpgradeSkill(slot))
            {
                RefreshUI();
            }
        }

        private void OnResetTreeClicked()
        {
            ResolveProgression();
            if (_playerProgression != null)
            {
                _playerProgression.ResetSkillTree();
                RefreshUI();
            }
        }

        private void OnScoreChanged(ScoreChangedEvent evt) => RefreshUI();
        private void OnSkillStateChanged(SkillStateChangedEvent evt) => RefreshUI();
        private void OnSkillPointsChanged(SkillPointsChangedEvent evt) => RefreshUI();
        private void OnPlayerExpChanged(PlayerExpChangedEvent evt) => RefreshUI();
        private void OnPlayerLeveledUp(PlayerLeveledUpEvent evt) => RefreshUI();
        private void OnPlayerRespawned(PlayerRespawnedEvent evt) => RefreshUI();

        private void ResolveProgression()
        {
            if (_playerProgression != null && _playerSkills != null) return;

            PlayerController controller = FindAnyObjectByType<PlayerController>();
            if (controller != null)
            {
                if (_playerProgression == null) _playerProgression = controller.Progression;
                if (_playerSkills == null) _playerSkills = controller.Skills;
            }

            if (_playerProgression == null) _playerProgression = FindAnyObjectByType<PlayerProgression>();
            if (_playerSkills == null) _playerSkills = FindAnyObjectByType<PlayerSkills>();
        }
    }
}
