namespace Platformer2D.Core.StateMachine
{
    /// <summary>
    /// Giao diện chuẩn cho toàn bộ State trong State Machine (dành cho Player, Enemy, Boss, UI,...).
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Gọi 1 lần khi bắt đầu bước vào state.
        /// </summary>
        void Enter();

        /// <summary>
        /// Gọi mỗi frame (tương đương MonoBehaviour.Update).
        /// </summary>
        void Update();

        /// <summary>
        /// Gọi mỗi physics step (tương đương MonoBehaviour.FixedUpdate) dành cho di chuyển vật lý 2D (Rigidbody2D).
        /// </summary>
        void FixedUpdate();

        /// <summary>
        /// Gọi 1 lần khi rời khỏi state để dọn dẹp logic/animation.
        /// </summary>
        void Exit();
    }
}
