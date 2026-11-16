namespace Platformer2D.Core.StateMachine
{
    /// <summary>
    /// Lớp cơ sở trừu tượng cho các State, cung cấp các hàm ảo (virtual) để lớp con chỉ override những gì cần thiết.
    /// </summary>
    public abstract class BaseState : IState
    {
        protected readonly StateMachine _stateMachine;

        protected BaseState(StateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void Exit() { }
    }
}
