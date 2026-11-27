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
    /// - Play (Level_01), Select Level (Level 1-3), Test Lab (MechanicsTestScene), Settings và Quit.
    /// - Hỗ trợ Esc để đóng bảng Settings hoặc Select Level.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private SettingsPanelUI _settingsPanel;
        [SerializeField] private GameObject _levelSelectPanel;

        [Header("Menu Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _levelSelectButton;
        [SerializeField] private Button _testLabButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        [Header("Level Select Buttons")]
        [SerializeField] private Button _level1Button;
        [SerializeField] private Button _level2Button;
        [SerializeField] private Button _level3Button;
        [SerializeField] private Button _levelSelectBackButton;
        [SerializeField] private Button _levelSelectCloseButton;

        private void Awake()
        {
            if (_playButton != null) _playButton.onClick.AddListener(OnPlayClicked);
            if (_levelSelectButton != null) _levelSelectButton.onClick.AddListener(OnLevelSelectClicked);
            if (_testLabButton != null) _testLabButton.onClick.AddListener(OnTestLabClicked);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettingsClicked);
            if (_quitButton != null) _quitButton.onClick.AddListener(OnQuitClicked);

            if (_level1Button != null) _level1Button.onClick.AddListener(OnLevel1Clicked);
            if (_level2Button != null) _level2Button.onClick.AddListener(OnLevel2Clicked);
            if (_level3Button != null) _level3Button.onClick.AddListener(OnLevel3Clicked);
            if (_levelSelectBackButton != null) _levelSelectBackButton.onClick.AddListener(CloseLevelSelect);
            if (_levelSelectCloseButton != null) _levelSelectCloseButton.onClick.AddListener(CloseLevelSelect);
        }

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;

            if (_mainPanel != null) _mainPanel.SetActive(true);
            if (_settingsPanel != null) _settingsPanel.gameObject.SetActive(false);
            if (_levelSelectPanel != null) _levelSelectPanel.SetActive(false);

            if (GameManager.HasInstance)
            {
                GameManager.Instance.SetState(GameState.MainMenu);
            }
        }

        private void OnDestroy()
        {
            if (_playButton != null) _playButton.onClick.RemoveListener(OnPlayClicked);
            if (_levelSelectButton != null) _levelSelectButton.onClick.RemoveListener(OnLevelSelectClicked);
            if (_testLabButton != null) _testLabButton.onClick.RemoveListener(OnTestLabClicked);
            if (_settingsButton != null) _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            if (_quitButton != null) _quitButton.onClick.RemoveListener(OnQuitClicked);

            if (_level1Button != null) _level1Button.onClick.RemoveListener(OnLevel1Clicked);
            if (_level2Button != null) _level2Button.onClick.RemoveListener(OnLevel2Clicked);
            if (_level3Button != null) _level3Button.onClick.RemoveListener(OnLevel3Clicked);
            if (_levelSelectBackButton != null) _levelSelectBackButton.onClick.RemoveListener(CloseLevelSelect);
            if (_levelSelectCloseButton != null) _levelSelectCloseButton.onClick.RemoveListener(CloseLevelSelect);
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

            if (_settingsPanel != null && _settingsPanel.IsOpen)
            {
                _settingsPanel.Close();
            }
            else if (_levelSelectPanel != null && _levelSelectPanel.activeSelf)
            {
                CloseLevelSelect();
            }
        }

        private void OnPlayClicked()
        {
            LoadTargetScene(SceneNames.Level01);
        }

        private void OnLevelSelectClicked()
        {
            if (_levelSelectPanel == null) return;

            if (_mainPanel != null) _mainPanel.SetActive(false);
            _levelSelectPanel.SetActive(true);
        }

        private void CloseLevelSelect()
        {
            if (_levelSelectPanel != null) _levelSelectPanel.SetActive(false);
            if (_mainPanel != null) _mainPanel.SetActive(true);
        }

        private void OnLevel1Clicked() => LoadTargetScene(SceneNames.Level01);
        private void OnLevel2Clicked() => LoadTargetScene(SceneNames.Level02);
        private void OnLevel3Clicked() => LoadTargetScene(SceneNames.Level03);

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
