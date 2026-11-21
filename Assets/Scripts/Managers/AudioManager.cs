using System;
using System.Collections.Generic;
using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Singletons;

namespace Platformer2D.Managers
{
    [System.Serializable]
    public class SoundItem
    {
        public string Id;
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume = 1f;
        [Range(0.1f, 3f)] public float Pitch = 1f;
        public bool Loop = false;
    }

    /// <summary>
    /// Quản lý âm thanh toàn diện (BGM, SFX). Hỗ trợ phát âm thanh qua ID hoặc tự động nhận Event từ EventBus.
    /// </summary>
    public class AudioManager : PersistentSingleton<AudioManager>
    {
        [Header("Audio Configurations")]
        [SerializeField] private List<SoundItem> _soundLibrary = new List<SoundItem>();

        [Header("Volume Settings")]
        [Range(0f, 1f)] [SerializeField] private float _masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float _musicVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 1f;

        [Header("SFX Channels")]
        [SerializeField] private int _sfxChannelCount = 10;

        private const string PrefMasterVolume = "Audio_MasterVolume";
        private const string PrefMusicVolume = "Audio_MusicVolume";
        private const string PrefSfxVolume = "Audio_SfxVolume";

        private AudioSource _musicSource;
        private readonly List<AudioSource> _sfxSources = new List<AudioSource>();
        private readonly Dictionary<string, SoundItem> _soundDict = new Dictionary<string, SoundItem>();

        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(PrefMasterVolume, _masterVolume);
                UpdateVolumes();
            }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(PrefMusicVolume, _musicVolume);
                UpdateVolumes();
            }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(PrefSfxVolume, _sfxVolume);
                UpdateVolumes();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (IsDuplicateInstance) return;

            LoadVolumeSettings();
            InitializeAudioSources();
            IndexSoundLibrary();
        }

        private void OnEnable()
        {
            if (IsDuplicateInstance) return;
            EventBus.Subscribe<PlaySoundEvent>(OnPlaySoundEvent);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlaySoundEvent>(OnPlaySoundEvent);
        }

        private void LoadVolumeSettings()
        {
            _masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMasterVolume, _masterVolume));
            _musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMusicVolume, _musicVolume));
            _sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfxVolume, _sfxVolume));
        }

        public void SaveSettings()
        {
            PlayerPrefs.Save();
        }

        private void InitializeAudioSources()
        {
            // BGM Source
            var musicObj = new GameObject("BGM_Source");
            musicObj.transform.SetParent(transform);
            _musicSource = musicObj.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;

            // SFX Source Pool
            var sfxContainer = new GameObject("SFX_Channels");
            sfxContainer.transform.SetParent(transform);
            for (int i = 0; i < _sfxChannelCount; i++)
            {
                var sfxObj = new GameObject($"SFX_Channel_{i}");
                sfxObj.transform.SetParent(sfxContainer.transform);
                var source = sfxObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _sfxSources.Add(source);
            }

            UpdateVolumes();
        }

        private void IndexSoundLibrary()
        {
            _soundDict.Clear();
            foreach (var item in _soundLibrary)
            {
                if (!string.IsNullOrEmpty(item.Id) && item.Clip != null)
                {
                    _soundDict[item.Id] = item;
                }
            }
        }

        private void UpdateVolumes()
        {
            if (_musicSource != null)
            {
                _musicSource.volume = _musicVolume * _masterVolume;
            }
            foreach (var source in _sfxSources)
            {
                if (source != null)
                {
                    source.volume = _sfxVolume * _masterVolume;
                }
            }
        }

        /// <summary>
        /// Phát nhạc nền (BGM).
        /// </summary>
        public void PlayMusic(string soundId)
        {
            if (_soundDict.TryGetValue(soundId, out var item))
            {
                PlayMusic(item.Clip, item.Volume);
            }
            else
            {
                Debug.LogWarning($"[AudioManager] Không tìm thấy BGM ID: {soundId}");
            }
        }

        public void PlayMusic(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            _musicSource.clip = clip;
            _musicSource.volume = volume * _musicVolume * _masterVolume;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            if (_musicSource != null)
            {
                _musicSource.Stop();
            }
        }

        /// <summary>
        /// Phát hiệu ứng âm thanh (SFX) theo Sound ID.
        /// </summary>
        public void PlaySFX(string soundId, float volumeMultiplier = 1f)
        {
            if (string.IsNullOrEmpty(soundId)) return;

            if (_soundDict.TryGetValue(soundId, out var item))
            {
                PlaySFX(item.Clip, item.Volume * volumeMultiplier, item.Pitch);
                return;
            }

            // Fallback âm thanh click UI nếu ID là "UI_Click" hoặc "Click"
            if (soundId.Contains("Click") || soundId.Contains("UI"))
            {
                if (_soundDict.TryGetValue("Click", out item) ||
                    _soundDict.TryGetValue("Coin", out item) ||
                    _soundDict.TryGetValue("Jump", out item))
                {
                    PlaySFX(item.Clip, item.Volume * volumeMultiplier * 0.4f, item.Pitch);
                    return;
                }
            }

            // Fallback cho JumpPad / Bounce -> dùng âm Jump
            if (soundId.Contains("Bounce"))
            {
                if (_soundDict.TryGetValue("Jump", out item))
                {
                    PlaySFX(item.Clip, item.Volume * volumeMultiplier * 1.1f, 1.25f);
                    return;
                }
            }

            // Fallback cho thùng nổ, đập vỡ gỗ -> dùng âm Hit / EnemyHurt
            if (soundId.Contains("Hit") || soundId.Contains("Explosion") || soundId.Contains("Break"))
            {
                if (_soundDict.TryGetValue("EnemyHurt", out item) ||
                    _soundDict.TryGetValue("Hit", out item) ||
                    _soundDict.TryGetValue("PlayerHurt", out item))
                {
                    PlaySFX(item.Clip, item.Volume * volumeMultiplier, 0.85f);
                    return;
                }
            }

            // Fallback cho vật phẩm / thức ăn -> dùng âm Coin
            if (soundId.Contains("Food") || soundId.Contains("Eat") || soundId.Contains("Item"))
            {
                if (_soundDict.TryGetValue("Coin", out item))
                {
                    PlaySFX(item.Clip, item.Volume * volumeMultiplier, 1.15f);
                    return;
                }
            }

            if (!soundId.Contains("Click"))
            {
                Debug.LogWarning($"[AudioManager] Không tìm thấy SFX ID: {soundId}");
            }
        }

        /// <summary>
        /// Phát trực tiếp một AudioClip SFX.
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;

            var source = GetAvailableSFXSource();
            if (source != null)
            {
                source.clip = clip;
                source.volume = volume * _sfxVolume * _masterVolume;
                source.pitch = pitch;
                source.Play();
            }
        }

        private AudioSource GetAvailableSFXSource()
        {
            for (int i = 0; i < _sfxSources.Count; i++)
            {
                if (!_sfxSources[i].isPlaying)
                {
                    return _sfxSources[i];
                }
            }
            // Nếu tất cả kênh đều bận, dùng kênh đầu tiên
            return _sfxSources[0];
        }

        private void OnPlaySoundEvent(PlaySoundEvent evt)
        {
            if (evt.Type == SoundType.BGM)
            {
                PlayMusic(evt.SoundId);
            }
            else
            {
                PlaySFX(evt.SoundId, evt.Volume);
            }
        }
    }
}
