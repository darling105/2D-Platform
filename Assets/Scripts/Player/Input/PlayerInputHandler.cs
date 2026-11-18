using UnityEngine;
using UnityEngine.InputSystem;

namespace Platformer2D.Player.Input
{
    /// <summary>
    /// Xử lý toàn bộ việc đọc Input người chơi (Keyboard, Gamepad, Mouse):
    /// - Tách biệt hoàn toàn việc đọc phím khỏi logic vật lý và chuyển động (Tuân thủ nguyên tắc SRP).
    /// - Hỗ trợ cả bàn phím lẫn tay cầm Gamepad tự động.
    /// - Quản lý Jump Buffer và các cờ kích hoạt đòn đánh, lướt, trượt, ném.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Buffer Timers")]
        [Tooltip("Thời gian Jump Buffer (ghi nhớ lệnh nhảy trước khi chạm đất)")]
        [SerializeField] private float _jumpBufferTime = 0.15f;

        public Vector2 MoveInput { get; private set; }
        public float VerticalInput { get; private set; }
        public bool IsDownHeld { get; private set; }

        public bool JumpTriggered => _jumpBufferTimer > 0f;
        public bool JumpHeld { get; private set; }
        public bool DashTriggered { get; private set; }
        public bool SlideTriggered { get; private set; }
        public bool AttackTriggered { get; private set; }
        public bool ThrowTriggered { get; private set; }

        private float _jumpBufferTimer;

        private void Update()
        {
            ReadInput();
            UpdateTimers();
        }

        private void ReadInput()
        {
            float moveX = 0f;
            VerticalInput = 0f;
            IsDownHeld = false;
            DashTriggered = false;
            SlideTriggered = false;
            AttackTriggered = false;
            ThrowTriggered = false;
            JumpHeld = false;

            // 1. Keyboard & Mouse Input
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    moveX -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    moveX += 1f;

                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                    VerticalInput += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                {
                    VerticalInput -= 1f;
                    IsDownHeld = true;
                }

                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
                {
                    _jumpBufferTimer = _jumpBufferTime;
                }

                if (Keyboard.current.spaceKey.isPressed || Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                {
                    JumpHeld = true;
                }

                if (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.kKey.wasPressedThisFrame)
                {
                    DashTriggered = true;
                }

                if (Keyboard.current.cKey.wasPressedThisFrame || Keyboard.current.leftCtrlKey.wasPressedThisFrame)
                {
                    SlideTriggered = true;
                }

                if (Keyboard.current.jKey.wasPressedThisFrame || Keyboard.current.zKey.wasPressedThisFrame || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
                {
                    AttackTriggered = true;
                }

                if (Keyboard.current.uKey.wasPressedThisFrame || Keyboard.current.iKey.wasPressedThisFrame || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame))
                {
                    ThrowTriggered = true;
                }
            }

            // 2. Gamepad Input
            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                if (Mathf.Abs(stick.x) > 0.2f)
                {
                    moveX = Mathf.Sign(stick.x);
                }

                if (stick.y > 0.3f) VerticalInput += 1f;
                if (stick.y < -0.3f)
                {
                    VerticalInput -= 1f;
                    IsDownHeld = true;
                }

                if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                {
                    _jumpBufferTimer = _jumpBufferTime;
                }

                if (Gamepad.current.buttonSouth.isPressed)
                {
                    JumpHeld = true;
                }

                if (Gamepad.current.rightShoulder.wasPressedThisFrame || Gamepad.current.buttonEast.wasPressedThisFrame)
                {
                    DashTriggered = true;
                }

                if (Gamepad.current.leftStickButton.wasPressedThisFrame)
                {
                    SlideTriggered = true;
                }

                if (Gamepad.current.buttonWest.wasPressedThisFrame)
                {
                    AttackTriggered = true;
                }

                if (Gamepad.current.buttonNorth.wasPressedThisFrame)
                {
                    ThrowTriggered = true;
                }
            }

            MoveInput = new Vector2(moveX, 0f);
        }

        private void UpdateTimers()
        {
            if (_jumpBufferTimer > 0f)
            {
                _jumpBufferTimer -= Time.deltaTime;
            }
        }

        public void ConsumeJump()
        {
            _jumpBufferTimer = 0f;
        }

        public void ResetInputs()
        {
            MoveInput = Vector2.zero;
            VerticalInput = 0f;
            IsDownHeld = false;
            _jumpBufferTimer = 0f;
            JumpHeld = false;
            DashTriggered = false;
            SlideTriggered = false;
            AttackTriggered = false;
            ThrowTriggered = false;
        }
    }
}
