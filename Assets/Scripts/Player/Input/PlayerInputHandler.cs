using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Platformer2D.Core.Architecture;
using Platformer2D.Managers;

namespace Platformer2D.Player.Input
{
    /// <summary>
    /// Xử lý toàn bộ việc đọc Input người chơi (Keyboard, Gamepad, Mouse):
    /// - Tách biệt hoàn toàn việc đọc phím khỏi logic vật lý và chuyển động (Tuân thủ nguyên tắc SRP).
    /// - Hỗ trợ cả bàn phím lẫn tay cầm Gamepad tự động.
    /// - Quản lý Jump Buffer và các cờ kích hoạt đòn đánh, lướt, ném.
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
        public bool ExplicitJumpTriggered => _explicitJumpBufferTimer > 0f;
        public bool JumpHeld { get; private set; }
        public bool DashTriggered { get; private set; }
        public bool AttackTriggered { get; private set; }
        public bool ThrowTriggered { get; private set; }
        public bool Skill1Triggered { get; private set; }
        public bool Skill2Triggered { get; private set; }
        public bool Skill3Triggered { get; private set; }
        public bool Skill4Triggered { get; private set; }

        private float _jumpBufferTimer;
        private float _explicitJumpBufferTimer;

        private void Update()
        {
            if (Time.timeScale <= 0f || (GameManager.HasInstance && GameManager.Instance.CurrentState != GameState.Playing))
            {
                ResetInputs();
                return;
            }

            ReadInput();
            UpdateTimers();
        }

        private void ReadInput()
        {
            float moveX = 0f;
            VerticalInput = 0f;
            IsDownHeld = false;
            DashTriggered = false;
            AttackTriggered = false;
            ThrowTriggered = false;
            JumpHeld = false;
            Skill1Triggered = false;
            Skill2Triggered = false;
            Skill3Triggered = false;
            Skill4Triggered = false;

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

                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    _explicitJumpBufferTimer = _jumpBufferTime;
                }

                if (Keyboard.current.spaceKey.isPressed || Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                {
                    JumpHeld = true;
                }

                if (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.kKey.wasPressedThisFrame)
                {
                    DashTriggered = true;
                }

                bool overUI = IsPointerOverUI();
                if (Keyboard.current.jKey.wasPressedThisFrame || Keyboard.current.zKey.wasPressedThisFrame || (!overUI && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
                {
                    AttackTriggered = true;
                }

                if (Keyboard.current.uKey.wasPressedThisFrame || Keyboard.current.iKey.wasPressedThisFrame || (!overUI && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame))
                {
                    ThrowTriggered = true;
                }

                if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame)
                    Skill1Triggered = true;
                if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
                    Skill2Triggered = true;
                if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.rKey.wasPressedThisFrame)
                    Skill3Triggered = true;
                if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame)
                    Skill4Triggered = true;
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
                    _explicitJumpBufferTimer = _jumpBufferTime;
                }

                if (Gamepad.current.buttonSouth.isPressed)
                {
                    JumpHeld = true;
                }

                if (Gamepad.current.rightShoulder.wasPressedThisFrame || Gamepad.current.buttonEast.wasPressedThisFrame)
                {
                    DashTriggered = true;
                }

                if (Gamepad.current.buttonWest.wasPressedThisFrame)
                {
                    AttackTriggered = true;
                }

                if (Gamepad.current.buttonNorth.wasPressedThisFrame)
                {
                    ThrowTriggered = true;
                }

                if (Gamepad.current.dpad.left.wasPressedThisFrame) Skill1Triggered = true;
                if (Gamepad.current.dpad.up.wasPressedThisFrame) Skill2Triggered = true;
                if (Gamepad.current.dpad.right.wasPressedThisFrame) Skill3Triggered = true;
                if (Gamepad.current.dpad.down.wasPressedThisFrame) Skill4Triggered = true;
            }

            MoveInput = new Vector2(moveX, 0f);
        }

        public bool IsSkillSlotTriggered(int slotIndex)
        {
            return slotIndex switch
            {
                0 => Skill1Triggered,
                1 => Skill2Triggered,
                2 => Skill3Triggered,
                3 => Skill4Triggered,
                _ => false
            };
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void UpdateTimers()
        {
            if (_jumpBufferTimer > 0f)
            {
                _jumpBufferTimer -= Time.deltaTime;
            }
            if (_explicitJumpBufferTimer > 0f)
            {
                _explicitJumpBufferTimer -= Time.deltaTime;
            }
        }

        public void ConsumeJump()
        {
            _jumpBufferTimer = 0f;
            _explicitJumpBufferTimer = 0f;
        }

        public void ResetInputs()
        {
            MoveInput = Vector2.zero;
            VerticalInput = 0f;
            IsDownHeld = false;
            _jumpBufferTimer = 0f;
            _explicitJumpBufferTimer = 0f;
            JumpHeld = false;
            DashTriggered = false;
            AttackTriggered = false;
            ThrowTriggered = false;
            Skill1Triggered = false;
            Skill2Triggered = false;
            Skill3Triggered = false;
            Skill4Triggered = false;
        }
    }
}
