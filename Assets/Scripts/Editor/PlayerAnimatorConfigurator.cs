#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Platformer2D.EditorTools
{
    /// <summary>
    /// Công cụ tự động cấu hình các tham số (Parameters) và chuyển đổi trạng thái (Transitions)
    /// cho Pink_Monster_0.controller, phục vụ trọn bộ cơ chế:
    /// - Combo Attack 1 & 2
    /// - Walk Attack
    /// - Throw (Ném đá/đạn)
    /// - Climb (Leo thang)
    /// - Push (Đẩy thùng)
    /// - Slide (Trượt đất)
    /// </summary>
    public static class PlayerAnimatorConfigurator
    {
        [MenuItem("Tools/Platformer Mechanics/Cấu hình đầy đủ Animator Controller cho Player")]
        public static void ConfigurePlayerAnimator()
        {
            string path = "Assets/Animations/Player/Pink_Monster_0.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                Debug.LogError("Không tìm thấy Animator Controller tại: " + path);
                return;
            }

            // 1. Đảm bảo toàn bộ Parameters cần thiết đã tồn tại
            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Attack2", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "WalkAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Throw", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "IsClimbing", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsPushing", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsSliding", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Hurt", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "IsDead", AnimatorControllerParameterType.Bool);

            var rootStateMachine = controller.layers[0].stateMachine;
            var states = rootStateMachine.states;

            AnimatorState idleState = null;
            AnimatorState runState = null;
            AnimatorState attack1State = null;
            AnimatorState attack2State = null;
            AnimatorState walkAttackState = null;
            AnimatorState throwState = null;
            AnimatorState climbState = null;
            AnimatorState pushState = null;

            foreach (var s in states)
            {
                if (s.state.name == "Pink_Monster_Idle") idleState = s.state;
                else if (s.state.name == "Pink_Monster_Run") runState = s.state;
                else if (s.state.name == "Pink_Monster_Attack_1") attack1State = s.state;
                else if (s.state.name == "Pink_Monster_Attack_2") attack2State = s.state;
                else if (s.state.name == "Pink_Monster_Walk_Attack") walkAttackState = s.state;
                else if (s.state.name == "Pink_Monster_Throw") throwState = s.state;
                else if (s.state.name == "Pink_Monster_Climb") climbState = s.state;
                else if (s.state.name == "Pink_Monster_Push") pushState = s.state;
            }

            // 2. Chuyển tiếp Attack 2
            if (attack2State != null)
            {
                EnsureAnyStateTransition(rootStateMachine, attack2State, "Attack2");
                if (idleState != null) EnsureExitToState(attack2State, idleState);
            }

            // 3. Chuyển tiếp Walk Attack
            if (walkAttackState != null)
            {
                EnsureAnyStateTransition(rootStateMachine, walkAttackState, "WalkAttack");
                if (idleState != null) EnsureExitToState(walkAttackState, idleState);
            }

            // 4. Chuyển tiếp Throw
            if (throwState != null)
            {
                EnsureAnyStateTransition(rootStateMachine, throwState, "Throw");
                if (idleState != null) EnsureExitToState(throwState, idleState);
            }

            // 5. Chuyển tiếp Climb (Leo thang)
            if (climbState != null)
            {
                EnsureAnyStateTransitionBool(rootStateMachine, climbState, "IsClimbing", true);
                if (idleState != null)
                {
                    bool hasExit = climbState.transitions.Any(t => t.destinationState == idleState);
                    if (!hasExit)
                    {
                        var trans = climbState.AddTransition(idleState);
                        trans.hasExitTime = false;
                        trans.duration = 0f;
                        trans.canTransitionToSelf = false;
                        trans.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing");
                    }
                }
            }

            // 6. Chuyển tiếp Push (Đẩy thùng)
            if (pushState != null && idleState != null)
            {
                bool hasPushIn = idleState.transitions.Any(t => t.destinationState == pushState);
                if (!hasPushIn)
                {
                    var toPush = idleState.AddTransition(pushState);
                    toPush.hasExitTime = false;
                    toPush.duration = 0.05f;
                    toPush.canTransitionToSelf = false;
                    toPush.AddCondition(AnimatorConditionMode.If, 0, "IsPushing");
                }

                if (runState != null)
                {
                    bool hasRunToPush = runState.transitions.Any(t => t.destinationState == pushState);
                    if (!hasRunToPush)
                    {
                        var toPush = runState.AddTransition(pushState);
                        toPush.hasExitTime = false;
                        toPush.duration = 0.05f;
                        toPush.canTransitionToSelf = false;
                        toPush.AddCondition(AnimatorConditionMode.If, 0, "IsPushing");
                    }
                }

                bool hasPushOut = pushState.transitions.Any(t => t.destinationState == idleState);
                if (!hasPushOut)
                {
                    var fromPush = pushState.AddTransition(idleState);
                    fromPush.hasExitTime = false;
                    fromPush.duration = 0.05f;
                    fromPush.canTransitionToSelf = false;
                    fromPush.AddCondition(AnimatorConditionMode.IfNot, 0, "IsPushing");
                }
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[PlayerAnimatorConfigurator] Đã cấu hình hoàn chỉnh Animator Controller với toàn bộ Animation Group 1!");
        }

        private static void EnsureParameter(AnimatorController controller, string paramName, AnimatorControllerParameterType type)
        {
            if (!controller.parameters.Any(p => p.name == paramName))
            {
                controller.AddParameter(paramName, type);
            }
        }

        private static void EnsureAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState targetState, string triggerParam)
        {
            bool exists = stateMachine.anyStateTransitions.Any(t => t.destinationState == targetState);
            if (!exists)
            {
                var trans = stateMachine.AddAnyStateTransition(targetState);
                trans.hasExitTime = false;
                trans.duration = 0f;
                trans.canTransitionToSelf = false;
                trans.AddCondition(AnimatorConditionMode.If, 0, triggerParam);
            }
        }

        private static void EnsureAnyStateTransitionBool(AnimatorStateMachine stateMachine, AnimatorState targetState, string boolParam, bool value)
        {
            bool exists = stateMachine.anyStateTransitions.Any(t => t.destinationState == targetState);
            if (!exists)
            {
                var trans = stateMachine.AddAnyStateTransition(targetState);
                trans.hasExitTime = false;
                trans.duration = 0f;
                trans.canTransitionToSelf = false;
                trans.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, boolParam);
            }
        }

        private static void EnsureExitToState(AnimatorState sourceState, AnimatorState exitState)
        {
            bool exists = sourceState.transitions.Any(t => t.destinationState == exitState);
            if (!exists)
            {
                var trans = sourceState.AddTransition(exitState);
                trans.hasExitTime = true;
                trans.exitTime = 0.85f;
                trans.duration = 0.05f;
                trans.canTransitionToSelf = false;
            }
        }
    }
}
#endif
