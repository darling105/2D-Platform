#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Platformer2D.EditorTools
{
    /// <summary>
    /// Công cụ tự động tạo trọn bộ Animation Clips và Animator Controller hoàn chỉnh cho Pink Monster.
    /// </summary>
    public static class PlayerAnimationGenerator
    {
        private const string SOURCE_FOLDER = "Assets/GameAssets/Player/1 Pink_Monster";
        private const string OUTPUT_FOLDER = "Assets/Animations/Player/Pink_Monster";

        [MenuItem("Tools/Pixel Art/Tự động tạo trọn bộ Animation cho Pink Monster")]
        public static void GenerateAllAnimationsForPinkMonster()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations/Player"))
            {
                AssetDatabase.CreateFolder("Assets/Animations", "Player");
            }
            if (!AssetDatabase.IsValidFolder(OUTPUT_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/Animations/Player", "Pink_Monster");
            }

            var animConfigs = new (string fileName, string clipName, bool loop, float fps)[]
            {
                ("Pink_Monster_Idle_4.png", "Pink_Idle", true, 8f),
                ("Pink_Monster_Run_6.png", "Pink_Run", true, 10f),
                ("Pink_Monster_Walk_6.png", "Pink_Walk", true, 8f),
                ("Pink_Monster_Jump_8.png", "Pink_Jump", false, 10f),
                ("Pink_Monster_Attack1_4.png", "Pink_Attack1", false, 12f),
                ("Pink_Monster_Attack2_6.png", "Pink_Attack2", false, 12f),
                ("Pink_Monster_Hurt_4.png", "Pink_Hurt", false, 10f),
                ("Pink_Monster_Death_8.png", "Pink_Death", false, 8f)
            };

            var createdClips = new Dictionary<string, AnimationClip>();

            foreach (var cfg in animConfigs)
            {
                string sourcePath = $"{SOURCE_FOLDER}/{cfg.fileName}";
                var sprites = LoadSpritesFromPath(sourcePath);

                if (sprites == null || sprites.Length == 0)
                {
                    Debug.LogWarning($"[PlayerAnimationGenerator] Không tìm thấy sprite trong '{sourcePath}'. Bỏ qua.");
                    continue;
                }

                string clipPath = $"{OUTPUT_FOLDER}/{cfg.clipName}.anim";
                var clip = CreateAnimationClip(clipPath, sprites, cfg.fps, cfg.loop);
                createdClips[cfg.clipName] = clip;
            }

            // Tạo Animator Controller
            string controllerPath = $"{OUTPUT_FOLDER}/PinkMonster_Controller.controller";
            var controller = CreateAnimatorController(controllerPath, createdClips);

            // Tự động gắn vào Player đang chọn trong Scene (nếu có)
            AssignToSelectedOrScenePlayer(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Thành công!",
                $"Đã tạo thành công {createdClips.Count} Animation Clips và 1 Animator Controller hoàn chỉnh tại:\n{OUTPUT_FOLDER}\n\nĐã sẵn sàng hoạt động!",
                "Tuyệt vời"
            );
        }

        private static Sprite[] LoadSpritesFromPath(string assetPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            return assets.OfType<Sprite>().OrderBy(s => s.name).ToArray();
        }

        private static AnimationClip CreateAnimationClip(string savePath, Sprite[] sprites, float frameRate, bool isLoop)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(savePath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, savePath);
            }
            else
            {
                clip.ClearCurves();
            }

            clip.frameRate = frameRate;

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / frameRate,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = isLoop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateAnimatorController(string path, Dictionary<string, AnimationClip> clips)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            // 1. Thêm Parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("YVelocity", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

            var rootStateMachine = controller.layers[0].stateMachine;

            // 2. Tạo States
            AnimatorState idleState = null;
            AnimatorState runState = null;
            AnimatorState jumpState = null;
            AnimatorState attackState = null;
            AnimatorState hurtState = null;
            AnimatorState deathState = null;

            if (clips.TryGetValue("Pink_Idle", out var idleClip))
            {
                idleState = rootStateMachine.AddState("Idle");
                idleState.motion = idleClip;
                rootStateMachine.defaultState = idleState;
            }

            if (clips.TryGetValue("Pink_Run", out var runClip))
            {
                runState = rootStateMachine.AddState("Run");
                runState.motion = runClip;
            }

            if (clips.TryGetValue("Pink_Jump", out var jumpClip))
            {
                jumpState = rootStateMachine.AddState("Jump");
                jumpState.motion = jumpClip;
            }

            if (clips.TryGetValue("Pink_Attack1", out var attackClip))
            {
                attackState = rootStateMachine.AddState("Attack");
                attackState.motion = attackClip;
            }

            if (clips.TryGetValue("Pink_Hurt", out var hurtClip))
            {
                hurtState = rootStateMachine.AddState("Hurt");
                hurtState.motion = hurtClip;
            }

            if (clips.TryGetValue("Pink_Death", out var deathClip))
            {
                deathState = rootStateMachine.AddState("Death");
                deathState.motion = deathClip;
            }

            // 3. Thiết lập Transitions giữa các States
            // Idle <-> Run
            if (idleState != null && runState != null)
            {
                var idleToRun = idleState.AddTransition(runState);
                idleToRun.hasExitTime = false;
                idleToRun.duration = 0f;
                idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

                var runToIdle = runState.AddTransition(idleState);
                runToIdle.hasExitTime = false;
                runToIdle.duration = 0f;
                runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            }

            // Any State -> Jump (khi rời mặt đất)
            if (jumpState != null)
            {
                var anyToJump = rootStateMachine.AddAnyStateTransition(jumpState);
                anyToJump.hasExitTime = false;
                anyToJump.duration = 0f;
                anyToJump.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded"); // false handled below
                anyToJump.conditions = new[]
                {
                    new AnimatorCondition { mode = AnimatorConditionMode.IfNot, parameter = "IsGrounded", threshold = 0 },
                    new AnimatorCondition { mode = AnimatorConditionMode.IfNot, parameter = "IsDead", threshold = 0 }
                };

                if (idleState != null)
                {
                    var jumpToIdle = jumpState.AddTransition(idleState);
                    jumpToIdle.hasExitTime = false;
                    jumpToIdle.duration = 0f;
                    jumpToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
                }
            }

            // Any State -> Attack
            if (attackState != null && idleState != null)
            {
                var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
                anyToAttack.hasExitTime = false;
                anyToAttack.duration = 0f;
                anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");

                var attackToIdle = attackState.AddTransition(idleState);
                attackToIdle.hasExitTime = true;
                attackToIdle.exitTime = 1f;
                attackToIdle.duration = 0f;
            }

            // Any State -> Hurt
            if (hurtState != null && idleState != null)
            {
                var anyToHurt = rootStateMachine.AddAnyStateTransition(hurtState);
                anyToHurt.hasExitTime = false;
                anyToHurt.duration = 0f;
                anyToHurt.AddCondition(AnimatorConditionMode.If, 0, "Hurt");

                var hurtToIdle = hurtState.AddTransition(idleState);
                hurtToIdle.hasExitTime = true;
                hurtToIdle.exitTime = 1f;
                hurtToIdle.duration = 0f;
            }

            // Any State -> Death
            if (deathState != null)
            {
                var anyToDeath = rootStateMachine.AddAnyStateTransition(deathState);
                anyToDeath.hasExitTime = false;
                anyToDeath.duration = 0f;
                anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AssignToSelectedOrScenePlayer(RuntimeAnimatorController controller)
        {
            GameObject playerObj = Selection.activeGameObject;
            if (playerObj == null)
            {
                playerObj = GameObject.Find("Pink_Monster_0") ?? GameObject.Find("Player");
            }

            if (playerObj != null)
            {
                var animator = playerObj.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = playerObj.AddComponent<Animator>();
                }
                animator.runtimeAnimatorController = controller;
                Debug.Log($"[PlayerAnimationGenerator] Đã tự động gán Animator Controller vào '{playerObj.name}' trong Scene!");
            }
        }
    }
}
#endif
