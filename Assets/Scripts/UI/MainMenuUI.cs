using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Platformer2D.Core.Architecture;
using Platformer2D.Managers;

namespace Platformer2D.UI
{
    /// <summary>
    /// Điều khiển giao diện màn hình chính (Main Menu):
    /// - Play (Level_01), Test Lab (MechanicsTestScene), Settings và Quit.
    /// - Hỗ trợ Esc để đóng bảng Settings.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private SettingsPanelUI _settingsPanel;

        [Header("Menu Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _testLabButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        private void Awake()
        {
            if (_playButton != null) _playButton.onClick.AddListener(OnPlayClicked);
            if (_testLabButton != null) _testLabButton.onClick.AddListener(OnTestLabClicked);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettingsClicked);
            if (_quitButton != null) _quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;

            if (_mainPanel != null) _mainPanel.SetActive(true);
            if (_settingsPanel != null) _settingsPanel.gameObject.SetActive(false);

            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetState(GameState.MainMenu);
            }
        }

        private void OnDestroy()
        {
            if (_playButton != null) _playButton.onClick.RemoveListener(OnPlayClicked);
            if (_testLabButton != null) _testLabButton.onClick.RemoveListener(OnTestLabClicked);
            if (_settingsButton != null) _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            if (_quitButton != null) _quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        private void Update()
        {
            if (_settingsPanel != null && _settingsPanel.IsOpen &&
                Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                _settingsPanel.Close();
            }
        }

        private void OnPlayClicked()
        {
            LoadTargetScene(SceneNames.Level01);
        }

        private void OnTestLabClicked()
        {
            LoadTargetScene(SceneNames.MechanicsTest);
        }

        private void OnSettingsClicked()
        {
            if (_settingsPanel == null) return;

            if (_mainPanel != null) _mainPanel.SetActive(false);
            _settingsPanel.Open(() =>
            {
                if (_mainPanel != null) _mainPanel.SetActive(true);
            });
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void LoadTargetScene(string sceneName)
        {
            if (SceneLoader.HasInstance)
            {
                if (SceneLoader.Instance.IsLoading) return;
                if (GameManager.HasInstance) GameManager.Instance.ResetScore();
                SceneLoader.Instance.LoadSceneAsync(sceneName);
            }
            else
            {
                Time.timeScale = 1f;
                if (GameManager.HasInstance) GameManager.Instance.ResetScore();
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}
