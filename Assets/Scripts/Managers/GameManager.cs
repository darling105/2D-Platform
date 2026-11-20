using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Singletons;

namespace Platformer2D.Managers
{
    /// <summary>
    /// Quản lý vòng đời trò chơi: điểm số, trạng thái game (MainMenu, Play, Pause, Game Over, Victory).
    /// </summary>
    public class GameManager : PersistentSingleton<GameManager>
    {
        [Header("Game Settings")]
        [SerializeField] private bool _autoStartOnAwake = true;

        public GameState CurrentState { get; private set; } = GameState.Initializing;
        public int CurrentScore { get; private set; } = 0;

        private int _silverKeys;
        private int _goldenKeys;

        protected override void Awake()
        {
            base.Awake();
            if (IsDuplicateInstance) return;

            if (_autoStartOnAwake)
            {
                string activeScene = SceneManager.GetActiveScene().name;
                SetState(activeScene == SceneNames.MainMenu ? GameState.MainMenu : GameState.Playing);
            }
        }

        private void OnEnable()
        {
            if (IsDuplicateInstance) return;

            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Subscribe<PlayerRespawnedEvent>(OnPlayerRespawned);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Unsubscribe<PlayerRespawnedEvent>(OnPlayerRespawned);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Time.timeScale = 1f;
            ResetKeys();
            SetState(scene.name == SceneNames.MainMenu ? GameState.MainMenu : GameState.Playing);
        }

        /// <summary>
        /// Thay đổi trạng thái game và phát tán GameStateChangedEvent.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            GameState oldState = CurrentState;
            CurrentState = newState;

            Time.timeScale = (newState == GameState.Paused || newState == GameState.Victory) ? 0f : 1f;

            EventBus.Publish(new GameStateChangedEvent(oldState, newState));
            Debug.Log($"[GameManager] Trạng thái thay đổi: {oldState} -> {newState}");
        }

        /// <summary>
        /// Tạm dừng hoặc tiếp tục game.
        /// </summary>
        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
            {
                SetState(GameState.Paused);
            }
            else if (CurrentState == GameState.Paused)
            {
                SetState(GameState.Playing);
            }
        }

        /// <summary>
        /// Cộng điểm và thông báo qua EventBus.
        /// </summary>
        public void AddScore(int points)
        {
            if (points == 0) return;

            CurrentScore += points;
            EventBus.Publish(new ScoreChangedEvent(CurrentScore, points));
        }

        /// <summary>
        /// Tiêu xu/điểm khi mở khóa hoặc nâng cấp kỹ năng. Trả về true nếu đủ xu.
        /// </summary>
        public bool TrySpendScore(int cost)
        {
            if (cost <= 0) return true;
            if (CurrentScore < cost) return false;

            CurrentScore -= cost;
            EventBus.Publish(new ScoreChangedEvent(CurrentScore, -cost));
            return true;
        }

        /// <summary>
        /// Reset điểm số và chìa khóa về 0 (ví dụ khi bắt đầu màn mới hoặc replay).
        /// </summary>
        public void ResetScore()
        {
            int delta = -CurrentScore;
            CurrentScore = 0;
            EventBus.Publish(new ScoreChangedEvent(CurrentScore, delta));
            ResetKeys();
        }

        public int GetKeyCount(KeyType type) => type == KeyType.Golden ? _goldenKeys : _silverKeys;

        public void AddKey(KeyType type, int amount = 1)
        {
            if (amount <= 0) return;

            int updated;
            if (type == KeyType.Golden)
            {
                _goldenKeys += amount;
                updated = _goldenKeys;
            }
            else
            {
                _silverKeys += amount;
                updated = _silverKeys;
            }

            EventBus.Publish(new KeyCountChangedEvent(type, updated));
        }

        public bool TryUseKey(KeyType type)
        {
            if (type == KeyType.Golden)
            {
                if (_goldenKeys <= 0) return false;
                _goldenKeys--;
                EventBus.Publish(new KeyCountChangedEvent(type, _goldenKeys));
                return true;
            }

            if (_silverKeys <= 0) return false;
            _silverKeys--;
            EventBus.Publish(new KeyCountChangedEvent(type, _silverKeys));
            return true;
        }

        public void ResetKeys()
        {
            _silverKeys = 0;
            _goldenKeys = 0;
            EventBus.Publish(new KeyCountChangedEvent(KeyType.Silver, 0));
            EventBus.Publish(new KeyCountChangedEvent(KeyType.Golden, 0));
        }

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            SetState(GameState.GameOver);
        }

        private void OnPlayerRespawned(PlayerRespawnedEvent evt)
        {
            SetState(GameState.Playing);
        }
    }
}
