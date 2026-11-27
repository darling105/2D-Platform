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
using Platformer2D.Enemies.Bosses;
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
        private const string PrefabExplosiveBarrelPath = "Assets/Prefabs/Environment/ExplosiveBarrel.prefab";

        private const string PrefabCoinPath = "Assets/Prefabs/Items/CoinItem.prefab";
        private const string PrefabRockPickupPath = "Assets/Prefabs/Items/RockPickup.prefab";
        private const string PrefabHealthPickupPath = "Assets/Prefabs/Items/HealthPickup.prefab";
        private const string PrefabFoodMeatPath = "Assets/Prefabs/Items/Food_Meat.prefab";
        private const string PrefabFoodCakePath = "Assets/Prefabs/Items/Food_Cake.prefab";
        private const string PrefabFoodCookiesPath = "Assets/Prefabs/Items/Food_Cookies.prefab";
        private const string PrefabSilverKeyPath = "Assets/Prefabs/Items/SilverKey.prefab";
        private const string PrefabGoldenKeyPath = "Assets/Prefabs/Items/GoldenKey.prefab";
        private const string PrefabEnemyOozeTestPath = "Assets/Prefabs/Enemies/Enemy_Ooze_Test.prefab";
        private const string PrefabEnemyOozePath = "Assets/Prefabs/Enemies/Enemy_Ooze.prefab";
        private const string PrefabEnemyRedPath = "Assets/Prefabs/Enemies/Enemy_Red.prefab";
        private const string PrefabEnemyTinyPath = "Assets/Prefabs/Enemies/Enemy_Tiny.prefab";
        private const string PrefabEnemyMagePath = "Assets/Prefabs/Enemies/Enemy_Mage.prefab";
        private const string PrefabEnemyBearPath = "Assets/Prefabs/Enemies/Enemy_Bear.prefab";
        private const string PrefabEnemyYellowPath = "Assets/Prefabs/Enemies/Enemy_Yellow.prefab";

        private const string PrefabBossDemonPath = "Assets/Prefabs/Bosses/Boss_Demon.prefab";
        private const string PrefabBossGoblinKingPath = "Assets/Prefabs/Bosses/Boss_GoblinKing.prefab";
        private const string PrefabBossNecromancerPath = "Assets/Prefabs/Bosses/Boss_Necromancer.prefab";
        private const string PrefabVictoryToL2Path = "Assets/Prefabs/Environment/VictoryGoal_ToLevel2.prefab";
        private const string PrefabVictoryToL3Path = "Assets/Prefabs/Environment/VictoryGoal_ToLevel3.prefab";
        private const string PrefabVictoryClearPath = "Assets/Prefabs/Environment/VictoryGoal_GameClear.prefab";

        // Cave Environment Prefabs
        private const string PrefabOneWayCavePath = "Assets/Prefabs/Environment/OneWayPlatform_Cave.prefab";
        private const string PrefabFallingCavePath = "Assets/Prefabs/Environment/FallingPlatform_Cave.prefab";
        private const string PrefabSpikeCavePath = "Assets/Prefabs/Environment/SpikeHazard_Cave.prefab";
        private const string PrefabCrateCavePath = "Assets/Prefabs/Environment/Crate_Cave.prefab";
        private const string PrefabLadderCavePath = "Assets/Prefabs/Environment/Ladder_Cave.prefab";
        private const string PrefabJumpPadCavePath = "Assets/Prefabs/Environment/JumpPad_Cave.prefab";

        // Hell Environment Prefabs
        private const string PrefabOneWayHellPath = "Assets/Prefabs/Environment/OneWayPlatform_Hell.prefab";
        private const string PrefabFallingHellPath = "Assets/Prefabs/Environment/FallingPlatform_Hell.prefab";
        private const string PrefabSpikeHellPath = "Assets/Prefabs/Environment/SpikeHazard_Hell.prefab";
        private const string PrefabCrateHellPath = "Assets/Prefabs/Environment/Crate_Hell.prefab";
        private const string PrefabLadderHellPath = "Assets/Prefabs/Environment/Ladder_Hell.prefab";
        private const string PrefabJumpPadHellPath = "Assets/Prefabs/Environment/JumpPad_Hell.prefab";

        // Cave Monsters Prefabs
        private const string PrefabEnemyCaveBatPath = "Assets/Prefabs/Enemies/Cave/Enemy_Cave_Bat.prefab";
        private const string PrefabEnemyCaveGoblinMagePath = "Assets/Prefabs/Enemies/Cave/Enemy_Cave_GoblinMage.prefab";
        private const string PrefabEnemyCaveGoblinMeleePath = "Assets/Prefabs/Enemies/Cave/Enemy_Cave_GoblinMelee.prefab";
        private const string PrefabEnemyCaveGoblinRangePath = "Assets/Prefabs/Enemies/Cave/Enemy_Cave_GoblinRange.prefab";
        private const string PrefabEnemyCaveSmallMushroomPath = "Assets/Prefabs/Enemies/Cave/Enemy_Cave_SmallMushroom.prefab";
        private const string PrefabEnemyCaveBigMushroomPath = "Assets/Prefabs/Enemies/Cave/Enemy_Cave_BigMushroom.prefab";

        // Hell Monsters Prefabs
        private const string PrefabEnemyHellSkeletonPath = "Assets/Prefabs/Enemies/Hell/Enemy_Hell_Skeleton.prefab";
        private const string PrefabEnemyHellZombiePath = "Assets/Prefabs/Enemies/Hell/Enemy_Hell_Zombie.prefab";
        private const string PrefabEnemyHellHellhoundPath = "Assets/Prefabs/Enemies/Hell/Enemy_Hell_Hellhound.prefab";
        private const string PrefabEnemyHellBeholderPath = "Assets/Prefabs/Enemies/Hell/Enemy_Hell_Beholder.prefab";
        private const string PrefabEnemyHellHuskyPath = "Assets/Prefabs/Enemies/Hell/Enemy_Hell_Husky.prefab";
        private const string PrefabEnemyHellDarkGoblinPath = "Assets/Prefabs/Enemies/Hell/Enemy_Hell_DarkGoblin.prefab";

        // Cave Tileset (Hang Động Thạch Nhũ)
        private const string SpriteCaveTopLeft = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_01.png";
        private const string SpriteCaveTopMid = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_02.png";
        private const string SpriteCaveTopRight = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_03.png";
        private const string SpriteCaveFillLeft = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_10.png";
        private const string SpriteCaveFillMid = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_11.png";
        private const string SpriteCaveFillRight = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_12.png";
        private const string SpriteCaveBotLeft = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_19.png";
        private const string SpriteCaveBotMid = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_20.png";
        private const string SpriteCaveBotRight = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_21.png";
        private const string SpriteCaveWallTop = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_04.png";
        private const string SpriteCaveWallMid = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_13.png";
        private const string SpriteCaveWallBot = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_22.png";

        // Hell Tileset (Hỏa Ngục Hắc Thạch)
        private const string SpriteHellTopLeft = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_01.png";
        private const string SpriteHellTopMid = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_02.png";
        private const string SpriteHellTopRight = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_03.png";
        private const string SpriteHellFillLeft = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_10.png";
        private const string SpriteHellFillMid = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_11.png";
        private const string SpriteHellFillRight = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_12.png";
        private const string SpriteHellBotLeft = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_19.png";
        private const string SpriteHellBotMid = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_20.png";
        private const string SpriteHellBotRight = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_21.png";
        private const string SpriteHellWallTop = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_04.png";
        private const string SpriteHellWallMid = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_13.png";
        private const string SpriteHellWallBot = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_22.png";

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
        private const string SpriteGiantMushroom = "Assets/GameAssets/Cave-Tileset/Objects/Giant_Mushrooms/1.png";
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
            public GameObject ExplosiveBarrel;
            public GameObject FoodMeat;
            public GameObject FoodCake;
            public GameObject FoodCookies;
            public GameObject EnemyOoze;
            public GameObject EnemyRed;
            public GameObject EnemyTiny;
            public GameObject EnemyMage;
            public GameObject EnemyBear;
            public GameObject EnemyYellow;
            public GameObject VictoryGoal;
            public GameObject BossDemon;
            public GameObject BossGoblinKing;
            public GameObject BossNecromancer;
            public GameObject VictoryGoalToL2;
            public GameObject VictoryGoalToL3;
            public GameObject VictoryGoalClear;

            // Cave Environment
            public GameObject OneWayPlatformCave;
            public GameObject FallingPlatformCave;
            public GameObject SpikeCave;
            public GameObject CrateCave;
            public GameObject LadderCave;
            public GameObject JumpPadCave;

            // Hell Environment
            public GameObject OneWayPlatformHell;
            public GameObject FallingPlatformHell;
            public GameObject SpikeHell;
            public GameObject CrateHell;
            public GameObject LadderHell;
            public GameObject JumpPadHell;

            // Cave Monsters
            public GameObject EnemyCaveBat;
            public GameObject EnemyCaveSmallMushroom;
            public GameObject EnemyCaveGoblinMelee;
            public GameObject EnemyCaveGoblinRange;
            public GameObject EnemyCaveGoblinMage;
            public GameObject EnemyCaveBigMushroom;

            // Hell Monsters
            public GameObject EnemyHellSkeleton;
            public GameObject EnemyHellZombie;
            public GameObject EnemyHellHellhound;
            public GameObject EnemyHellDarkGoblin;
            public GameObject EnemyHellHusky;
            public GameObject EnemyHellBeholder;
        }

        public enum BiomeType
        {
            Field,
            Cave,
            Hell
        }

        private struct EnemyConfig
        {
            public string Name;
            public string PrefabPath;
            public string SpritePath;
            public string ControllerPath;
            public EnemyArchetype Archetype;
            public int MaxHealth;
            public float MoveSpeed;
            public float ChaseMultiplier;
            public float AggroRange;
            public float AttackRange;
            public int AttackDamage;
            public float AttackCooldown;
            public float AttackWindup;
            public float MaxPatrolDistance;
            public float Scale;
            public string ProjectileSpritePath;
            public float ProjectileSpeed;
            public float ProjectileLifetime;
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

            // Bước 3: Dựng trọn bộ 5 Scene (MainMenu, Level_01, Level_02, Level_03, MechanicsTestScene)
            CreateMainMenuScene(donors);
            CreateLevel01Scene(donors, envPrefabs);
            CreateLevel02Scene(donors, envPrefabs);
            CreateLevel03Scene(donors, envPrefabs);
            CreateMechanicsTestScene(donors, envPrefabs);

            // Bước 4: Cấu hình Build Settings & mở Level_01 để trải nghiệm ngay
            ConfigureBuildSettings();
            EditorSceneManager.OpenScene(SceneNames.Level01Path, OpenSceneMode.Single);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Hoàn tất Setup!",
                "Đã tổ chức xong kiến trúc Scene & Prefabs chuyên nghiệp:\n" +
                "• Level_01 (Đồng Cỏ - Field): 7 khu vực (~240m) & Trùm Demon Boss\n" +
                "• Level_02 (Hang Động - Cave): 7 khu vực (~240m) & Trùm Goblin King Boss\n" +
                "• Level_03 (Địa Ngục - Hell): 7 khu vực (~240m) & Trùm Necromancer Boss\n" +
                "• Scene Test: MechanicsTestScene.unity\n" +
                "• Core, Mechanics & Boss Prefabs: Assets/Prefabs/\n\n" +
                "Đã mở sẵn Level_01. Bạn có thể bấm Play ngay!",
                "Tuyệt vời");
        }

        [MenuItem("Tools/Platformer Scenes/1. Mở MechanicsTestScene (Test Cơ Chế)")]
        public static void OpenMechanicsTestScene() => OpenTargetScene(SceneNames.MechanicsTestPath);

        [MenuItem("Tools/Platformer Scenes/2. Mở MainMenu (Scene Chính)")]
        public static void OpenMainMenuScene() => OpenTargetScene(SceneNames.MainMenuPath);

        [MenuItem("Tools/Platformer Scenes/3. Mở Level_01 (Scene Chính)")]
        public static void OpenLevel01Scene() => OpenTargetScene(SceneNames.Level01Path);

        [MenuItem("Tools/Platformer Scenes/4. Cập nhật 6 Loại Quái & Dựng lại Level_01 (Độ khó 1)")]
        public static void RebuildAllEnemiesAndLevelsMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            RebuildAllEnemiesAndLevelsHeadless();
            EditorSceneManager.OpenScene(SceneNames.Level01Path, OpenSceneMode.Single);

            EditorUtility.DisplayDialog(
                "Cập nhật Quái & Level_01 hoàn tất!",
                "Đã tạo mới toàn bộ 6 Prefab Quái vật (AI thông minh, né gai, quay đúng hướng) và dựng hoàn chỉnh Level_01 (Độ khó 1, tối ưu hóa Map) cùng MechanicsTestScene!",
                "Tuyệt vời");
        }

        [MenuItem("Tools/Platformer Scenes/5. Dựng trọn bộ 3 Màn Chơi (Field, Cave, Hell) + 3 Đấu Trường Boss")]
        public static void BuildAll3LevelsAndBossesMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            bool confirm = EditorUtility.DisplayDialog(
                "Dựng 3 Màn Chơi & 3 Trận Đánh Boss",
                "Hệ thống sẽ tự động khởi tạo trọn bộ:\n" +
                "1. Level_01 (Đồng Cỏ): 7 khu vực (~240m), Đấu trường Chúa Quỷ (Demon Boss) -> chuyển Level_02\n" +
                "2. Level_02 (Hang Động): 7 khu vực (~240m), Đấu trường Vua Goblin (Goblin King) -> chuyển Level_03\n" +
                "3. Level_03 (Hỏa Ngục): 7 khu vực (~240m), Đấu trường Pháp Sư Chiêu Hồn (Necromancer) -> Phá đảo Game!\n" +
                "4. Đăng ký tự động Build Settings cho cả 3 màn chơi.\n\n" +
                "Thời lượng mỗi màn 5 - 10 phút. Bắt đầu dựng ngay?",
                "Dựng ngay", "Hủy");

            if (!confirm) return;

            BuildAll3LevelsAndBossesHeadless();
            EditorSceneManager.OpenScene(SceneNames.Level01Path, OpenSceneMode.Single);

            EditorUtility.DisplayDialog(
                "Hoàn tất dựng 3 Màn Chơi!",
                "Đã dựng thành công trọn bộ 3 Level với 3 Biome độc bản và 3 Trận Boss nghẹt thở:\n" +
                "• Level_01 (Field): Đã mở sẵn, sẵn sàng Play!\n" +
                "• Level_02 (Cave): Hang Động Thạch Nhũ & Nấm Khổng Lồ\n" +
                "• Level_03 (Hell): Hỏa Ngục Dung Nham & Hắc Thạch\n\n" +
                "Bạn có thể bấm Play để trải nghiệm hành trình xuyên suốt 3 thế giới!",
                "Chiến ngay!");
        }

        [MenuItem("Tools/Platformer Scenes/6. Mở Level_02 (Hang Động - Cave)")]
        public static void OpenLevel02Scene() => OpenTargetScene(SceneNames.Level02Path);

        [MenuItem("Tools/Platformer Scenes/7. Mở Level_03 (Địa Ngục - Hell)")]
        public static void OpenLevel03Scene() => OpenTargetScene(SceneNames.Level03Path);

        /// <summary>
        /// Tạo lại 6 Prefab Quái vật, tối ưu hóa Map và dựng lại Level_01 + MechanicsTestScene không chặn Dialog.
        /// </summary>
        public static void RebuildAllEnemiesAndLevelsHeadless()
        {
            EnsureFolderStructure();

            DonorAssets donors = LoadExistingDonorsOrExtractFallback();
            EnvironmentPrefabs envPrefabs = CreateGameplayPrefabs(donors);
            UISetupTool.BuildUIPrefabsOnly();

            CreateLevel01Scene(donors, envPrefabs);
            CreateMechanicsTestScene(donors, envPrefabs);
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void BuildAll3LevelsAndBossesHeadless()
        {
            EnsureFolderStructure();

            DonorAssets donors = LoadExistingDonorsOrExtractFallback();
            EnvironmentPrefabs envPrefabs = CreateGameplayPrefabs(donors);
            UISetupTool.BuildUIPrefabsOnly();

            CreateLevel01Scene(donors, envPrefabs);
            CreateLevel02Scene(donors, envPrefabs);
            CreateLevel03Scene(donors, envPrefabs);
            CreateMechanicsTestScene(donors, envPrefabs);
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static DonorAssets LoadExistingDonorsOrExtractFallback()
        {
            DonorAssets donors = new DonorAssets
            {
                PlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPlayerPath),
                CameraRigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabCameraRigPath),
                GlobalLightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabGlobalLightPath),
                GameSystemsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabGameSystemsPath),
                FrictionlessMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/Frictionless2D.physicsMaterial2D"),
                SquareSprite = LoadFirstSprite("Assets/GameAssets/UI/2 Bars/Square.png")
            };

            if (donors.PlayerPrefab != null && donors.PlayerPrefab.TryGetComponent<SpriteRenderer>(out var playerSr))
            {
                donors.SpriteMaterial = playerSr.sharedMaterial;
            }

            if (donors.PlayerPrefab == null || donors.CameraRigPrefab == null || donors.GlobalLightPrefab == null || donors.SpriteMaterial == null)
            {
                if (File.Exists(SampleScenePath))
                {
                    donors = ExtractDonorPrefabsFromSampleScene();
                    donors.GameSystemsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabGameSystemsPath) ?? CreateGameSystemsPrefab();
                    return donors;
                }
            }

            if (donors.PlayerPrefab != null && donors.PlayerPrefab.GetComponent<PlayerProgression>() == null)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPlayerPath);
                if (contents != null)
                {
                    contents.AddComponent<PlayerProgression>();
                    PrefabUtility.SaveAsPrefabAsset(contents, PrefabPlayerPath);
                    PrefabUtility.UnloadPrefabContents(contents);
                    donors.PlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPlayerPath);
                }
            }

            if (donors.GameSystemsPrefab == null)
            {
                donors.GameSystemsPrefab = CreateGameSystemsPrefab();
            }

            return donors;
        }

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
                "Assets/Prefabs/Enemies/Cave",
                "Assets/Prefabs/Enemies/Hell",
                "Assets/Prefabs/Bosses",
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
                if (playerTemp.GetComponent<PlayerProgression>() == null)
                {
                    playerTemp.AddComponent<PlayerProgression>();
                }
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

            // 4. Jump Pad (Nấm nhún khổng lồ bật nhảy sống động)
            GameObject jumpPad = new GameObject("JumpPad");
            GameObject mushroomVisual = CreateSpriteObject("Visual", SpriteGiantMushroom, donors, Color.white, 0);
            mushroomVisual.transform.SetParent(jumpPad.transform, false);
            const float jumpPadScale = 0.5f;
            mushroomVisual.transform.localScale = new Vector3(jumpPadScale, jumpPadScale, 1f);
            mushroomVisual.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            BoxCollider2D jpCol = jumpPad.AddComponent<BoxCollider2D>();
            jpCol.size = new Vector2(1.25f, 1.2f);
            jpCol.offset = new Vector2(0f, 0.65f);
            JumpPad jpComp = jumpPad.AddComponent<JumpPad>();
            SetFloatProperty(jpComp, "_bounceForce", 16.5f);
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

            // 10. Bộ 6 Prefab Quái vật Field (Ooze, Red, Tiny, Mage, Bear, Yellow) + bản tương thích Enemy_Ooze_Test
            var oozeCfg = new EnemyConfig
            {
                Name = "Enemy_Ooze",
                PrefabPath = PrefabEnemyOozePath,
                SpritePath = "Assets/GameAssets/Monster-Field/3 Ooze/Ooze.png",
                ControllerPath = "Assets/Animations/Enemies/Field/Ooze/Ooze.controller",
                Archetype = EnemyArchetype.PatrolSlime,
                MaxHealth = 3,
                MoveSpeed = 1.6f,
                ChaseMultiplier = 1.25f,
                AggroRange = 4.8f,
                AttackRange = 1.25f,
                AttackDamage = 1,
                AttackCooldown = 1.55f,
                AttackWindup = 0.28f,
                MaxPatrolDistance = 3.2f,
                Scale = 1.0f
            };
            prefabs.EnemyOoze = CreateEnemyPrefab(oozeCfg, donors);

            oozeCfg.Name = "Enemy_Ooze_Test";
            oozeCfg.PrefabPath = PrefabEnemyOozeTestPath;
            CreateEnemyPrefab(oozeCfg, donors);

            prefabs.EnemyRed = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Red",
                PrefabPath = PrefabEnemyRedPath,
                SpritePath = "Assets/GameAssets/Monster-Field/4 Red/Red.png",
                ControllerPath = "Assets/Animations/Enemies/Field/Red/Red.controller",
                Archetype = EnemyArchetype.FastChaser,
                MaxHealth = 3,
                MoveSpeed = 2.2f,
                ChaseMultiplier = 1.35f,
                AggroRange = 6.2f,
                AttackRange = 1.35f,
                AttackDamage = 1,
                AttackCooldown = 1.20f,
                AttackWindup = 0.22f,
                MaxPatrolDistance = 3.5f,
                Scale = 1.0f
            }, donors);

            prefabs.EnemyTiny = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Tiny",
                PrefabPath = PrefabEnemyTinyPath,
                SpritePath = "Assets/GameAssets/Monster-Field/5 Tiny/Tiny.png",
                ControllerPath = "Assets/Animations/Enemies/Field/Tiny/Tiny.controller",
                Archetype = EnemyArchetype.SpikeSummoner,
                MaxHealth = 3,
                MoveSpeed = 1.8f,
                ChaseMultiplier = 1.15f,
                AggroRange = 6.5f,
                AttackRange = 4.8f,
                AttackDamage = 1,
                AttackCooldown = 2.0f,
                AttackWindup = 0.34f,
                MaxPatrolDistance = 3.0f,
                Scale = 1.05f,
                ProjectileSpritePath = "Assets/GameAssets/Monster-Field/5 Tiny/Attack_Spike.png",
                ProjectileSpeed = 4.6f,
                ProjectileLifetime = 1.4f
            }, donors);

            prefabs.EnemyMage = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Mage",
                PrefabPath = PrefabEnemyMagePath,
                SpritePath = "Assets/GameAssets/Monster-Field/2 Mage/Mage.png",
                ControllerPath = "Assets/Animations/Enemies/Field/Mage/Mage.controller",
                Archetype = EnemyArchetype.RangedMage,
                MaxHealth = 4,
                MoveSpeed = 1.4f,
                ChaseMultiplier = 1.10f,
                AggroRange = 8.0f,
                AttackRange = 6.8f,
                AttackDamage = 1,
                AttackCooldown = 2.20f,
                AttackWindup = 0.38f,
                MaxPatrolDistance = 2.4f,
                Scale = 1.05f,
                ProjectileSpritePath = "Assets/GameAssets/Monster-Field/2 Mage/Attack_ball.png",
                ProjectileSpeed = 5.2f,
                ProjectileLifetime = 2.6f
            }, donors);

            prefabs.EnemyBear = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Bear",
                PrefabPath = PrefabEnemyBearPath,
                SpritePath = "Assets/GameAssets/Monster-Field/1 Bear/Bear.png",
                ControllerPath = "Assets/Animations/Enemies/Field/Bear/Bear.controller",
                Archetype = EnemyArchetype.HeavyBruiser,
                MaxHealth = 5,
                MoveSpeed = 1.5f,
                ChaseMultiplier = 1.30f,
                AggroRange = 5.8f,
                AttackRange = 1.50f,
                AttackDamage = 1,
                AttackCooldown = 1.70f,
                AttackWindup = 0.34f,
                MaxPatrolDistance = 3.2f,
                Scale = 1.15f
            }, donors);

            prefabs.EnemyYellow = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Yellow",
                PrefabPath = PrefabEnemyYellowPath,
                SpritePath = "Assets/GameAssets/Monster-Field/6 Yellow/Yellow.png",
                ControllerPath = "Assets/Animations/Enemies/Field/Yellow/Yellow.controller",
                Archetype = EnemyArchetype.EliteCrusher,
                MaxHealth = 6,
                MoveSpeed = 1.6f,
                ChaseMultiplier = 1.32f,
                AggroRange = 6.8f,
                AttackRange = 1.60f,
                AttackDamage = 1,
                AttackCooldown = 1.50f,
                AttackWindup = 0.32f,
                MaxPatrolDistance = 4.0f,
                Scale = 1.25f
            }, donors);

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
            SetObjectArrayProperty(chestComp, "_dropPrefabs", new Object[] { prefabs.RockPickup, prefabs.HealthPickup, AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFoodMeatPath) });
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

            // 14. Tương tác mới: Thùng nổ & Vật phẩm thức ăn
            prefabs.ExplosiveBarrel = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabExplosiveBarrelPath);
            prefabs.FoodMeat = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFoodMeatPath);
            prefabs.FoodCake = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFoodCakePath);
            prefabs.FoodCookies = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFoodCookiesPath);

            // 15. Boss Prefabs (Demon, Goblin King, Necromancer)
            prefabs.BossDemon = CreateBossPrefab<DemonBoss>(
                "Boss_Demon",
                "Assets/GameAssets/Boss-Field/2 Demon/Demon_Boss.png",
                "Assets/Animations/Bosses/Field/Demon/Demon.controller",
                1.45f,
                donors,
                PrefabBossDemonPath);

            prefabs.BossGoblinKing = CreateBossPrefab<GoblinKingBoss>(
                "Boss_GoblinKing",
                "Assets/GameAssets/Boss-Cave/2 Goblin king/GoblinKing.png",
                "Assets/Animations/Bosses/Cave/Goblin_king/Goblin_king.controller",
                1.40f,
                donors,
                PrefabBossGoblinKingPath);

            prefabs.BossNecromancer = CreateBossPrefab<NecromancerBoss>(
                "Boss_Necromancer",
                "Assets/GameAssets/Boss-Hell/1 Necromancer/Necromancer.png",
                "Assets/Animations/Bosses/Hell/Necromancer/Necromancer.controller",
                1.45f,
                donors,
                PrefabBossNecromancerPath);

            // 16. Chuyển màn sau khi thắng Boss
            prefabs.VictoryGoalToL2 = CreateCustomVictoryGoal("VictoryGoal_ToLevel2", SceneNames.Level02, donors, pickupVfxFrames, PrefabVictoryToL2Path);
            prefabs.VictoryGoalToL3 = CreateCustomVictoryGoal("VictoryGoal_ToLevel3", SceneNames.Level03, donors, pickupVfxFrames, PrefabVictoryToL3Path);
            prefabs.VictoryGoalClear = CreateCustomVictoryGoal("VictoryGoal_GameClear", "", donors, pickupVfxFrames, PrefabVictoryClearPath);

            // 17. PREFABS MÔI TRƯỜNG ĐẶC TRƯNG HANG ĐỘNG (CAVE)
            prefabs.OneWayPlatformCave = CreateOneWayPlatformPrefab(
                "Platform_OneWay_Cave",
                "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_28.png",
                "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_29.png",
                "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_30.png",
                donors,
                PrefabOneWayCavePath);

            prefabs.FallingPlatformCave = CreateFallingPlatformPrefab(
                "FallingPlatform_Cave",
                "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_28.png",
                "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_29.png",
                "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_30.png",
                donors,
                PrefabFallingCavePath);

            prefabs.SpikeCave = CreateSpikePrefab("SpikeHazard_Cave", "Assets/GameAssets/Cave-Tileset/Objects/Spikes/1.png", donors, PrefabSpikeCavePath);
            prefabs.CrateCave = CreateCratePrefab("Crate_Cave", "Assets/GameAssets/Cave-Tileset/Objects/Boxes/Box1.png", prefabs.RockPickup, donors, PrefabCrateCavePath);
            prefabs.LadderCave = CreateLadderPrefab("Ladder_Cave", "Assets/GameAssets/Cave-Tileset/Objects/Ladders/1.png", donors, PrefabLadderCavePath);
            prefabs.JumpPadCave = CreateJumpPadPrefab("JumpPad_Cave", "Assets/GameAssets/Cave-Tileset/Objects/Giant_Mushrooms/1.png", donors, PrefabJumpPadCavePath);

            // 18. PREFABS MÔI TRƯỜNG ĐẶC TRƯNG HỎA NGỤC (HELL)
            prefabs.OneWayPlatformHell = CreateOneWayPlatformPrefab(
                "Platform_OneWay_Hell",
                "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_28.png",
                "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_29.png",
                "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_30.png",
                donors,
                PrefabOneWayHellPath);

            prefabs.FallingPlatformHell = CreateFallingPlatformPrefab(
                "FallingPlatform_Hell",
                "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_28.png",
                "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_29.png",
                "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_30.png",
                donors,
                PrefabFallingHellPath);

            prefabs.SpikeHell = CreateSpikePrefab("SpikeHazard_Hell", "Assets/GameAssets/Hell-Tileset/Objects/Spikes/1.png", donors, PrefabSpikeHellPath);
            prefabs.CrateHell = CreateCratePrefab("Crate_Hell", "Assets/GameAssets/Hell-Tileset/Objects/Boxes/1.png", prefabs.RockPickup, donors, PrefabCrateHellPath);
            prefabs.LadderHell = CreateLadderPrefab("Ladder_Hell", "Assets/GameAssets/Hell-Tileset/Objects/Ladders/1.png", donors, PrefabLadderHellPath);
            prefabs.JumpPadHell = CreateJumpPadPrefab("JumpPad_Hell", "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_31.png", donors, PrefabJumpPadHellPath, new Color(1.0f, 0.75f, 0.55f, 1f), true);

            // 19. BỘ 6 QUÁI VẬT HANG ĐỘNG (CAVE)
            prefabs.EnemyCaveBat = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Cave_Bat",
                PrefabPath = PrefabEnemyCaveBatPath,
                SpritePath = "Assets/GameAssets/Monster-Cave/1 Bat/Bat.png",
                ControllerPath = "Assets/Animations/Enemies/Cave/Bat/Bat.controller",
                Archetype = EnemyArchetype.FastChaser,
                MaxHealth = 3,
                MoveSpeed = 2.4f,
                ChaseMultiplier = 1.35f,
                AggroRange = 6.5f,
                AttackRange = 1.35f,
                AttackDamage = 1,
                AttackCooldown = 1.20f,
                AttackWindup = 0.20f,
                MaxPatrolDistance = 3.5f,
                Scale = 1.0f
            }, donors);

            prefabs.EnemyCaveSmallMushroom = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Cave_SmallMushroom",
                PrefabPath = PrefabEnemyCaveSmallMushroomPath,
                SpritePath = "Assets/GameAssets/Monster-Cave/5 Small_Mushroom/Small_Mushroom.png",
                ControllerPath = "Assets/Animations/Enemies/Cave/Small_Mushroom/Small_Mushroom.controller",
                Archetype = EnemyArchetype.PatrolSlime,
                MaxHealth = 3,
                MoveSpeed = 1.6f,
                ChaseMultiplier = 1.25f,
                AggroRange = 5.0f,
                AttackRange = 1.25f,
                AttackDamage = 1,
                AttackCooldown = 1.50f,
                AttackWindup = 0.28f,
                MaxPatrolDistance = 3.0f,
                Scale = 1.0f
            }, donors);

            prefabs.EnemyCaveGoblinMelee = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Cave_GoblinMelee",
                PrefabPath = PrefabEnemyCaveGoblinMeleePath,
                SpritePath = "Assets/GameAssets/Monster-Cave/3 Goblin_Melee/Goblin_Melee.png",
                ControllerPath = "Assets/Animations/Enemies/Cave/Goblin_Melee/Goblin_Melee.controller",
                Archetype = EnemyArchetype.HeavyBruiser,
                MaxHealth = 4,
                MoveSpeed = 1.8f,
                ChaseMultiplier = 1.30f,
                AggroRange = 6.0f,
                AttackRange = 1.50f,
                AttackDamage = 1,
                AttackCooldown = 1.60f,
                AttackWindup = 0.30f,
                MaxPatrolDistance = 3.5f,
                Scale = 1.10f
            }, donors);

            prefabs.EnemyCaveGoblinRange = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Cave_GoblinRange",
                PrefabPath = PrefabEnemyCaveGoblinRangePath,
                SpritePath = "Assets/GameAssets/Monster-Cave/4 Goblin_Range/Goblin_Range.png",
                ControllerPath = "Assets/Animations/Enemies/Cave/Goblin_Range/Goblin_Range.controller",
                Archetype = EnemyArchetype.SpikeSummoner,
                MaxHealth = 3,
                MoveSpeed = 1.7f,
                ChaseMultiplier = 1.15f,
                AggroRange = 7.0f,
                AttackRange = 5.5f,
                AttackDamage = 1,
                AttackCooldown = 2.0f,
                AttackWindup = 0.32f,
                MaxPatrolDistance = 3.0f,
                Scale = 1.05f
            }, donors);

            prefabs.EnemyCaveGoblinMage = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Cave_GoblinMage",
                PrefabPath = PrefabEnemyCaveGoblinMagePath,
                SpritePath = "Assets/GameAssets/Monster-Cave/2 Goblin_Mage/Goblin_Mage.png",
                ControllerPath = "Assets/Animations/Enemies/Cave/Goblin_Mage/Goblin_Mage.controller",
                Archetype = EnemyArchetype.RangedMage,
                MaxHealth = 4,
                MoveSpeed = 1.4f,
                ChaseMultiplier = 1.10f,
                AggroRange = 8.0f,
                AttackRange = 6.5f,
                AttackDamage = 1,
                AttackCooldown = 2.20f,
                AttackWindup = 0.35f,
                MaxPatrolDistance = 2.5f,
                Scale = 1.05f,
                ProjectileSpritePath = "Assets/GameAssets/Monster-Cave/2 Goblin_Mage/Attack_balls.png",
                ProjectileSpeed = 5.0f,
                ProjectileLifetime = 2.5f
            }, donors);

            prefabs.EnemyCaveBigMushroom = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Cave_BigMushroom",
                PrefabPath = PrefabEnemyCaveBigMushroomPath,
                SpritePath = "Assets/GameAssets/Monster-Cave/6 Big_Mushroom/Big_Mushroom.png",
                ControllerPath = "Assets/Animations/Enemies/Cave/Big_Mushroom/Big_Mushroom.controller",
                Archetype = EnemyArchetype.EliteCrusher,
                MaxHealth = 7,
                MoveSpeed = 1.5f,
                ChaseMultiplier = 1.25f,
                AggroRange = 6.5f,
                AttackRange = 1.60f,
                AttackDamage = 2,
                AttackCooldown = 1.80f,
                AttackWindup = 0.38f,
                MaxPatrolDistance = 3.0f,
                Scale = 1.25f
            }, donors);

            // 20. BỘ 6 QUÁI VẬT HỎA NGỤC (HELL)
            prefabs.EnemyHellSkeleton = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Hell_Skeleton",
                PrefabPath = PrefabEnemyHellSkeletonPath,
                SpritePath = "Assets/GameAssets/Monster-Hell/1 Skeleton/Skeleton.png",
                ControllerPath = "Assets/Animations/Enemies/Hell/Skeleton/Skeleton.controller",
                Archetype = EnemyArchetype.FastChaser,
                MaxHealth = 4,
                MoveSpeed = 2.2f,
                ChaseMultiplier = 1.35f,
                AggroRange = 6.5f,
                AttackRange = 1.40f,
                AttackDamage = 1,
                AttackCooldown = 1.30f,
                AttackWindup = 0.24f,
                MaxPatrolDistance = 3.5f,
                Scale = 1.05f
            }, donors);

            prefabs.EnemyHellZombie = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Hell_Zombie",
                PrefabPath = PrefabEnemyHellZombiePath,
                SpritePath = "Assets/GameAssets/Monster-Hell/2 Zombie/Zombie.png",
                ControllerPath = "Assets/Animations/Enemies/Hell/Zombie/Zombie.controller",
                Archetype = EnemyArchetype.PatrolSlime,
                MaxHealth = 5,
                MoveSpeed = 1.3f,
                ChaseMultiplier = 1.20f,
                AggroRange = 5.5f,
                AttackRange = 1.30f,
                AttackDamage = 1,
                AttackCooldown = 1.70f,
                AttackWindup = 0.32f,
                MaxPatrolDistance = 3.0f,
                Scale = 1.05f
            }, donors);

            prefabs.EnemyHellHellhound = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Hell_Hellhound",
                PrefabPath = PrefabEnemyHellHellhoundPath,
                SpritePath = "Assets/GameAssets/Monster-Hell/3 Hellhound/Hellhound.png",
                ControllerPath = "Assets/Animations/Enemies/Hell/Hellhound/Hellhound.controller",
                Archetype = EnemyArchetype.FastChaser,
                MaxHealth = 4,
                MoveSpeed = 2.7f,
                ChaseMultiplier = 1.40f,
                AggroRange = 7.5f,
                AttackRange = 1.40f,
                AttackDamage = 1,
                AttackCooldown = 1.10f,
                AttackWindup = 0.20f,
                MaxPatrolDistance = 4.0f,
                Scale = 1.10f
            }, donors);

            prefabs.EnemyHellDarkGoblin = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Hell_DarkGoblin",
                PrefabPath = PrefabEnemyHellDarkGoblinPath,
                SpritePath = "Assets/GameAssets/Monster-Hell/6 Dark_Goblin/DarkGoblin.png",
                ControllerPath = "Assets/Animations/Enemies/Hell/Dark_Goblin/Dark_Goblin.controller",
                Archetype = EnemyArchetype.HeavyBruiser,
                MaxHealth = 5,
                MoveSpeed = 1.7f,
                ChaseMultiplier = 1.30f,
                AggroRange = 6.2f,
                AttackRange = 1.50f,
                AttackDamage = 1,
                AttackCooldown = 1.50f,
                AttackWindup = 0.30f,
                MaxPatrolDistance = 3.2f,
                Scale = 1.10f
            }, donors);

            prefabs.EnemyHellHusky = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Hell_Husky",
                PrefabPath = PrefabEnemyHellHuskyPath,
                SpritePath = "Assets/GameAssets/Monster-Hell/5 Husky/Husky.png",
                ControllerPath = "Assets/Animations/Enemies/Hell/Husky/Husky.controller",
                Archetype = EnemyArchetype.HeavyBruiser,
                MaxHealth = 6,
                MoveSpeed = 1.9f,
                ChaseMultiplier = 1.35f,
                AggroRange = 6.8f,
                AttackRange = 1.60f,
                AttackDamage = 2,
                AttackCooldown = 1.60f,
                AttackWindup = 0.32f,
                MaxPatrolDistance = 3.5f,
                Scale = 1.15f
            }, donors);

            prefabs.EnemyHellBeholder = CreateEnemyPrefab(new EnemyConfig
            {
                Name = "Enemy_Hell_Beholder",
                PrefabPath = PrefabEnemyHellBeholderPath,
                SpritePath = "Assets/GameAssets/Monster-Hell/4 Beholder/Beholder.png",
                ControllerPath = "Assets/Animations/Enemies/Hell/Beholder/Beholder.controller",
                Archetype = EnemyArchetype.RangedMage,
                MaxHealth = 6,
                MoveSpeed = 1.5f,
                ChaseMultiplier = 1.15f,
                AggroRange = 8.5f,
                AttackRange = 7.0f,
                AttackDamage = 2,
                AttackCooldown = 2.0f,
                AttackWindup = 0.36f,
                MaxPatrolDistance = 3.0f,
                Scale = 1.15f,
                ProjectileSpritePath = "Assets/GameAssets/Monster-Hell/4 Beholder/Attack_ray.png",
                ProjectileSpeed = 5.5f,
                ProjectileLifetime = 2.8f
            }, donors);

            return prefabs;
        }

        private static GameObject CreateBossPrefab<TBoss>(
            string bossName,
            string spritePath,
            string controllerPath,
            float scale,
            DonorAssets donors,
            string prefabPath) where TBoss : BaseBoss
        {
            GameObject boss = CreateSpriteObject(bossName, spritePath, donors, Color.white, 0);
            boss.transform.localScale = new Vector3(scale, scale, 1f);

            Animator anim = boss.AddComponent<Animator>();
            if (!string.IsNullOrEmpty(controllerPath))
            {
                anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            }

            Rigidbody2D rb = boss.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 12f;
            rb.linearDamping = 1.5f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            if (donors.FrictionlessMaterial != null) rb.sharedMaterial = donors.FrictionlessMaterial;

            BoxCollider2D col = boss.AddComponent<BoxCollider2D>();
            SpriteRenderer sr = boss.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                Bounds sb = sr.sprite.bounds;
                col.size = new Vector2(Mathf.Clamp(sb.size.x * 0.70f, 1.2f, 2.6f), Mathf.Clamp(sb.size.y * 0.88f, 1.4f, 3.0f));
                col.offset = new Vector2(sb.center.x, sb.min.y + col.size.y * 0.5f);
            }
            else
            {
                col.size = new Vector2(1.8f, 2.2f);
                col.offset = Vector2.zero;
            }
            if (donors.FrictionlessMaterial != null) col.sharedMaterial = donors.FrictionlessMaterial;

            TBoss bossComp = boss.AddComponent<TBoss>();

            DamageDealer dealer = boss.AddComponent<DamageDealer>();
            SetIntProperty(dealer, "_damageAmount", 2);
            SetStringProperty(dealer, "_targetTag", "Player");
            SetFloatProperty(dealer, "_knockbackHorizontal", 6.5f);
            SetFloatProperty(dealer, "_knockbackVertical", 4.5f);

            return SaveTempAsPrefab(boss, prefabPath);
        }

        private static GameObject CreateCustomVictoryGoal(
            string goalName,
            string targetNextScene,
            DonorAssets donors,
            Sprite[] pickupVfxFrames,
            string prefabPath)
        {
            GameObject victoryGoal = CreateSpriteObject(goalName, "Assets/GameAssets/Field-Tileset/Objects_Animated/Golden_Chest_Open.png", donors, Color.white, 0);
            victoryGoal.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
            BoxCollider2D victoryCol = victoryGoal.AddComponent<BoxCollider2D>();
            victoryCol.isTrigger = true;
            victoryCol.size = new Vector2(0.84375f, 0.5625f);
            victoryCol.offset = Vector2.zero;
            VictoryGoal victoryComp = victoryGoal.AddComponent<VictoryGoal>();
            SetBoolProperty(victoryComp, "_requireGoldenKey", true);
            SetStringProperty(victoryComp, "_nextSceneName", targetNextScene);
            SetSpriteArrayProperty(victoryComp, "_openFrames", LoadAllSpritesSorted("Assets/GameAssets/Field-Tileset/Objects_Animated/Golden_Chest_Open.png"));
            SetSpriteArrayProperty(victoryComp, "_pickupVfxFrames", pickupVfxFrames);
            return SaveTempAsPrefab(victoryGoal, prefabPath);
        }

        private static GameObject CreateOneWayPlatformPrefab(
            string name,
            string leftSpritePath,
            string midSpritePath,
            string rightSpritePath,
            DonorAssets donors,
            string savePath)
        {
            GameObject oneWay = new GameObject(name);
            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(oneWay.transform, false);
            float width = BuildHorizontalTileStrip(
                visuals.transform,
                3,
                LoadFirstSprite(leftSpritePath),
                LoadFirstSprite(midSpritePath),
                LoadFirstSprite(rightSpritePath),
                0.15f,
                donors,
                -2);
            BoxCollider2D col = oneWay.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width > 0f ? width : 3.0f, 0.50f);
            col.offset = new Vector2(0f, -0.10f);
            col.usedByEffector = true;
            PlatformEffector2D effector = oneWay.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 170f;
            oneWay.AddComponent<OneWayPlatform>();
            return SaveTempAsPrefab(oneWay, savePath);
        }

        private static GameObject CreateFallingPlatformPrefab(
            string name,
            string leftSpritePath,
            string midSpritePath,
            string rightSpritePath,
            DonorAssets donors,
            string savePath)
        {
            GameObject falling = new GameObject(name);
            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(falling.transform, false);
            float width = BuildHorizontalTileStrip(
                visuals.transform,
                2,
                LoadFirstSprite(leftSpritePath),
                LoadFirstSprite(midSpritePath),
                LoadFirstSprite(rightSpritePath),
                0.20f,
                donors,
                0);
            BoxCollider2D col = falling.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width > 0f ? width : 1.97f, 0.40f);
            col.offset = Vector2.zero;
            Rigidbody2D rb = falling.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            falling.AddComponent<FallingPlatform>();
            return SaveTempAsPrefab(falling, savePath);
        }

        private static GameObject CreateSpikePrefab(string name, string spritePath, DonorAssets donors, string savePath)
        {
            GameObject spike = CreateSpriteObject(name, spritePath, donors, Color.white, 0);
            BoxCollider2D col = spike.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.80f, 0.58f);
            col.offset = new Vector2(0f, -0.085f);
            SpikeHazard comp = spike.AddComponent<SpikeHazard>();
            SetIntProperty(comp, "_damage", 1);
            SetBoolProperty(comp, "_instantKill", false);
            SetVector2Property(comp, "_knockbackForce", new Vector2(4f, 7f));
            return SaveTempAsPrefab(spike, savePath);
        }

        private static GameObject CreateCratePrefab(string name, string spritePath, GameObject dropPrefab, DonorAssets donors, string savePath)
        {
            GameObject crate = CreateSpriteObject(name, spritePath, donors, Color.white, 0);
            BoxCollider2D col = crate.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.96f, 0.75f);
            col.offset = new Vector2(0f, -0.015625f);
            if (donors.FrictionlessMaterial != null) col.sharedMaterial = donors.FrictionlessMaterial;
            Rigidbody2D rb = crate.AddComponent<Rigidbody2D>();
            if (donors.FrictionlessMaterial != null) rb.sharedMaterial = donors.FrictionlessMaterial;
            PushableBox pushable = crate.AddComponent<PushableBox>();
            SetBoolProperty(pushable, "_isBreakable", true);
            SetIntProperty(pushable, "_hitsToBreak", 3);
            SetObjectProperty(pushable, "_dropPrefab", dropPrefab);
            return SaveTempAsPrefab(crate, savePath);
        }

        private static GameObject CreateLadderPrefab(string name, string spritePath, DonorAssets donors, string savePath)
        {
            GameObject ladder = new GameObject(name);
            BoxCollider2D ladderCol = ladder.AddComponent<BoxCollider2D>();
            ladderCol.size = new Vector2(0.75f, 4.20f);
            ladderCol.offset = new Vector2(0f, 0.10f);
            ladderCol.isTrigger = true;
            ladder.AddComponent<Ladder>();

            Sprite ladderSprite = LoadFirstSprite(spritePath);
            for (int i = 0; i < 4; i++)
            {
                GameObject segObj = new GameObject($"Segment_{i}");
                segObj.transform.SetParent(ladder.transform, false);
                segObj.transform.localPosition = new Vector3(0f, -1.5f + i * 1.0f, 0f);
                SpriteRenderer sr = segObj.AddComponent<SpriteRenderer>();
                sr.sprite = ladderSprite;
                if (donors.SpriteMaterial != null) sr.sharedMaterial = donors.SpriteMaterial;
                sr.sortingOrder = -1;
            }
            return SaveTempAsPrefab(ladder, savePath);
        }

        private static GameObject CreateJumpPadPrefab(string name, string spritePath, DonorAssets donors, string savePath, Color? tint = null, bool isTileSprite = false)
        {
            GameObject jumpPad = new GameObject(name);
            GameObject visual = CreateSpriteObject("Visual", spritePath, donors, tint ?? Color.white, 0);
            visual.transform.SetParent(jumpPad.transform, false);
            if (isTileSprite)
            {
                visual.transform.localScale = new Vector3(1.15f, 0.85f, 1f);
                visual.transform.localPosition = new Vector3(0f, 0.42f, 0f);
                Platformer2D.VFX.AmbientGlowPulse.Attach(visual.transform, new Color(1f, 0.32f, 0.12f, 0.45f), 1.4f, -1, Vector3.zero);
            }
            else
            {
                visual.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                visual.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            }
            BoxCollider2D col = jumpPad.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.25f, isTileSprite ? 0.85f : 1.2f);
            col.offset = new Vector2(0f, isTileSprite ? 0.42f : 0.65f);
            JumpPad comp = jumpPad.AddComponent<JumpPad>();
            SetFloatProperty(comp, "_bounceForce", 16.5f);
            return SaveTempAsPrefab(jumpPad, savePath);
        }

        private static GameObject GetOneWayPlatform(EnvironmentPrefabs p, BiomeType biome)
        {
            if (biome == BiomeType.Cave) return p.OneWayPlatformCave != null ? p.OneWayPlatformCave : p.OneWayPlatform;
            if (biome == BiomeType.Hell) return p.OneWayPlatformHell != null ? p.OneWayPlatformHell : p.OneWayPlatform;
            return p.OneWayPlatform;
        }

        private static GameObject GetFallingPlatform(EnvironmentPrefabs p, BiomeType biome)
        {
            if (biome == BiomeType.Cave) return p.FallingPlatformCave != null ? p.FallingPlatformCave : p.FallingPlatform;
            if (biome == BiomeType.Hell) return p.FallingPlatformHell != null ? p.FallingPlatformHell : p.FallingPlatform;
            return p.FallingPlatform;
        }

        private static GameObject GetSpike(EnvironmentPrefabs p, BiomeType biome)
        {
            if (biome == BiomeType.Cave) return p.SpikeCave != null ? p.SpikeCave : p.Spike;
            if (biome == BiomeType.Hell) return p.SpikeHell != null ? p.SpikeHell : p.Spike;
            return p.Spike;
        }

        private static GameObject GetCrate(EnvironmentPrefabs p, BiomeType biome)
        {
            if (biome == BiomeType.Cave) return p.CrateCave != null ? p.CrateCave : p.Crate;
            if (biome == BiomeType.Hell) return p.CrateHell != null ? p.CrateHell : p.Crate;
            return p.Crate;
        }

        private static GameObject GetLadder(EnvironmentPrefabs p, BiomeType biome)
        {
            if (biome == BiomeType.Cave) return p.LadderCave != null ? p.LadderCave : p.Ladder;
            if (biome == BiomeType.Hell) return p.LadderHell != null ? p.LadderHell : p.Ladder;
            return p.Ladder;
        }

        private static GameObject GetJumpPad(EnvironmentPrefabs p, BiomeType biome)
        {
            if (biome == BiomeType.Cave) return p.JumpPadCave != null ? p.JumpPadCave : p.JumpPad;
            if (biome == BiomeType.Hell) return p.JumpPadHell != null ? p.JumpPadHell : p.JumpPad;
            return p.JumpPad;
        }

        private static GameObject CreateEnemyPrefab(EnemyConfig cfg, DonorAssets donors)
        {
            GameObject enemy = CreateSpriteObject(cfg.Name, cfg.SpritePath, donors, Color.white, 0);
            enemy.transform.localScale = new Vector3(cfg.Scale, cfg.Scale, 1f);

            Animator anim = enemy.AddComponent<Animator>();
            if (!string.IsNullOrEmpty(cfg.ControllerPath))
            {
                anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(cfg.ControllerPath);
            }

            Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            if (donors.FrictionlessMaterial != null) rb.sharedMaterial = donors.FrictionlessMaterial;

            BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
            SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                Bounds sb = sr.sprite.bounds;
                col.size = new Vector2(Mathf.Clamp(sb.size.x * 0.82f, 0.50f, 1.05f), Mathf.Clamp(sb.size.y * 0.94f, 0.55f, 1.15f));
                col.offset = new Vector2(sb.center.x, sb.min.y + col.size.y * 0.5f);
            }
            else
            {
                col.size = new Vector2(0.80f, 0.72f);
                col.offset = Vector2.zero;
            }
            if (donors.FrictionlessMaterial != null) col.sharedMaterial = donors.FrictionlessMaterial;

            Health health = enemy.AddComponent<Health>();
            SetIntProperty(health, "_maxHealth", cfg.MaxHealth);
            SetBoolProperty(health, "_isPlayer", false);

            BaseEnemy ai = enemy.AddComponent<BaseEnemy>();
            SetIntProperty(ai, "_archetype", (int)cfg.Archetype);
            SetBoolProperty(ai, "_spriteFacesLeftByDefault", true);
            SetFloatProperty(ai, "_moveSpeed", cfg.MoveSpeed);
            SetFloatProperty(ai, "_chaseSpeedMultiplier", cfg.ChaseMultiplier);
            SetFloatProperty(ai, "_aggroRange", cfg.AggroRange);
            SetFloatProperty(ai, "_attackRange", cfg.AttackRange);
            SetIntProperty(ai, "_attackDamage", cfg.AttackDamage);
            SetFloatProperty(ai, "_attackCooldown", cfg.AttackCooldown);
            SetFloatProperty(ai, "_attackWindup", cfg.AttackWindup);
            SetFloatProperty(ai, "_maxPatrolDistance", cfg.MaxPatrolDistance);
            SetObjectProperty(ai, "_animator", anim);

            if (!string.IsNullOrEmpty(cfg.ProjectileSpritePath))
            {
                SetSpriteArrayProperty(ai, "_projectileFrames", LoadAllSpritesSorted(cfg.ProjectileSpritePath));
                SetFloatProperty(ai, "_projectileSpeed", cfg.ProjectileSpeed);
                SetFloatProperty(ai, "_projectileLifetime", cfg.ProjectileLifetime);
            }

            DamageDealer dealer = enemy.AddComponent<DamageDealer>();
            SetIntProperty(dealer, "_damageAmount", cfg.AttackDamage);
            SetStringProperty(dealer, "_targetTag", "Player");
            SetFloatProperty(dealer, "_knockbackHorizontal", 4.5f);
            SetFloatProperty(dealer, "_knockbackVertical", 3.5f);

            return SaveTempAsPrefab(enemy, cfg.PrefabPath);
        }

        private static GameObject InstantiateEnemyOnSurface(GameObject prefab, Transform parent, float x, float surfaceTopY)
        {
            if (prefab == null) return null;

            float bottomOffset = 0.36f;
            if (prefab.TryGetComponent<BoxCollider2D>(out var col))
            {
                bottomOffset = -(col.offset.y - col.size.y * 0.5f) * prefab.transform.localScale.y;
            }

            return InstantiatePrefab(prefab, parent, new Vector3(x, surfaceTopY + bottomOffset + 0.02f, 0f));
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

            // Props trang trí dọc 5 khu vực Level_01
            CreateGroundProp("Prop_Z1_BackTreeA", bgSection, SpriteTreeBack1, -2.0f, -1.5f, donors, -14, 1f, false, backTreeTint);
            CreateGroundProp("Prop_Z1_BackTreeB", bgSection, SpriteTreeBack2, 9.5f, -1.5f, donors, -14, 1f, true, backTreeTint);
            CreateGroundProp("Prop_Z1_TreeA", bgSection, SpriteTree1, -4.6f, -1.5f, donors, -8);
            CreateGroundProp("Prop_Z1_Sign", bgSection, SpriteSignRight, -1.0f, -1.5f, donors, -6);
            CreateGroundProp("Prop_Z1_GrassA", bgSection, SpriteGrassTuft, 0.2f, -1.5f, donors, -5);
            CreateGroundProp("Prop_Z1_Rock", bgSection, SpriteRockSmall, 6.8f, -1.5f, donors, -6);
            CreateGroundProp("Prop_Z1_TreeB", bgSection, SpriteTree3, 14.5f, -1.5f, donors, -8, 1f, true);

            CreateGroundProp("Prop_Z2_BackTreeA", bgSection, SpriteTreeBack3, 24.5f, -1.5f, donors, -14, 1f, false, backTreeTint);
            CreateGroundProp("Prop_Z2_BackTreeB", bgSection, SpriteTreeBack4, 38.0f, -1.5f, donors, -14, 1f, true, backTreeTint);
            CreateGroundProp("Prop_Z2_GrassA", bgSection, SpriteGrass3, 29.0f, -1.5f, donors, -5);
            CreateGroundProp("Prop_Z2_Tree", bgSection, SpriteTree2, 33.2f, -1.5f, donors, -8);
            CreateGroundProp("Prop_Z2_RockBig", bgSection, SpriteRockBig, 44.0f, -1.5f, donors, -7);

            CreateGroundProp("Prop_Z3_BackTreeA", bgSection, SpriteTreeBack1, 58.5f, -1.5f, donors, -14, 1f, false, backTreeTint);
            CreateGroundProp("Prop_Z3_BackTreeB", bgSection, SpriteTreeBack2, 71.0f, -1.5f, donors, -14, 1f, true, backTreeTint);
            CreateGroundProp("Prop_Z3_TreeA", bgSection, SpriteTree4, 59.5f, -1.5f, donors, -8);
            CreateGroundProp("Prop_Z3_GrassA", bgSection, SpriteGrass5, 65.5f, -1.5f, donors, -5);
            CreateGroundProp("Prop_Z3_SignJump", bgSection, SpriteSignJumpUp, 73.2f, -1.5f, donors, -6);

            CreateGroundProp("Prop_Z4_BackTree", bgSection, SpriteTreeBack3, 86.5f, 1.5f, donors, -14, 1f, false, backTreeTint);
            CreateGroundProp("Prop_Z4_Grass", bgSection, SpriteGrass4, 82.0f, 1.5f, donors, -5);
            CreateGroundProp("Prop_Z4_Rock", bgSection, SpriteRockSmall2, 87.5f, 1.5f, donors, -6);

            CreateGroundProp("Prop_Z5_BackTreeA", bgSection, SpriteTreeBack4, 102.0f, 0.0f, donors, -14, 1f, false, backTreeTint);
            CreateGroundProp("Prop_Z5_BackTreeB", bgSection, SpriteTreeBack1, 113.5f, 0.0f, donors, -14, 1f, true, backTreeTint);
            CreateGroundProp("Prop_Z5_TreeA", bgSection, SpriteTree1, 99.5f, 0.0f, donors, -8);
            CreateGroundProp("Prop_Z5_RockBig", bgSection, SpriteRockBig2, 108.0f, 0.0f, donors, -7);
            CreateGroundProp("Prop_Z5_SignGoal", bgSection, SpriteSignRight, 114.2f, 0.0f, donors, -6);

            Transform playerSection = CreateSection("=== PLAYER ===");
            Vector3 startPos = new Vector3(-3.5f, -0.5f, 0f);
            GameObject spawnPoint = new GameObject("DefaultSpawnPoint");
            spawnPoint.transform.SetParent(playerSection, false);
            spawnPoint.transform.position = startPos;
            SetObjectProperty(cpManager, "_defaultSpawnPoint", spawnPoint.transform);
            InstantiatePrefab(donors.PlayerPrefab, playerSection, startPos);

            // KHU 1: Đồng Cỏ Khởi Đầu (-6 .. +18, topY = -1.5)
            Transform z1 = CreateSection("=== ZONE 1 - DONG CO KHOI DAU ===");
            CreateTiledGround("Ground_Zone1", z1, 6.0f, -1.5f, 24, 3, donors);
            CreateTiledStoneWall("Wall_Start", z1, -5.6f, -1.5f, 5, donors);
            InstantiatePrefab(p.RockPickup, z1, new Vector3(1.2f, -1.18f, 0f));
            InstantiatePrefab(p.Crate, z1, new Vector3(3.0f, -1.109f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z1, new Vector3(5.0f, 0.70f, 0f));
            InstantiatePrefab(p.Coin, z1, new Vector3(4.5f, 1.25f, 0f));
            InstantiatePrefab(p.Coin, z1, new Vector3(5.5f, 1.25f, 0f));
            // Bệ nhún Nấm Khổng Lồ bí mật Zone 1 -> nhảy lên đài cao lấy Bánh quy hồi Thể lực
            InstantiatePrefab(p.JumpPad, z1, new Vector3(7.5f, -1.5f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z1, new Vector3(7.5f, 2.50f, 0f));
            InstantiatePrefab(p.FoodCookies, z1, new Vector3(7.5f, 3.05f, 0f));
            InstantiateEnemyOnSurface(p.EnemyOoze, z1, 11.5f, -1.5f);
            InstantiatePrefab(p.Ladder, z1, new Vector3(16.0f, 0.50f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z1, new Vector3(16.0f, 2.35f, 0f));
            InstantiatePrefab(p.Coin, z1, new Vector3(15.3f, 2.90f, 0f));
            InstantiatePrefab(p.Coin, z1, new Vector3(16.7f, 2.90f, 0f));

            // KHU 2: Thung Lũng Suối Đá (20.5 .. 46.5, topY = -1.5)
            Transform z2 = CreateSection("=== ZONE 2 - THUNG LUNG SUOI DA ===");
            CreateTiledGround("Ground_Zone2", z2, 33.5f, -1.5f, 26, 3, donors);
            InstantiatePrefab(p.Checkpoint, z2, new Vector3(22.0f, -1.031f, 0f));
            InstantiatePrefab(p.Spike, z2, new Vector3(25.5f, -1.125f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z2, new Vector3(25.5f, 0.45f, 0f));
            InstantiatePrefab(p.Coin, z2, new Vector3(24.9f, 1.00f, 0f));
            InstantiatePrefab(p.Coin, z2, new Vector3(26.1f, 1.00f, 0f));
            CreateTiledStoneWall("Wall_Divider_Z2", z2, 27.5f, -1.5f, 1, donors);
            InstantiateEnemyOnSurface(p.EnemyRed, z2, 31.5f, -1.5f);
            // Thùng thuốc nổ chiến thuật cạnh EnemyRed để kích nổ diện rộng
            InstantiatePrefab(p.ExplosiveBarrel, z2, new Vector3(33.0f, -1.12f, 0f));
            InstantiatePrefab(p.Ladder, z2, new Vector3(35.5f, 0.50f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z2, new Vector3(35.5f, 2.35f, 0f));
            InstantiatePrefab(p.Coin, z2, new Vector3(34.6f, 2.90f, 0f));
            InstantiatePrefab(p.SilverKey, z2, new Vector3(35.5f, 2.95f, 0f));
            InstantiatePrefab(p.FoodCake, z2, new Vector3(36.4f, 2.95f, 0f));
            InstantiateEnemyOnSurface(p.EnemyTiny, z2, 41.5f, -1.5f);
            InstantiatePrefab(p.HealthPickup, z2, new Vector3(45.0f, -1.12f, 0f));

            // KHU 3: Đài Cổ Thụ & Pháp Sư (MovingPlatform 48.2..51.8 -> Ground 53.5..77.5, topY = -1.5)
            Transform z3 = CreateSection("=== ZONE 3 - DAI CO THU & PHAP SU ===");
            CreateMovingPlatformSystem(z3, new Vector3(48.2f, -1.75f, 0f), new Vector3(51.8f, -1.75f, 0f), donors);
            CreateTiledGround("Ground_Zone3", z3, 65.5f, -1.5f, 24, 3, donors);
            InstantiatePrefab(p.Checkpoint, z3, new Vector3(55.0f, -1.031f, 0f));
            InstantiatePrefab(p.SilverChest, z3, new Vector3(57.2f, -1.12f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z3, new Vector3(61.5f, 0.55f, 0f));
            InstantiatePrefab(p.Coin, z3, new Vector3(60.9f, 1.10f, 0f));
            InstantiatePrefab(p.Coin, z3, new Vector3(62.1f, 1.10f, 0f));
            InstantiateEnemyOnSurface(p.EnemyBear, z3, 62.5f, -1.5f);
            // Thùng nổ ở giữa đài cổ thụ
            InstantiatePrefab(p.ExplosiveBarrel, z3, new Vector3(66.0f, -1.12f, 0f));
            InstantiateEnemyOnSurface(p.EnemyMage, z3, 69.5f, -1.5f);
            InstantiatePrefab(p.OneWayPlatform, z3, new Vector3(69.5f, 1.60f, 0f));
            InstantiatePrefab(p.RockPickup, z3, new Vector3(68.9f, 2.08f, 0f));
            InstantiatePrefab(p.FoodMeat, z3, new Vector3(70.0f, 2.15f, 0f));
            InstantiatePrefab(p.JumpPad, z3, new Vector3(74.8f, -1.5f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z3, new Vector3(77.0f, 0.0f, 0f));

            // KHU 4: Đèo Gió Cao Nguyên (78.5 .. 128.5, topY = +1.5)
            Transform z4 = CreateSection("=== ZONE 4 - DEO GIO CAO NGUYEN ===");
            CreateTiledGround("Ground_Zone4", z4, 98.5f, 1.5f, 40, 3, donors);
            InstantiatePrefab(p.Checkpoint, z4, new Vector3(80.0f, 1.969f, 0f));
            InstantiatePrefab(p.FallingPlatform, z4, new Vector3(84.5f, 3.40f, 0f));
            InstantiatePrefab(p.Coin, z4, new Vector3(84.5f, 4.15f, 0f));
            InstantiatePrefab(p.FallingPlatform, z4, new Vector3(87.5f, 3.40f, 0f));
            InstantiatePrefab(p.FoodCookies, z4, new Vector3(87.5f, 4.15f, 0f));
            InstantiateEnemyOnSurface(p.EnemyOoze, z4, 85.5f, 1.5f);
            InstantiateEnemyOnSurface(p.EnemyTiny, z4, 92.0f, 1.5f);
            InstantiatePrefab(p.HealthPickup, z4, new Vector3(95.8f, 1.88f, 0f));
            InstantiatePrefab(p.RockPickup, z4, new Vector3(97.6f, 1.82f, 0f));
            CreateMovingPlatformSystem(z4, new Vector3(101.0f, 2.5f, 0f), new Vector3(106.0f, 2.5f, 0f), donors);
            InstantiateEnemyOnSurface(p.EnemyMage, z4, 112.0f, 1.5f);
            InstantiatePrefab(p.OneWayPlatform, z4, new Vector3(112.0f, 3.2f, 0f));
            InstantiatePrefab(p.FoodMeat, z4, new Vector3(112.0f, 3.8f, 0f));
            // Cầu nối bục sụt lún + bục di chuyển bắc qua hố 118.5 .. 131.0 sang Zone 5
            InstantiatePrefab(p.FallingPlatform, z4, new Vector3(121.5f, 1.4f, 0f));
            InstantiatePrefab(p.FallingPlatform, z4, new Vector3(125.0f, 1.0f, 0f));
            CreateMovingPlatformSystem(z4, new Vector3(127.5f, 0.5f, 0f), new Vector3(129.8f, 0.5f, 0f), donors);

            // KHU 5: Tàn Tích Đá Cổ (131.5 .. 164.5, topY = 0.0)
            Transform z5 = CreateSection("=== ZONE 5 - TAN TICH DA CO ===");
            CreateTiledGround("Ground_Zone5", z5, 148.0f, 0.0f, 34, 3, donors);
            InstantiatePrefab(p.Checkpoint, z5, new Vector3(133.0f, 0.469f, 0f));
            InstantiatePrefab(p.Spike, z5, new Vector3(138.0f, 0.375f, 0f));
            InstantiatePrefab(p.Spike, z5, new Vector3(141.0f, 0.375f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z5, new Vector3(139.5f, 1.8f, 0f));
            InstantiatePrefab(p.FoodCake, z5, new Vector3(139.5f, 2.4f, 0f));
            InstantiateEnemyOnSurface(p.EnemyTiny, z5, 146.0f, 0.0f);
            InstantiatePrefab(p.ExplosiveBarrel, z5, new Vector3(149.0f, 0.38f, 0f));
            InstantiateEnemyOnSurface(p.EnemyBear, z5, 154.0f, 0.0f);
            InstantiatePrefab(p.JumpPad, z5, new Vector3(161.0f, 0.0f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z5, new Vector3(161.0f, 3.0f, 0f));
            InstantiatePrefab(p.Coin, z5, new Vector3(161.0f, 3.6f, 0f));

            // KHU 6: Tiền Đồn Chúa Quỷ (167.5 .. 198.5, topY = -1.5)
            Transform z6 = CreateSection("=== ZONE 6 - TIEN DON CHUA QUY ===");
            CreateTiledGround("Ground_Zone6", z6, 183.0f, -1.5f, 32, 3, donors);
            InstantiatePrefab(p.Checkpoint, z6, new Vector3(169.5f, -1.031f, 0f));
            InstantiateEnemyOnSurface(p.EnemyYellow, z6, 175.0f, -1.5f);
            InstantiatePrefab(p.ExplosiveBarrel, z6, new Vector3(178.5f, -1.12f, 0f));
            InstantiatePrefab(p.OneWayPlatform, z6, new Vector3(182.0f, 0.8f, 0f));
            InstantiateEnemyOnSurface(p.EnemyMage, z6, 182.0f, 0.8f);
            InstantiatePrefab(p.SilverChest, z6, new Vector3(186.0f, -1.12f, 0f));
            InstantiateEnemyOnSurface(p.EnemyRed, z6, 191.0f, -1.5f);

            // KHU 7: Đấu Trường Chúa Quỷ Đồng Cỏ (Demon Boss Arena) (199 .. 245, topY = -1.5)
            // Mặt đất nối liền khít từ Zone 6, có Tiền Sảnh Checkpoint trang trọng ngay trước cửa đấu trường!
            Transform z7 = CreateSection("=== ZONE 7 - DAU TRUONG CHUA QUY (DEMON BOSS) ===");
            CreateBossArena(z7, 199.0f, -1.5f, 46f, p.BossDemon, p.VictoryGoalToL2, BiomeType.Field, donors, p);

            Transform hazardsSection = CreateSection("=== HAZARDS & BOUNDS ===");
            CreateKillZone("KillZone", hazardsSection, new Vector3(120f, -10f, 0f), new Vector2(280f, 3f));

            Transform uiRoot = CreateSection("=== UI ===");
            AttachUIAndEventSystem(uiRoot, UISetupTool.PrefabGameplayUIPath, "Gameplay_HUD_Root");

            EditorSceneManager.SaveScene(scene, SceneNames.Level01Path);
        }

        private static void CreateLevel02Scene(DonorAssets donors, EnvironmentPrefabs p)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Transform systems = CreateSection("=== SYSTEMS ===");
            InstantiatePrefab(donors.GameSystemsPrefab, systems, Vector3.zero);
            GameObject cpManagerObj = new GameObject("CheckpointManager");
            cpManagerObj.transform.SetParent(systems, false);
            CheckpointManager cpManager = cpManagerObj.AddComponent<CheckpointManager>();

            Transform camLight = CreateSection("=== CAMERA & LIGHT ===");
            InstantiatePrefab(donors.CameraRigPrefab, camLight, new Vector3(0f, 0f, -10f));
            GameObject lightObj = InstantiatePrefab(donors.GlobalLightPrefab, camLight, Vector3.zero);
            SetLightColor(lightObj, new Color(0.42f, 0.52f, 0.85f, 1f));

            Transform bgSection = CreateSection("=== BACKGROUND & PROPS ===");
            CreateCaveParallaxBackground(bgSection, donors);

            Transform playerSection = CreateSection("=== PLAYER ===");
            Vector3 startPos = new Vector3(-3.5f, -0.5f, 0f);
            GameObject spawnPoint = new GameObject("DefaultSpawnPoint");
            spawnPoint.transform.SetParent(playerSection, false);
            spawnPoint.transform.position = startPos;
            SetObjectProperty(cpManager, "_defaultSpawnPoint", spawnPoint.transform);
            InstantiatePrefab(donors.PlayerPrefab, playerSection, startPos);

            // KHU 1: Cửa Hang Ẩm Ướt (-6 .. +22, topY = -1.5)
            Transform z1 = CreateSection("=== ZONE 1 - CUA HANG AM UOT ===");
            CreateBiomeGround("Ground_Zone1", z1, 8.0f, -1.5f, 28, 3, BiomeType.Cave, donors);
            CreateBiomeWall("Wall_Start", z1, -5.6f, -1.5f, 5, BiomeType.Cave, donors);
            InstantiatePrefab(p.RockPickup, z1, new Vector3(1.2f, -1.18f, 0f));
            InstantiatePrefab(GetCrate(p, BiomeType.Cave), z1, new Vector3(3.5f, -1.109f, 0f));
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Cave), z1, new Vector3(6.0f, 0.70f, 0f));
            InstantiatePrefab(p.Coin, z1, new Vector3(5.5f, 1.25f, 0f));
            InstantiatePrefab(p.Coin, z1, new Vector3(6.5f, 1.25f, 0f));
            InstantiatePrefab(GetJumpPad(p, BiomeType.Cave), z1, new Vector3(8.5f, -1.5f, 0f));
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Cave), z1, new Vector3(8.5f, 2.50f, 0f));
            InstantiatePrefab(p.FoodCookies, z1, new Vector3(8.5f, 3.05f, 0f));
            InstantiateEnemyOnSurface(p.EnemyCaveSmallMushroom, z1, 13.5f, -1.5f);
            InstantiatePrefab(GetLadder(p, BiomeType.Cave), z1, new Vector3(18.0f, 0.50f, 0f));
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Cave), z1, new Vector3(18.0f, 2.35f, 0f));
            InstantiatePrefab(p.Coin, z1, new Vector3(17.3f, 2.90f, 0f));
            InstantiatePrefab(p.Coin, z1, new Vector3(18.7f, 2.90f, 0f));

            // KHU 2: Vực Thạch Nhũ & Bẫy Gai Đá (25 .. 56, topY = -1.5)
            Transform z2 = CreateSection("=== ZONE 2 - VUC THACH NHU & GAI DA ===");
            CreateBiomeGround("Ground_Zone2", z2, 40.5f, -1.5f, 30, 3, BiomeType.Cave, donors);
            InstantiatePrefab(p.Checkpoint, z2, new Vector3(27.0f, -1.031f, 0f));
            InstantiatePrefab(GetSpike(p, BiomeType.Cave), z2, new Vector3(30.5f, -1.125f, 0f));
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Cave), z2, new Vector3(30.5f, 0.45f, 0f));
            InstantiatePrefab(p.Coin, z2, new Vector3(30.5f, 1.00f, 0f));
            InstantiateEnemyOnSurface(p.EnemyCaveGoblinMelee, z2, 35.0f, -1.5f);
            InstantiatePrefab(p.ExplosiveBarrel, z2, new Vector3(37.0f, -1.12f, 0f));
            InstantiatePrefab(GetLadder(p, BiomeType.Cave), z2, new Vector3(40.5f, 0.50f, 0f));
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Cave), z2, new Vector3(40.5f, 2.35f, 0f));
            InstantiatePrefab(p.SilverKey, z2, new Vector3(40.5f, 2.95f, 0f));
            InstantiatePrefab(p.FoodCake, z2, new Vector3(41.4f, 2.95f, 0f));
            InstantiateEnemyOnSurface(p.EnemyCaveGoblinRange, z2, 47.0f, -1.5f);
            InstantiatePrefab(p.HealthPickup, z2, new Vector3(53.0f, -1.12f, 0f));

            // KHU 3: Rừng Nấm Dạ Quang & Gấu Đá (59 .. 92, topY = -1.5)
            Transform z3 = CreateSection("=== ZONE 3 - RUNG NAM DA QUANG & GAU DA ===");
            CreateMovingPlatformSystem(z3, new Vector3(56.5f, -1.75f, 0f), new Vector3(60.5f, -1.75f, 0f), donors, 3, BiomeType.Cave);
            CreateBiomeGround("Ground_Zone3", z3, 75.5f, -1.5f, 28, 3, BiomeType.Cave, donors);
            InstantiatePrefab(p.Checkpoint, z3, new Vector3(63.0f, -1.031f, 0f));
            InstantiatePrefab(p.SilverChest, z3, new Vector3(66.0f, -1.12f, 0f));
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Cave), z3, new Vector3(69.5f, 0.55f, 0f));
            InstantiateEnemyOnSurface(p.EnemyCaveBigMushroom, z3, 71.0f, -1.5f);
            InstantiatePrefab(p.ExplosiveBarrel, z3, new Vector3(74.5f, -1.12f, 0f));
            InstantiateEnemyOnSurface(p.EnemyCaveGoblinMage, z3, 78.0f, -1.5f);
            InstantiatePrefab(GetLadder(p, BiomeType.Cave), z3, new Vector3(83.0f, 0.50f, 0f));
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Cave), z3, new Vector3(83.0f, 2.20f, 0f));
            InstantiatePrefab(p.FoodMeat, z3, new Vector3(83.0f, 2.75f, 0f));
            InstantiatePrefab(GetJumpPad(p, BiomeType.Cave), z3, new Vector3(87.5f, -1.5f, 0f));
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Cave), z3, new Vector3(91.2f, 0.4f, 0f));

            // KHU 4: Mỏ Đá Bỏ Hoang & Sàn Sụp Lún (93.5 .. 119.5, topY = +1.5)
            Transform z4 = CreateSection("=== ZONE 4 - MO DA BO HOANG & SAN SUP LUN ===");
            CreateBiomeGround("Ground_Zone4", z4, 106.5f, 1.5f, 26, 3, BiomeType.Cave, donors);
            InstantiatePrefab(p.Checkpoint, z4, new Vector3(95.5f, 1.969f, 0f));
            InstantiateEnemyOnSurface(p.EnemyCaveBat, z4, 102.0f, 1.5f);
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Cave), z4, new Vector3(106.0f, 3.2f, 0f));
            InstantiatePrefab(p.Coin, z4, new Vector3(106.0f, 3.8f, 0f));
            InstantiateEnemyOnSurface(p.EnemyCaveGoblinRange, z4, 112.0f, 1.5f);
            InstantiatePrefab(p.HealthPickup, z4, new Vector3(116.5f, 1.88f, 0f));
            InstantiatePrefab(p.RockPickup, z4, new Vector3(117.8f, 1.82f, 0f));
            // Cầu bục sụt lún + bục di chuyển bắc qua vực sâu (119.5 .. 134.5) sang Zone 5
            InstantiatePrefab(GetFallingPlatform(p, BiomeType.Cave), z4, new Vector3(122.5f, 1.3f, 0f));
            InstantiatePrefab(p.Coin, z4, new Vector3(122.5f, 2.0f, 0f));
            InstantiatePrefab(GetFallingPlatform(p, BiomeType.Cave), z4, new Vector3(126.0f, 0.9f, 0f));
            InstantiatePrefab(p.FoodCookies, z4, new Vector3(126.0f, 1.6f, 0f));
            CreateMovingPlatformSystem(z4, new Vector3(129.0f, 0.5f, 0f), new Vector3(132.5f, 0.5f, 0f), donors, 3, BiomeType.Cave);

            // KHU 5: Hẻm Núi Bóng Tối & Pháp Sư (134.5 .. 160.5, topY = 0.0)
            Transform z5 = CreateSection("=== ZONE 5 - HEM NUI BONG TOI & PHAP SU ===");
            CreateBiomeGround("Ground_Zone5", z5, 147.5f, 0.0f, 26, 3, BiomeType.Cave, donors);
            InstantiatePrefab(p.Checkpoint, z5, new Vector3(135.0f, 0.469f, 0f));
            InstantiateEnemyOnSurface(p.EnemyCaveGoblinMelee, z5, 139.0f, 0.0f);
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Cave), z5, new Vector3(143.0f, 1.6f, 0f));
            InstantiatePrefab(p.FoodCake, z5, new Vector3(143.0f, 2.15f, 0f));
            InstantiateEnemyOnSurface(p.EnemyCaveGoblinMage, z5, 149.0f, 0.0f);
            InstantiatePrefab(p.ExplosiveBarrel, z5, new Vector3(153.0f, 0.38f, 0f));
            InstantiateEnemyOnSurface(p.EnemyCaveBat, z5, 158.0f, 0.0f);

            // KHU 6: Doanh Trại Goblin & Checkpoint Tiền Đồn (167 .. 199, topY = -1.5)
            Transform z6 = CreateSection("=== ZONE 6 - DOANH TRAI GOBLIN ===");
            CreateMovingPlatformSystem(z6, new Vector3(162.0f, -0.75f, 0f), new Vector3(165.5f, -0.75f, 0f), donors, 3, BiomeType.Cave);
            CreateBiomeGround("Ground_Zone6", z6, 183.0f, -1.5f, 32, 3, BiomeType.Cave, donors);
            CreateGroundProp("Cave_Rock_1", z6, "Assets/GameAssets/Cave-Tileset/Objects/Rocks/1.png", 195.0f, -1.5f, donors, -1);
            CreateGroundProp("Cave_Pointer_1", z6, "Assets/GameAssets/Cave-Tileset/Objects/Pointers/1.png", 168.0f, -1.5f, donors, -1);
            InstantiatePrefab(p.Checkpoint, z6, new Vector3(169.5f, -1.031f, 0f));
            InstantiateEnemyOnSurface(p.EnemyCaveBigMushroom, z6, 176.0f, -1.5f);
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Cave), z6, new Vector3(181.0f, 0.8f, 0f));
            InstantiateEnemyOnSurface(p.EnemyCaveGoblinMage, z6, 181.0f, 0.8f);
            InstantiatePrefab(p.SilverChest, z6, new Vector3(186.0f, -1.12f, 0f));
            InstantiateEnemyOnSurface(p.EnemyCaveGoblinMelee, z6, 191.0f, -1.5f);

            // KHU 7: Hoàng Cung Vua Goblin (Goblin King Boss Arena) (199 .. 245, topY = -1.5)
            // Mặt đất nối liền khít từ Zone 6, có Tiền Sảnh Checkpoint trang trọng ngay trước cửa đấu trường!
            Transform z7 = CreateSection("=== ZONE 7 - HOANG CUNG VUA GOBLIN (GOBLIN KING BOSS) ===");
            CreateBossArena(z7, 199.0f, -1.5f, 46f, p.BossGoblinKing, p.VictoryGoalToL3, BiomeType.Cave, donors, p);

            Transform hazardsSection = CreateSection("=== HAZARDS & BOUNDS ===");
            CreateKillZone("KillZone", hazardsSection, new Vector3(120f, -10f, 0f), new Vector2(280f, 3f));

            Transform uiRoot = CreateSection("=== UI ===");
            AttachUIAndEventSystem(uiRoot, UISetupTool.PrefabGameplayUIPath, "Gameplay_HUD_Root");

            EditorSceneManager.SaveScene(scene, SceneNames.Level02Path);
        }

        private static void CreateLevel03Scene(DonorAssets donors, EnvironmentPrefabs p)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Transform systems = CreateSection("=== SYSTEMS ===");
            InstantiatePrefab(donors.GameSystemsPrefab, systems, Vector3.zero);
            GameObject cpManagerObj = new GameObject("CheckpointManager");
            cpManagerObj.transform.SetParent(systems, false);
            CheckpointManager cpManager = cpManagerObj.AddComponent<CheckpointManager>();

            Transform camLight = CreateSection("=== CAMERA & LIGHT ===");
            InstantiatePrefab(donors.CameraRigPrefab, camLight, new Vector3(0f, 0f, -10f));
            GameObject lightObj = InstantiatePrefab(donors.GlobalLightPrefab, camLight, Vector3.zero);
            SetLightColor(lightObj, new Color(0.98f, 0.42f, 0.32f, 1f));

            Transform bgSection = CreateSection("=== BACKGROUND & PROPS ===");
            CreateHellParallaxBackground(bgSection, donors);

            Transform playerSection = CreateSection("=== PLAYER ===");
            Vector3 startPos = new Vector3(-3.5f, -0.5f, 0f);
            GameObject spawnPoint = new GameObject("DefaultSpawnPoint");
            spawnPoint.transform.SetParent(playerSection, false);
            spawnPoint.transform.position = startPos;
            SetObjectProperty(cpManager, "_defaultSpawnPoint", spawnPoint.transform);
            InstantiatePrefab(donors.PlayerPrefab, playerSection, startPos);

            // KHU 1: Bờ Vực Hắc Thạch (-6 .. +22, topY = -1.5)
            Transform z1 = CreateSection("=== ZONE 1 - BO VUC HAC THACH ===");
            CreateBiomeGround("Ground_Zone1", z1, 8.0f, -1.5f, 28, 3, BiomeType.Hell, donors);
            CreateBiomeWall("Wall_Start", z1, -5.6f, -1.5f, 5, BiomeType.Hell, donors);
            InstantiatePrefab(p.RockPickup, z1, new Vector3(1.2f, -1.18f, 0f));
            InstantiatePrefab(GetCrate(p, BiomeType.Hell), z1, new Vector3(3.5f, -1.109f, 0f));
            InstantiateEnemyOnSurface(p.EnemyHellSkeleton, z1, 6.0f, -1.5f);
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Hell), z1, new Vector3(10.0f, 0.70f, 0f));
            InstantiatePrefab(p.Coin, z1, new Vector3(10.0f, 1.25f, 0f));
            InstantiatePrefab(GetJumpPad(p, BiomeType.Hell), z1, new Vector3(14.0f, -1.5f, 0f));
            InstantiateEnemyOnSurface(p.EnemyHellZombie, z1, 18.0f, -1.5f);

            // KHU 2: Cầu Đá Đứt Gãy Trên Dung Nham (25 .. 56, topY = -1.5)
            Transform z2 = CreateSection("=== ZONE 2 - CAU DA DUT GAY DUNG NHAM ===");
            CreateBiomeGround("Ground_Zone2", z2, 40.5f, -1.5f, 30, 3, BiomeType.Hell, donors);
            InstantiatePrefab(p.Checkpoint, z2, new Vector3(27.0f, -1.031f, 0f));
            InstantiatePrefab(GetSpike(p, BiomeType.Hell), z2, new Vector3(31.0f, -1.125f, 0f));
            InstantiatePrefab(GetSpike(p, BiomeType.Hell), z2, new Vector3(34.0f, -1.125f, 0f));
            InstantiateEnemyOnSurface(p.EnemyHellHellhound, z2, 38.0f, -1.5f);
            InstantiatePrefab(p.ExplosiveBarrel, z2, new Vector3(41.0f, -1.12f, 0f));
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Hell), z2, new Vector3(45.0f, 2.0f, 0f));
            InstantiatePrefab(p.SilverKey, z2, new Vector3(45.0f, 2.6f, 0f));
            InstantiatePrefab(p.FoodCookies, z2, new Vector3(46.0f, 2.6f, 0f));
            InstantiateEnemyOnSurface(p.EnemyHellDarkGoblin, z2, 50.0f, -1.5f);
            InstantiatePrefab(p.HealthPickup, z2, new Vector3(54.0f, -1.12f, 0f));

            // KHU 3: Vách Đá Quỷ Dữ & Thùng Nổ (59 .. 92, topY = -1.5)
            Transform z3 = CreateSection("=== ZONE 3 - VACH DA QUY DU & THUNG NO ===");
            CreateMovingPlatformSystem(z3, new Vector3(56.5f, -1.75f, 0f), new Vector3(60.5f, -1.75f, 0f), donors, 3, BiomeType.Hell);
            CreateBiomeGround("Ground_Zone3", z3, 75.5f, -1.5f, 28, 3, BiomeType.Hell, donors);
            InstantiatePrefab(p.Checkpoint, z3, new Vector3(63.0f, -1.031f, 0f));
            InstantiatePrefab(p.SilverChest, z3, new Vector3(66.0f, -1.12f, 0f));
            InstantiateEnemyOnSurface(p.EnemyHellHusky, z3, 72.0f, -1.5f);
            InstantiatePrefab(p.ExplosiveBarrel, z3, new Vector3(75.0f, -1.12f, 0f));
            InstantiatePrefab(p.ExplosiveBarrel, z3, new Vector3(77.5f, -1.12f, 0f));
            InstantiateEnemyOnSurface(p.EnemyHellBeholder, z3, 81.0f, -1.5f);
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Hell), z3, new Vector3(86.0f, 2.0f, 0f));
            InstantiatePrefab(p.FoodMeat, z3, new Vector3(86.0f, 2.6f, 0f));
            InstantiatePrefab(GetJumpPad(p, BiomeType.Hell), z3, new Vector3(87.5f, -1.5f, 0f));
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Hell), z3, new Vector3(91.2f, 0.4f, 0f));

            // KHU 4: Cầu Treo Dung Nham & Sàn Sụp Lún (93.5 .. 119.5, topY = +1.5)
            Transform z4 = CreateSection("=== ZONE 4 - CAU TREO DUNG NHAM & SAN SUP LUN ===");
            CreateBiomeGround("Ground_Zone4", z4, 106.5f, 1.5f, 26, 3, BiomeType.Hell, donors);
            InstantiatePrefab(p.Checkpoint, z4, new Vector3(95.5f, 1.969f, 0f));
            InstantiateEnemyOnSurface(p.EnemyHellZombie, z4, 103.0f, 1.5f);
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Hell), z4, new Vector3(109.0f, 3.2f, 0f));
            InstantiateEnemyOnSurface(p.EnemyHellBeholder, z4, 109.0f, 3.2f);
            InstantiatePrefab(p.HealthPickup, z4, new Vector3(116.5f, 1.88f, 0f));
            InstantiatePrefab(p.FoodMeat, z4, new Vector3(117.8f, 1.88f, 0f));
            // Cầu bục sụt lún + bục di chuyển bắc qua biển dung nham (119.5 .. 134.5) sang Zone 5
            InstantiatePrefab(GetFallingPlatform(p, BiomeType.Hell), z4, new Vector3(122.5f, 1.3f, 0f));
            InstantiatePrefab(p.Coin, z4, new Vector3(122.5f, 2.0f, 0f));
            InstantiatePrefab(GetFallingPlatform(p, BiomeType.Hell), z4, new Vector3(126.0f, 0.9f, 0f));
            InstantiatePrefab(p.FoodCookies, z4, new Vector3(126.0f, 1.6f, 0f));
            CreateMovingPlatformSystem(z4, new Vector3(129.0f, 0.5f, 0f), new Vector3(132.5f, 0.5f, 0f), donors, 3, BiomeType.Hell);

            // KHU 5: Lò Luyện Linh Hồn (134.5 .. 160.5, topY = 0.0)
            Transform z5 = CreateSection("=== ZONE 5 - LO LUYEN LINH HON ===");
            CreateBiomeGround("Ground_Zone5", z5, 147.5f, 0.0f, 26, 3, BiomeType.Hell, donors);
            InstantiatePrefab(p.Checkpoint, z5, new Vector3(135.0f, 0.469f, 0f));
            InstantiateEnemyOnSurface(p.EnemyHellSkeleton, z5, 138.0f, 0.0f);
            InstantiatePrefab(p.ExplosiveBarrel, z5, new Vector3(143.0f, 0.38f, 0f));
            InstantiateEnemyOnSurface(p.EnemyHellHusky, z5, 148.0f, 0.0f);
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Hell), z5, new Vector3(154.0f, 1.8f, 0f));
            InstantiatePrefab(p.FoodCake, z5, new Vector3(154.0f, 2.35f, 0f));
            InstantiateEnemyOnSurface(p.EnemyHellHellhound, z5, 159.0f, 0.0f);

            // KHU 6: Tiền Đồn Hắc Ám & Checkpoint Tiền Đồn (167 .. 199, topY = -1.5)
            Transform z6 = CreateSection("=== ZONE 6 - TIEN DON HAC AM ===");
            CreateMovingPlatformSystem(z6, new Vector3(162.0f, -0.75f, 0f), new Vector3(165.5f, -0.75f, 0f), donors, 3, BiomeType.Hell);
            CreateBiomeGround("Ground_Zone6", z6, 183.0f, -1.5f, 32, 3, BiomeType.Hell, donors);
            CreateGroundProp("Hell_Rock_1", z6, "Assets/GameAssets/Hell-Tileset/Objects/Rocks/1.png", 195.0f, -1.5f, donors, -1);
            CreateGroundProp("Hell_Grass_1", z6, "Assets/GameAssets/Hell-Tileset/Objects/Grass/1.png", 172.0f, -1.5f, donors, -1);
            CreateGroundProp("Hell_Pointer_1", z6, "Assets/GameAssets/Hell-Tileset/Objects/Pointers/1.png", 168.0f, -1.5f, donors, -1);
            CreateGroundProp("Hell_Grass_2", z6, "Assets/GameAssets/Hell-Tileset/Objects/Grass/2.png", 195.0f, -1.5f, donors, -1);
            InstantiatePrefab(p.Checkpoint, z6, new Vector3(169.5f, -1.031f, 0f));
            InstantiateEnemyOnSurface(p.EnemyHellDarkGoblin, z6, 176.0f, -1.5f);
            InstantiatePrefab(GetOneWayPlatform(p, BiomeType.Hell), z6, new Vector3(181.0f, 0.8f, 0f));
            InstantiateEnemyOnSurface(p.EnemyHellBeholder, z6, 181.0f, 0.8f);
            InstantiatePrefab(p.SilverChest, z6, new Vector3(186.0f, -1.12f, 0f));
            InstantiateEnemyOnSurface(p.EnemyHellHusky, z6, 191.0f, -1.5f);

            // KHU 7: Tế Đàn Pháp Sư Chiêu Hồn (Necromancer Boss Arena) (199 .. 245, topY = -1.5)
            // Mặt đất nối liền khít từ Zone 6, có Tiền Sảnh Checkpoint trang trọng ngay trước cửa đấu trường!
            Transform z7 = CreateSection("=== ZONE 7 - TE DAN PHAP SU CHIEU HON (NECROMANCER BOSS) ===");
            CreateBossArena(z7, 199.0f, -1.5f, 46f, p.BossNecromancer, p.VictoryGoalClear, BiomeType.Hell, donors, p);

            Transform hazardsSection = CreateSection("=== HAZARDS & BOUNDS ===");
            CreateKillZone("Lava_KillZone", hazardsSection, new Vector3(120f, -9f, 0f), new Vector2(280f, 3f));

            Transform uiRoot = CreateSection("=== UI ===");
            AttachUIAndEventSystem(uiRoot, UISetupTool.PrefabGameplayUIPath, "Gameplay_HUD_Root");

            EditorSceneManager.SaveScene(scene, SceneNames.Level03Path);
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
            // Zone 4 props (Floor_Zone4: 52 .. 84)
            CreateGroundProp("Prop_Z4_BackTreeA", bgSection, SpriteTreeBack1, 57.5f, -1.5f, donors, -14, 1f, false, backTreeTint);
            CreateGroundProp("Prop_Z4_BackTreeB", bgSection, SpriteTreeBack3, 66.2f, -1.5f, donors, -14, 1f, true, backTreeTint);
            CreateGroundProp("Prop_Z4_BackTreeC", bgSection, SpriteTreeBack4, 75.5f, -1.5f, donors, -14, 1f, false, backTreeTint);
            CreateGroundProp("Prop_Z4_TreeA", bgSection, SpriteTree2, 53.8f, -1.5f, donors, -8);
            CreateGroundProp("Prop_Z4_GrassA", bgSection, SpriteGrass2, 55.3f, -1.5f, donors, -5);
            CreateGroundProp("Prop_Z4_RockBig", bgSection, SpriteRockBig2, 61.8f, -1.5f, donors, -7);
            CreateGroundProp("Prop_Z4_Rock", bgSection, SpriteRockSmall, 63.8f, -1.5f, donors, -6);
            CreateGroundProp("Prop_Z4_GrassB", bgSection, SpriteGrass4, 71.2f, -1.5f, donors, -5, 1f, true);
            CreateGroundProp("Prop_Z4_TreeB", bgSection, SpriteTree4, 77.5f, -1.5f, donors, -8, 1f, true);
            CreateGroundProp("Prop_Z4_SignGoal", bgSection, SpriteSignRight, 80.2f, -1.5f, donors, -6);

            Transform playerSection = CreateSection("=== PLAYER ===");
            Vector3 playerStartPos = new Vector3(0f, -0.5f, 0f);
            GameObject spawnPoint = new GameObject("DefaultSpawnPoint");
            spawnPoint.transform.SetParent(playerSection, false);
            spawnPoint.transform.position = playerStartPos;
            SetObjectProperty(cpManager, "_defaultSpawnPoint", spawnPoint.transform);
            InstantiatePrefab(donors.PlayerPrefab, playerSection, playerStartPos);

            // ZONE 1: Chạy, 2x Cầu thang (Ladder + One-Way), Nhặt đá & 2x Thùng gỗ (Đẩy kê chân / Đập vỡ)
            Transform z1 = CreateSection("=== ZONE 1 - LOCOMOTION, LADDER, CRATE ===");
            CreateTiledGround("Floor_Zone1", z1, -1f, -1.5f, 18, 3, donors);
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
            CreateTiledGround("Floor_Zone2", z2, 15.0f, -1.5f, 14, 3, donors);
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
            CreateTiledGround("Floor_JumpPadBase", z3, 26.5f, -1.5f, 9, 3, donors);
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

            // ZONE 4: Checkpoint #2, 2x Bẫy gai & Hồi máu, Vách ngăn gai, Đủ 6 loại Quái & Điểm thắng
            Transform z4 = CreateSection("=== ZONE 4 - CHECKPOINT, HAZARDS, ITEMS & COMBAT ===");
            CreateTiledGround("Floor_Zone4", z4, 68.0f, -1.5f, 32, 3, donors);
            InstantiatePrefab(p.Checkpoint, z4, new Vector3(53.0f, -1.031f, 0f));
            InstantiatePrefab(p.RockPickup, z4, new Vector3(54.2f, -1.18f, 0f));
            InstantiatePrefab(p.Coin, z4, new Vector3(54.9f, -0.95f, 0f));
            InstantiatePrefab(p.Coin, z4, new Vector3(55.7f, -0.95f, 0f));
            // 2x Bẫy gai + 2x Hồi máu
            InstantiatePrefab(p.Spike, z4, new Vector3(56.8f, -1.125f, 0f));
            InstantiatePrefab(p.HealthPickup, z4, new Vector3(58.3f, -1.12f, 0f));
            InstantiatePrefab(p.Spike, z4, new Vector3(59.8f, -1.125f, 0f));
            InstantiatePrefab(p.HealthPickup, z4, new Vector3(61.1f, -1.12f, 0f));
            // Trụ đá thấp ngăn cách khu bẫy gai khỏi khu vực chiến đấu của Quái
            CreateTiledStoneWall("Wall_HazardDivider", z4, 62.2f, -1.5f, 1, donors);
            // Khu chiến đấu với đủ 6 chủng loại Quái & Chìa khóa Vàng trên bệ cao
            InstantiatePrefab(p.RockPickup, z4, new Vector3(63.4f, -1.18f, 0f));
            InstantiateEnemyOnSurface(p.EnemyOoze, z4, 64.8f, -1.5f);
            InstantiateEnemyOnSurface(p.EnemyRed, z4, 67.4f, -1.5f);
            InstantiatePrefab(p.OneWayPlatform, z4, new Vector3(70.0f, 1.25f, 0f));
            InstantiatePrefab(p.Coin, z4, new Vector3(69.1f, 1.80f, 0f));
            InstantiatePrefab(p.GoldenKey, z4, new Vector3(70.0f, 1.85f, 0f));
            InstantiatePrefab(p.Coin, z4, new Vector3(70.9f, 1.80f, 0f));
            InstantiateEnemyOnSurface(p.EnemyTiny, z4, 70.2f, -1.5f);
            InstantiateEnemyOnSurface(p.EnemyMage, z4, 73.4f, -1.5f);
            InstantiateEnemyOnSurface(p.EnemyBear, z4, 76.2f, -1.5f);
            InstantiateEnemyOnSurface(p.EnemyYellow, z4, 79.2f, -1.5f);
            // Điểm chiến thắng cuối màn (Rương vàng)
            InstantiatePrefab(p.VictoryGoal, z4, new Vector3(81.8f, -1.12f, 0f));
            CreateTiledStoneWall("Wall_ArenaEnd", z4, 83.6f, -1.5f, 5, donors);

            // HAZARDS & BOUNDS
            Transform hazards = CreateSection("=== HAZARDS & BOUNDS ===");
            CreateKillZone("KillZone", hazards, new Vector3(38f, -8f, 0f), new Vector2(180f, 2f));

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
                new EditorBuildSettingsScene(SceneNames.Level02Path, true),
                new EditorBuildSettingsScene(SceneNames.Level03Path, true),
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
            string prefix = "Tile",
            bool isStatic = false)
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
                if (isStatic) tileObj.isStatic = true;

                SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                if (donors.SpriteMaterial != null) sr.sharedMaterial = donors.SpriteMaterial;
                sr.sortingOrder = sortingOrder;
                sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                sr.receiveShadows = false;

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

        private static void CreateBiomeGround(
            string name,
            Transform parent,
            float centerX,
            float surfaceTopY,
            int tileCols,
            int tileRows,
            BiomeType biome,
            DonorAssets donors)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.isStatic = true;

            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(root.transform, false);
            visuals.isStatic = true;

            string topLPath = SpriteFieldTopLeft, topMPath = SpriteFieldTopMid, topRPath = SpriteFieldTopRight;
            string fillLPath = SpriteFieldFillLeft, fillMPath = SpriteFieldFillMid, fillRPath = SpriteFieldFillRight;
            string botLPath = SpriteFieldBotLeft, botMPath = SpriteFieldBotMid, botRPath = SpriteFieldBotRight;
            string ceilLPath = SpriteFieldCeilingLeft, ceilMPath = SpriteFieldCeilingMid, ceilRPath = SpriteFieldCeilingRight;

            if (biome == BiomeType.Cave)
            {
                topLPath = SpriteCaveTopLeft; topMPath = SpriteCaveTopMid; topRPath = SpriteCaveTopRight;
                fillLPath = SpriteCaveFillLeft; fillMPath = SpriteCaveFillMid; fillRPath = SpriteCaveFillRight;
                botLPath = SpriteCaveBotLeft; botMPath = SpriteCaveBotMid; botRPath = SpriteCaveBotRight;
                ceilLPath = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_28.png";
                ceilMPath = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_29.png";
                ceilRPath = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_30.png";
            }
            else if (biome == BiomeType.Hell)
            {
                topLPath = SpriteHellTopLeft; topMPath = SpriteHellTopMid; topRPath = SpriteHellTopRight;
                fillLPath = SpriteHellFillLeft; fillMPath = SpriteHellFillMid; fillRPath = SpriteHellFillRight;
                botLPath = SpriteHellBotLeft; botMPath = SpriteHellBotMid; botRPath = SpriteHellBotRight;
                ceilLPath = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_28.png";
                ceilMPath = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_29.png";
                ceilRPath = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_30.png";
            }

            float width = tileCols;
            if (tileRows <= 1)
            {
                const float height = 0.94f;
                root.transform.position = new Vector3(centerX, surfaceTopY - height * 0.5f, 0f);
                width = BuildHorizontalTileStrip(
                    visuals.transform,
                    tileCols,
                    LoadFirstSprite(ceilLPath),
                    LoadFirstSprite(ceilMPath),
                    LoadFirstSprite(ceilRPath),
                    height * 0.5f,
                    donors,
                    0,
                    "Row0_Tile",
                    isStatic: true);

                BoxCollider2D col = root.AddComponent<BoxCollider2D>();
                col.size = new Vector2(width > 0f ? width : tileCols, height);
                if (donors.FrictionlessMaterial != null) col.sharedMaterial = donors.FrictionlessMaterial;
                return;
            }

            float totalHeight = tileRows;
            root.transform.position = new Vector3(centerX, surfaceTopY - totalHeight * 0.5f, 0f);

            Sprite topL = LoadFirstSprite(topLPath);
            Sprite topM = LoadFirstSprite(topMPath);
            Sprite topR = LoadFirstSprite(topRPath);
            Sprite fillL = LoadFirstSprite(fillLPath);
            Sprite fillM = LoadFirstSprite(fillMPath);
            Sprite fillR = LoadFirstSprite(fillRPath);
            Sprite botL = LoadFirstSprite(botLPath);
            Sprite botM = LoadFirstSprite(botMPath);
            Sprite botR = LoadFirstSprite(botRPath);

            for (int r = 0; r < tileRows; r++)
            {
                float rowTopY = totalHeight * 0.5f - r;
                float rowWidth;
                if (r == 0)
                {
                    rowWidth = BuildHorizontalTileStrip(visuals.transform, tileCols, topL, topM, topR, rowTopY, donors, 0, $"Row{r}_Tile", isStatic: true);
                }
                else if (r == tileRows - 1)
                {
                    rowWidth = BuildHorizontalTileStrip(visuals.transform, tileCols, botL, botM, botR, rowTopY, donors, 0, $"Row{r}_Tile", isStatic: true);
                }
                else
                {
                    rowWidth = BuildHorizontalTileStrip(visuals.transform, tileCols, fillL, fillM, fillR, rowTopY, donors, 0, $"Row{r}_Tile", isStatic: true);
                }

                if (r == 0 && rowWidth > 0f)
                {
                    width = rowWidth;
                }
            }

            BoxCollider2D groundCol = root.AddComponent<BoxCollider2D>();
            groundCol.size = new Vector2(width, totalHeight);
            if (donors.FrictionlessMaterial != null) groundCol.sharedMaterial = donors.FrictionlessMaterial;
        }

        private static void CreateBiomeWall(
            string name,
            Transform parent,
            float centerX,
            float bottomY,
            int tileRows,
            BiomeType biome,
            DonorAssets donors)
        {
            if (tileRows <= 0) return;

            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.isStatic = true;
            float totalHeight = tileRows;
            root.transform.position = new Vector3(centerX, bottomY + totalHeight * 0.5f, 0f);

            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(root.transform, false);
            visuals.isStatic = true;

            string topPath = SpriteFieldWallTop, midPath = SpriteFieldWallMid, botPath = SpriteFieldWallBot;
            if (biome == BiomeType.Cave)
            {
                topPath = SpriteCaveWallTop; midPath = SpriteCaveWallMid; botPath = SpriteCaveWallBot;
            }
            else if (biome == BiomeType.Hell)
            {
                topPath = SpriteHellWallTop; midPath = SpriteHellWallMid; botPath = SpriteHellWallBot;
            }

            Sprite topSp = LoadFirstSprite(topPath);
            Sprite midSp = LoadFirstSprite(midPath);
            Sprite botSp = LoadFirstSprite(botPath);

            for (int r = 0; r < tileRows; r++)
            {
                Sprite sp = r == 0 ? topSp : (r == tileRows - 1 ? botSp : midSp);
                if (sp == null) sp = midSp ?? donors.SquareSprite;

                GameObject tileObj = new GameObject($"WallTile_{r:00}");
                tileObj.transform.SetParent(visuals.transform, false);
                tileObj.isStatic = true;

                SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                if (donors.SpriteMaterial != null) sr.sharedMaterial = donors.SpriteMaterial;
                sr.sortingOrder = 0;
                sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                sr.receiveShadows = false;

                float topY = totalHeight * 0.5f - r;
                float localY = sp != null ? topY - sp.bounds.max.y : topY - 0.5f;
                tileObj.transform.localPosition = new Vector3(0f, localY, 0f);
            }

            BoxCollider2D col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.88f, totalHeight);
            if (donors.FrictionlessMaterial != null) col.sharedMaterial = donors.FrictionlessMaterial;
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
            CreateBiomeGround(name, parent, centerX, surfaceTopY, tileCols, tileRows, BiomeType.Field, donors);
        }

        private static void CreateTiledStoneWall(
            string name,
            Transform parent,
            float centerX,
            float bottomY,
            int tileRows,
            DonorAssets donors)
        {
            CreateBiomeWall(name, parent, centerX, bottomY, tileRows, BiomeType.Field, donors);
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

        private static void CreateCaveParallaxBackground(Transform parent, DonorAssets donors)
        {
            GameObject camObj = GameObject.FindWithTag("MainCamera");
            if (camObj != null && camObj.TryGetComponent<Camera>(out var mainCam))
            {
                mainCam.backgroundColor = new Color(0.05f, 0.05f, 0.10f, 1f);
            }

            GameObject root = new GameObject("CaveParallax_Root");
            root.transform.SetParent(parent, false);

            (string file, int sortOrder, float fx)[] layers =
            {
                ("6.png", -80, 0.06f),
                ("5.png", -70, 0.16f),
                ("4.png", -60, 0.30f),
                ("3.png", -50, 0.48f),
                ("2.png", -40, 0.66f),
                ("1.png", -35, 0.82f)
            };

            const float scale = ParallaxLayer.DefaultPanelScale;
            const float worldBottomY = ParallaxLayer.DefaultWorldBottomY;
            const float unifiedFy = ParallaxLayer.UnifiedVerticalParallaxFactor;

            for (int i = 0; i < layers.Length; i++)
            {
                var cfg = layers[i];
                string path = "Assets/GameAssets/Cave-Tileset/Background/Layers/" + cfg.file;
                Sprite sp = LoadFirstSprite(path);
                if (sp == null) continue;

                float panelWidth = Mathf.Max(1f, sp.bounds.size.x * scale - 0.02f);
                float layerCenterY = worldBottomY - sp.bounds.min.y * scale;
                GameObject layerObj = new GameObject($"Parallax_Cave_Layer_{i + 1}_{cfg.file.Replace(".png", "")}");
                layerObj.transform.SetParent(root.transform, false);
                layerObj.transform.position = new Vector3(0f, layerCenterY, 0f);

                ParallaxLayer parallax = layerObj.AddComponent<ParallaxLayer>();
                parallax.Configure(cfg.fx, unifiedFy, panelWidth, 0f, true, worldBottomY);

                for (int copy = -1; copy <= 1; copy++)
                {
                    GameObject panel = CreateSpriteObject($"Panel_{copy + 1}", path, donors, Color.white, cfg.sortOrder);
                    panel.transform.SetParent(layerObj.transform, false);
                    panel.transform.localPosition = new Vector3(copy * panelWidth, 0f, 0f);
                    panel.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }
        }

        private static void CreateHellParallaxBackground(Transform parent, DonorAssets donors)
        {
            GameObject camObj = GameObject.FindWithTag("MainCamera");
            if (camObj != null && camObj.TryGetComponent<Camera>(out var mainCam))
            {
                mainCam.backgroundColor = new Color(0.12f, 0.02f, 0.02f, 1f);
            }

            GameObject root = new GameObject("HellParallax_Root");
            root.transform.SetParent(parent, false);

            (string file, int sortOrder, float fx)[] layers =
            {
                ("7.png", -90, 0.04f),
                ("6.png", -80, 0.12f),
                ("5.png", -70, 0.22f),
                ("4.png", -60, 0.36f),
                ("3.png", -50, 0.52f),
                ("2.png", -40, 0.68f),
                ("1.png", -35, 0.84f)
            };

            const float scale = ParallaxLayer.DefaultPanelScale;
            const float worldBottomY = ParallaxLayer.DefaultWorldBottomY;
            const float unifiedFy = ParallaxLayer.UnifiedVerticalParallaxFactor;

            for (int i = 0; i < layers.Length; i++)
            {
                var cfg = layers[i];
                string path = "Assets/GameAssets/Hell-Tileset/Background/Layers/" + cfg.file;
                Sprite sp = LoadFirstSprite(path);
                if (sp == null) continue;

                float panelWidth = Mathf.Max(1f, sp.bounds.size.x * scale - 0.02f);
                float layerCenterY = worldBottomY - sp.bounds.min.y * scale;
                GameObject layerObj = new GameObject($"Parallax_Hell_Layer_{i + 1}_{cfg.file.Replace(".png", "")}");
                layerObj.transform.SetParent(root.transform, false);
                layerObj.transform.position = new Vector3(0f, layerCenterY, 0f);

                ParallaxLayer parallax = layerObj.AddComponent<ParallaxLayer>();
                parallax.Configure(cfg.fx, unifiedFy, panelWidth, 0f, true, worldBottomY);

                for (int copy = -1; copy <= 1; copy++)
                {
                    GameObject panel = CreateSpriteObject($"Panel_{copy + 1}", path, donors, Color.white, cfg.sortOrder);
                    panel.transform.SetParent(layerObj.transform, false);
                    panel.transform.localPosition = new Vector3(copy * panelWidth, 0f, 0f);
                    panel.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }
        }

        private static void CreateBossArena(
            Transform parent,
            float startX,
            float surfaceTopY,
            float arenaWidth,
            GameObject bossPrefab,
            GameObject victoryGoalPrefab,
            BiomeType biome,
            DonorAssets donors,
            EnvironmentPrefabs p)
        {
            GameObject arenaRoot = new GameObject("BossArena");
            arenaRoot.transform.SetParent(parent, false);

            float arenaCenterX = startX + arenaWidth * 0.5f;

            // 1. Mặt đất đấu trường (rộng hơn để bao trùm cả tiền sảnh checkpoint và hậu trường victory)
            CreateBiomeGround("Arena_Ground", arenaRoot.transform, arenaCenterX, surfaceTopY, (int)arenaWidth + 8, 4, biome, donors);

            // 2. Tiền sảnh Checkpoint Cửa Phòng Boss: đặt ngay trước cửa vào để người chơi respawn tại đây
            Vector3 doorstepCpPos = new Vector3(startX + 2.5f, surfaceTopY + 0.469f, 0f);
            InstantiatePrefab(p.Checkpoint, arenaRoot.transform, doorstepCpPos);
            InstantiatePrefab(p.HealthPickup, arenaRoot.transform, new Vector3(startX + 4.2f, surfaceTopY + 0.38f, 0f));
            InstantiatePrefab(p.FoodMeat, arenaRoot.transform, new Vector3(startX + 5.2f, surfaceTopY + 0.38f, 0f));

            // 3. Vách đá chặn cửa vào (Entry Barrier) - Đặt tại startX + 7.5m (sau tiền sảnh), ban đầu TẮT (SetActive=false)
            float entryBarrierX = startX + 7.5f;
            GameObject entryBarrier = new GameObject("EntryBarrier");
            entryBarrier.transform.SetParent(arenaRoot.transform, false);
            entryBarrier.transform.position = new Vector3(entryBarrierX, surfaceTopY, 0f);
            CreateBiomeWall("BarrierWall_Entry", entryBarrier.transform, entryBarrierX, surfaceTopY, 8, biome, donors);
            entryBarrier.SetActive(false);

            // 4. Vách đá chặn lối ra (Exit Barrier) - ban đầu BẬT (SetActive=true), khi Boss chết sẽ mở
            float exitBarrierX = startX + arenaWidth - 3.5f;
            GameObject exitBarrier = new GameObject("ExitBarrier");
            exitBarrier.transform.SetParent(arenaRoot.transform, false);
            exitBarrier.transform.position = new Vector3(exitBarrierX, surfaceTopY, 0f);
            CreateBiomeWall("BarrierWall_Exit", exitBarrier.transform, exitBarrierX, surfaceTopY, 8, biome, donors);
            exitBarrier.SetActive(true);

            // 5. Vách tường bao cuối cùng phía sau Victory Goal
            CreateBiomeWall("Wall_Arena_End", arenaRoot.transform, startX + arenaWidth + 5.0f, surfaceTopY, 8, biome, donors);

            // 6. Boss Instance - đặt ở nửa phải sàn đấu
            float bossSpawnX = startX + 28.0f;
            Vector3 bossSpawnPos = new Vector3(bossSpawnX, surfaceTopY + 0.8f, 0f);
            GameObject bossInstance = InstantiatePrefab(bossPrefab, arenaRoot.transform, bossSpawnPos);
            BaseBoss bossScript = bossInstance != null ? bossInstance.GetComponent<BaseBoss>() : null;

            // 7. BossArenaTrigger - ĐẶT SÂU TRONG PHÒNG ĐẤU (cách cửa vào 9m tại startX + 16.5m)
            // Người chơi bắt buộc phải bước hẳn vào giữa phòng thì cửa sau lưng mới đóng sập!
            float triggerX = startX + 16.5f;
            GameObject triggerObj = new GameObject("BossArenaTrigger");
            triggerObj.transform.SetParent(arenaRoot.transform, false);
            triggerObj.transform.position = new Vector3(triggerX, surfaceTopY + 2.5f, 0f);
            BoxCollider2D trigCol = triggerObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(2.5f, 6.0f);

            BossArenaTrigger arenaTrigger = triggerObj.AddComponent<BossArenaTrigger>();
            SetObjectProperty(arenaTrigger, "_boss", bossScript);
            SetObjectProperty(arenaTrigger, "_entryBarrier", entryBarrier);
            SetObjectProperty(arenaTrigger, "_exitBarrier", exitBarrier);
            SetObjectProperty(arenaTrigger, "_victoryRewardPrefab", p.GoldenKey);
            SetVector3Property(arenaTrigger, "_rewardSpawnOffset", new Vector3(0f, 1.5f, 0f));
            SetVector3Property(arenaTrigger, "_doorstepCheckpointPos", doorstepCpPos);
            SetBoolProperty(arenaTrigger, "_autoRegisterDoorstepCheckpoint", true);
            SetBoolProperty(arenaTrigger, "_clampCamera", true);
            SetFloatProperty(arenaTrigger, "_arenaMinX", startX + 9f);
            SetFloatProperty(arenaTrigger, "_arenaMaxX", startX + arenaWidth - 7f);

            // 8. Bệ chiến thuật né đòn trong đấu trường (dùng bệ đúng Biome!)
            GameObject oneWayArena = GetOneWayPlatform(p, biome);
            InstantiatePrefab(oneWayArena, arenaRoot.transform, new Vector3(startX + 13.5f, surfaceTopY + 2.4f, 0f));
            InstantiatePrefab(oneWayArena, arenaRoot.transform, new Vector3(startX + 21.0f, surfaceTopY + 4.0f, 0f));
            InstantiatePrefab(oneWayArena, arenaRoot.transform, new Vector3(startX + 28.5f, surfaceTopY + 2.4f, 0f));

            // 9. Rương Vàng Chiến Thắng (đặt sau lối ra)
            Vector3 victoryPos = new Vector3(startX + arenaWidth + 1.2f, surfaceTopY + 0.38f, 0f);
            InstantiatePrefab(victoryGoalPrefab != null ? victoryGoalPrefab : p.VictoryGoal, arenaRoot.transform, victoryPos);
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

        private static void CreateMovingPlatformSystem(Transform parent, Vector3 startPos, Vector3 endPos, DonorAssets donors, int tileCount = 3, BiomeType biome = BiomeType.Field)
        {
            GameObject root = new GameObject("MovingPlatform_System");
            root.transform.SetParent(parent, false);
            root.transform.position = startPos;

            GameObject platform = new GameObject("Platform_Moving");
            platform.transform.SetParent(root.transform, false);
            platform.transform.position = startPos;

            string moveLeft = SpriteFieldMoveLeft;
            string moveMid = SpriteFieldMoveMid;
            string moveRight = SpriteFieldMoveRight;

            if (biome == BiomeType.Cave)
            {
                moveLeft = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_28.png";
                moveMid = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_29.png";
                moveRight = "Assets/GameAssets/Cave-Tileset/Tiles/Tileset/Tileset_30.png";
            }
            else if (biome == BiomeType.Hell)
            {
                moveLeft = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_28.png";
                moveMid = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_29.png";
                moveRight = "Assets/GameAssets/Hell-Tileset/Tiles/Tileset/TileSet_30.png";
            }

            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(platform.transform, false);
            float width = BuildHorizontalTileStrip(
                visuals.transform,
                tileCount,
                LoadFirstSprite(moveLeft),
                LoadFirstSprite(moveMid),
                LoadFirstSprite(moveRight),
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

        private static void SetVector3Property(Object target, string propertyName, Vector3 value) =>
            EditSerializedProperty(target, propertyName, p => p.vector3Value = value);

        private static void SetLightColor(GameObject lightObj, Color color)
        {
            if (lightObj == null) return;
            Component comp = lightObj.GetComponent("Light2D");
            if (comp != null)
            {
                SerializedObject so = new SerializedObject(comp);
                SerializedProperty prop = so.FindProperty("m_Color");
                if (prop != null)
                {
                    prop.colorValue = color;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

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
