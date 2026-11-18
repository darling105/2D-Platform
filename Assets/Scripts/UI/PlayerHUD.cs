using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Platformer2D.Core.Architecture;
using Platformer2D.Managers;

namespace Platformer2D.UI
{
    /// <summary>
    /// Quản lý giao diện HUD (Heads-Up Display) trong màn chơi:
    /// - Tự động cập nhật máu (Slider / Text) khi máu Player thay đổi (qua EventBus).
    /// - Tự động cập nhật điểm số nhặt coin (qua EventBus).
    /// - Hiển thị bảng Game Over và nút Chơi lại (Replay) khi hy sinh.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [Header("Health UI")]
        [Tooltip("Thanh trượt hiển thị máu (tùy chọn)")]
        [SerializeField] private Slider _healthSlider;
        [Tooltip("Text hiển thị số máu ví dụ: 3 / 3 (tùy chọn)")]
        [SerializeField] private TextMeshProUGUI _healthText;

        [Header("Score UI")]
        [Tooltip("Text hiển thị số coin / điểm")]
        [SerializeField] private TextMeshProUGUI _scoreText;

        [Header("Game Over Screen")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Button _restartButton;

        private void Awake()
        {
            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnHealthChanged(PlayerHealthChangedEvent evt)
        {
            if (_healthSlider != null)
            {
                _healthSlider.maxValue = evt.MaxHealth;
                _healthSlider.value = evt.CurrentHealth;
            }

            if (_healthText != null)
            {
                _healthText.text = $"HP: {evt.CurrentHealth}/{evt.MaxHealth}";
            }
        }

        private void OnScoreChanged(ScoreChangedEvent evt)
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"Coins: {evt.CurrentScore}";
            }
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState == GameState.GameOver)
            {
                if (_gameOverPanel != null)
                {
                    _gameOverPanel.SetActive(true);
                }
            }
        }

        private void OnRestartClicked()
        {
            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.ReloadCurrentScene();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                );
            }
        }
    }
}
