using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Platformer2D.Core.Architecture;
using Platformer2D.Managers;
using Platformer2D.Player.Components;
using Platformer2D.Player.Controllers;

namespace Platformer2D.UI
{
    /// <summary>
    /// Quản lý giao diện HUD & các bảng trạng thái trong màn chơi:
    /// - Thanh máu Pixel Art + chữ HP, bộ đếm Đá ném, bộ đếm Coins, nút Skills & Pause.
    /// - Thông báo nhanh khi lưu Checkpoint, nhặt Đá hoặc mở khóa/nâng cấp kỹ năng.
    /// - Bảng Skill Upgrades (Tab), Pause (Esc), Settings, Game Over và Victory.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [Header("Health UI")]
        [SerializeField] private Image _healthBarFill;
        [SerializeField] private Text _healthText;

        [Header("Score, Ammo & Top Controls")]
        [SerializeField] private Text _rockText;
        [SerializeField] private Text _scoreText;
        [SerializeField] private Button _skillsButton;
        [SerializeField] private Button _pauseButton;

        [Header("Skill System UI")]
        [SerializeField] private SkillTreePanelUI _skillTreePanel;

        [Header("Checkpoint Toast")]
        [SerializeField] private GameObject _toastContainer;
        [SerializeField] private Text _toastText;

        [Header("Overlays & Backdrop")]
        [SerializeField] private GameObject _dimBackdrop;

        [Header("Pause Panel")]
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _pauseCloseButton;
        [SerializeField] private Button _pauseRestartButton;
        [SerializeField] private Button _pauseSettingsButton;
        [SerializeField] private Button _pauseMainMenuButton;

        [Header("Settings Panel")]
        [SerializeField] private SettingsPanelUI _settingsPanel;

        [Header("Game Over Screen")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Text _gameOverCoinsText;
        [SerializeField] private Button _checkpointRespawnButton;
        [SerializeField] private Button _gameOverRestartButton;
        [SerializeField] private Button _gameOverMainMenuButton;

        [Header("Victory Screen")]
        [SerializeField] private GameObject _victoryPanel;
        [SerializeField] private Text _victoryCoinsText;
        [SerializeField] private Button _victoryRestartButton;
        [SerializeField] private Button _victoryMainMenuButton;

        private Color _baseFillColor = Color.white;
        private Color _baseToastOutlineColor = new Color(0.282f, 0.808f, 0.431f, 1f);
        private Outline _toastOutline;
        private int _lastKnownHealth = -1;
        private Coroutine _healthFlashCoroutine;
        private Coroutine _toastCoroutine;

        private void Awake()
        {
            if (_healthBarFill != null)
            {
                _baseFillColor = _healthBarFill.color;
            }
            if (_toastContainer != null)
            {
                _toastOutline = _toastContainer.GetComponent<Outline>();
                if (_toastOutline != null)
                {
                    _baseToastOutlineColor = _toastOutline.effectColor;
                }
            }

            BindButtons();
            HideAllOverlays();
            if (_toastContainer != null)
            {
                _toastContainer.SetActive(false);
            }
        }

        private void Start()
        {
            SyncInitialState();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<RockCountChangedEvent>(OnRockCountChanged);
            EventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Subscribe<CheckpointActivatedEvent>(OnCheckpointActivated);
            EventBus.Subscribe<HudToastMessageEvent>(OnHudToastMessage);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<RockCountChangedEvent>(OnRockCountChanged);
            EventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Unsubscribe<CheckpointActivatedEvent>(OnCheckpointActivated);
            EventBus.Unsubscribe<HudToastMessageEvent>(OnHudToastMessage);
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void Update()
        {
            if (SceneLoader.HasInstance && SceneLoader.Instance.IsLoading) return;

            bool tabPressed = (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) ||
                              (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame);

            if (tabPressed && GameManager.HasInstance && GameManager.Instance.CurrentState == GameState.Playing)
            {
                ToggleSkillTree();
                return;
            }

            bool escapePressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                                 (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

            if (!escapePressed) return;

            if (_skillTreePanel != null && _skillTreePanel.IsOpen)
            {
                _skillTreePanel.Close();
                return;
            }

            if (_settingsPanel != null && _settingsPanel.IsOpen)
            {
                _settingsPanel.Close();
                return;
            }

            if (GameManager.HasInstance)
            {
                GameState state = GameManager.Instance.CurrentState;
                if (state == GameState.Playing || state == GameState.Paused)
                {
                    GameManager.Instance.TogglePause();
                }
            }
        }

        private void BindButtons()
        {
            if (_skillsButton != null) _skillsButton.onClick.AddListener(OnSkillsButtonClicked);
            if (_pauseButton != null) _pauseButton.onClick.AddListener(OnPauseButtonClicked);
            if (_resumeButton != null) _resumeButton.onClick.AddListener(OnResumeClicked);
            if (_pauseCloseButton != null) _pauseCloseButton.onClick.AddListener(OnResumeClicked);
            if (_pauseRestartButton != null) _pauseRestartButton.onClick.AddListener(OnRestartClicked);
            if (_pauseSettingsButton != null) _pauseSettingsButton.onClick.AddListener(OnPauseSettingsClicked);
            if (_pauseMainMenuButton != null) _pauseMainMenuButton.onClick.AddListener(OnMainMenuClicked);

            if (_checkpointRespawnButton != null) _checkpointRespawnButton.onClick.AddListener(OnCheckpointRespawnClicked);
            if (_gameOverRestartButton != null) _gameOverRestartButton.onClick.AddListener(OnRestartClicked);
            if (_gameOverMainMenuButton != null) _gameOverMainMenuButton.onClick.AddListener(OnMainMenuClicked);

            if (_victoryRestartButton != null) _victoryRestartButton.onClick.AddListener(OnRestartClicked);
            if (_victoryMainMenuButton != null) _victoryMainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        private void UnbindButtons()
        {
            if (_skillsButton != null) _skillsButton.onClick.RemoveListener(OnSkillsButtonClicked);
            if (_pauseButton != null) _pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
            if (_resumeButton != null) _resumeButton.onClick.RemoveListener(OnResumeClicked);
            if (_pauseCloseButton != null) _pauseCloseButton.onClick.RemoveListener(OnResumeClicked);
            if (_pauseRestartButton != null) _pauseRestartButton.onClick.RemoveListener(OnRestartClicked);
            if (_pauseSettingsButton != null) _pauseSettingsButton.onClick.RemoveListener(OnPauseSettingsClicked);
            if (_pauseMainMenuButton != null) _pauseMainMenuButton.onClick.RemoveListener(OnMainMenuClicked);

            if (_checkpointRespawnButton != null) _checkpointRespawnButton.onClick.RemoveListener(OnCheckpointRespawnClicked);
            if (_gameOverRestartButton != null) _gameOverRestartButton.onClick.RemoveListener(OnRestartClicked);
            if (_gameOverMainMenuButton != null) _gameOverMainMenuButton.onClick.RemoveListener(OnMainMenuClicked);

            if (_victoryRestartButton != null) _victoryRestartButton.onClick.RemoveListener(OnRestartClicked);
            if (_victoryMainMenuButton != null) _victoryMainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }

        private void SyncInitialState()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                _lastKnownHealth = player.CurrentHealth;
                ApplyHealthUI(player.CurrentHealth, player.MaxHealth, false);

                PlayerCombat combat = player.GetComponent<PlayerCombat>();
                if (combat != null)
                {
                    ApplyRockUI(combat.CurrentRocks);
                }
            }

            int score = GameManager.HasInstance ? GameManager.Instance.CurrentScore : 0;
            ApplyScoreUI(score);

            if (GameManager.HasInstance)
            {
                ApplyGameStateUI(GameManager.Instance.CurrentState);
            }
        }

        private void OnHealthChanged(PlayerHealthChangedEvent evt)
        {
            bool flash = _lastKnownHealth >= 0 && evt.CurrentHealth != _lastKnownHealth;
            bool isHeal = evt.CurrentHealth > _lastKnownHealth;
            _lastKnownHealth = evt.CurrentHealth;

            ApplyHealthUI(evt.CurrentHealth, evt.MaxHealth, flash, isHeal);
        }

        private void ApplyHealthUI(int currentHealth, int maxHealth, bool flash, bool isHeal = false)
        {
            int clampedMax = Mathf.Max(1, maxHealth);
            int clampedCurrent = Mathf.Clamp(currentHealth, 0, clampedMax);

            if (_healthBarFill != null)
            {
                _healthBarFill.fillAmount = (float)clampedCurrent / clampedMax;

                if (flash && gameObject.activeInHierarchy)
                {
                    if (_healthFlashCoroutine != null) StopCoroutine(_healthFlashCoroutine);
                    Color flashColor = isHeal ? new Color(0.55f, 1f, 0.65f, 1f) : new Color(1f, 0.72f, 0.72f, 1f);
                    _healthFlashCoroutine = StartCoroutine(FlashHealthBarRoutine(flashColor));
                }
            }

            if (_healthText != null)
            {
                _healthText.text = $"{clampedCurrent}/{clampedMax}";
            }
        }

        private IEnumerator FlashHealthBarRoutine(Color flashColor)
        {
            if (_healthBarFill == null) yield break;

            _healthBarFill.color = flashColor;
            float elapsed = 0f;
            const float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_healthBarFill != null)
                {
                    _healthBarFill.color = Color.Lerp(flashColor, _baseFillColor, Mathf.Clamp01(elapsed / duration));
                }
                yield return null;
            }

            if (_healthBarFill != null)
            {
                _healthBarFill.color = _baseFillColor;
            }
            _healthFlashCoroutine = null;
        }

        private void OnRockCountChanged(RockCountChangedEvent evt)
        {
            ApplyRockUI(evt.CurrentRocks);
        }

        private void ApplyRockUI(int currentRocks)
        {
            if (_rockText != null)
            {
                _rockText.text = Mathf.Max(0, currentRocks).ToString();
            }
        }

        private void OnScoreChanged(ScoreChangedEvent evt)
        {
            ApplyScoreUI(evt.CurrentScore);
        }

        private void ApplyScoreUI(int score)
        {
            string formatted = Mathf.Max(0, score).ToString();
            if (_scoreText != null)
            {
                _scoreText.text = formatted;
            }

            string coinsLine = $"COINS: {formatted}";
            if (_gameOverCoinsText != null) _gameOverCoinsText.text = coinsLine;
            if (_victoryCoinsText != null) _victoryCoinsText.text = coinsLine;
        }

        private void OnCheckpointActivated(CheckpointActivatedEvent evt)
        {
            ShowCustomToast("CHECKPOINT SAVED!", _baseToastOutlineColor);
        }

        private void OnHudToastMessage(HudToastMessageEvent evt)
        {
            ShowCustomToast(evt.Message, evt.AccentColor);
        }

        private void ShowCustomToast(string message, Color accentColor)
        {
            if (_toastContainer == null || !gameObject.activeInHierarchy) return;

            if (_toastText != null)
            {
                _toastText.text = message;
                _toastText.color = accentColor;
            }
            if (_toastOutline != null)
            {
                _toastOutline.effectColor = accentColor;
            }

            if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
            _toastCoroutine = StartCoroutine(ShowToastRoutine());
        }

        private IEnumerator ShowToastRoutine()
        {
            _toastContainer.SetActive(true);
            yield return new WaitForSecondsRealtime(2.0f);
            if (_toastContainer != null)
            {
                _toastContainer.SetActive(false);
            }
            _toastCoroutine = null;
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            ApplyGameStateUI(evt.NewState);
        }

        private void ApplyGameStateUI(GameState state)
        {
            HideAllOverlays();

            int currentCoins = GameManager.HasInstance ? GameManager.Instance.CurrentScore : 0;
            ApplyScoreUI(currentCoins);

            switch (state)
            {
                case GameState.Paused:
                    if (_dimBackdrop != null) _dimBackdrop.SetActive(true);
                    if (_pausePanel != null) _pausePanel.SetActive(true);
                    break;

                case GameState.GameOver:
                    if (_dimBackdrop != null) _dimBackdrop.SetActive(true);
                    if (_checkpointRespawnButton != null)
                    {
                        _checkpointRespawnButton.gameObject.SetActive(CheckpointManager.HasInstance);
                    }
                    if (_gameOverPanel != null) _gameOverPanel.SetActive(true);
                    break;

                case GameState.Victory:
                    if (_dimBackdrop != null) _dimBackdrop.SetActive(true);
                    if (_victoryPanel != null) _victoryPanel.SetActive(true);
                    break;
            }
        }

        private void HideAllOverlays()
        {
            if (_dimBackdrop != null) _dimBackdrop.SetActive(false);
            if (_pausePanel != null) _pausePanel.SetActive(false);
            if (_settingsPanel != null) _settingsPanel.gameObject.SetActive(false);
            if (_skillTreePanel != null && _skillTreePanel.IsOpen) _skillTreePanel.Close();
            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
            if (_victoryPanel != null) _victoryPanel.SetActive(false);
        }

        private void OnSkillsButtonClicked()
        {
            if (GameManager.HasInstance && GameManager.Instance.CurrentState != GameState.Playing) return;
            ToggleSkillTree();
        }

        private void ToggleSkillTree()
        {
            if (_skillTreePanel == null) return;

            if (_skillTreePanel.IsOpen)
            {
                _skillTreePanel.Close();
                return;
            }

            if (_dimBackdrop != null) _dimBackdrop.SetActive(true);
            _skillTreePanel.Open(() =>
            {
                if (_dimBackdrop != null && (!GameManager.HasInstance || GameManager.Instance.CurrentState == GameState.Playing))
                {
                    _dimBackdrop.SetActive(false);
                }
            });
        }

        private void OnPauseButtonClicked()
        {
            if (GameManager.HasInstance && GameManager.Instance.CurrentState == GameState.Playing)
            {
                GameManager.Instance.SetState(GameState.Paused);
            }
        }

        private void OnResumeClicked()
        {
            if (GameManager.HasInstance && GameManager.Instance.CurrentState == GameState.Paused)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }
        }

        private void OnPauseSettingsClicked()
        {
            if (_settingsPanel == null) return;

            if (_pausePanel != null) _pausePanel.SetActive(false);
            _settingsPanel.Open(() =>
            {
                if (_pausePanel != null && GameManager.HasInstance && GameManager.Instance.CurrentState == GameState.Paused)
                {
                    _pausePanel.SetActive(true);
                }
            });
        }

        private void OnCheckpointRespawnClicked()
        {
            if (CheckpointManager.HasInstance)
            {
                CheckpointManager.Instance.RespawnNow();
            }
            else
            {
                OnRestartClicked();
            }
        }

        private void OnRestartClicked()
        {
            if (SceneLoader.HasInstance)
            {
                if (SceneLoader.Instance.IsLoading) return;
                SceneLoader.Instance.ReloadCurrentSceneWithReset();
            }
            else
            {
                Time.timeScale = 1f;
                if (GameManager.HasInstance)
                {
                    GameManager.Instance.ResetScore();
                    GameManager.Instance.SetState(GameState.Playing);
                }
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void OnMainMenuClicked()
        {
            if (SceneLoader.HasInstance)
            {
                if (SceneLoader.Instance.IsLoading) return;
                SceneLoader.Instance.LoadMainMenu();
            }
            else
            {
                Time.timeScale = 1f;
                if (GameManager.HasInstance) GameManager.Instance.ResetScore();
                SceneManager.LoadScene(SceneNames.MainMenu);
            }
        }
    }
}
