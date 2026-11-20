#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Platformer2D.EditorTools
{
    /// <summary>
    /// Tool tự động tạo toàn bộ Animation Clips (.anim) và Animator Controllers (.controller)
    /// cho tất cả các nhân vật (Player, Quái vật, Boss) trong toàn bộ gói Asset của dự án.
    /// - Quét tự động 30 nhân vật: 3 Players, 18 Monsters (Field/Cave/Hell), 9 Bosses (Field/Cave/Hell).
    /// - Nhận diện sprite sheet đa khung hình, tự động trích xuất frames theo thứ tự X tăng dần.
    /// - Tự động thiết lập loopTime (Idle/Walk/Run/Climb: Loop; Attack/Hurt/Death: Không Loop).
    /// - Tự động dựng Animator Controller hoàn chỉnh với AnyState transitions (Death, Hurt, Attack)
    ///   và xử lý triệt để lỗi loop kẹt self-transition (canTransitionToSelf = false).
    /// </summary>
    public class CharacterAnimationGeneratorTool : EditorWindow
    {
        private const float DefaultFrameRate = 12f;

        private Vector2 _scrollPos;
        private bool _generatePlayers = true;
        private bool _preservePinkMonster = true;
        private bool _generateMonsters = true;
        private bool _generateBosses = true;
        private bool _overwriteClips = true;
        private float _customFps = DefaultFrameRate;

        private string _lastLog = "Sẵn sàng tạo Animation cho toàn bộ 30 nhân vật.";

        [MenuItem("Tools/Animations/1. Tự động tạo Animation cho TẤT CẢ nhân vật (30 Nhân Vật)", false, 10)]
        public static void GenerateAllAnimationsDirectly()
        {
            int totalClips = 0;
            int totalControllers = 0;

            try
            {
                EditorUtility.DisplayProgressBar("Tạo Animation Nhân Vật", "Đang khởi tạo thư mục...", 0.05f);
                EnsureOutputDirectories();

                // 1. Players (3)
                GenerateCategory("Player", "Assets/GameAssets/Player", "Assets/Animations/Player", true, ref totalClips, ref totalControllers, 0.1f, 0.35f, DefaultFrameRate, true, true);

                // 2. Monsters (18)
                GenerateCategory("Monster-Field", "Assets/GameAssets/Monster-Field", "Assets/Animations/Enemies/Field", false, ref totalClips, ref totalControllers, 0.35f, 0.55f);
                GenerateCategory("Monster-Cave", "Assets/GameAssets/Monster-Cave", "Assets/Animations/Enemies/Cave", false, ref totalClips, ref totalControllers, 0.55f, 0.70f);
                GenerateCategory("Monster-Hell", "Assets/GameAssets/Monster-Hell", "Assets/Animations/Enemies/Hell", false, ref totalClips, ref totalControllers, 0.70f, 0.85f);

                // 3. Bosses (9)
                GenerateCategory("Boss-Field", "Assets/GameAssets/Boss-Field", "Assets/Animations/Bosses/Field", false, ref totalClips, ref totalControllers, 0.85f, 0.90f);
                GenerateCategory("Boss-Cave", "Assets/GameAssets/Boss-Cave", "Assets/Animations/Bosses/Cave", false, ref totalClips, ref totalControllers, 0.90f, 0.95f);
                GenerateCategory("Boss-Hell", "Assets/GameAssets/Boss-Hell", "Assets/Animations/Bosses/Hell", false, ref totalClips, ref totalControllers, 0.95f, 1.0f);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "Hoàn tất tạo Animation!",
                    $"Đã tạo thành công toàn bộ Animation cho gói asset:\n\n" +
                    $"• Tổng số Animation Clip (.anim): {totalClips}\n" +
                    $"• Tổng số Animator Controller (.controller): {totalControllers}\n" +
                    $"• Bao gồm đầy đủ 30 nhân vật (3 Players, 18 Monsters, 9 Bosses).\n\n" +
                    $"Vị trí lưu trữ: Assets/Animations/",
                    "Tuyệt vời!");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Tools/Animations/2. Cửa sổ Quản lý & Tạo Animation Nhân Vật", false, 11)]
        public static void OpenWindow()
        {
            var window = GetWindow<CharacterAnimationGeneratorTool>("Quản lý Animation");
            window.minSize = new Vector2(480, 560);
            window.Show();
        }

        [MenuItem("Tools/Animations/3. Gom gọn Animation Pink Monster vào thư mục riêng", false, 12)]
        public static void OrganizePinkMonsterDirectly()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Gom gọn Animation", "Đang xử lý thư mục Pink Monster...", 0.3f);
                string playerFolder = "Assets/Animations/Player";
                string pinkFolder = "Assets/Animations/Player/Pink_Monster";

                EnsureDirectory(pinkFolder);

                // 1. Quét các file gốc của Pink Monster nằm ngoài thư mục Player
                if (!Directory.Exists(playerFolder)) return;

                string[] rootFiles = Directory.GetFiles(playerFolder, "Pink_Monster*.*", SearchOption.TopDirectoryOnly)
                    .Where(p => !p.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (rootFiles.Length == 0)
                {
                    EditorUtility.DisplayDialog("Gom gọn Animation", "Tất cả file Pink Monster đã nằm gọn gàng bên trong Assets/Animations/Player/Pink_Monster/ rồi!", "OK");
                    return;
                }

                // 2. Dọn các file trùng lặp tạm trong subfolder (file chưa liên kết được tạo trước đó)
                string[] subFiles = Directory.GetFiles(pinkFolder, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(p => !p.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                foreach (string subFile in subFiles)
                {
                    string fName = Path.GetFileName(subFile);
                    // Nếu ở ngoài root cũng có file cùng tên thì xóa file tạm trong subfolder để nhường chỗ cho file gốc có GUID chuẩn
                    bool hasRootConflict = rootFiles.Any(r => Path.GetFileName(r).Equals(fName, StringComparison.OrdinalIgnoreCase));
                    if (hasRootConflict || fName.Equals("Pink_Monster.controller", StringComparison.OrdinalIgnoreCase)
                                        || fName.StartsWith("Pink_Monster_Attack1.anim", StringComparison.OrdinalIgnoreCase)
                                        || fName.StartsWith("Pink_Monster_Attack2.anim", StringComparison.OrdinalIgnoreCase))
                    {
                        AssetDatabase.DeleteAsset(subFile.Replace("\\", "/"));
                    }
                }

                // 3. Di chuyển toàn bộ file gốc từ Player/ vào Player/Pink_Monster/ bằng AssetDatabase.MoveAsset (Bảo toàn 100% GUID và Prefab reference)
                int movedCount = 0;
                foreach (string rootFile in rootFiles)
                {
                    string unityPath = rootFile.Replace("\\", "/");
                    string fileName = Path.GetFileName(unityPath);
                    string targetPath = $"{pinkFolder}/{fileName}";

                    string error = AssetDatabase.MoveAsset(unityPath, targetPath);
                    if (string.IsNullOrEmpty(error))
                    {
                        movedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"[AnimationTool] Không thể di chuyển {unityPath} -> {targetPath}: {error}");
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "Gom gọn thành công!",
                    $"Đã di chuyển an toàn {movedCount} file Animation & Controller của Pink Monster vào thư mục:\n" +
                    $"{pinkFolder}/\n\n" +
                    $"Toàn bộ GUID và liên kết Animator trên Player Prefab được bảo toàn nguyên vẹn 100%!",
                    "Hoàn tất");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("CÔNG CỤ TẠO ANIMATION TỰ ĐỘNG", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Tự động quét và tạo toàn bộ Animation Clips (.anim) cùng Animator Controllers (.controller) " +
                "cho 30 nhân vật (Players, Monsters, Bosses) từ các tệp Sprite Sheet trong Assets/GameAssets/.",
                MessageType.Info);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("TÙY CHỌN DANH MỤC:", EditorStyles.boldLabel);
            _generatePlayers = EditorGUILayout.ToggleLeft("Nhân vật Player (Pink, Owlet, Dude)", _generatePlayers);
            if (_generatePlayers)
            {
                EditorGUI.indentLevel++;
                _preservePinkMonster = EditorGUILayout.ToggleLeft("Bảo tồn cấu hình gốc Pink Monster (Đã có sẵn)", _preservePinkMonster);
                EditorGUI.indentLevel--;
            }
            _generateMonsters = EditorGUILayout.ToggleLeft("Quái vật thường (18 loài: Field, Cave, Hell)", _generateMonsters);
            _generateBosses = EditorGUILayout.ToggleLeft("Trùm / Bosses (9 con: Field, Cave, Hell)", _generateBosses);

            GUILayout.Space(8);
            EditorGUILayout.LabelField("CẤU HÌNH XUẤT:", EditorStyles.boldLabel);
            _customFps = EditorGUILayout.Slider("Frame Rate (FPS)", _customFps, 6f, 30f);
            _overwriteClips = EditorGUILayout.ToggleLeft("Ghi đè nếu đã tồn tại file", _overwriteClips);

            GUILayout.Space(12);
            GUI.backgroundColor = new Color(0.35f, 0.85f, 0.45f, 1f);
            if (GUILayout.Button("TẠO ANIMATION THEO TÙY CHỌN", GUILayout.Height(36)))
            {
                RunCustomGeneration();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(6);
            if (GUILayout.Button("TẠO TOÀN BỘ 30 NHÂN VẬT (1-CLICK ALL)", GUILayout.Height(30)))
            {
                GenerateAllAnimationsDirectly();
            }

            GUILayout.Space(6);
            GUI.backgroundColor = new Color(0.9f, 0.6f, 0.95f, 1f);
            if (GUILayout.Button("GOM GỌN PINK MONSTER VÀO THƯ MỤC RIÊNG", GUILayout.Height(30)))
            {
                OrganizePinkMonsterDirectly();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(15);
            EditorGUILayout.LabelField("TRẠNG THÁI TIẾN TRÌNH:", EditorStyles.boldLabel);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(140));
            EditorGUILayout.TextArea(_lastLog, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void RunCustomGeneration()
        {
            int totalClips = 0;
            int totalControllers = 0;

            try
            {
                EditorUtility.DisplayProgressBar("Tạo Animation", "Đang xử lý...", 0.1f);
                EnsureOutputDirectories();

                if (_generatePlayers)
                {
                    GenerateCategory("Player", "Assets/GameAssets/Player", "Assets/Animations/Player", true, ref totalClips, ref totalControllers, 0.1f, 0.4f, _customFps, _overwriteClips, _preservePinkMonster);
                }
                if (_generateMonsters)
                {
                    GenerateCategory("Monster-Field", "Assets/GameAssets/Monster-Field", "Assets/Animations/Enemies/Field", false, ref totalClips, ref totalControllers, 0.4f, 0.6f, _customFps, _overwriteClips);
                    GenerateCategory("Monster-Cave", "Assets/GameAssets/Monster-Cave", "Assets/Animations/Enemies/Cave", false, ref totalClips, ref totalControllers, 0.6f, 0.75f, _customFps, _overwriteClips);
                    GenerateCategory("Monster-Hell", "Assets/GameAssets/Monster-Hell", "Assets/Animations/Enemies/Hell", false, ref totalClips, ref totalControllers, 0.75f, 0.85f, _customFps, _overwriteClips);
                }
                if (_generateBosses)
                {
                    GenerateCategory("Boss-Field", "Assets/GameAssets/Boss-Field", "Assets/Animations/Bosses/Field", false, ref totalClips, ref totalControllers, 0.85f, 0.90f, _customFps, _overwriteClips);
                    GenerateCategory("Boss-Cave", "Assets/GameAssets/Boss-Cave", "Assets/Animations/Bosses/Cave", false, ref totalClips, ref totalControllers, 0.90f, 0.95f, _customFps, _overwriteClips);
                    GenerateCategory("Boss-Hell", "Assets/GameAssets/Boss-Hell", "Assets/Animations/Bosses/Hell", false, ref totalClips, ref totalControllers, 0.95f, 1.0f, _customFps, _overwriteClips);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                _lastLog = $"[Hoàn tất lúc {DateTime.Now:HH:mm:ss}]\n" +
                           $"• Tổng Animation Clips: {totalClips}\n" +
                           $"• Tổng Controllers: {totalControllers}\n" +
                           $"• Đã lưu tại: Assets/Animations/";
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void EnsureOutputDirectories()
        {
            string[] folders =
            {
                "Assets/Animations",
                "Assets/Animations/Player",
                "Assets/Animations/Enemies",
                "Assets/Animations/Enemies/Field",
                "Assets/Animations/Enemies/Cave",
                "Assets/Animations/Enemies/Hell",
                "Assets/Animations/Bosses",
                "Assets/Animations/Bosses/Field",
                "Assets/Animations/Bosses/Cave",
                "Assets/Animations/Bosses/Hell"
            };

            for (int i = 0; i < folders.Length; i++)
            {
                EnsureDirectory(folders[i]);
            }
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureDirectory(parent);
            }
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void GenerateCategory(
            string categoryName,
            string sourceRoot,
            string destRoot,
            bool isPlayerCategory,
            ref int totalClips,
            ref int totalControllers,
            float progressStart,
            float progressEnd,
            float fps = DefaultFrameRate,
            bool overwrite = true,
            bool preservePinkMonster = true)
        {
            if (!Directory.Exists(sourceRoot)) return;

            string[] characterDirs = Directory.GetDirectories(sourceRoot);
            int dirCount = characterDirs.Length;

            for (int i = 0; i < dirCount; i++)
            {
                string charDir = characterDirs[i].Replace("\\", "/");
                string dirName = Path.GetFileName(charDir);

                // Bỏ qua các thư mục không phải nhân vật như Font, PSD, Icon
                if (dirName.Equals("Font", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("PSD", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("Icon", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Tên chuẩn hóa của nhân vật (loại bỏ tiền tố số "1 ", "2 ", "3 "...)
                string cleanCharName = Regex.Replace(dirName, @"^\d+\s*", "").Trim();
                cleanCharName = cleanCharName.Replace(" ", "_");

                if (cleanCharName.Equals("Pink_Monster", StringComparison.OrdinalIgnoreCase) && preservePinkMonster)
                {
                    // Nếu Pink Monster đã có cấu hình hoàn chỉnh sẵn có thì bỏ qua để tránh ghi đè
                    bool hasExistingFiles = File.Exists("Assets/Animations/Player/Pink_Monster_0.controller") ||
                                           File.Exists("Assets/Animations/Player/Pink_Monster/Pink_Monster_0.controller");
                    if (hasExistingFiles)
                    {
                        Debug.Log("[AnimationTool] Bỏ qua Pink_Monster vì đã được thiết lập và cấu hình sẵn trong dự án.");
                        continue;
                    }
                }

                float p = Mathf.Lerp(progressStart, progressEnd, (float)i / Mathf.Max(1, dirCount));
                EditorUtility.DisplayProgressBar("Tạo Animation", $"Đang xử lý: {cleanCharName} ({categoryName})", p);

                string targetCharFolder = $"{destRoot}/{cleanCharName}";
                EnsureDirectory(targetCharFolder);

                // Quét toàn bộ file ảnh trong thư mục nhân vật
                string[] pngFiles = Directory.GetFiles(charDir, "*.png", SearchOption.TopDirectoryOnly);
                var createdClips = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);

                for (int f = 0; f < pngFiles.Length; f++)
                {
                    string pngPath = pngFiles[f].Replace("\\", "/");
                    string fileName = Path.GetFileNameWithoutExtension(pngPath);

                    // Bỏ qua các asset đạn hoặc dust VFX
                    if (IsIgnoredNonCharacterAsset(fileName, cleanCharName)) continue;

                    Sprite[] sprites = LoadSortedSprites(pngPath);
                    if (sprites == null || sprites.Length <= 1)
                    {
                        // Nếu chỉ có 1 sprite và trùng tên nhân vật -> Ảnh banner/chân dung, bỏ qua
                        continue;
                    }

                    string animName = ExtractAnimationName(fileName, cleanCharName);
                    bool isLoop = DetermineLoop(animName);

                    string clipPath = $"{targetCharFolder}/{animName}.anim";
                    AnimationClip clip = CreateOrUpdateClip(clipPath, sprites, fps, isLoop, overwrite);
                    if (clip != null)
                    {
                        createdClips[animName] = clip;
                        totalClips++;
                    }
                }

                // Tự động tạo / cập nhật Animator Controller cho nhân vật
                if (createdClips.Count > 0)
                {
                    string controllerPath = $"{targetCharFolder}/{cleanCharName}.controller";
                    if (isPlayerCategory)
                    {
                        CreatePlayerController(controllerPath, cleanCharName, createdClips);
                    }
                    else
                    {
                        CreateMonsterOrBossController(controllerPath, cleanCharName, createdClips);
                    }
                    totalControllers++;
                }
            }
        }

        private static bool IsIgnoredNonCharacterAsset(string fileName, string charName)
        {
            string lower = fileName.ToLowerInvariant();
            if (lower.Contains("dust") ||
                lower.Contains("rock1") || lower.Contains("rock2") ||
                lower.Contains("ball_attack") || lower.Contains("attack_ball") ||
                lower.Contains("attack_ray") || lower.Contains("attack_spike") ||
                lower.Contains("iceblock") || lower.Contains("fireball") ||
                lower.Contains("orb_of_venom") || lower.Contains("skull"))
            {
                return true;
            }

            // Ảnh chân dung đơn lẻ trùng tên nhân vật
            if (fileName.Equals(charName, StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals(charName.Replace("_", ""), StringComparison.OrdinalIgnoreCase))
            {
                return false; // Sẽ được kiểm tra sprites.Length <= 1 tiếp theo
            }

            return false;
        }

        private static string ExtractAnimationName(string fileName, string charName)
        {
            // 1. Loại bỏ số frame ở đuôi (ví dụ _4, _6, _8)
            string name = Regex.Replace(fileName, @"_\d+$", "");

            // 2. Chuẩn hóa ký tự '+' (ví dụ Walk+Attack -> Walk_Attack)
            name = name.Replace("+", "_");

            // 3. Nếu tên file chưa có tiền tố nhân vật thì thêm vào để phân biệt
            if (!name.StartsWith(charName, StringComparison.OrdinalIgnoreCase))
            {
                name = $"{charName}_{name}";
            }

            return name;
        }

        private static bool DetermineLoop(string animName)
        {
            string lower = animName.ToLowerInvariant();
            if (lower.Contains("idle") ||
                lower.Contains("walk") ||
                lower.Contains("run") ||
                lower.Contains("climb") ||
                lower.Contains("push") ||
                lower.Contains("fly") ||
                lower.Contains("move"))
            {
                return true;
            }

            // Hurt, Death, Attack, Jump, Throw, Sneer, Cast: không loop
            return false;
        }

        private static AnimationClip CreateOrUpdateClip(
            string clipPath,
            Sprite[] sprites,
            float fps,
            bool isLoop,
            bool overwrite)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip != null && !overwrite)
            {
                return clip;
            }

            bool isNew = clip == null;
            if (isNew)
            {
                clip = new AnimationClip();
            }
            else
            {
                clip.ClearCurves();
            }

            clip.frameRate = fps;

            // Gán Sprite curve cho SpriteRenderer ở root ("")
            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
            float frameDuration = 1f / Mathf.Max(1f, fps);

            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i * frameDuration,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = isLoop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (isNew)
            {
                AssetDatabase.CreateAsset(clip, clipPath);
            }
            else
            {
                EditorUtility.SetDirty(clip);
            }

            return clip;
        }

        #region Animator Controller Generators

        private static void CreatePlayerController(
            string controllerPath,
            string charName,
            Dictionary<string, AnimationClip> clips)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            // 1. Thêm đầy đủ Parameters chuẩn cho Player
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack2", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("WalkAttack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Throw", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsClimbing", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsPushing", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

            var rootStateMachine = controller.layers[0].stateMachine;

            // 2. Tìm các Animation Clip theo từ khóa
            AnimationClip idleClip = FindClip(clips, "idle");
            AnimationClip walkClip = FindClip(clips, "walk");
            AnimationClip runClip = FindClip(clips, "run") ?? walkClip;
            AnimationClip jumpClip = FindClip(clips, "jump");
            AnimationClip climbClip = FindClip(clips, "climb");
            AnimationClip pushClip = FindClip(clips, "push");
            AnimationClip atk1Clip = FindClip(clips, "attack1") ?? FindClip(clips, "attack");
            AnimationClip atk2Clip = FindClip(clips, "attack2");
            AnimationClip walkAtkClip = FindClip(clips, "walk_attack") ?? FindClip(clips, "walk+attack");
            AnimationClip throwClip = FindClip(clips, "throw");
            AnimationClip hurtClip = FindClip(clips, "hurt");
            AnimationClip deathClip = FindClip(clips, "death");

            // 3. Tạo các States
            AnimatorState idleState = AddState(rootStateMachine, $"{charName}_Idle", idleClip, new Vector3(200, 100));
            rootStateMachine.defaultState = idleState;

            AnimatorState walkState = AddState(rootStateMachine, $"{charName}_Walk", walkClip, new Vector3(460, 100));
            AnimatorState runState = AddState(rootStateMachine, $"{charName}_Run", runClip, new Vector3(720, 100));
            AnimatorState jumpState = AddState(rootStateMachine, $"{charName}_Jump", jumpClip, new Vector3(200, 240));
            AnimatorState climbState = AddState(rootStateMachine, $"{charName}_Climb", climbClip, new Vector3(460, 240));
            AnimatorState pushState = AddState(rootStateMachine, $"{charName}_Push", pushClip, new Vector3(720, 240));

            AnimatorState atk1State = AddState(rootStateMachine, $"{charName}_Attack_1", atk1Clip, new Vector3(200, -80));
            AnimatorState atk2State = AddState(rootStateMachine, $"{charName}_Attack_2", atk2Clip, new Vector3(460, -80));
            AnimatorState walkAtkState = AddState(rootStateMachine, $"{charName}_Walk_Attack", walkAtkClip, new Vector3(720, -80));
            AnimatorState throwState = AddState(rootStateMachine, $"{charName}_Throw", throwClip, new Vector3(200, -180));
            AnimatorState hurtState = AddState(rootStateMachine, $"{charName}_Hurt", hurtClip, new Vector3(460, -180));
            AnimatorState deathState = AddState(rootStateMachine, $"{charName}_Death", deathClip, new Vector3(720, -180));

            // 4. Locomotion Transitions (Idle <-> Walk <-> Run)
            if (walkState != null)
            {
                AddTransition(idleState, walkState, "Speed", AnimatorConditionMode.Greater, 0.05f);
                AddTransition(walkState, idleState, "Speed", AnimatorConditionMode.Less, 0.05f);
            }
            if (runState != null && runState != walkState)
            {
                AddTransition(walkState, runState, "Speed", AnimatorConditionMode.Greater, 0.65f);
                AddTransition(runState, walkState, "Speed", AnimatorConditionMode.Less, 0.65f);
            }

            // 5. AnyState Transitions (Hurt, Death, Attacks, Jump, Climb, Push)
            if (deathState != null)
            {
                var deathTrans = rootStateMachine.AddAnyStateTransition(deathState);
                deathTrans.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                deathTrans.duration = 0f;
                deathTrans.hasExitTime = false;
                deathTrans.canTransitionToSelf = false;
            }

            if (hurtState != null)
            {
                var hurtTrans = rootStateMachine.AddAnyStateTransition(hurtState);
                hurtTrans.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
                hurtTrans.duration = 0f;
                hurtTrans.hasExitTime = false;
                hurtTrans.canTransitionToSelf = false;
                AddExitTransition(hurtState, idleState);
            }

            if (jumpState != null)
            {
                var jumpTrans = rootStateMachine.AddAnyStateTransition(jumpState);
                jumpTrans.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
                jumpTrans.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                jumpTrans.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing");
                jumpTrans.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                jumpTrans.duration = 0f;
                jumpTrans.hasExitTime = false;
                jumpTrans.canTransitionToSelf = false;

                var landTrans = jumpState.AddTransition(idleState);
                landTrans.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
                landTrans.duration = 0f;
                landTrans.hasExitTime = false;
            }

            if (climbState != null)
            {
                var climbTrans = rootStateMachine.AddAnyStateTransition(climbState);
                climbTrans.AddCondition(AnimatorConditionMode.If, 0, "IsClimbing");
                climbTrans.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                climbTrans.duration = 0f;
                climbTrans.hasExitTime = false;
                climbTrans.canTransitionToSelf = false;

                var exitClimb = climbState.AddTransition(idleState);
                exitClimb.AddCondition(AnimatorConditionMode.IfNot, 0, "IsClimbing");
                exitClimb.duration = 0f;
                exitClimb.hasExitTime = false;
            }

            if (pushState != null)
            {
                var pushTrans = rootStateMachine.AddAnyStateTransition(pushState);
                pushTrans.AddCondition(AnimatorConditionMode.If, 0, "IsPushing");
                pushTrans.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                pushTrans.duration = 0f;
                pushTrans.hasExitTime = false;
                pushTrans.canTransitionToSelf = false;

                var exitPush = pushState.AddTransition(idleState);
                exitPush.AddCondition(AnimatorConditionMode.IfNot, 0, "IsPushing");
                exitPush.duration = 0f;
                exitPush.hasExitTime = false;
            }

            if (atk1State != null)
            {
                AddAnyTriggerTransition(rootStateMachine, atk1State, "Attack");
                AddExitTransition(atk1State, idleState);
            }
            if (atk2State != null)
            {
                AddAnyTriggerTransition(rootStateMachine, atk2State, "Attack2");
                AddExitTransition(atk2State, idleState);
            }
            if (walkAtkState != null)
            {
                AddAnyTriggerTransition(rootStateMachine, walkAtkState, "WalkAttack");
                AddExitTransition(walkAtkState, idleState);
            }
            if (throwState != null)
            {
                AddAnyTriggerTransition(rootStateMachine, throwState, "Throw");
                AddExitTransition(throwState, idleState);
            }

            EditorUtility.SetDirty(controller);
        }

        private static void CreateMonsterOrBossController(
            string controllerPath,
            string charName,
            Dictionary<string, AnimationClip> clips)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack2", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

            var rootStateMachine = controller.layers[0].stateMachine;

            AnimationClip idleClip = FindClip(clips, "idle");
            AnimationClip walkClip = FindClip(clips, "walk") ?? FindClip(clips, "run");
            AnimationClip hurtClip = FindClip(clips, "hurt");
            AnimationClip deathClip = FindClip(clips, "death");
            AnimationClip atk1Clip = FindClip(clips, "attack1") ?? FindClip(clips, "attack");
            AnimationClip atk2Clip = FindClip(clips, "attack2");
            AnimationClip sneerClip = FindClip(clips, "sneer");

            if (idleClip == null && walkClip != null) idleClip = walkClip;

            AnimatorState idleState = AddState(rootStateMachine, $"{charName}_Idle", idleClip, new Vector3(200, 100));
            rootStateMachine.defaultState = idleState;

            AnimatorState walkState = AddState(rootStateMachine, $"{charName}_Walk", walkClip, new Vector3(460, 100));
            AnimatorState hurtState = AddState(rootStateMachine, $"{charName}_Hurt", hurtClip, new Vector3(200, -80));
            AnimatorState deathState = AddState(rootStateMachine, $"{charName}_Death", deathClip, new Vector3(460, -80));
            AnimatorState atk1State = AddState(rootStateMachine, $"{charName}_Attack", atk1Clip, new Vector3(200, 240));
            AnimatorState atk2State = AddState(rootStateMachine, $"{charName}_Attack2", atk2Clip, new Vector3(460, 240));
            AnimatorState sneerState = AddState(rootStateMachine, $"{charName}_Sneer", sneerClip, new Vector3(720, 100));

            // Locomotion (Idle <-> Walk)
            if (walkState != null && walkState != idleState)
            {
                AddTransition(idleState, walkState, "Speed", AnimatorConditionMode.Greater, 0.05f);
                AddTransition(walkState, idleState, "Speed", AnimatorConditionMode.Less, 0.05f);
            }

            // Death
            if (deathState != null)
            {
                var deathTrans = rootStateMachine.AddAnyStateTransition(deathState);
                deathTrans.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                deathTrans.duration = 0f;
                deathTrans.hasExitTime = false;
                deathTrans.canTransitionToSelf = false;
            }

            // Hurt
            if (hurtState != null)
            {
                var hurtTrans = rootStateMachine.AddAnyStateTransition(hurtState);
                hurtTrans.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
                hurtTrans.duration = 0f;
                hurtTrans.hasExitTime = false;
                hurtTrans.canTransitionToSelf = false;
                AddExitTransition(hurtState, idleState);
            }

            // Attacks
            if (atk1State != null)
            {
                AddAnyTriggerTransition(rootStateMachine, atk1State, "Attack");
                AddExitTransition(atk1State, idleState);
            }
            if (atk2State != null)
            {
                AddAnyTriggerTransition(rootStateMachine, atk2State, "Attack2");
                AddExitTransition(atk2State, idleState);
            }
            if (sneerState != null)
            {
                AddExitTransition(sneerState, idleState);
            }

            EditorUtility.SetDirty(controller);
        }

        private static AnimatorState AddState(
            AnimatorStateMachine stateMachine,
            string stateName,
            AnimationClip clip,
            Vector3 position)
        {
            if (clip == null) return null;
            var state = stateMachine.AddState(stateName, position);
            state.motion = clip;
            return state;
        }

        private static void AddTransition(
            AnimatorState from,
            AnimatorState to,
            string paramName,
            AnimatorConditionMode mode,
            float threshold)
        {
            if (from == null || to == null) return;
            var trans = from.AddTransition(to);
            trans.AddCondition(mode, threshold, paramName);
            trans.duration = 0f;
            trans.hasExitTime = false;
        }

        private static void AddAnyTriggerTransition(
            AnimatorStateMachine sm,
            AnimatorState to,
            string triggerParam)
        {
            if (sm == null || to == null) return;
            var trans = sm.AddAnyStateTransition(to);
            trans.AddCondition(AnimatorConditionMode.If, 0, triggerParam);
            trans.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            trans.duration = 0f;
            trans.hasExitTime = false;
            trans.canTransitionToSelf = false;
        }

        private static void AddExitTransition(AnimatorState from, AnimatorState to)
        {
            if (from == null || to == null) return;
            var trans = from.AddTransition(to);
            trans.hasExitTime = true;
            trans.exitTime = 1f;
            trans.duration = 0.05f;
        }

        private static AnimationClip FindClip(Dictionary<string, AnimationClip> clips, string keyword)
        {
            foreach (var kvp in clips)
            {
                if (kvp.Key.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        private static Sprite[] LoadSortedSprites(string assetPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null) return Array.Empty<Sprite>();

            return assets.OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
        }

        #endregion
    }
}
#endif
