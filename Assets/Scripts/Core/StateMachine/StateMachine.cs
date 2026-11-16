using System;
using UnityEngine;

namespace Platformer2D.Core.StateMachine
{
    /// <summary>
    /// Bộ máy điều khiển trạng thái (Finite State Machine).
    /// Áp dụng cho Player Controller, Quái vật AI, Boss FSM, v.v.
    /// </summary>
    public class StateMachine
    {
        public IState CurrentState { get; private set; }
        public IState PreviousState { get; private set; }

        public event Action<IState, IState> OnStateChanged;

        /// <summary>
        /// Khởi tạo trạng thái ban đầu cho máy trạng thái.
        /// </summary>
        public void Initialize(IState startingState)
        {
            if (startingState == null)
            {
                Debug.LogWarning("[StateMachine] Khởi tạo với startingState là null.");
                return;
            }

            CurrentState = startingState;
            CurrentState.Enter();
        }

        /// <summary>
        /// Chuyển đổi sang trạng thái mới: Exit state cũ -> gán state mới -> Enter state mới.
        /// </summary>
        public void ChangeState(IState newState)
        {
            if (newState == null || newState == CurrentState)
            {
                return;
            }

            PreviousState = CurrentState;
            CurrentState?.Exit();

            CurrentState = newState;
            CurrentState.Enter();

            OnStateChanged?.Invoke(PreviousState, CurrentState);
        }

        /// <summary>
        /// Update logic của state hiện tại (gọi trong MonoBehaviour.Update).
        /// </summary>
        public void Update()
        {
            CurrentState?.Update();
        }

        /// <summary>
        /// Update vật lý 2D của state hiện tại (gọi trong MonoBehaviour.FixedUpdate).
        /// </summary>
        public void FixedUpdate()
        {
            CurrentState?.FixedUpdate();
        }
    }
}
