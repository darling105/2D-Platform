using System;
using UnityEngine;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.Singletons;

namespace Platformer2D.Managers
{
    /// <summary>
    /// Quản lý vòng đời trò chơi: điểm số, trạng thái game (Play, Pause, Game Over, Victory).
    /// </summary>
    public class GameManager : PersistentSingleton<GameManager>
    {
        [Header("Game Settings")]
        [SerializeField] private bool _autoStartOnAwake = true;

        public GameState CurrentState { get; private set; } = GameState.Initializing;
        public int CurrentScore { get; private set; } = 0;

        protected override void Awake()
        {
            base.Awake();
            if (_autoStartOnAwake)
            {
                SetState(GameState.Playing);
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        }

        /// <summary>
        /// Thay đổi trạng thái game và phát tán GameStateChangedEvent.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            GameState oldState = CurrentState;
            CurrentState = newState;

            switch (newState)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                    // Có thể thêm delay hoặc slow-motion tại đây
                    break;
                case GameState.Victory:
                    break;
            }

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
        /// Reset điểm số về 0 (ví dụ khi bắt đầu màn mới hoặc replay).
        /// </summary>
        public void ResetScore()
        {
            int delta = -CurrentScore;
            CurrentScore = 0;
            EventBus.Publish(new ScoreChangedEvent(CurrentScore, delta));
        }

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            SetState(GameState.GameOver);
        }
    }
}
