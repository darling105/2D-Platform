using System;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer2D.Core.Architecture
{
    /// <summary>
    /// Type-Safe Event Bus theo mô hình Publish/Subscribe (Observer Pattern).
    /// Giúp tách rời (decouple) hoàn toàn các module: Player, UI, Audio, Manager,...
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Đăng ký lắng nghe sự kiện loại T.
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : IEvent
        {
            if (handler == null) return;

            lock (_lock)
            {
                Type eventType = typeof(T);
                if (!_subscribers.TryGetValue(eventType, out var handlers))
                {
                    handlers = new List<Delegate>();
                    _subscribers[eventType] = handlers;
                }

                if (!handlers.Contains(handler))
                {
                    handlers.Add(handler);
                }
            }
        }

        /// <summary>
        /// Hủy đăng ký lắng nghe sự kiện loại T. Luôn gọi trong OnDisable/OnDestroy để tránh Memory Leak.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            if (handler == null) return;

            lock (_lock)
            {
                Type eventType = typeof(T);
                if (_subscribers.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(handler);
                    if (handlers.Count == 0)
                    {
                        _subscribers.Remove(eventType);
                    }
                }
            }
        }

        /// <summary>
        /// Phát tán sự kiện tới toàn bộ subscriber đang lắng nghe.
        /// </summary>
        public static void Publish<T>(T eventData) where T : IEvent
        {
            List<Delegate> handlersCopy;
            lock (_lock)
            {
                Type eventType = typeof(T);
                if (!_subscribers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
                {
                    return;
                }
                handlersCopy = new List<Delegate>(handlers);
            }

            for (int i = 0; i < handlersCopy.Count; i++)
            {
                try
                {
                    if (handlersCopy[i] is Action<T> action)
                    {
                        action.Invoke(eventData);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Lỗi khi xử lý event '{typeof(T).Name}': {ex}");
                }
            }
        }

        /// <summary>
        /// Xóa sạch mọi subscriber (thường dùng khi reset game hoặc dọn dẹp unit test).
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                _subscribers.Clear();
            }
        }
    }

    #region Game State Events

    public enum GameState
    {
        Initializing,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    public readonly struct GameStateChangedEvent : IEvent
    {
        public readonly GameState PreviousState;
        public readonly GameState NewState;

        public GameStateChangedEvent(GameState previousState, GameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }

    #endregion

    #region Player Events

    public readonly struct PlayerHealthChangedEvent : IEvent
    {
        public readonly int CurrentHealth;
        public readonly int MaxHealth;

        public PlayerHealthChangedEvent(int currentHealth, int maxHealth)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
        }
    }

    public readonly struct PlayerDiedEvent : IEvent
    {
        public readonly Vector2 DeathPosition;

        public PlayerDiedEvent(Vector2 deathPosition)
        {
            DeathPosition = deathPosition;
        }
    }

    public readonly struct PlayerRespawnedEvent : IEvent
    {
        public readonly Vector3 RespawnPosition;

        public PlayerRespawnedEvent(Vector3 respawnPosition)
        {
            RespawnPosition = respawnPosition;
        }
    }

    public readonly struct CheckpointActivatedEvent : IEvent
    {
        public readonly Vector3 CheckpointPosition;

        public CheckpointActivatedEvent(Vector3 checkpointPosition)
        {
            CheckpointPosition = checkpointPosition;
        }
    }

    #endregion

    #region Score Events

    public readonly struct ScoreChangedEvent : IEvent
    {
        public readonly int CurrentScore;
        public readonly int DeltaScore;

        public ScoreChangedEvent(int currentScore, int deltaScore)
        {
            CurrentScore = currentScore;
            DeltaScore = deltaScore;
        }
    }

    #endregion

    #region Audio Request Events

    public enum SoundType
    {
        SFX,
        BGM
    }

    public readonly struct PlaySoundEvent : IEvent
    {
        public readonly string SoundId;
        public readonly SoundType Type;
        public readonly Vector3? WorldPosition;
        public readonly float Volume;

        public PlaySoundEvent(string soundId, SoundType type = SoundType.SFX, Vector3? worldPosition = null, float volume = 1f)
        {
            SoundId = soundId;
            Type = type;
            WorldPosition = worldPosition;
            Volume = volume;
        }
    }

    #endregion
}
