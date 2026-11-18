#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Platformer2D.Environment.Platforms;
using Platformer2D.Environment.Hazards;
using Platformer2D.Environment.Interactive;
using Platformer2D.Items;
using Platformer2D.Managers;
using Platformer2D.Player.Controllers;
using Platformer2D.Player.Components;
using Platformer2D.Player.Input;
using Platformer2D.VFX;

namespace Platformer2D.EditorTools
{
    /// <summary>
    /// Công cụ Editor hỗ trợ tạo nhanh các đối tượng cơ chế (Mechanics) vào Scene:
    /// - One-Way Platform (Sàn 1 chiều)
    /// - Moving Platform (Sàn di chuyển)
    /// - Falling Platform (Sàn sụp lún)
    /// - Jump Pad (Bệ nhún lò xo)
    /// - Spike Hazard (Bẫy gai)
    /// - KillZone (Vực sâu tử thần)
    /// - Checkpoint & CheckpointManager
    /// - Health Pickup (Trái tim hồi máu)
    /// </summary>
    public static class MechanicsSetupTool
    {
        [MenuItem("Tools/Platformer Mechanics/Tự động đồng bộ đủ Components (Movement, Combat, Input, Animation) vào Player")]
        public static void EnsureAllPlayerComponents()
        {
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy PlayerController trong Scene hiện tại!", "OK");
                return;
            }

            if (player.GetComponent<PlayerInputHandler>() == null) Undo.AddComponent<PlayerInputHandler>(player.gameObject);
            if (player.GetComponent<PlayerMovement>() == null) Undo.AddComponent<PlayerMovement>(player.gameObject);
            if (player.GetComponent<PlayerCombat>() == null) Undo.AddComponent<PlayerCombat>(player.gameObject);
            if (player.GetComponent<PlayerAnimationSync>() == null) Undo.AddComponent<PlayerAnimationSync>(player.gameObject);
            if (player.GetComponent<GhostTrail>() == null) Undo.AddComponent<GhostTrail>(player.gameObject);

            EditorUtility.SetDirty(player.gameObject);
            EditorUtility.DisplayDialog("Thành công", "Đã đảm bảo Player có đầy đủ các components theo chuẩn SRP:\n- PlayerInputHandler\n- PlayerMovement\n- PlayerCombat\n- PlayerAnimationSync\n- GhostTrail", "Tuyệt vời");
        }

        [MenuItem("Tools/Platformer Mechanics/1. Gắn GhostTrail vào Player trong Scene")]
        public static void AttachGhostTrailToPlayer()
        {
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy PlayerController trong Scene hiện tại!", "OK");
                return;
            }

            if (player.GetComponent<GhostTrail>() == null)
            {
                Undo.AddComponent<GhostTrail>(player.gameObject);
                EditorUtility.DisplayDialog("Thành công", "Đã gắn thành công GhostTrail vào nhân vật Player!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Thông báo", "Player đã có sẵn component GhostTrail.", "OK");
            }
        }

        [MenuItem("Tools/Platformer Mechanics/2. Tạo CheckpointManager trong Scene")]
        public static void CreateCheckpointManager()
        {
            CheckpointManager manager = Object.FindAnyObjectByType<CheckpointManager>();
            if (manager != null)
            {
                Selection.activeGameObject = manager.gameObject;
                EditorUtility.DisplayDialog("Thông báo", "CheckpointManager đã tồn tại trong Scene!", "OK");
                return;
            }

            GameObject obj = new GameObject("CheckpointManager");
            Undo.RegisterCreatedObjectUndo(obj, "Create CheckpointManager");
            obj.AddComponent<CheckpointManager>();
            Selection.activeGameObject = obj;
            Debug.Log("[Mechanics] Đã tạo CheckpointManager trong Scene.");
        }

        [MenuItem("Tools/Platformer Mechanics/3. Tạo mẫu Sàn 1 chiều (One-Way Platform)")]
        public static void CreateOneWayPlatform()
        {
            GameObject obj = new GameObject("Platform_OneWay");
            Undo.RegisterCreatedObjectUndo(obj, "Create OneWayPlatform");

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 0.4f);
            col.usedByEffector = true;

            PlatformEffector2D effector = obj.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 170f;

            obj.AddComponent<OneWayPlatform>();

            Vector3 spawnPos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            spawnPos.z = 0;
            obj.transform.position = spawnPos;

            Selection.activeGameObject = obj;
            Debug.Log("[Mechanics] Đã tạo Sàn 1 chiều (One-Way Platform).");
        }

        [MenuItem("Tools/Platformer Mechanics/4. Tạo mẫu Sàn di chuyển (Moving Platform)")]
        public static void CreateMovingPlatform()
        {
            GameObject root = new GameObject("MovingPlatform_System");
            Undo.RegisterCreatedObjectUndo(root, "Create MovingPlatform");

            Vector3 spawnPos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            spawnPos.z = 0;
            root.transform.position = spawnPos;

            GameObject platform = new GameObject("Platform");
            platform.transform.SetParent(root.transform);
            platform.transform.localPosition = Vector3.zero;

            BoxCollider2D col = platform.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 0.6f);

            MovingPlatform mp = platform.AddComponent<MovingPlatform>();

            // Tạo sẵn 2 điểm Waypoints mẫu
            GameObject wp1 = new GameObject("Waypoint_A");
            wp1.transform.SetParent(root.transform);
            wp1.transform.localPosition = Vector3.zero;

            GameObject wp2 = new GameObject("Waypoint_B");
            wp2.transform.SetParent(root.transform);
            wp2.transform.localPosition = new Vector3(5f, 0f, 0f);

            // Gán SerializedProperty waypoints
            SerializedObject so = new SerializedObject(mp);
            SerializedProperty prop = so.FindProperty("_waypoints");
            prop.arraySize = 2;
            prop.GetArrayElementAtIndex(0).objectReferenceValue = wp1.transform;
            prop.GetArrayElementAtIndex(1).objectReferenceValue = wp2.transform;
            so.ApplyModifiedProperties();

            Selection.activeGameObject = platform;
            Debug.Log("[Mechanics] Đã tạo Sàn di chuyển (Moving Platform) với 2 Waypoints mẫu.");
        }

        [MenuItem("Tools/Platformer Mechanics/5. Tạo mẫu Sàn sụp lún (Falling Platform)")]
        public static void CreateFallingPlatform()
        {
            GameObject obj = new GameObject("Platform_Falling");
            Undo.RegisterCreatedObjectUndo(obj, "Create FallingPlatform");

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.5f, 0.5f);

            obj.AddComponent<FallingPlatform>();

            Vector3 spawnPos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            spawnPos.z = 0;
            obj.transform.position = spawnPos;

            Selection.activeGameObject = obj;
            Debug.Log("[Mechanics] Đã tạo Sàn sụp lún (Falling Platform).");
        }

        [MenuItem("Tools/Platformer Mechanics/6. Tạo mẫu Bệ nhún lò xo (Jump Pad)")]
        public static void CreateJumpPad()
        {
            GameObject obj = new GameObject("JumpPad");
            Undo.RegisterCreatedObjectUndo(obj, "Create JumpPad");

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 0.4f);
            col.isTrigger = true;

            obj.AddComponent<JumpPad>();

            Vector3 spawnPos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            spawnPos.z = 0;
            obj.transform.position = spawnPos;

            Selection.activeGameObject = obj;
            Debug.Log("[Mechanics] Đã tạo Bệ nhún (Jump Pad).");
        }

        [MenuItem("Tools/Platformer Mechanics/7. Tạo mẫu Bẫy chông gai (Spike Hazard)")]
        public static void CreateSpikeHazard()
        {
            GameObject obj = new GameObject("Hazard_Spikes");
            Undo.RegisterCreatedObjectUndo(obj, "Create SpikeHazard");

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 0.5f);
            col.isTrigger = true;

            obj.AddComponent<SpikeHazard>();

            Vector3 spawnPos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            spawnPos.z = 0;
            obj.transform.position = spawnPos;

            Selection.activeGameObject = obj;
            Debug.Log("[Mechanics] Đã tạo Bẫy chông gai (Spike Hazard).");
        }

        [MenuItem("Tools/Platformer Mechanics/8. Tạo Vực sâu tử thần (KillZone)")]
        public static void CreateKillZone()
        {
            GameObject obj = new GameObject("KillZone_Pit");
            Undo.RegisterCreatedObjectUndo(obj, "Create KillZone");

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(100f, 4f);
            col.isTrigger = true;

            obj.AddComponent<KillZone>();

            Vector3 spawnPos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            spawnPos.y = -10f; // Nằm sâu bên dưới màn chơi
            spawnPos.z = 0;
            obj.transform.position = spawnPos;

            Selection.activeGameObject = obj;
            Debug.Log("[Mechanics] Đã tạo Vực sâu tử thần (KillZone) ở độ cao Y = -10.");
        }

        [MenuItem("Tools/Platformer Mechanics/9. Tạo Cột mốc Checkpoint")]
        public static void CreateCheckpoint()
        {
            GameObject obj = new GameObject("Checkpoint");
            Undo.RegisterCreatedObjectUndo(obj, "Create Checkpoint");

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 2f);
            col.isTrigger = true;

            obj.AddComponent<Checkpoint>();

            Vector3 spawnPos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            spawnPos.z = 0;
            obj.transform.position = spawnPos;

            Selection.activeGameObject = obj;
            Debug.Log("[Mechanics] Đã tạo Checkpoint.");
        }

        [MenuItem("Tools/Platformer Mechanics/10. Tạo Trái tim hồi máu (Health Pickup)")]
        public static void CreateHealthPickup()
        {
            GameObject obj = new GameObject("Pickup_Heart");
            Undo.RegisterCreatedObjectUndo(obj, "Create HealthPickup");

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            Sprite defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/UI/2 Bars/Circle.png");
            if (defaultSprite != null)
            {
                sr.sprite = defaultSprite;
            }
            sr.color = new Color(1f, 0.25f, 0.45f, 1f); // Màu hồng/đỏ nổi bật

            CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.isTrigger = true;

            obj.AddComponent<HealthPickup>();

            Vector3 spawnPos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            spawnPos.z = 0;
            obj.transform.position = spawnPos;

            Selection.activeGameObject = obj;
            Debug.Log("[Mechanics] Đã tạo Trái tim hồi máu (Health Pickup).");
        }

        [MenuItem("Tools/Platformer Mechanics/11. Tạo Thang leo (Ladder)")]
        public static void CreateLadder()
        {
            GameObject obj = new GameObject("Ladder_Interactive");
            Undo.RegisterCreatedObjectUndo(obj, "Create Ladder");

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 4f);
            col.isTrigger = true;

            obj.AddComponent<Ladder>();

            Vector3 spawnPos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            spawnPos.z = 0;
            obj.transform.position = spawnPos;

            Selection.activeGameObject = obj;
            Debug.Log("[Mechanics] Đã tạo Thang leo (Ladder).");
        }

        [MenuItem("Tools/Platformer Mechanics/12. Tạo Thùng gỗ đẩy được (Pushable Box)")]
        public static void CreatePushableBox()
        {
            GameObject obj = new GameObject("Crate_Pushable");
            Undo.RegisterCreatedObjectUndo(obj, "Create PushableBox");

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            Sprite defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/UI/2 Bars/Square.png");
            if (defaultSprite != null)
            {
                sr.sprite = defaultSprite;
            }
            sr.color = new Color(0.8f, 0.5f, 0.25f, 1f);

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);

            obj.AddComponent<PushableBox>();

            Vector3 spawnPos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            spawnPos.z = 0;
            obj.transform.position = spawnPos;

            Selection.activeGameObject = obj;
            Debug.Log("[Mechanics] Đã tạo Thùng gỗ đẩy được (Pushable Box).");
        }
    }
}
#endif
