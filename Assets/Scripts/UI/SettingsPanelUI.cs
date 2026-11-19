using System;
using UnityEngine;
using UnityEngine.UI;
using Platformer2D.Managers;

namespace Platformer2D.UI
{
    /// <summary>
    /// Bảng cài đặt âm lượng tái sử dụng giữa Main Menu và Pause Menu:
    /// - Kết nối trực tiếp thanh trượt Master / Music / SFX tới AudioManager.
    /// - Tự đồng bộ và lưu lại bằng PlayerPrefs khi đóng bảng.
    /// </summary>
    public class SettingsPanelUI : MonoBehaviour
    {
        private const string PrefMasterVolume = "Audio_MasterVolume";
        private const string PrefMusicVolume = "Audio_MusicVolume";
        private const string PrefSfxVolume = "Audio_SfxVolume";

        [Header("Sliders")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("Value Labels")]
        [SerializeField] private Text _masterValueText;
        [SerializeField] private Text _musicValueText;
        [SerializeField] private Text _sfxValueText;

        [Header("Buttons")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _backButton;

        private Action _onClosed;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            if (_masterSlider != null) _masterSlider.onValueChanged.AddListener(OnMasterChanged);
            if (_musicSlider != null) _musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(OnSfxChanged);

            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
            if (_backButton != null) _backButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (_masterSlider != null) _masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            if (_musicSlider != null) _musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (_sfxSlider != null) _sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);

            if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
            if (_backButton != null) _backButton.onClick.RemoveListener(Close);
        }

        public void Open(Action onClosed = null)
        {
            _onClosed = onClosed;
            gameObject.SetActive(true);
            SyncFromAudio();
        }

        public void Close()
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.SaveSettings();
            }
            else
            {
                PlayerPrefs.Save();
            }

            gameObject.SetActive(false);

            Action callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        }

        private void SyncFromAudio()
        {
            float master = AudioManager.HasInstance ? AudioManager.Instance.MasterVolume : PlayerPrefs.GetFloat(PrefMasterVolume, 1f);
            float music = AudioManager.HasInstance ? AudioManager.Instance.MusicVolume : PlayerPrefs.GetFloat(PrefMusicVolume, 1f);
            float sfx = AudioManager.HasInstance ? AudioManager.Instance.SfxVolume : PlayerPrefs.GetFloat(PrefSfxVolume, 1f);

            if (_masterSlider != null) _masterSlider.SetValueWithoutNotify(master);
            if (_musicSlider != null) _musicSlider.SetValueWithoutNotify(music);
            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(sfx);

            UpdateValueText(_masterValueText, master);
            UpdateValueText(_musicValueText, music);
            UpdateValueText(_sfxValueText, sfx);
        }

        private void OnMasterChanged(float value)
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.MasterVolume = value;
            }
            else
            {
                PlayerPrefs.SetFloat(PrefMasterVolume, Mathf.Clamp01(value));
            }
            UpdateValueText(_masterValueText, value);
        }

        private void OnMusicChanged(float value)
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.MusicVolume = value;
            }
            else
            {
                PlayerPrefs.SetFloat(PrefMusicVolume, Mathf.Clamp01(value));
            }
            UpdateValueText(_musicValueText, value);
        }

        private void OnSfxChanged(float value)
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.SfxVolume = value;
            }
            else
            {
                PlayerPrefs.SetFloat(PrefSfxVolume, Mathf.Clamp01(value));
            }
            UpdateValueText(_sfxValueText, value);
        }

        private static void UpdateValueText(Text label, float value)
        {
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
            }
        }
    }
}
