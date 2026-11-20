#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Platformer2D.CameraControl;
using Platformer2D.Combat;
using Platformer2D.Core.Architecture;
using Platformer2D.Core.ObjectPool;
using Platformer2D.Enemies.Base;
using Platformer2D.Environment.Hazards;
using Platformer2D.Environment.Interactive;
using Platformer2D.Environment.Platforms;
using Platformer2D.Environment.Visuals;
using Platformer2D.Items;
using Platformer2D.Managers;
using Platformer2D.Player.Components;
using Platformer2D.Player.Controllers;

namespace Platformer2D.EditorTools
{
    /// <summary>
    /// Công cụ chuẩn hóa kiến trúc Scene & Prefabs cho dự án:
    /// - Tách biệt Scene Chính (Assets/Scenes/Main/) và Scene Test Cơ Chế (Assets/Scenes/Testing/).
    /// - Đóng gói Player, Camera, Light, GameSystems và toàn bộ vật thể cơ chế thành Prefab chuẩn tại Assets/Prefabs/.
    /// - Tự động dựng MechanicsTestScene (4 khu vực test toàn bộ cơ chế) và đăng ký EditorBuildSettings.
    /// - Giữ nguyên SampleScene.unity làm bản sao lưu an toàn.
    /// </summary>
    public static class SceneArchitectureTool
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        private const string PrefabGameSystemsPath = "Assets/Prefabs/Core/GameSystems.prefab";
        private const string PrefabCameraRigPath = "Assets/Prefabs/Core/MainCameraRig.prefab";
        private const string PrefabGlobalLightPath = "Assets/Prefabs/Core/GlobalLight2D.prefab";
        private const string PrefabPlayerPath = "Assets/Prefabs/Player/Player.prefab";

        private const string PrefabLadderPath = "Assets/Prefabs/Environment/Ladder_Interactive.prefab";
        private const string PrefabCratePath = "Assets/Prefabs/Environment/Crate_Pushable.prefab";
        private const string PrefabOneWayPath = "Assets/Prefabs/Environment/Platform_OneWay.prefab";
        private const string PrefabJumpPadPath = "Assets/Prefabs/Environment/JumpPad.prefab";
        private const string PrefabFallingPath = "Assets/Prefabs/Environment/FallingPlatform.prefab";
        private const string PrefabSpikePath = "Assets/Prefabs/Environment/SpikeHazard.prefab";
        private const string PrefabCheckpointPath = "Assets/Prefabs/Environment/Checkpoint.prefab";
        private const string PrefabSilverChestPath = "Assets/Prefabs/Environment/SilverChest.prefab";
        private const string PrefabVictoryGoalPath = "Assets/Prefabs/Environment/VictoryGoal.prefab";

        private const string PrefabCoinPath = "Assets/Prefabs/Items/CoinItem.prefab";
        private const string PrefabRockPickupPath = "Assets/Prefabs/Items/RockPickup.prefab";
        private const string PrefabHealthPickupPath = "Assets/Prefabs/Items/HealthPickup.prefab";
        private const string PrefabSilverKeyPath = "Assets/Prefabs/Items/SilverKey.prefab";
        private const string PrefabGoldenKeyPath = "Assets/Prefabs/Items/GoldenKey.prefab";
        private const string PrefabEnemyOozePath = "Assets/Prefabs/Enemies/Enemy_Ooze_Test.prefab";

        // Field Tileset (Mặt đất cỏ, Sàn 1 chiều, Trần đất)
        private const string SpriteFieldTopLeft = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_01.png";
        private const string SpriteFieldTopMid = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_02.png";
        private const string SpriteFieldTopRight = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_03.png";
        private const string SpriteFieldFillLeft = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_08.png";
        private const string SpriteFieldFillMid = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_09.png";
        private const string SpriteFieldFillRight = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_10.png";
        private const string SpriteFieldBotLeft = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_15.png";
        private const string SpriteFieldBotMid = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_16.png";
        private const string SpriteFieldBotRight = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_17.png";
        private const string SpriteFieldCeilingLeft = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_22.png";
        private const string SpriteFieldCeilingMid = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_23.png";
        private const string SpriteFieldCeilingRight = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_24.png";
        private const string SpriteFieldLedgeLeft = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_36.png";
        private const string SpriteFieldLedgeMid = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_37.png";
        private const string SpriteFieldLedgeRight = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_38.png";

        // Field Tileset bổ sung (Đảo cỏ di chuyển, Bệ sỏi vụn sụp lún, Trụ đất phủ cỏ & Thùng gỗ lò xo)
        private const string SpriteFieldWallTop = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_04.png";
        private const string SpriteFieldWallMid = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_11.png";
        private const string SpriteFieldWallBot = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_18.png";
        private const string SpriteFieldMoveLeft = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_22.png";
        private const string SpriteFieldMoveMid = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_23.png";
        private const string SpriteFieldMoveRight = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_24.png";
        private const string SpriteFieldFallLeft = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_39.png";
        private const string SpriteFieldFallMid = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_40.png";
        private const string SpriteFieldFallRight = "Assets/GameAssets/Field-Tileset/Tiles/Tileset/TileSet_41.png";
        private const string SpriteJumpBarrel = "Assets/GameAssets/Field-Tileset/Objects/Boxes/Barrel2.png";

        // Background Parallax 8 lớp & Props trang trí khu vực Field
        private const string SpriteSkyBackground = "Assets/GameAssets/Field-Tileset/Background/Background.png";
        private const string FieldParallaxDir = "Assets/GameAssets/Field-Tileset/Background/Layers/";
        private const string SpriteTree1 = "Assets/GameAssets/Field-Tileset/Objects/Trees/Tree1.png";
        private const string SpriteTree2 = "Assets/GameAssets/Field-Tileset/Objects/Trees/Tree2.png";
        private const string SpriteTree3 = "Assets/GameAssets/Field-Tileset/Objects/Trees/Tree3.png";
        private const string SpriteTree4 = "Assets/GameAssets/Field-Tileset/Objects/Trees/Tree4.png";
        private const string SpriteTreeBack1 = "Assets/GameAssets/Field-Tileset/Objects/Trees/Tree_Back1.png";
        private const string SpriteTreeBack2 = "Assets/GameAssets/Field-Tileset/Objects/Trees/Tree_Back2.png";
        private const string SpriteTreeBack3 = "Assets/GameAssets/Field-Tileset/Objects/Trees/Tree_Back3.png";
        private const string SpriteTreeBack4 = "Assets/GameAssets/Field-Tileset/Objects/Trees/Tree_Back4.png";
        private const string SpriteGrassTuft = "Assets/GameAssets/Field-Tileset/Objects/Grass/Grass1_1.png";
        private const string SpriteGrass2 = "Assets/GameAssets/Field-Tileset/Objects/Grass/Grass2_1.png";
        private const string SpriteGrass3 = "Assets/GameAssets/Field-Tileset/Objects/Grass/Grass3_1.png";
        private const string SpriteGrass4 = "Assets/GameAssets/Field-Tileset/Objects/Grass/Grass4_1.png";
        private const string SpriteGrass5 = "Assets/GameAssets/Field-Tileset/Objects/Grass/Grass5_1.png";
        private const string SpriteGrass6 = "Assets/GameAssets/Field-Tileset/Objects/Grass/Grass6_1.png";
        private const string SpriteRockSmall = "Assets/GameAssets/Field-Tileset/Objects/Rocks/Rock_Small1.png";
        private const string SpriteRockSmall2 = "Assets/GameAssets/Field-Tileset/Objects/Rocks/Rock_Small2.png";
        private const string SpriteRockBig = "Assets/GameAssets/Field-Tileset/Objects/Rocks/Rock_Big1.png";
        private const string SpriteRockBig2 = "Assets/GameAssets/Field-Tileset/Objects/Rocks/Rock_Big2.png";
        private const string SpriteSignRight = "Assets/GameAssets/Field-Tileset/Objects/Pointers/1_1.png";
        private const string SpriteSignJumpUp = "Assets/GameAssets/Field-Tileset/Objects/Pointers/3_1.png";

        private struct DonorAssets
        {
            public GameObject PlayerPrefab;
            public GameObject CameraRigPrefab;
            public GameObject GlobalLightPrefab;
            public GameObject GameSystemsPrefab;
            public Material SpriteMaterial;
            public Sprite SquareSprite;
            public PhysicsMaterial2D FrictionlessMaterial;
        }

        private struct EnvironmentPrefabs
        {
            public GameObject Ladder;
            public GameObject Crate;
            public GameObject OneWayPlatform;
            public GameObject JumpPad;
            public GameObject FallingPlatform;
            public GameObject Spike;
            public GameObject Checkpoint;
            public GameObject Coin;
            public GameObject RockPickup;
            public GameObject HealthPickup;
            public GameObject SilverKey;
            public GameObject GoldenKey;
            public GameObject SilverChest;
            public GameObject EnemyOoze;
            public GameObject VictoryGoal;
        }

        [MenuItem("Tools/Platformer Scenes/0. Setup Kiến trúc Scene & Core Prefabs")]
        public static void SetupProjectArchitecture()
        {
            if (!File.Exists(SampleScenePath))
            {
                EditorUtility.DisplayDialog("Thiếu Scene gốc", $"Không tìm thấy {SampleScenePath} để trích xuất Player & Camera đã căn chỉnh!", "Đóng");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Setup Kiến trúc Scene & Prefabs",
                "Công cụ sẽ thực hiện:\n" +
                "1. Giữ nguyên SampleScene.unity làm bản sao lưu.\n" +
                "2. Đóng gói Prefab chuẩn vào Assets/Prefabs/ (Player, Camera, GameSystems, UI, Cơ chế, Quái, Vật phẩm).\n" +
                "3. Tạo 3 Scene chuẩn với Pixel Art tileset, nền môi trường & hệ thống UI + Kỹ năng:\n" +
                "   - Main/MainMenu.unity\n" +
                "   - Main/Level_01.unity\n" +
                "   - Testing/MechanicsTestScene.unity (4 khu test đầy đủ)\n" +
                "4. Cập nhật Build Settings và mở sẵn MechanicsTestScene.\n\nTiếp tục?",
                "Chạy Setup", "Hủy");

            if (!confirmed) return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolderStructure();

            // Bước 1: Mở SampleScene ở chế độ chỉ đọc để clone Player, Camera, Light và Material chuẩn URP 2D
            DonorAssets donors = ExtractDonorPrefabsFromSampleScene();

            // Bước 2: Đóng gói GameSystems, UI Prefabs và toàn bộ Prefab cơ chế môi trường / vật phẩm / quái
            donors.GameSystemsPrefab = CreateGameSystemsPrefab();
            EnvironmentPrefabs envPrefabs = CreateGameplayPrefabs(donors);
            UISetupTool.BuildUIPrefabsOnly();

            // Bước 3: Dựng 3 Scene (MainMenu, Level_01, MechanicsTestScene)
            CreateMainMenuScene(donors);
            CreateLevel01Scene(donors, envPrefabs);
            CreateMechanicsTestScene(donors, envPrefabs);

            // Bước 4: Cấu hình Build Settings & mở MechanicsTestScene để test ngay
            ConfigureBuildSettings();
            EditorSceneManager.OpenScene(SceneNames.MechanicsTestPath, OpenSceneMode.Single);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Hoàn tất Setup!",
                "Đã tổ chức xong kiến trúc Scene & Prefabs chuyên nghiệp:\n" +
                "• Scene Test: Assets/Scenes/Testing/MechanicsTestScene.unity (đang mở)\n" +
                "• Scene Chính: Assets/Scenes/Main/MainMenu.unity & Level_01.unity\n" +
                "• Core & Mechanics Prefabs: Assets/Prefabs/\n\n" +
                "Bạn có thể bấm Play ngay để test 4 khu cơ chế trong MechanicsTestScene!",
                "Tuyệt vời");
        }

        [MenuItem("Tools/Platformer Scenes/1. Mở MechanicsTestScene (Test Cơ Chế)")]
        public static void OpenMechanicsTestScene() => OpenTargetScene(SceneNames.MechanicsTestPath);

        [MenuItem("Tools/Platformer Scenes/2. Mở MainMenu (Scene Chính)")]
        public static void OpenMainMenuScene() => OpenTargetScene(SceneNames.MainMenuPath);

        [MenuItem("Tools/Platformer Scenes/3. Mở Level_01 (Scene Chính)")]
        public static void OpenLevel01Scene() => OpenTargetScene(SceneNames.Level01Path);

        private static void OpenTargetScene(string scenePath)
        {
            if (!File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog(
                    "Chưa có Scene",
                    $"Chưa tìm thấy '{scenePath}'.\nHãy chạy 'Tools -> Platformer Scenes -> 0. Setup Kiến trúc Scene & Core Prefabs' trước nhé!",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        private static void EnsureFolderStructure()
        {
            string[] folders =
            {
                "Assets/Prefabs/Core",
                "Assets/Prefabs/Player",
                "Assets/Prefabs/Environment",
                "Assets/Prefabs/Items",
                "Assets/Prefabs/Enemies",
                "Assets/Scenes/Main",
                "Assets/Scenes/Testing"
            };

            for (int i = 0; i < folders.Length; i++)
            {
                EnsureFolder(folders[i]);
            }
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath)) return;

            string[] parts = assetFolderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static DonorAssets ExtractDonorPrefabsFromSampleScene()
        {
            EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);

            DonorAssets donors = new DonorAssets
            {
                FrictionlessMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/Frictionless2D.physicsMaterial2D")
            };

            GameObject groundObj = GameObject.Find("Ground");
            if (groundObj != null && groundObj.TryGetComponent<SpriteRenderer>(out var groundSr))
            {
                donors.SpriteMaterial = groundSr.sharedMaterial;
                donors.SquareSprite = groundSr.sprite;
            }
            if (donors.SquareSprite == null)
            {
                donors.SquareSprite = LoadFirstSprite("Assets/GameAssets/UI/2 Bars/Square.png");
            }

            PlayerController playerCtrl = Object.FindAnyObjectByType<PlayerController>();
            if (playerCtrl != null)
            {
                GameObject playerTemp = Object.Instantiate(playerCtrl.gameObject);
                playerTemp.name = "Player";
                playerTemp.transform.position = Vector3.zero;
                PlayerSkills skills = playerTemp.GetComponent<PlayerSkills>() ?? playerTemp.AddComponent<PlayerSkills>();
                ConfigurePlayerSkillAssets(skills);
                donors.PlayerPrefab = SaveTempAsPrefab(playerTemp, PrefabPlayerPath);
            }

            CameraFollow2D camFollow = Object.FindAnyObjectByType<CameraFollow2D>();
            if (camFollow != null)
            {
                GameObject camTemp = Object.Instantiate(camFollow.gameObject);
                camTemp.name = "MainCameraRig";
                camTemp.transform.position = new Vector3(0f, 0f, -10f);
                SetObjectProperty(camTemp.GetComponent<CameraFollow2D>(), "_target", null);
                donors.CameraRigPrefab = SaveTempAsPrefab(camTemp, PrefabCameraRigPath);
            }

            GameObject lightObj = GameObject.Find("Global Light 2D");
            if (lightObj != null)
            {
                bool wasActive = lightObj.activeSelf;
                lightObj.SetActive(false);
                try
                {
                    GameObject lightTemp = Object.Instantiate(lightObj);
                    lightTemp.name = "GlobalLight2D";
                    lightTemp.SetActive(wasActive);
                    donors.GlobalLightPrefab = SaveTempAsPrefab(lightTemp, PrefabGlobalLightPath);
                }
                finally
                {
                    lightObj.SetActive(wasActive);
                }
            }
            else
            {
                GameObject fallbackLight = new GameObject("GlobalLight2D");
                donors.GlobalLightPrefab = SaveTempAsPrefab(fallbackLight, PrefabGlobalLightPath);
            }

            // Không lưu đè lên SampleScene.unity nhằm bảo toàn bản gốc
            return donors;
        }

        private static GameObject CreateGameSystemsPrefab()
        {
            GameObject root = new GameObject("GameSystems");

            CreateSystemChild<GameManager>(root.transform, "GameManager");
            CreateSystemChild<SceneLoader>(root.transform, "SceneLoader");
            CreateSystemChild<AudioManager>(root.transform, "AudioManager");
            CreateSystemChild<ObjectPoolManager>(root.transform, "ObjectPoolManager");

            return SaveTempAsPrefab(root, PrefabGameSystemsPath);
        }

        private static void CreateSystemChild<T>(Transform parent, string childName) where T : Component
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent, false);
            child.AddComponent<T>();
        }

        private static EnvironmentPrefabs CreateGameplayPrefabs(DonorAssets donors)
        {
            EnvironmentPrefabs prefabs = new EnvironmentPrefabs();

            // 1. Ladder
            GameObject ladder = new GameObject("Ladder_Interactive");
            BoxCollider2D ladderCol = ladder.AddComponent<BoxCollider2D>();
            ladderCol.size = new Vector2(0.75f, 4.20f);
            ladderCol.offset = new Vector2(0f, 0.10f);
            ladderCol.isTrigger = true;
            ladder.AddComponent<Ladder>();

            Sprite ladderDown = LoadFirstSprite("Assets/GameAssets/Field-Tileset/Objects/Ladders/Ladder_down_1.png");
            Sprite ladderMid = LoadFirstSprite("Assets/GameAssets/Field-Tileset/Objects/Ladders/Ladder_middle_1.png");
            Sprite ladderTop = LoadFirstSprite("Assets/GameAssets/Field-Tileset/Objects/Ladders/Ladder_top_1.png");
            (string name, float y, Sprite sprite)[] segments =
            {
                ("Segment_Bottom", -1.5f, ladderDown),
                ("Segment_Mid_1", -0.5f, ladderMid),
                ("Segment_Mid_2", 0.5f, ladderMid),
                ("Segment_Top", 1.5f, ladderTop)
            };
            foreach (var seg in segments)
            {
                GameObject segObj = new GameObject(seg.name);
                segObj.transform.SetParent(ladder.transform, false);
                segObj.transform.localPosition = new Vector3(0f, seg.y, 0f);
                SpriteRenderer sr = segObj.AddComponent<SpriteRenderer>();
                sr.sprite = seg.sprite;
                if (donors.SpriteMaterial != null) sr.sharedMaterial = donors.SpriteMaterial;
                sr.sortingOrder = -1;
            }
            prefabs.Ladder = SaveTempAsPrefab(ladder, PrefabLadderPath);

            // 1b. Rock Pickup (tạo trước để gắn làm vật phẩm rơi mặc định cho Thùng gỗ)
            GameObject rockPickup = CreateSpriteObject("RockPickup", "Assets/GameAssets/Player/1 Pink_Monster/Rock2.png", donors, Color.white, 1);
            rockPickup.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
            CircleCollider2D rockCol = rockPickup.AddComponent<CircleCollider2D>();
            rockCol.isTrigger = true;
            rockCol.radius = 0.22f;
            RockPickup rockComp = rockPickup.AddComponent<RockPickup>();
            SetIntProperty(rockComp, "_rockAmount", 3);
            prefabs.RockPickup = SaveTempAsPrefab(rockPickup, PrefabRockPickupPath);

            // 2. Pushable Crate
            GameObject crate = CreateSpriteObject("Crate_Pushable", "Assets/GameAssets/Field-Tileset/Objects/Boxes/Box1.png", donors, Color.white, 0);
            BoxCollider2D crateCol = crate.AddComponent<BoxCollider2D>();
            crateCol.size = new Vector2(0.96f, 0.75f);
            crateCol.offset = new Vector2(0f, -0.015625f);
            if (donors.FrictionlessMaterial != null) crateCol.sharedMaterial = donors.FrictionlessMaterial;
            Rigidbody2D crateRb = crate.AddComponent<Rigidbody2D>();
            if (donors.FrictionlessMaterial != null) crateRb.sharedMaterial = donors.FrictionlessMaterial;
            PushableBox pushable = crate.AddComponent<PushableBox>();
            SetBoolProperty(pushable, "_isBreakable", true);
            SetIntProperty(pushable, "_hitsToBreak", 3);
            SetObjectProperty(pushable, "_dropPrefab", prefabs.RockPickup);
            prefabs.Crate = SaveTempAsPrefab(crate, PrefabCratePath);

            // 3. One-Way Platform (Bệ cỏ mỏng 3 tile)
            GameObject oneWay = new GameObject("Platform_OneWay");
            GameObject oneWayVisuals = new GameObject("Visuals");
            oneWayVisuals.transform.SetParent(oneWay.transform, false);
            float oneWayWidth = BuildHorizontalTileStrip(
                oneWayVisuals.transform,
                3,
                LoadFirstSprite(SpriteFieldLedgeLeft),
                LoadFirstSprite(SpriteFieldLedgeMid),
                LoadFirstSprite(SpriteFieldLedgeRight),
                0.15f,
                donors,
                -2);
            BoxCollider2D oneWayCol = oneWay.AddComponent<BoxCollider2D>();
            oneWayCol.size = new Vector2(oneWayWidth > 0f ? oneWayWidth : 3.0f, 0.50f);
            oneWayCol.offset = new Vector2(0f, -0.10f);
            oneWayCol.usedByEffector = true;
            PlatformEffector2D effector = oneWay.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 170f;
            oneWay.AddComponent<OneWayPlatform>();
            prefabs.OneWayPlatform = SaveTempAsPrefab(oneWay, PrefabOneWayPath);

            // 4. Jump Pad (Thùng gỗ lò xo bật nhảy đồng bộ Field)
            GameObject jumpPad = new GameObject("JumpPad");
            GameObject barrelVisual = CreateSpriteObject("Visual", SpriteJumpBarrel, donors, Color.white, 0);
            barrelVisual.transform.SetParent(jumpPad.transform, false);
            const float jumpPadScale = 1.15f;
            barrelVisual.transform.localScale = new Vector3(jumpPadScale, jumpPadScale, 1f);
            Sprite barrelSprite = barrelVisual.GetComponent<SpriteRenderer>().sprite;
            float barrelOffsetY = barrelSprite != null ? -barrelSprite.bounds.min.y * jumpPadScale : 0.45f;
            barrelVisual.transform.localPosition = new Vector3(0f, barrelOffsetY, 0f);
            BoxCollider2D jpCol = jumpPad.AddComponent<BoxCollider2D>();
            jpCol.size = new Vector2(0.95f, 0.86f);
            jpCol.offset = new Vector2(0f, 0.43f);
            JumpPad jpComp = jumpPad.AddComponent<JumpPad>();
            SetFloatProperty(jpComp, "_bounceForce", 15.5f);
            prefabs.JumpPad = SaveTempAsPrefab(jumpPad, PrefabJumpPadPath);

            // 5. Falling Platform (Bệ đất sỏi mỏng sụp lún Field)
            GameObject falling = new GameObject("FallingPlatform");
            GameObject fallingVisuals = new GameObject("Visuals");
            fallingVisuals.transform.SetParent(falling.transform, false);
            float fallingWidth = BuildHorizontalTileStrip(
                fallingVisuals.transform,
                2,
                LoadFirstSprite(SpriteFieldFallLeft),
                LoadFirstSprite(SpriteFieldFallMid),
                LoadFirstSprite(SpriteFieldFallRight),
                0.20f,
                donors,
                0);
            BoxCollider2D fallingCol = falling.AddComponent<BoxCollider2D>();
            fallingCol.size = new Vector2(fallingWidth > 0f ? fallingWidth : 1.97f, 0.40f);
            fallingCol.offset = Vector2.zero;
            Rigidbody2D fallingRb = falling.AddComponent<Rigidbody2D>();
            fallingRb.bodyType = RigidbodyType2D.Kinematic;
            falling.AddComponent<FallingPlatform>();
            prefabs.FallingPlatform = SaveTempAsPrefab(falling, PrefabFallingPath);

            // 6. Spike Hazard
            GameObject spike = CreateSpriteObject("SpikeHazard", "Assets/GameAssets/Field-Tileset/Objects/Spikes/Spikes1_1.png", donors, Color.white, 0);
            BoxCollider2D spikeCol = spike.AddComponent<BoxCollider2D>();
            spikeCol.isTrigger = true;
            spikeCol.size = new Vector2(0.80f, 0.58f);
            spikeCol.offset = new Vector2(0f, -0.085f);
            SpikeHazard spikeComp = spike.AddComponent<SpikeHazard>();
            SetIntProperty(spikeComp, "_damage", 1);
            SetBoolProperty(spikeComp, "_instantKill", false);
            SetVector2Property(spikeComp, "_knockbackForce", new Vector2(4f, 7f));
            prefabs.Spike = SaveTempAsPrefab(spike, PrefabSpikePath);

            Sprite[] pickupVfxFrames = LoadAllSpritesSorted("Assets/GameAssets/Field-Tileset/Objects_Animated/Pickup.png");

            // 7. Checkpoint
            GameObject checkpoint = CreateSpriteObject("Checkpoint", "Assets/GameAssets/Field-Tileset/Objects_Animated/Flag.png", donors, Color.white, -1);
            BoxCollider2D cpCol = checkpoint.AddComponent<BoxCollider2D>();
            cpCol.isTrigger = true;
            cpCol.size = new Vector2(0.85f, 0.9375f);
            cpCol.offset = Vector2.zero;
            Checkpoint cpComp = checkpoint.AddComponent<Checkpoint>();
            SetSpriteArrayProperty(cpComp, "_waveFrames", LoadAllSpritesSorted("Assets/GameAssets/Field-Tileset/Objects_Animated/Flag.png"));
            SetSpriteArrayProperty(cpComp, "_activateVfxFrames", pickupVfxFrames);
            prefabs.Checkpoint = SaveTempAsPrefab(checkpoint, PrefabCheckpointPath);

            // 8. Coin Item
            GameObject coin = CreateSpriteObject("CoinItem", "Assets/GameAssets/Field-Tileset/Objects_Animated/Coin.png", donors, Color.white, 1);
            CircleCollider2D coinCol = coin.AddComponent<CircleCollider2D>();
            coinCol.isTrigger = true;
            coinCol.radius = 0.26f;
            CoinItem coinComp = coin.AddComponent<CoinItem>();
            SetSpriteArrayProperty(coinComp, "_animationFrames", LoadAllSpritesSorted("Assets/GameAssets/Field-Tileset/Objects_Animated/Coin.png"));
            prefabs.Coin = SaveTempAsPrefab(coin, PrefabCoinPath);

            // 9. Health Pickup
            GameObject heart = CreateSpriteObject("HealthPickup", "Assets/GameAssets/Field-Tileset/Objects/Food/Cherry.png", donors, Color.white, 1);
            heart.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
            CircleCollider2D heartCol = heart.AddComponent<CircleCollider2D>();
            heartCol.isTrigger = true;
            heartCol.radius = 0.24f;
            heart.AddComponent<HealthPickup>();
            prefabs.HealthPickup = SaveTempAsPrefab(heart, PrefabHealthPickupPath);

            // 10. Enemy Patrol Dummy (Ooze)
            GameObject enemy = CreateSpriteObject("Enemy_Ooze_Test", "Assets/GameAssets/Monster-Field/3 Ooze/Ooze.png", donors, Color.white, 0);
            Rigidbody2D enemyRb = enemy.AddComponent<Rigidbody2D>();
            enemyRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            BoxCollider2D enemyCol = enemy.AddComponent<BoxCollider2D>();
            enemyCol.size = new Vector2(0.88f, 0.6875f);
            enemyCol.offset = new Vector2(0f, -0.015625f);
            Health enemyHealth = enemy.AddComponent<Health>();
            SetIntProperty(enemyHealth, "_maxHealth", 4);
            SetBoolProperty(enemyHealth, "_isPlayer", false);
            BaseEnemy enemyAI = enemy.AddComponent<BaseEnemy>();
            SetFloatProperty(enemyAI, "_moveSpeed", 1.8f);
            DamageDealer enemyDealer = enemy.AddComponent<DamageDealer>();
            SetIntProperty(enemyDealer, "_damageAmount", 1);
            SetStringProperty(enemyDealer, "_targetTag", "Player");
            SetFloatProperty(enemyDealer, "_knockbackHorizontal", 5f);
            SetFloatProperty(enemyDealer, "_knockbackVertical", 4f);
            prefabs.EnemyOoze = SaveTempAsPrefab(enemy, PrefabEnemyOozePath);

            // 11. Chìa khóa Bạc & Vàng (hoạt ảnh xoay 12 frame)
            prefabs.SilverKey = CreateKeyPrefab(
                "SilverKey",
                "Assets/GameAssets/Field-Tileset/Objects_Animated/Silver_Key.png",
                KeyType.Silver,
                pickupVfxFrames,
                donors,
                PrefabSilverKeyPath);

            prefabs.GoldenKey = CreateKeyPrefab(
                "GoldenKey",
                "Assets/GameAssets/Field-Tileset/Objects_Animated/Golden_Key.png",
                KeyType.Golden,
                pickupVfxFrames,
                donors,
                PrefabGoldenKeyPath);

            // 12. Rương Bạc (cần Chìa khóa Bạc để mở)
            GameObject silverChest = CreateSpriteObject("SilverChest", "Assets/GameAssets/Field-Tileset/Objects_Animated/Silver_Chest_Open.png", donors, Color.white, 0);
            silverChest.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
            BoxCollider2D silverCol = silverChest.AddComponent<BoxCollider2D>();
            silverCol.isTrigger = true;
            silverCol.size = new Vector2(0.84375f, 0.5625f);
            silverCol.offset = Vector2.zero;
            TreasureChest chestComp = silverChest.AddComponent<TreasureChest>();
            SetBoolProperty(chestComp, "_requireKey", true);
            SetIntProperty(chestComp, "_requiredKeyType", (int)KeyType.Silver);
            SetIntProperty(chestComp, "_bonusCoins", 40);
            SetObjectProperty(chestComp, "_dropPrefab", prefabs.RockPickup);
            SetObjectArrayProperty(chestComp, "_dropPrefabs", new Object[] { prefabs.RockPickup, prefabs.HealthPickup });
            SetSpriteArrayProperty(chestComp, "_openFrames", LoadAllSpritesSorted("Assets/GameAssets/Field-Tileset/Objects_Animated/Silver_Chest_Open.png"));
            SetSpriteArrayProperty(chestComp, "_pickupVfxFrames", pickupVfxFrames);
            prefabs.SilverChest = SaveTempAsPrefab(silverChest, PrefabSilverChestPath);

            // 13. Victory Goal (Rương Vàng về đích - cần Chìa khóa Vàng)
            GameObject victoryGoal = CreateSpriteObject("VictoryGoal", "Assets/GameAssets/Field-Tileset/Objects_Animated/Golden_Chest_Open.png", donors, Color.white, 0);
            victoryGoal.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
            BoxCollider2D victoryCol = victoryGoal.AddComponent<BoxCollider2D>();
            victoryCol.isTrigger = true;
            victoryCol.size = new Vector2(0.84375f, 0.5625f);
            victoryCol.offset = Vector2.zero;
            VictoryGoal victoryComp = victoryGoal.AddComponent<VictoryGoal>();
            SetBoolProperty(victoryComp, "_requireGoldenKey", true);
            SetSpriteArrayProperty(victoryComp, "_openFrames", LoadAllSpritesSorted("Assets/GameAssets/Field-Tileset/Objects_Animated/Golden_Chest_Open.png"));
            SetSpriteArrayProperty(victoryComp, "_pickupVfxFrames", pickupVfxFrames);
            prefabs.VictoryGoal = SaveTempAsPrefab(victoryGoal, PrefabVictoryGoalPath);

            return prefabs;
        }

        private static GameObject CreateKeyPrefab(
            string objName,
            string spriteSheetPath,
            KeyType keyType,
            Sprite[] pickupVfxFrames,
            DonorAssets donors,
            string prefabPath)
        {
            GameObject keyObj = CreateSpriteObject(objName, spriteSheetPath, donors, Color.white, 1);
            keyObj.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
            CircleCollider2D keyCol = keyObj.AddComponent<CircleCollider2D>();
            keyCol.isTrigger = true;
            keyCol.radius = 0.24f;
            KeyItem keyComp = keyObj.AddComponent<KeyItem>();
            SetIntProperty(keyComp, "_keyType", (int)keyType);
            SetSpriteArrayProperty(keyComp, "_animationFrames", LoadAllSpritesSorted(spriteSheetPath));
            SetSpriteArrayProperty(keyComp, "_pickupVfxFrames", pickupVfxFrames);
            return SaveTempAsPrefab(keyObj, prefabPath);
        }

        private static void CreateMainMenuScene(DonorAssets donors)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Transform systems = CreateSection("=== SYSTEMS ===");
            InstantiatePrefab(donors.GameSystemsPrefab, systems, Vector3.zero);

            Transform camLight = CreateSection("=== CAMERA & LIGHT ===");
            InstantiatePrefab(donors.CameraRigPrefab, camLight, new Vector3(0f, 0f, -10f));
            InstantiatePrefab(donors.GlobalLightPrefab, camLight, Vector3.zero);

            Transform bgSection = CreateSection("=== BACKGROUND ===");
            GameObject bgObj = CreateSpriteObject("SkyBackground", SpriteSkyBackground, donors, new Color(0.95f, 0.97f, 1f, 1f), -100);
            bgObj.transform.SetParent(bgSection, false);
            bgObj.transform.position = Vector3.zero;
            bgObj.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            Transform uiRoot = CreateSection("=== UI ===");
            AttachUIAndEventSystem(uiRoot, UISetupTool.PrefabMainMenuUIPath, "MainMenu_Canvas_Root");

            EditorSceneManager.SaveScene(scene, SceneNames.MainMenuPath);
        }

        private static void CreateLevel01Scene(DonorAssets donors, EnvironmentPrefabs p)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Transform systems = CreateSection("=== SYSTEMS ===");
            InstantiatePrefab(donors.GameSystemsPrefab, systems, Vector3.zero);
            GameObject cpManagerObj = new GameObject("CheckpointManager");
            cpManagerObj.transform.SetParent(systems, false);
            CheckpointManager cpManager = cpManagerObj.AddComponent<CheckpointManager>();

            Transform camLight = CreateSection("=== CAMERA & LIGHT ===");
            InstantiatePrefab(donors.CameraRigPrefab, camLight, new Vector3(0f, 0f, -10f));
            InstantiatePrefab(donors.GlobalLightPrefab, camLight, Vector3.zero);

            Transform bgSection = CreateSection("=== BACKGROUND & PROPS ===");
            CreateFieldParallaxBackground(bgSection, donors);
            Color backTreeTint = new Color(0.84f, 0.93f, 0.92f, 0.92f);
            CreateGroundProp("Prop_BackTreeA", bgSection, SpriteTreeBack1, -4.5f, -1.5f, donors, -14, 1f, false, backTreeTint);
            CreateGroundProp("Prop_BackTreeB", bgSection, SpriteTreeBack3, 3.8f, -1.5f, donors, -14, 1f, true, backTreeTint);
            CreateGroundProp("Prop_TreeLeft", bgSection, SpriteTree1, -6.2f, -1.5f, donors, -8);
            CreateGroundProp("Prop_TreeRight", bgSection, SpriteTree3, 5.8f, -1.5f, donors, -8, 1f, true);
            CreateGroundProp("Prop_SignRight", bgSection, SpriteSignRight, -1.8f, -1.5f, donors, -6);
            CreateGroundProp("Prop_Rock", bgSection, SpriteRockSmall, 3.5f, -1.5f, donors, -6);
            CreateGroundProp("Prop_GrassA", bgSection, SpriteGrassTuft, -3.5f, -1.5f, donors, -5);
            CreateGroundProp("Prop_GrassB", bgSection, SpriteGrass2, 2.2f, -1.5f, donors, -5, 1f, true);

            Transform playerSection = CreateSection("=== PLAYER ===");
            GameObject spawnPoint = new GameObject("DefaultSpawnPoint");
            spawnPoint.transform.SetParent(playerSection, false);
            spawnPoint.transform.position = Vector3.zero;
            SetObjectProperty(cpManager, "_defaultSpawnPoint", spawnPoint.transform);
            InstantiatePrefab(donors.PlayerPrefab, playerSection, Vector3.zero);

            Transform envSection = CreateSection("=== ENVIRONMENT ===");
            CreateTiledGround("Ground_Start", envSection, 0f, -1.5f, 16, 5, donors);

            Transform hazardsSection = CreateSection("=== HAZARDS & INTERACTIVES ===");
            InstantiatePrefab(p.GoldenKey, hazardsSection, new Vector3(3.2f, -0.95f, 0f));
            InstantiatePrefab(p.VictoryGoal, hazardsSection, new Vector3(6.5f, -1.12f, 0f));
            CreateKillZone("KillZone", hazardsSection, new Vector3(0f, -12f, 0f), new Vector2(200f, 2f));

            Transform uiRoot = CreateSection("=== UI ===");
            AttachUIAndEventSystem(uiRoot, UISetupTool.PrefabGameplayUIPath, "Gameplay_HUD_Root");

            EditorSceneManager.SaveScene(scene, SceneNames.Level01Path);
        }

        private static void CreateMechanicsTestScene(DonorAssets donors, EnvironmentPrefabs p)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Transform systems = CreateSection("=== SYSTEMS ===");
            InstantiatePrefab(donors.GameSystemsPrefab, systems, Vector3.zero);
            GameObject cpManagerObj = new GameObject("CheckpointManager");
            cpManagerObj.transform.SetParent(systems, false);
            CheckpointManager cpManager = cpManagerObj.AddComponent<CheckpointManager>();

            Transform camLight = CreateSection("=== CAMERA & LIGHT ===");
            InstantiatePrefab(donors.CameraRigPrefab, camLight, new Vector3(0f, 0f, -10f));
            InstantiatePrefab(donors.GlobalLightPrefab, camLight, Vector3.zero);

            Transform bgSection = CreateSection("=== BACKGROUND & PROPS ===");
            CreateFieldParallaxBackground(bgSection, donors);
            Color backTreeTint = new Color(0.84f, 0.93f, 0.92f, 0.92f);

            // Zone 1 props (Floor_Zone1: -10 .. +8)
            CreateGroundProp("Prop_Z1_BackTreeA", bgSection, SpriteTreeBack1, -6.0f, -1.5f, donors, -14, 1f, false, backTreeTint);
            CreateGroundProp("Prop_Z1_BackTreeB", bgSection, SpriteTreeBack2, 2.4f, -1.5f, donors, -14, 1f, true, backTreeTint);
            CreateGroundProp("Prop_Z1_TreeA", bgSection, SpriteTree1, -9.0f, -1.5f, donors, -8);
            CreateGroundProp("Prop_Z1_GrassA", bgSection, SpriteGrassTuft, -5.6f, -1.5f, donors, -5);
            CreateGroundProp("Prop_Z1_Sign", bgSection, SpriteSignRight, -1.2f, -1.5f, donors, -6);
            CreateGroundProp("Prop_Z1_GrassB", bgSection, SpriteGrass2, 0.8f, -1.5f, donors, -5, 1f, true);
            CreateGroundProp("Prop_Z1_GrassC", bgSection, SpriteGrass4, 4.3f, -1.5f, donors, -5);
            CreateGroundProp("Prop_Z1_Rock", bgSection, SpriteRockSmall, 7.5f, -1.5f, donors, -6);
            // Zone 2 props (Floor_Zone2: 8 .. 22)
            CreateGroundProp("Prop_Z2_BackTreeA", bgSection, SpriteTreeBack3, 12.2f, -1.5f, donors, -14, 1f, false, backTreeTint);
            CreateGroundProp("Prop_Z2_BackTreeB", bgSection, SpriteTreeBack4, 19.6f, -1.5f, donors, -14, 1f, true, backTreeTint);
            CreateGroundProp("Prop_Z2_RockBig", bgSection, SpriteRockBig, 8.2f, -1.5f, donors, -7);
            CreateGroundProp("Prop_Z2_GrassA", bgSection, SpriteGrass3, 11.2f, -1.5f, donors, -5);
            CreateGroundProp("Prop_Z2_GrassB", bgSection, SpriteGrass5, 14.0f, -1.5f, donors, -5, 1f, true);
            CreateGroundProp("Prop_Z2_Tree", bgSection, SpriteTree3, 16.8f, -1.5f, donors, -8);
            // Zone 3 props (Floor_JumpPadBase: 22 .. 31)
            CreateGroundProp("Prop_Z3_BackTree", bgSection, SpriteTreeBack2, 26.2f, -1.5f, donors, -14, 1f, false, backTreeTint);
            CreateGroundProp("Prop_Z3_SignJump", bgSection, SpriteSignJumpUp, 23.6f, -1.5f, donors, -6);
            CreateGroundProp("Prop_Z3_Grass", bgSection, SpriteGrass6, 27.0f, -1.5f, donors, -5);
            CreateGroundProp("Prop_Z3_RockSmall", bgSection, SpriteRockSmall2, 30.2f, -1.5f, donors, -6);
            // Zone 4 props (Floor_Zone4: 52 .. 78)
            CreateGroundProp("Prop_Z4_BackTreeA", bgSection, SpriteTreeBack1, 57.5f, -1.5f, donors, -14, 1f, false, backTreeTint);
            CreateGroundProp("Prop_Z4_BackTreeB", bgSection, SpriteTreeBack3, 66.2f, -1.5f, donors, -14, 1f, true, backTreeTint);
            CreateGroundProp("Prop_Z4_BackTreeC", bgSection, SpriteTreeBack4, 74.2f, -1.5f, donors, -14, 1f, false, backTreeTint);
            CreateGroundProp("Prop_Z4_TreeA", bgSection, SpriteTree2, 53.8f, -1.5f, donors, -8);
            CreateGroundProp("Prop_Z4_GrassA", bgSection, SpriteGrass2, 55.3f, -1.5f, donors, -5);
            CreateGroundProp("Prop_Z4_RockBig", bgSection, SpriteRockBig2, 62.0f, -1.5f, donors, -7);
            CreateGroundProp("Prop_Z4_Rock", bgSection, SpriteRockSmall, 63.8f, -1.5f, donors, -6);
            CreateGroundProp("Prop_Z4_GrassB", bgSection, SpriteGrass4, 68.2f, -1.5f, donors, -5, 1f, true);
            CreateGroundProp("Prop_Z4_TreeB", bgSection, SpriteTree4, 71.8f, -1.5f, donors, -8, 1f, true);
            CreateGroundProp("Prop_Z4_SignGoal", bgSection, SpriteSignRight, 73.6f, -1.5f, donors, -6);

            Transform playerSection = CreateSection("=== PLAYER ===");
            Vector3 playerStartPos = new Vector3(0f, -0.5f, 0f);
            GameObject spawnPoint = new GameObject("DefaultSpawnPoint");
            spawnPoint.transform.SetParent(playerSection, false);
            spawnPoint.transform.position = playerStartPos;
            SetObjectProperty(cpManager, "_defaultSpawnPoint", spawnPoint.transform);
            InstantiatePrefab(donors.PlayerPrefab, playerSection, playerStartPos);

            // ZONE 1: Chạy, 2x Cầu thang (Ladder + One-Way), Nhặt đá & 2x Thùng gỗ (Đẩy kê chân / Đập vỡ)
            Transform z1 = CreateSection("=== ZONE 1 - LOCOMOTION, LADDER, CRATE ===");
            CreateTiledGround("Floor_Zone1", z1, -1f, -1.5f, 18, 5, donors);
            // Cặp thang #1 & #2 để test leo/tụt 2 lần
            InstantiatePrefab(p.Ladder, z1, new Vector3(-3.2f, 0.5f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z1, new Vector3(-3.2f, 2.35f, 0f));
            InstantiatePrefab(p.Coin, z1, new Vector3(-3.2f, 2.90f, 0f));
            InstantiatePrefab(p.Coin, z1, new Vector3(-2.2f, 2.90f, 0f));
            InstantiatePrefab(p.Ladder, z1, new Vector3(-7.2f, 0.5f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z1, new Vector3(-7.2f, 2.35f, 0f));
            InstantiatePrefab(p.Coin, z1, new Vector3(-7.2f, 2.90f, 0f));
            InstantiatePrefab(p.Coin, z1, new Vector3(-5.2f, -0.95f, 0f));
            InstantiatePrefab(p.Coin, z1, new Vector3(-1.6f, -0.95f, 0f));
            // Đá & 2 Thùng gỗ (Thùng #1 đẩy kê chân nhảy lên bệ cao, Thùng #2 đập vỡ lấy đá)
            InstantiatePrefab(p.RockPickup, z1, new Vector3(1.6f, -1.18f, 0f));
            InstantiatePrefab(p.Crate, z1, new Vector3(3.2f, -1.109f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z1, new Vector3(4.5f, 1.35f, 0f));
            InstantiatePrefab(p.Coin, z1, new Vector3(4.5f, 1.90f, 0f));
            InstantiatePrefab(p.RockPickup, z1, new Vector3(5.2f, -1.18f, 0f));
            InstantiatePrefab(p.Crate, z1, new Vector3(6.5f, -1.109f, 0f));

            // ZONE 2: Double Jump 3 tầng & 2 tuyến Air Dash vượt khoảng trống
            Transform z2 = CreateSection("=== ZONE 2 - DOUBLE JUMP & AIR DASH ===");
            CreateTiledGround("Floor_Zone2", z2, 15.0f, -1.5f, 14, 5, donors);
            InstantiatePrefab(p.OneWayPlatform, z2, new Vector3(10.0f, 0.35f, 0f));
            InstantiatePrefab(p.Coin, z2, new Vector3(10.0f, 0.90f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z2, new Vector3(14.5f, 2.05f, 0f));
            InstantiatePrefab(p.Coin, z2, new Vector3(14.5f, 2.60f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z2, new Vector3(10.2f, 3.85f, 0f));
            InstantiatePrefab(p.Coin, z2, new Vector3(9.6f, 4.40f, 0f));
            InstantiatePrefab(p.Coin, z2, new Vector3(10.8f, 4.40f, 0f));
            // Đài Air Dash #1 & #2 trên không + Cặp Chìa khóa Bạc / Rương Bạc (Lần test 1)
            InstantiatePrefab(p.OneWayPlatform, z2, new Vector3(19.5f, 2.05f, 0f));
            InstantiatePrefab(p.RockPickup, z2, new Vector3(18.7f, 2.52f, 0f));
            InstantiatePrefab(p.Coin, z2, new Vector3(19.7f, 2.60f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z2, new Vector3(15.5f, 3.85f, 0f));
            InstantiatePrefab(p.Coin, z2, new Vector3(14.6f, 4.40f, 0f));
            InstantiatePrefab(p.SilverKey, z2, new Vector3(15.5f, 4.45f, 0f));
            InstantiatePrefab(p.Coin, z2, new Vector3(16.4f, 4.40f, 0f));
            InstantiatePrefab(p.Coin, z2, new Vector3(19.3f, -0.95f, 0f));
            InstantiatePrefab(p.SilverChest, z2, new Vector3(20.8f, -1.12f, 0f));

            // ZONE 3: Checkpoint #1, 2x Bệ nhún (JumpPad), 2x Sàn sụp lún (Falling), 2x Sàn di chuyển (MovingPlatform)
            Transform z3 = CreateSection("=== ZONE 3 - PLATFORMS (JUMPPAD, FALLING, MOVING) ===");
            CreateTiledGround("Floor_JumpPadBase", z3, 26.5f, -1.5f, 9, 5, donors);
            InstantiatePrefab(p.Checkpoint, z3, new Vector3(22.8f, -1.031f, 0f));
            InstantiatePrefab(p.JumpPad, z3, new Vector3(25.0f, -1.5f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z3, new Vector3(25.0f, 2.20f, 0f));
            InstantiatePrefab(p.Coin, z3, new Vector3(25.0f, 2.75f, 0f));
            InstantiatePrefab(p.JumpPad, z3, new Vector3(29.0f, -1.5f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z3, new Vector3(29.0f, 2.40f, 0f));
            InstantiatePrefab(p.Coin, z3, new Vector3(29.0f, 2.95f, 0f));
            // 2x Sàn sụp lún
            InstantiatePrefab(p.FallingPlatform, z3, new Vector3(33.2f, -1.70f, 0f));
            InstantiatePrefab(p.Coin, z3, new Vector3(33.2f, -0.95f, 0f));
            InstantiatePrefab(p.FallingPlatform, z3, new Vector3(36.2f, -1.70f, 0f));
            InstantiatePrefab(p.Coin, z3, new Vector3(36.2f, -0.95f, 0f));
            // 2x Sàn di chuyển
            CreateMovingPlatformSystem(z3, new Vector3(39.6f, -1.75f, 0f), new Vector3(42.6f, -1.75f, 0f), donors);
            CreateMovingPlatformSystem(z3, new Vector3(46.2f, -1.75f, 0f), new Vector3(49.4f, -1.75f, 0f), donors);

            // ZONE 4: Checkpoint #2, 2x Bẫy gai & Hồi máu, Đá ném, 2x Quái tuần tra & Điểm thắng
            Transform z4 = CreateSection("=== ZONE 4 - CHECKPOINT, HAZARDS, ITEMS & COMBAT ===");
            CreateTiledGround("Floor_Zone4", z4, 65.0f, -1.5f, 26, 5, donors);
            InstantiatePrefab(p.Checkpoint, z4, new Vector3(53.0f, -1.031f, 0f));
            InstantiatePrefab(p.RockPickup, z4, new Vector3(54.2f, -1.18f, 0f));
            InstantiatePrefab(p.Coin, z4, new Vector3(54.9f, -0.95f, 0f));
            InstantiatePrefab(p.Coin, z4, new Vector3(55.7f, -0.95f, 0f));
            // 2x Bẫy gai + 2x Hồi máu
            InstantiatePrefab(p.Spike, z4, new Vector3(56.8f, -1.125f, 0f));
            InstantiatePrefab(p.HealthPickup, z4, new Vector3(58.3f, -1.12f, 0f));
            InstantiatePrefab(p.Spike, z4, new Vector3(59.8f, -1.125f, 0f));
            InstantiatePrefab(p.HealthPickup, z4, new Vector3(61.3f, -1.12f, 0f));
            // Khu chiến đấu với 2x Quái tuần tra Ooze & Chìa khóa Vàng trên bệ chiến đấu (Lần test 2)
            InstantiatePrefab(p.RockPickup, z4, new Vector3(62.6f, -1.18f, 0f));
            InstantiatePrefab(p.EnemyOoze, z4, new Vector3(65.5f, -1.14f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z4, new Vector3(67.8f, 1.25f, 0f));
            InstantiatePrefab(p.Coin, z4, new Vector3(66.9f, 1.80f, 0f));
            InstantiatePrefab(p.GoldenKey, z4, new Vector3(67.8f, 1.85f, 0f));
            InstantiatePrefab(p.Coin, z4, new Vector3(68.7f, 1.80f, 0f));
            InstantiatePrefab(p.EnemyOoze, z4, new Vector3(70.2f, -1.14f, 0f));
            InstantiatePrefab(p.Coin, z4, new Vector3(72.0f, -0.95f, 0f));
            InstantiatePrefab(p.Coin, z4, new Vector3(72.7f, -0.95f, 0f));
            // Điểm chiến thắng cuối màn (Rương vàng)
            InstantiatePrefab(p.VictoryGoal, z4, new Vector3(75.5f, -1.12f, 0f));
            CreateTiledStoneWall("Wall_ArenaEnd", z4, 77.6f, -1.5f, 5, donors);

            // HAZARDS & BOUNDS
            Transform hazards = CreateSection("=== HAZARDS & BOUNDS ===");
            CreateKillZone("KillZone", hazards, new Vector3(34f, -8f, 0f), new Vector2(168f, 2f));

            // UI
            Transform uiRoot = CreateSection("=== UI ===");
            AttachUIAndEventSystem(uiRoot, UISetupTool.PrefabGameplayUIPath, "TestHUD_Canvas_Root");

            EditorSceneManager.SaveScene(scene, SceneNames.MechanicsTestPath);
        }

        private static void AttachUIAndEventSystem(Transform uiRoot, string uiPrefabPath, string fallbackPlaceholderName)
        {
            GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(uiPrefabPath);
            if (uiPrefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab);
                instance.transform.SetParent(uiRoot, false);
                UISetupTool.EnsureEventSystemInActiveScene();
            }
            else
            {
                GameObject placeholder = new GameObject(fallbackPlaceholderName);
                placeholder.transform.SetParent(uiRoot, false);
            }
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneNames.MainMenuPath, true),
                new EditorBuildSettingsScene(SceneNames.Level01Path, true),
                new EditorBuildSettingsScene(SceneNames.MechanicsTestPath, true)
            };
        }

        private static Transform CreateSection(string sectionName)
        {
            return new GameObject(sectionName).transform;
        }

        private static GameObject SaveTempAsPrefab(GameObject temp, string assetPath)
        {
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(temp, assetPath);
            Object.DestroyImmediate(temp);
            return savedPrefab;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Transform parent, Vector3 position, Vector3? localScale = null)
        {
            if (prefab == null) return null;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            if (localScale.HasValue)
            {
                instance.transform.localScale = localScale.Value;
            }
            return instance;
        }

        private static GameObject CreateSpriteObject(string objName, string spritePath, DonorAssets donors, Color color, int sortingOrder)
        {
            GameObject obj = new GameObject(objName);
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            Sprite sprite = !string.IsNullOrEmpty(spritePath) ? LoadFirstSprite(spritePath) : null;
            sr.sprite = sprite != null ? sprite : donors.SquareSprite;
            if (donors.SpriteMaterial != null) sr.sharedMaterial = donors.SpriteMaterial;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return obj;
        }

        private static float BuildHorizontalTileStrip(
            Transform parent,
            int tileCount,
            Sprite leftSprite,
            Sprite midSprite,
            Sprite rightSprite,
            float localTopY,
            DonorAssets donors,
            int sortingOrder,
            string prefix = "Tile")
        {
            if (tileCount <= 0) return 0f;

            Sprite fallback = midSprite ?? leftSprite ?? rightSprite ?? donors.SquareSprite;
            Sprite[] picked = new Sprite[tileCount];
            float totalWidth = 0f;

            for (int i = 0; i < tileCount; i++)
            {
                Sprite sp;
                if (tileCount == 1)
                {
                    sp = midSprite ?? fallback;
                }
                else if (i == 0)
                {
                    sp = leftSprite ?? fallback;
                }
                else if (i == tileCount - 1)
                {
                    sp = rightSprite ?? fallback;
                }
                else
                {
                    sp = midSprite ?? fallback;
                }

                picked[i] = sp != null ? sp : fallback;
                totalWidth += picked[i] != null ? picked[i].bounds.size.x : 1f;
            }

            float cursorX = -totalWidth * 0.5f;
            for (int i = 0; i < tileCount; i++)
            {
                Sprite sp = picked[i];
                GameObject tileObj = new GameObject($"{prefix}_{i:00}");
                tileObj.transform.SetParent(parent, false);

                SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                if (donors.SpriteMaterial != null) sr.sharedMaterial = donors.SpriteMaterial;
                sr.sortingOrder = sortingOrder;

                if (sp != null)
                {
                    tileObj.transform.localPosition = new Vector3(
                        cursorX - sp.bounds.min.x,
                        localTopY - sp.bounds.max.y,
                        0f);
                    cursorX += sp.bounds.size.x;
                }
                else
                {
                    tileObj.transform.localPosition = new Vector3(cursorX + 0.5f, localTopY - 0.5f, 0f);
                    cursorX += 1f;
                }
            }

            return totalWidth;
        }

        private static void CreateTiledGround(
            string name,
            Transform parent,
            float centerX,
            float surfaceTopY,
            int tileCols,
            int tileRows,
            DonorAssets donors)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);

            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(root.transform, false);

            float width = tileCols;
            if (tileRows <= 1)
            {
                const float height = 0.94f;
                root.transform.position = new Vector3(centerX, surfaceTopY - height * 0.5f, 0f);
                width = BuildHorizontalTileStrip(
                    visuals.transform,
                    tileCols,
                    LoadFirstSprite(SpriteFieldCeilingLeft),
                    LoadFirstSprite(SpriteFieldCeilingMid),
                    LoadFirstSprite(SpriteFieldCeilingRight),
                    height * 0.5f,
                    donors,
                    0,
                    "Row0_Tile");

                BoxCollider2D col = root.AddComponent<BoxCollider2D>();
                col.size = new Vector2(width > 0f ? width : tileCols, height);
                return;
            }

            float totalHeight = tileRows;
            root.transform.position = new Vector3(centerX, surfaceTopY - totalHeight * 0.5f, 0f);

            Sprite topL = LoadFirstSprite(SpriteFieldTopLeft);
            Sprite topM = LoadFirstSprite(SpriteFieldTopMid);
            Sprite topR = LoadFirstSprite(SpriteFieldTopRight);
            Sprite fillL = LoadFirstSprite(SpriteFieldFillLeft);
            Sprite fillM = LoadFirstSprite(SpriteFieldFillMid);
            Sprite fillR = LoadFirstSprite(SpriteFieldFillRight);
            Sprite botL = LoadFirstSprite(SpriteFieldBotLeft);
            Sprite botM = LoadFirstSprite(SpriteFieldBotMid);
            Sprite botR = LoadFirstSprite(SpriteFieldBotRight);

            for (int r = 0; r < tileRows; r++)
            {
                float rowTopY = totalHeight * 0.5f - r;
                float rowWidth;
                if (r == 0)
                {
                    rowWidth = BuildHorizontalTileStrip(visuals.transform, tileCols, topL, topM, topR, rowTopY, donors, 0, $"Row{r}_Tile");
                }
                else if (r == tileRows - 1)
                {
                    rowWidth = BuildHorizontalTileStrip(visuals.transform, tileCols, botL, botM, botR, rowTopY, donors, 0, $"Row{r}_Tile");
                }
                else
                {
                    rowWidth = BuildHorizontalTileStrip(visuals.transform, tileCols, fillL, fillM, fillR, rowTopY, donors, 0, $"Row{r}_Tile");
                }

                if (r == 0 && rowWidth > 0f)
                {
                    width = rowWidth;
                }
            }

            BoxCollider2D groundCol = root.AddComponent<BoxCollider2D>();
            groundCol.size = new Vector2(width, totalHeight);
        }

        private static void CreateTiledStoneWall(
            string name,
            Transform parent,
            float centerX,
            float bottomY,
            int tileRows,
            DonorAssets donors)
        {
            if (tileRows <= 0) return;

            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            float totalHeight = tileRows;
            root.transform.position = new Vector3(centerX, bottomY + totalHeight * 0.5f, 0f);

            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(root.transform, false);

            Sprite topSp = LoadFirstSprite(SpriteFieldWallTop);
            Sprite midSp = LoadFirstSprite(SpriteFieldWallMid);
            Sprite botSp = LoadFirstSprite(SpriteFieldWallBot);

            for (int r = 0; r < tileRows; r++)
            {
                Sprite sp = r == 0 ? topSp : (r == tileRows - 1 ? botSp : midSp);
                if (sp == null) sp = midSp ?? donors.SquareSprite;

                GameObject tileObj = new GameObject($"WallTile_{r:00}");
                tileObj.transform.SetParent(visuals.transform, false);

                SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                if (donors.SpriteMaterial != null) sr.sharedMaterial = donors.SpriteMaterial;
                sr.sortingOrder = 0;

                float topY = totalHeight * 0.5f - r;
                float localY = sp != null ? topY - sp.bounds.max.y : topY - 0.5f;
                tileObj.transform.localPosition = new Vector3(0f, localY, 0f);
            }

            BoxCollider2D col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.88f, totalHeight);
        }

        private static readonly Color32 FieldBottomSkirtColor = new Color32(47, 88, 90, 255);
        private const float BottomSkirtWorldHeight = 6.3f;

        [InitializeOnLoadMethod]
        private static void AutoRepairOpenSceneParallaxOnLoad()
        {
            EditorApplication.delayCall -= RepairOpenSceneParallaxIfNeeded;
            EditorApplication.delayCall += RepairOpenSceneParallaxIfNeeded;
        }

        private static void RepairOpenSceneParallaxIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying) return;

            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded) return;

            GameObject rootObj = GameObject.Find("FieldParallax_Root");
            if (rootObj == null) return;

            ParallaxLayer[] layers = rootObj.GetComponentsInChildren<ParallaxLayer>(true);
            if (layers == null || layers.Length == 0) return;

            Sprite whiteSquare = LoadWhiteSquareSprite();
            bool anyModified = false;

            const float scale = ParallaxLayer.DefaultPanelScale;
            const float worldBottomY = ParallaxLayer.DefaultWorldBottomY;
            const float unifiedFy = ParallaxLayer.UnifiedVerticalParallaxFactor;

            for (int i = 0; i < layers.Length; i++)
            {
                ParallaxLayer layer = layers[i];
                if (layer == null) continue;

                var panels = new System.Collections.Generic.List<(Transform tr, SpriteRenderer sr)>();
                Transform layerTr = layer.transform;
                for (int c = 0; c < layerTr.childCount; c++)
                {
                    Transform child = layerTr.GetChild(c);
                    if (child.name.Contains("BottomSkirt")) continue;
                    if (child.TryGetComponent<SpriteRenderer>(out var sr) && sr.sprite != null)
                    {
                        panels.Add((child, sr));
                    }
                }

                if (panels.Count == 0) continue;

                Sprite sp = panels[0].sr.sprite;
                float panelWidth = Mathf.Max(1f, sp.bounds.size.x * scale - 0.02f);
                float expectedCenterY = worldBottomY - sp.bounds.min.y * scale;

                if (!Mathf.Approximately(layerTr.position.y, expectedCenterY))
                {
                    layerTr.position = new Vector3(layerTr.position.x, expectedCenterY, layerTr.position.z);
                    EditorUtility.SetDirty(layerTr);
                    anyModified = true;
                }

                SerializedObject so = new SerializedObject(layer);
                bool layerPropsChanged = false;
                layerPropsChanged |= UpdateFloatIfChanged(so.FindProperty("_parallaxFactorY"), unifiedFy);
                layerPropsChanged |= UpdateFloatIfChanged(so.FindProperty("_panelWidth"), panelWidth);
                layerPropsChanged |= UpdateBoolIfChanged(so.FindProperty("_alignBottomOnStart"), true);
                layerPropsChanged |= UpdateFloatIfChanged(so.FindProperty("_worldBottomY"), worldBottomY);
                if (layerPropsChanged)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(layer);
                    anyModified = true;
                }

                bool isForegroundGrassLayer = IsLayerOneSprite(sp, layer.gameObject.name);
                int halfCount = panels.Count / 2;
                for (int p = 0; p < panels.Count; p++)
                {
                    (Transform panelTr, SpriteRenderer panelSr) = panels[p];
                    Vector3 expectedScale = new Vector3(scale, scale, 1f);
                    Vector3 expectedPos = new Vector3((p - halfCount) * panelWidth, 0f, 0f);

                    if ((panelTr.localScale - expectedScale).sqrMagnitude > 0.0001f)
                    {
                        panelTr.localScale = expectedScale;
                        EditorUtility.SetDirty(panelTr);
                        anyModified = true;
                    }
                    if ((panelTr.localPosition - expectedPos).sqrMagnitude > 0.0001f)
                    {
                        panelTr.localPosition = expectedPos;
                        EditorUtility.SetDirty(panelTr);
                        anyModified = true;
                    }
                    if (panelSr.color != Color.white)
                    {
                        panelSr.color = Color.white;
                        EditorUtility.SetDirty(panelSr);
                        anyModified = true;
                    }

                    if (isForegroundGrassLayer)
                    {
                        anyModified |= EnsureBottomSkirt(
                            panelTr,
                            panelSr.sprite,
                            scale,
                            -35,
                            whiteSquare,
                            panelSr.sharedMaterial);
                    }
                }
            }

            if (anyModified)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                SceneView.RepaintAll();
            }
        }

        private static bool UpdateFloatIfChanged(SerializedProperty prop, float target)
        {
            if (prop == null || Mathf.Approximately(prop.floatValue, target)) return false;
            prop.floatValue = target;
            return true;
        }

        private static bool UpdateBoolIfChanged(SerializedProperty prop, bool target)
        {
            if (prop == null || prop.boolValue == target) return false;
            prop.boolValue = target;
            return true;
        }

        private static bool IsLayerOneSprite(Sprite sp, string layerName)
        {
            string assetPath = sp != null ? AssetDatabase.GetAssetPath(sp) : string.Empty;
            return (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith("/1.png"))
                || (!string.IsNullOrEmpty(layerName) && layerName.EndsWith("_1"));
        }

        private static Sprite LoadWhiteSquareSprite(Sprite preferred = null)
        {
            if (preferred != null && preferred.texture != null && preferred.texture.name == "Square"
                && Mathf.Approximately(preferred.bounds.size.x, 1f)
                && Mathf.Approximately(preferred.bounds.size.y, 1f))
            {
                return preferred;
            }

            string packageSquarePath = AssetDatabase.GUIDToAssetPath("311925a002f4447b3a28927169b83ea6");
            if (!string.IsNullOrEmpty(packageSquarePath))
            {
                Sprite pkgSprite = LoadFirstSprite(packageSquarePath);
                if (pkgSprite != null) return pkgSprite;
            }

            return preferred != null
                ? preferred
                : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        }

        private static bool EnsureBottomSkirt(
            Transform panelTransform,
            Sprite layerSprite,
            float panelScale,
            int sortingOrder,
            Sprite squareSprite,
            Material spriteMaterial)
        {
            if (panelTransform == null || layerSprite == null || squareSprite == null) return false;

            Transform existing = panelTransform.Find("BottomSkirt");
            bool changed = false;
            GameObject skirtObj;
            if (existing == null)
            {
                skirtObj = new GameObject("BottomSkirt");
                skirtObj.transform.SetParent(panelTransform, false);
                changed = true;
            }
            else
            {
                skirtObj = existing.gameObject;
            }

            SpriteRenderer sr = skirtObj.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = skirtObj.AddComponent<SpriteRenderer>();
                changed = true;
            }

            if (sr.sprite != squareSprite)
            {
                sr.sprite = squareSprite;
                changed = true;
            }
            if (spriteMaterial != null && sr.sharedMaterial != spriteMaterial)
            {
                sr.sharedMaterial = spriteMaterial;
                changed = true;
            }
            Color targetColor = FieldBottomSkirtColor;
            if (sr.color != targetColor)
            {
                sr.color = targetColor;
                changed = true;
            }
            if (sr.sortingOrder != sortingOrder)
            {
                sr.sortingOrder = sortingOrder;
                changed = true;
            }

            Bounds sb = squareSprite.bounds;
            if (sb.size.x <= 0.0001f || sb.size.y <= 0.0001f) return changed;

            float targetWidth = layerSprite.bounds.size.x;
            float targetHeight = BottomSkirtWorldHeight / Mathf.Max(0.01f, panelScale);
            float targetTopY = layerSprite.bounds.min.y + 0.02f;
            Vector2 desiredCenter = new Vector2(layerSprite.bounds.center.x, targetTopY - targetHeight * 0.5f);

            float sx = targetWidth / sb.size.x;
            float sy = targetHeight / sb.size.y;
            Vector3 desiredScale = new Vector3(sx, sy, 1f);
            Vector3 desiredPos = new Vector3(desiredCenter.x - sb.center.x * sx, desiredCenter.y - sb.center.y * sy, 0f);

            if ((skirtObj.transform.localScale - desiredScale).sqrMagnitude > 0.0001f)
            {
                skirtObj.transform.localScale = desiredScale;
                changed = true;
            }
            if ((skirtObj.transform.localPosition - desiredPos).sqrMagnitude > 0.0001f)
            {
                skirtObj.transform.localPosition = desiredPos;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(skirtObj);
            }
            return changed;
        }

        private static void CreateFieldParallaxBackground(Transform parent, DonorAssets donors)
        {
            GameObject camObj = GameObject.FindWithTag("MainCamera");
            if (camObj != null && camObj.TryGetComponent<Camera>(out var mainCam))
            {
                mainCam.backgroundColor = new Color(0.141f, 0.565f, 0.804f, 1f);
            }

            GameObject root = new GameObject("FieldParallax_Root");
            root.transform.SetParent(parent, false);
            root.AddComponent<FieldAtmosphereController>();

            (string file, int sortOrder, float fx, float driftX)[] layers =
            {
                ("8.png", -100, 0.02f,  0.00f),
                ("7.png",  -90, 0.08f, -0.35f),
                ("6.png",  -80, 0.15f,  0.00f),
                ("5.png",  -70, 0.24f,  0.00f),
                ("4.png",  -60, 0.36f,  0.00f),
                ("3.png",  -50, 0.50f,  0.00f),
                ("2.png",  -40, 0.68f,  0.00f),
                ("1.png",  -35, 0.82f,  0.00f)
            };

            const float scale = ParallaxLayer.DefaultPanelScale;
            const float worldBottomY = ParallaxLayer.DefaultWorldBottomY;
            const float unifiedFy = ParallaxLayer.UnifiedVerticalParallaxFactor;
            Sprite whiteSquare = LoadWhiteSquareSprite(donors.SquareSprite);

            for (int i = 0; i < layers.Length; i++)
            {
                var cfg = layers[i];
                string path = FieldParallaxDir + cfg.file;
                Sprite sp = LoadFirstSprite(path);
                if (sp == null) continue;

                // Căn toàn bộ 8 lớp theo cùng mép đáy dưới mặt đất để triệt tiêu độ lệch chiều cao do auto-slicer cắt tỉa riêng từng ảnh
                float panelWidth = Mathf.Max(1f, sp.bounds.size.x * scale - 0.02f);
                float layerCenterY = worldBottomY - sp.bounds.min.y * scale;
                GameObject layerObj = new GameObject($"Parallax_Layer_{i + 1}_{cfg.file.Replace(".png", "")}");
                layerObj.transform.SetParent(root.transform, false);
                layerObj.transform.position = new Vector3(0f, layerCenterY, 0f);

                ParallaxLayer parallax = layerObj.AddComponent<ParallaxLayer>();
                parallax.Configure(cfg.fx, unifiedFy, panelWidth, cfg.driftX, true, worldBottomY);

                for (int copy = -1; copy <= 1; copy++)
                {
                    GameObject panel = CreateSpriteObject($"Panel_{copy + 1}", path, donors, Color.white, cfg.sortOrder);
                    panel.transform.SetParent(layerObj.transform, false);
                    panel.transform.localPosition = new Vector3(copy * panelWidth, 0f, 0f);
                    panel.transform.localScale = new Vector3(scale, scale, 1f);

                    if (cfg.file == "1.png")
                    {
                        EnsureBottomSkirt(panel.transform, sp, scale, -35, whiteSquare, donors.SpriteMaterial);
                    }
                }
            }
        }

        private static void CreateGroundProp(
            string name,
            Transform parent,
            string spritePath,
            float posX,
            float groundY,
            DonorAssets donors,
            int sortingOrder,
            float scale = 1f,
            bool flipX = false,
            Color? tint = null)
        {
            GameObject prop = CreateSpriteObject(name, spritePath, donors, tint ?? Color.white, sortingOrder);
            prop.transform.SetParent(parent, false);
            prop.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer sr = prop.GetComponent<SpriteRenderer>();
            sr.flipX = flipX;
            float offsetY = sr.sprite != null ? -sr.sprite.bounds.min.y * scale : 0.5f * scale;
            prop.transform.position = new Vector3(posX, groundY + offsetY, 0f);
        }

        private static void CreateKillZone(string name, Transform parent, Vector3 position, Vector2 size)
        {
            GameObject kz = new GameObject(name);
            kz.transform.SetParent(parent, false);
            kz.transform.position = position;
            BoxCollider2D col = kz.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;
            kz.AddComponent<KillZone>();
        }

        private static void CreateMovingPlatformSystem(Transform parent, Vector3 startPos, Vector3 endPos, DonorAssets donors, int tileCount = 3)
        {
            GameObject root = new GameObject("MovingPlatform_System");
            root.transform.SetParent(parent, false);
            root.transform.position = startPos;

            GameObject platform = new GameObject("Platform_Moving");
            platform.transform.SetParent(root.transform, false);
            platform.transform.position = startPos;

            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(platform.transform, false);
            float width = BuildHorizontalTileStrip(
                visuals.transform,
                tileCount,
                LoadFirstSprite(SpriteFieldMoveLeft),
                LoadFirstSprite(SpriteFieldMoveMid),
                LoadFirstSprite(SpriteFieldMoveRight),
                0.25f,
                donors,
                0);

            BoxCollider2D platformCol = platform.AddComponent<BoxCollider2D>();
            platformCol.size = new Vector2(Mathf.Max(2.8f, width - 0.1f), 0.72f);
            platformCol.offset = new Vector2(0f, -0.11f);
            MovingPlatform mp = platform.AddComponent<MovingPlatform>();

            GameObject wpA = new GameObject("Waypoint_A");
            wpA.transform.SetParent(root.transform, false);
            wpA.transform.position = startPos;

            GameObject wpB = new GameObject("Waypoint_B");
            wpB.transform.SetParent(root.transform, false);
            wpB.transform.position = endPos;

            SerializedObject so = new SerializedObject(mp);
            SerializedProperty prop = so.FindProperty("_waypoints");
            if (prop != null)
            {
                prop.arraySize = 2;
                prop.GetArrayElementAtIndex(0).objectReferenceValue = wpA.transform;
                prop.GetArrayElementAtIndex(1).objectReferenceValue = wpB.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigurePlayerSkillAssets(PlayerSkills skills)
        {
            if (skills == null) return;

            string[] iconPaths =
            {
                "Assets/GameAssets/Player-Skills/Icons/10-Fire-ball.png",
                "Assets/GameAssets/Player-Skills/Icons/1-Lightning.png",
                "Assets/GameAssets/Player-Skills/Icons/8-Shield.png",
                "Assets/GameAssets/Player-Skills/Icons/6-Spikes.png"
            };
            string[] texPaths =
            {
                "Assets/GameAssets/Player-Skills/10 Fire ball/Fire-ball.png",
                "Assets/GameAssets/Player-Skills/1 Lightning/Lightning.png",
                "Assets/GameAssets/Player-Skills/8 Shield/Shield.png",
                "Assets/GameAssets/Player-Skills/6 Spikes/Spikes.png"
            };

            SerializedObject so = new SerializedObject(skills);

            SerializedProperty iconsProp = so.FindProperty("_skillIcons");
            if (iconsProp != null && iconsProp.isArray)
            {
                iconsProp.arraySize = iconPaths.Length;
                for (int i = 0; i < iconPaths.Length; i++)
                {
                    iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = LoadFirstSprite(iconPaths[i]);
                }
            }

            SerializedProperty texProp = so.FindProperty("_skillTextures");
            if (texProp != null && texProp.isArray)
            {
                texProp.arraySize = texPaths.Length;
                for (int i = 0; i < texPaths.Length; i++)
                {
                    texProp.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(texPaths[i]);
                }
            }

            SerializedProperty explosionProp = so.FindProperty("_explosionTexture");
            if (explosionProp != null)
            {
                explosionProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GameAssets/Player-Skills/5 Explosion/Explosion.png");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite LoadFirstSprite(string assetPath)
        {
            return LoadAllSpritesSorted(assetPath).FirstOrDefault();
        }

        private static Sprite[] LoadAllSpritesSorted(string assetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null) return System.Array.Empty<Sprite>();

            return assets
                .OfType<Sprite>()
                .OrderBy(s => s.rect.x)
                .ToArray();
        }

        private static void SetSpriteArrayProperty(Object target, string propertyName, Sprite[] sprites)
        {
            if (target == null || sprites == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null && prop.isArray)
            {
                prop.arraySize = sprites.Length;
                for (int i = 0; i < sprites.Length; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetObjectArrayProperty(Object target, string propertyName, Object[] objects)
        {
            if (target == null || objects == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null && prop.isArray)
            {
                prop.arraySize = objects.Length;
                for (int i = 0; i < objects.Length; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetBoolProperty(Object target, string propertyName, bool value) =>
            EditSerializedProperty(target, propertyName, p => p.boolValue = value);

        private static void SetIntProperty(Object target, string propertyName, int value) =>
            EditSerializedProperty(target, propertyName, p => p.intValue = value);

        private static void SetFloatProperty(Object target, string propertyName, float value) =>
            EditSerializedProperty(target, propertyName, p => p.floatValue = value);

        private static void SetStringProperty(Object target, string propertyName, string value) =>
            EditSerializedProperty(target, propertyName, p => p.stringValue = value);

        private static void SetVector2Property(Object target, string propertyName, Vector2 value) =>
            EditSerializedProperty(target, propertyName, p => p.vector2Value = value);

        private static void SetObjectProperty(Object target, string propertyName, Object value) =>
            EditSerializedProperty(target, propertyName, p => p.objectReferenceValue = value);

        private static void EditSerializedProperty(Object target, string propertyName, System.Action<SerializedProperty> setter)
        {
            if (target == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                setter(prop);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
