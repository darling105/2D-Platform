#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Platformer2D.Core.Architecture;
using Platformer2D.UI;

namespace Platformer2D.EditorTools
{
    /// <summary>
    /// Công cụ 1-click dựng toàn bộ Hệ thống UI Pixel Art cho dự án:
    /// - Tạo 2 Prefab chuẩn vào Assets/Prefabs/UI/ (MainMenuCanvas, GameplayUICanvas).
    /// - Dựng giao diện đồng bộ CraftPix (Window.png, Bars, Icons, Logo, Portraits, Planes_ValMore.ttf).
    /// - Tự động gắn UI Prefab + EventSystem (InputSystemUIInputModule) vào MainMenu, Level_01 và MechanicsTestScene.
    /// </summary>
    public static class UISetupTool
    {
        public const string PrefabMainMenuUIPath = "Assets/Prefabs/UI/MainMenuCanvas.prefab";
        public const string PrefabGameplayUIPath = "Assets/Prefabs/UI/GameplayUICanvas.prefab";

        private const string FontPath = "Assets/GameAssets/UI/Font/Planes_ValMore.ttf";
        private const string LogoPath = "Assets/GameAssets/UI/5 Logo/Logo.png";
        private const string WindowSheetPath = "Assets/GameAssets/UI/Window.png";
        private const string PortraitPinkPath = "Assets/GameAssets/Portrait-Icon/1 Main characters/Portraits_01.png";
        private const string CoinIconPath = "Assets/GameAssets/Field-Tileset/Objects_Animated/Coin.png";
        private const string RockIconPath = "Assets/GameAssets/Player/1 Pink_Monster/Rock2.png";

        private const string SkillIconFireballPath = "Assets/GameAssets/Player-Skills/Icons/10-Fire-ball.png";
        private const string SkillIconLightningPath = "Assets/GameAssets/Player-Skills/Icons/1-Lightning.png";
        private const string SkillIconShieldPath = "Assets/GameAssets/Player-Skills/Icons/8-Shield.png";
        private const string SkillIconSpikesPath = "Assets/GameAssets/Player-Skills/Icons/6-Spikes.png";

        private const string HealthEmptyPath = "Assets/GameAssets/UI/2 Bars/Healthbar_empty.png";
        private const string HealthFullPath = "Assets/GameAssets/UI/2 Bars/Healthbar_full.png";
        private const string BandOffPath = "Assets/GameAssets/UI/2 Bars/Band2_off.png";
        private const string BandOnPath = "Assets/GameAssets/UI/2 Bars/Band2_on.png";
        private const string SliderHandlePath = "Assets/GameAssets/UI/2 Bars/Slider.png";
        private const string SquareBarPath = "Assets/GameAssets/UI/2 Bars/Square.png";

        private const string IconHomePath = "Assets/GameAssets/UI/3 Icons/Icons_01.png";
        private const string IconPausePath = "Assets/GameAssets/UI/3 Icons/Icons_09.png";
        private const string IconTrophyPath = "Assets/GameAssets/UI/3 Icons/Icons_10.png";
        private const string IconGamepadPath = "Assets/GameAssets/UI/3 Icons/Icons_17.png";
        private const string IconPlayPath = "Assets/GameAssets/UI/3 Icons/Icons_25.png";
        private const string IconSpeakerPath = "Assets/GameAssets/UI/3 Icons/Icons_31.png";
        private const string IconClosePath = "Assets/GameAssets/UI/3 Icons/Icons_37.png";
        private const string IconSettingsPath = "Assets/GameAssets/UI/3 Icons/Icons_39.png";
        private const string IconStarPath = "Assets/GameAssets/UI/3 Icons/Icons_40.png";
        private const string IconRestartPath = "Assets/GameAssets/UI/3 Icons/Icons_45.png";
        private const string IconExitPath = "Assets/GameAssets/UI/3 Icons/Icons_50.png";
        private const string IconHeartPath = "Assets/GameAssets/UI/3 Icons/Icons_57.png";
        private const string IconSkullPath = "Assets/GameAssets/UI/3 Icons/Icons_58.png";

        private static readonly Color ColOutlineDark = new Color(0.071f, 0.082f, 0.129f, 1f);     // #121521
        private static readonly Color ColPanelDark = new Color(0.102f, 0.122f, 0.200f, 0.96f);    // #1A1F33
        private static readonly Color ColPanelBorder = new Color(0.329f, 0.376f, 0.502f, 1f);     // #546080
        private static readonly Color ColTextWhite = new Color(0.961f, 0.969f, 1f, 1f);           // #F5F7FF
        private static readonly Color ColTextMuted = new Color(0.720f, 0.765f, 0.880f, 1f);
        private static readonly Color ColGold = new Color(1f, 0.831f, 0.278f, 1f);                // #FFD447
        private static readonly Color ColGreen = new Color(0.420f, 0.910f, 0.486f, 1f);           // #6BE87C
        private static readonly Color ColRed = new Color(1f, 0.340f, 0.435f, 1f);                 // #FF576F
        private static readonly Color ColCyan = new Color(0.420f, 0.820f, 1f, 1f);                // #6BD1FF

        [MenuItem("Tools/Platformer UI/1. Setup Hệ thống UI Pixel Art (Prefabs & 3 Scenes)")]
        public static void SetupCompleteUISystem()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/UI");
            EnsureCrispSkillAndUIAssets();

            Font pixelFont = LoadPixelFont();
            GameObject mainMenuPrefab = BuildMainMenuPrefab(pixelFont);
            GameObject gameplayHudPrefab = BuildGameplayHUDPrefab(pixelFont);

            int updatedScenes = 0;
            if (File.Exists(SceneNames.MainMenuPath))
            {
                InjectUIIntoScene(SceneNames.MainMenuPath, mainMenuPrefab, "MainMenu_Canvas_Root");
                updatedScenes++;
            }
            if (File.Exists(SceneNames.Level01Path))
            {
                InjectUIIntoScene(SceneNames.Level01Path, gameplayHudPrefab, "Gameplay_HUD_Root");
                updatedScenes++;
            }
            if (File.Exists(SceneNames.MechanicsTestPath))
            {
                InjectUIIntoScene(SceneNames.MechanicsTestPath, gameplayHudPrefab, "TestHUD_Canvas_Root");
                updatedScenes++;
            }

            if (File.Exists(SceneNames.MainMenuPath))
            {
                EditorSceneManager.OpenScene(SceneNames.MainMenuPath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Hoàn tất Setup UI Pixel Art!",
                $"Đã nâng cấp UI chuẩn Full HD (1920x1080) & làm nét ảnh Pixel tại Assets/Prefabs/UI/:\n" +
                $"• MainMenuCanvas.prefab\n" +
                $"• GameplayUICanvas.prefab\n\n" +
                $"Đã tích hợp UI & EventSystem (Input System) vào {updatedScenes} Scene.\n" +
                $"Hiện đang mở sẵn MainMenu.unity — bạn có thể bấm Play ngay để test!",
                "Tuyệt vời");
        }

        /// <summary>
        /// Dựng lại 2 Prefab UI (không mở dialog hay chuyển scene) để SceneArchitectureTool tự gọi đồng bộ.
        /// </summary>
        public static void BuildUIPrefabsOnly()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/UI");
            EnsureCrispSkillAndUIAssets();

            Font pixelFont = LoadPixelFont();
            BuildMainMenuPrefab(pixelFont);
            BuildGameplayHUDPrefab(pixelFont);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Dựng lại 2 Prefab UI và cập nhật vào cả 3 Scenes mà không mở Dialog chặn luồng.
        /// </summary>
        public static void RebuildPrefabsAndScenesHeadless()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/UI");
            EnsureCrispSkillAndUIAssets();

            Font pixelFont = LoadPixelFont();
            GameObject mainMenuPrefab = BuildMainMenuPrefab(pixelFont);
            GameObject gameplayHudPrefab = BuildGameplayHUDPrefab(pixelFont);

            if (File.Exists(SceneNames.MainMenuPath))
            {
                InjectUIIntoScene(SceneNames.MainMenuPath, mainMenuPrefab, "MainMenu_Canvas_Root");
            }
            if (File.Exists(SceneNames.Level01Path))
            {
                InjectUIIntoScene(SceneNames.Level01Path, gameplayHudPrefab, "Gameplay_HUD_Root");
            }
            if (File.Exists(SceneNames.MechanicsTestPath))
            {
                InjectUIIntoScene(SceneNames.MechanicsTestPath, gameplayHudPrefab, "TestHUD_Canvas_Root");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Ép chuẩn Point Filter (no filter), Uncompressed & tắt Mipmap cho toàn bộ ảnh Skill/UI/Player,
        /// đồng thời chuyển Font pixel sang HintedRaster để không bị mờ hay nhòe cạnh ở màn Full HD.
        /// </summary>
        public static void EnsureCrispSkillAndUIAssets()
        {
            string[] targetFolders =
            {
                "Assets/GameAssets/Player-Skills",
                "Assets/GameAssets/UI",
                "Assets/GameAssets/Player"
            };

            try
            {
                AssetDatabase.StartAssetEditing();

                string[] texGuids = AssetDatabase.FindAssets("t:Texture2D", targetFolders);
                for (int i = 0; i < texGuids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(texGuids[i]);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    bool changed = false;
                    if (importer.filterMode != FilterMode.Point)
                    {
                        importer.filterMode = FilterMode.Point;
                        changed = true;
                    }
                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        changed = true;
                    }
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        changed = true;
                    }

                    TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
                    if (standalone != null && (standalone.overridden || standalone.textureCompression != TextureImporterCompression.Uncompressed))
                    {
                        standalone.overridden = false;
                        standalone.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.SetPlatformTextureSettings(standalone);
                        changed = true;
                    }

                    if (changed)
                    {
                        importer.SaveAndReimport();
                    }
                }

                TrueTypeFontImporter fontImporter = AssetImporter.GetAtPath(FontPath) as TrueTypeFontImporter;
                if (fontImporter != null && fontImporter.fontRenderingMode != FontRenderingMode.HintedRaster)
                {
                    fontImporter.fontRenderingMode = FontRenderingMode.HintedRaster;
                    fontImporter.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Font pixelFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (pixelFont != null && pixelFont.material != null && pixelFont.material.mainTexture != null)
            {
                pixelFont.material.mainTexture.filterMode = FilterMode.Point;
            }
        }

        public static void EnsureEventSystemInActiveScene()
        {
            EventSystem[] existing = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            EventSystem target = existing.Length > 0 ? existing[0] : null;

            for (int i = 1; i < existing.Length; i++)
            {
                if (existing[i] != null)
                {
                    Object.DestroyImmediate(existing[i].gameObject);
                }
            }

            if (target == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                target = esObj.AddComponent<EventSystem>();
            }

            StandaloneInputModule legacyModule = target.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                Object.DestroyImmediate(legacyModule);
            }

            if (target.GetComponent<InputSystemUIInputModule>() == null)
            {
                target.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static GameObject BuildMainMenuPrefab(Font font)
        {
            GameObject canvasRoot = CreateCanvasRoot("MainMenuCanvas", 10);
            MainMenuUI menuUI = canvasRoot.AddComponent<MainMenuUI>();

            // 1. Logo banner phía trên
            Sprite logoSprite = LoadFirstSprite(LogoPath);
            GameObject logoObj = new GameObject("GameLogo");
            logoObj.transform.SetParent(canvasRoot.transform, false);
            Image logoImg = logoObj.AddComponent<Image>();
            logoImg.sprite = logoSprite;
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;
            RectTransform logoRt = logoObj.GetComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0.5f, 1f);
            logoRt.anchorMax = new Vector2(0.5f, 1f);
            logoRt.pivot = new Vector2(0.5f, 0.5f);
            logoRt.sizeDelta = new Vector2(675f, 306f);
            logoRt.anchoredPosition = new Vector2(0f, -168f);

            // Chữ tên game đặt vừa khung bảng gỗ trong Logo.png (anchorY 0.094..0.500)
            Text titleText = CreatePixelText(logoObj.transform, "GameTitle", "MONSTER QUEST", 44, ColGold, font, TextAnchor.MiddleCenter);
            SetAnchorBounds(titleText.rectTransform, new Vector2(0.18f, 0.23f), new Vector2(0.82f, 0.46f));

            Text subtitleText = CreatePixelText(logoObj.transform, "GameSubtitle", "2D PIXEL PLATFORMER", 20, ColTextMuted, font, TextAnchor.MiddleCenter);
            SetAnchorBounds(subtitleText.rectTransform, new Vector2(0.18f, 0.11f), new Vector2(0.82f, 0.25f));

            // 2. Bảng MENU chính (Window_2)
            RectTransform menuWindowRt = CreateWindowFrame(
                canvasRoot.transform,
                "MainPanel",
                "Window_2",
                "MENU",
                new Vector2(624f, 588f),
                new Vector2(39f, -102f),
                0.436f,
                0.932f,
                font,
                false,
                out _,
                out RectTransform menuContent);

            VerticalLayoutGroup menuLayout = menuContent.gameObject.AddComponent<VerticalLayoutGroup>();
            menuLayout.spacing = 18f;
            menuLayout.childAlignment = TextAnchor.MiddleCenter;
            menuLayout.childControlWidth = false;
            menuLayout.childControlHeight = false;
            menuLayout.childForceExpandWidth = false;
            menuLayout.childForceExpandHeight = false;

            Button playBtn = CreateActionButton(menuContent, "PlayButton", "PLAY", LoadFirstSprite(IconPlayPath), ColGreen, font, new Vector2(366f, 78f));
            Button testBtn = CreateActionButton(menuContent, "TestLabButton", "TEST LAB", LoadFirstSprite(IconGamepadPath), ColCyan, font, new Vector2(366f, 78f));
            Button settingsBtn = CreateActionButton(menuContent, "SettingsButton", "SETTINGS", LoadFirstSprite(IconSettingsPath), ColTextWhite, font, new Vector2(366f, 78f));
            Button quitBtn = CreateActionButton(menuContent, "QuitButton", "QUIT", LoadFirstSprite(IconExitPath), ColRed, font, new Vector2(366f, 78f));

            // 3. Thanh gợi ý phím điều khiển dưới cùng
            RectTransform hintBar = CreateBorderedBox(canvasRoot.transform, "ControlsHintBar", new Vector2(1380f, 52f), new Color(0.06f, 0.075f, 0.125f, 0.82f), new Color(0.28f, 0.33f, 0.45f, 0.9f));
            hintBar.anchorMin = new Vector2(0.5f, 0f);
            hintBar.anchorMax = new Vector2(0.5f, 0f);
            hintBar.pivot = new Vector2(0.5f, 0f);
            hintBar.anchoredPosition = new Vector2(0f, 26f);
            Text hintText = CreatePixelText(hintBar, "HintText", "A/D: MOVE  |  SPACE: JUMP  |  J: ATTACK  |  U: THROW  |  SHIFT: DASH  |  1-4: SKILLS  |  TAB: UPGRADE", 17, ColTextMuted, font, TextAnchor.MiddleCenter);
            StretchFull(hintText.rectTransform);

            // 4. Bảng Settings (mặc định ẩn)
            SettingsPanelUI settingsPanel = CreateSettingsPanel(canvasRoot.transform, font);
            settingsPanel.gameObject.SetActive(false);

            // Wire MainMenuUI
            SerializedObject so = new SerializedObject(menuUI);
            SetObject(so, "_mainPanel", menuWindowRt.gameObject);
            SetObject(so, "_settingsPanel", settingsPanel);
            SetObject(so, "_playButton", playBtn);
            SetObject(so, "_testLabButton", testBtn);
            SetObject(so, "_settingsButton", settingsBtn);
            SetObject(so, "_quitButton", quitBtn);
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveTempPrefab(canvasRoot, PrefabMainMenuUIPath);
        }

        private static GameObject BuildGameplayHUDPrefab(Font font)
        {
            GameObject canvasRoot = CreateCanvasRoot("GameplayUICanvas", 20);
            PlayerHUD hud = canvasRoot.AddComponent<PlayerHUD>();
            SkillTreePanelUI skillTreeUI = canvasRoot.AddComponent<SkillTreePanelUI>();

            // === A. TOP-LEFT: Chân dung Pink Monster & Thanh máu Pixel Art ===
            GameObject topLeft = new GameObject("TopLeft_Status");
            topLeft.transform.SetParent(canvasRoot.transform, false);
            RectTransform topLeftRt = topLeft.AddComponent<RectTransform>();
            topLeftRt.anchorMin = new Vector2(0f, 1f);
            topLeftRt.anchorMax = new Vector2(0f, 1f);
            topLeftRt.pivot = new Vector2(0f, 1f);
            topLeftRt.sizeDelta = new Vector2(540f, 126f);
            topLeftRt.anchoredPosition = new Vector2(32f, -26f);

            RectTransform portraitFrame = CreateBorderedBox(topLeft.transform, "PortraitFrame", new Vector2(114f, 114f), ColPanelDark, ColPanelBorder);
            portraitFrame.anchorMin = new Vector2(0f, 0.5f);
            portraitFrame.anchorMax = new Vector2(0f, 0.5f);
            portraitFrame.pivot = new Vector2(0f, 0.5f);
            portraitFrame.anchoredPosition = Vector2.zero;

            GameObject portraitImgObj = new GameObject("PortraitSprite");
            portraitImgObj.transform.SetParent(portraitFrame, false);
            Image portraitImg = portraitImgObj.AddComponent<Image>();
            portraitImg.sprite = LoadFirstSprite(PortraitPinkPath);
            portraitImg.preserveAspect = true;
            portraitImg.raycastTarget = false;
            RectTransform portraitImgRt = portraitImgObj.GetComponent<RectTransform>();
            portraitImgRt.anchorMin = new Vector2(0.5f, 0.5f);
            portraitImgRt.anchorMax = new Vector2(0.5f, 0.5f);
            portraitImgRt.sizeDelta = new Vector2(96f, 96f);

            // Nhãn tên + Tim + Số HP
            Text nameLabel = CreatePixelText(topLeft.transform, "CharacterName", "PINK MONSTER", 20, ColTextWhite, font, TextAnchor.MiddleLeft);
            nameLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            nameLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
            nameLabel.rectTransform.pivot = new Vector2(0f, 1f);
            nameLabel.rectTransform.sizeDelta = new Vector2(248f, 30f);
            nameLabel.rectTransform.anchoredPosition = new Vector2(135f, -10f);

            Image heartIcon = CreateIconImage(topLeft.transform, "HeartIcon", LoadFirstSprite(IconHeartPath), ColRed, new Vector2(24f, 24f));
            heartIcon.rectTransform.anchorMin = new Vector2(0f, 1f);
            heartIcon.rectTransform.anchorMax = new Vector2(0f, 1f);
            heartIcon.rectTransform.pivot = new Vector2(0f, 1f);
            heartIcon.rectTransform.anchoredPosition = new Vector2(393f, -14f);

            Text hpText = CreatePixelText(topLeft.transform, "HealthText", "3/3", 22, ColTextWhite, font, TextAnchor.MiddleLeft);
            hpText.rectTransform.anchorMin = new Vector2(0f, 1f);
            hpText.rectTransform.anchorMax = new Vector2(0f, 1f);
            hpText.rectTransform.pivot = new Vector2(0f, 1f);
            hpText.rectTransform.sizeDelta = new Vector2(96f, 30f);
            hpText.rectTransform.anchoredPosition = new Vector2(424f, -11f);

            // 1. Thanh máu (HP Bar) 360x32
            GameObject healthBarBgObj = new GameObject("HealthBar_Bg");
            healthBarBgObj.transform.SetParent(topLeft.transform, false);
            Image healthBgImg = healthBarBgObj.AddComponent<Image>();
            healthBgImg.sprite = LoadFirstSprite(HealthEmptyPath);
            healthBgImg.raycastTarget = false;
            RectTransform hbBgRt = healthBarBgObj.GetComponent<RectTransform>();
            hbBgRt.anchorMin = new Vector2(0f, 0f);
            hbBgRt.anchorMax = new Vector2(0f, 0f);
            hbBgRt.pivot = new Vector2(0f, 0f);
            hbBgRt.sizeDelta = new Vector2(360f, 32f);
            hbBgRt.anchoredPosition = new Vector2(135f, 66f);

            GameObject healthFillObj = new GameObject("HealthBar_Fill");
            healthFillObj.transform.SetParent(healthBarBgObj.transform, false);
            Image healthFillImg = healthFillObj.AddComponent<Image>();
            healthFillImg.sprite = LoadFirstSprite(HealthFullPath);
            healthFillImg.type = Image.Type.Filled;
            healthFillImg.fillMethod = Image.FillMethod.Horizontal;
            healthFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthFillImg.fillAmount = 1f;
            healthFillImg.raycastTarget = false;
            StretchFull(healthFillImg.rectTransform);

            // 2. Thanh Thể Lực (Stamina Bar) 360x22 (Màu xanh Emerald)
            GameObject staminaBarBgObj = new GameObject("StaminaBar_Bg");
            staminaBarBgObj.transform.SetParent(topLeft.transform, false);
            Image staminaBgImg = staminaBarBgObj.AddComponent<Image>();
            staminaBgImg.sprite = LoadFirstSprite(HealthEmptyPath);
            staminaBgImg.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);
            staminaBgImg.raycastTarget = false;
            RectTransform stamBgRt = staminaBarBgObj.GetComponent<RectTransform>();
            stamBgRt.anchorMin = new Vector2(0f, 0f);
            stamBgRt.anchorMax = new Vector2(0f, 0f);
            stamBgRt.pivot = new Vector2(0f, 0f);
            stamBgRt.sizeDelta = new Vector2(360f, 24f);
            stamBgRt.anchoredPosition = new Vector2(135f, 38f);

            GameObject staminaFillObj = new GameObject("StaminaBar_Fill");
            staminaFillObj.transform.SetParent(staminaBarBgObj.transform, false);
            Image staminaFillImg = staminaFillObj.AddComponent<Image>();
            staminaFillImg.sprite = LoadFirstSprite(SquareBarPath);
            staminaFillImg.color = ColGreen;
            staminaFillImg.type = Image.Type.Filled;
            staminaFillImg.fillMethod = Image.FillMethod.Horizontal;
            staminaFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            staminaFillImg.fillAmount = 1f;
            staminaFillImg.raycastTarget = false;
            StretchFull(staminaFillImg.rectTransform);

            Text staminaText = CreatePixelText(staminaBarBgObj.transform, "StaminaText", "100/100", 14, ColTextWhite, font, TextAnchor.MiddleCenter);
            StretchFull(staminaText.rectTransform);

            // 3. Thanh Kinh Nghiệm (EXP Bar) 360x18 (Màu Cyan)
            GameObject expBarBgObj = new GameObject("ExpBar_Bg");
            expBarBgObj.transform.SetParent(topLeft.transform, false);
            Image expBgImg = expBarBgObj.AddComponent<Image>();
            expBgImg.sprite = LoadFirstSprite(HealthEmptyPath);
            expBgImg.color = new Color(0.14f, 0.16f, 0.24f, 0.95f);
            expBgImg.raycastTarget = false;
            RectTransform expBgRt = expBarBgObj.GetComponent<RectTransform>();
            expBgRt.anchorMin = new Vector2(0f, 0f);
            expBgRt.anchorMax = new Vector2(0f, 0f);
            expBgRt.pivot = new Vector2(0f, 0f);
            expBgRt.sizeDelta = new Vector2(360f, 18f);
            expBgRt.anchoredPosition = new Vector2(135f, 16f);

            GameObject expFillObj = new GameObject("ExpBar_Fill");
            expFillObj.transform.SetParent(expBarBgObj.transform, false);
            Image expFillImg = expFillObj.AddComponent<Image>();
            expFillImg.sprite = LoadFirstSprite(SquareBarPath);
            expFillImg.color = ColCyan;
            expFillImg.type = Image.Type.Filled;
            expFillImg.fillMethod = Image.FillMethod.Horizontal;
            expFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            expFillImg.fillAmount = 0f;
            expFillImg.raycastTarget = false;
            StretchFull(expFillImg.rectTransform);

            Text expText = CreatePixelText(expBarBgObj.transform, "ExpText", "EXP 0/100", 13, ColTextWhite, font, TextAnchor.MiddleCenter);
            StretchFull(expText.rectTransform);

            // 4. Huy hiệu Cấp độ (Level Badge) đè lên góc dưới chân dung
            RectTransform lvBadgeRt = CreateBorderedBox(portraitFrame, "LevelBadge", new Vector2(76f, 26f), ColPanelDark, ColGold);
            lvBadgeRt.anchorMin = new Vector2(0.5f, 0f);
            lvBadgeRt.anchorMax = new Vector2(0.5f, 0f);
            lvBadgeRt.pivot = new Vector2(0.5f, 0.5f);
            lvBadgeRt.anchoredPosition = new Vector2(0f, 0f);
            Text lvBadgeTxt = CreatePixelText(lvBadgeRt, "LevelBadgeText", "Lv.1", 16, ColGold, font, TextAnchor.MiddleCenter);
            StretchFull(lvBadgeTxt.rectTransform);

            // === B. TOP-RIGHT: Rock Ammo, Coins Counter, Skills & Pause Button ===
            GameObject topRight = new GameObject("TopRight_Controls");
            topRight.transform.SetParent(canvasRoot.transform, false);
            RectTransform topRightRt = topRight.AddComponent<RectTransform>();
            topRightRt.anchorMin = new Vector2(1f, 1f);
            topRightRt.anchorMax = new Vector2(1f, 1f);
            topRightRt.pivot = new Vector2(1f, 1f);
            topRightRt.sizeDelta = new Vector2(690f, 78f);
            topRightRt.anchoredPosition = new Vector2(-32f, -28f);

            HorizontalLayoutGroup topRightRow = topRight.AddComponent<HorizontalLayoutGroup>();
            topRightRow.spacing = 12f;
            topRightRow.childAlignment = TextAnchor.MiddleRight;
            topRightRow.childControlWidth = false;
            topRightRow.childControlHeight = false;
            topRightRow.childForceExpandWidth = false;
            topRightRow.childForceExpandHeight = false;

            RectTransform rockPill = CreateBorderedBox(topRight.transform, "RockPill", new Vector2(156f, 72f), ColPanelDark, ColPanelBorder);
            Image rockIcon = CreateIconImage(rockPill, "RockIcon", LoadFirstSprite(RockIconPath), Color.white, new Vector2(40f, 40f));
            rockIcon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rockIcon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rockIcon.rectTransform.pivot = new Vector2(0f, 0.5f);
            rockIcon.rectTransform.anchoredPosition = new Vector2(15f, 0f);

            Text rockText = CreatePixelText(rockPill, "RockText", "0", 30, ColCyan, font, TextAnchor.MiddleRight);
            rockText.rectTransform.anchorMin = Vector2.zero;
            rockText.rectTransform.anchorMax = Vector2.one;
            rockText.rectTransform.offsetMin = new Vector2(58f, 3f);
            rockText.rectTransform.offsetMax = new Vector2(-18f, -3f);

            RectTransform coinPill = CreateBorderedBox(topRight.transform, "CoinPill", new Vector2(192f, 72f), ColPanelDark, ColPanelBorder);
            Image coinIcon = CreateIconImage(coinPill, "CoinIcon", LoadFirstSprite(CoinIconPath), Color.white, new Vector2(42f, 42f));
            coinIcon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            coinIcon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            coinIcon.rectTransform.pivot = new Vector2(0f, 0.5f);
            coinIcon.rectTransform.anchoredPosition = new Vector2(15f, 0f);

            Text scoreText = CreatePixelText(coinPill, "ScoreText", "0", 30, ColGold, font, TextAnchor.MiddleRight);
            scoreText.rectTransform.anchorMin = Vector2.zero;
            scoreText.rectTransform.anchorMax = Vector2.one;
            scoreText.rectTransform.offsetMin = new Vector2(60f, 3f);
            scoreText.rectTransform.offsetMax = new Vector2(-18f, -3f);

            Button skillsBtn = CreateActionButton(topRight.transform, "SkillsButton", "SKILLS", LoadFirstSprite(IconStarPath), ColCyan, font, new Vector2(210f, 72f));
            Text skillsBtnTxt = skillsBtn.transform.Find("Label")?.GetComponent<Text>();
            if (skillsBtnTxt != null) skillsBtnTxt.fontSize = 23;

            Button pauseBtn = CreateSquareIconButton(topRight.transform, "PauseButton", LoadFirstSprite(IconPausePath), ColTextWhite, new Vector2(72f, 72f));

            // === C. BOTTOM-CENTER: 4-Slot Skill Hotbar & Toast ===
            BuildSkillHotbarAndPanel(canvasRoot.transform, font, skillTreeUI, out GameObject skillPanelRoot);

            RectTransform toastRt = CreateBorderedBox(canvasRoot.transform, "CheckpointToast", new Vector2(740f, 66f), ColPanelDark, ColGreen);
            toastRt.anchorMin = new Vector2(0.5f, 0f);
            toastRt.anchorMax = new Vector2(0.5f, 0f);
            toastRt.pivot = new Vector2(0.5f, 0f);
            toastRt.anchoredPosition = new Vector2(0f, 154f);
            Text toastText = CreatePixelText(toastRt, "ToastText", "CHECKPOINT SAVED!", 23, ColGreen, font, TextAnchor.MiddleCenter);
            StretchFull(toastText.rectTransform);
            toastRt.gameObject.SetActive(false);

            // === D. DIM BACKDROP ===
            GameObject backdropObj = new GameObject("DimBackdrop");
            backdropObj.transform.SetParent(canvasRoot.transform, false);
            Image backdropImg = backdropObj.AddComponent<Image>();
            backdropImg.color = new Color(0.051f, 0.063f, 0.102f, 0.68f);
            backdropImg.raycastTarget = true;
            StretchFull(backdropImg.rectTransform);
            backdropObj.SetActive(false);

            // Đưa bảng SkillTreePanel lên trên DimBackdrop để làm tối nền phía sau
            if (skillPanelRoot != null)
            {
                skillPanelRoot.transform.SetAsLastSibling();
                skillPanelRoot.SetActive(false);
            }

            // === E. PAUSE PANEL (Window_2) ===
            RectTransform pausePanelRt = CreateWindowFrame(
                canvasRoot.transform,
                "PausePanel",
                "Window_2",
                "PAUSED",
                new Vector2(645f, 615f),
                new Vector2(39f, 0f),
                0.436f,
                0.932f,
                font,
                true,
                out Button pauseCloseBtn,
                out RectTransform pauseContent);

            VerticalLayoutGroup pauseLayout = pauseContent.gameObject.AddComponent<VerticalLayoutGroup>();
            pauseLayout.spacing = 18f;
            pauseLayout.childAlignment = TextAnchor.MiddleCenter;
            pauseLayout.childControlWidth = false;
            pauseLayout.childControlHeight = false;
            pauseLayout.childForceExpandWidth = false;
            pauseLayout.childForceExpandHeight = false;

            Button resumeBtn = CreateActionButton(pauseContent, "ResumeButton", "RESUME", LoadFirstSprite(IconPlayPath), ColGreen, font, new Vector2(378f, 78f));
            Button pauseRestartBtn = CreateActionButton(pauseContent, "PauseRestartButton", "RESTART", LoadFirstSprite(IconRestartPath), ColGold, font, new Vector2(378f, 78f));
            Button pauseSettingsBtn = CreateActionButton(pauseContent, "PauseSettingsButton", "SETTINGS", LoadFirstSprite(IconSettingsPath), ColTextWhite, font, new Vector2(378f, 78f));
            Button pauseMenuBtn = CreateActionButton(pauseContent, "PauseMainMenuButton", "MAIN MENU", LoadFirstSprite(IconHomePath), ColRed, font, new Vector2(378f, 78f));
            pausePanelRt.gameObject.SetActive(false);

            // === F. SETTINGS PANEL (Window_5) ===
            SettingsPanelUI settingsPanel = CreateSettingsPanel(canvasRoot.transform, font);
            settingsPanel.gameObject.SetActive(false);

            // === G. GAME OVER PANEL (Window_7) ===
            RectTransform gameOverRt = CreateWindowFrame(
                canvasRoot.transform,
                "GameOverPanel",
                "Window_7",
                "GAME OVER",
                new Vector2(810f, 660f),
                new Vector2(40f, 0f),
                0.449f,
                0.946f,
                font,
                false,
                out _,
                out RectTransform gameOverContent);

            VerticalLayoutGroup goLayout = gameOverContent.gameObject.AddComponent<VerticalLayoutGroup>();
            goLayout.spacing = 15f;
            goLayout.childAlignment = TextAnchor.MiddleCenter;
            goLayout.childControlWidth = false;
            goLayout.childControlHeight = false;
            goLayout.childForceExpandWidth = false;
            goLayout.childForceExpandHeight = false;

            GameObject goBadge = new GameObject("DeathBadge");
            goBadge.transform.SetParent(gameOverContent, false);
            RectTransform goBadgeRt = goBadge.AddComponent<RectTransform>();
            goBadgeRt.sizeDelta = new Vector2(450f, 72f);
            Image skullImg = CreateIconImage(goBadge.transform, "SkullIcon", LoadFirstSprite(IconSkullPath), ColRed, new Vector2(66f, 66f));
            skullImg.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            skullImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            skullImg.rectTransform.anchoredPosition = Vector2.zero;

            Text goCoinsText = CreatePixelText(gameOverContent, "GameOverCoinsText", "COINS: 0", 30, ColGold, font, TextAnchor.MiddleCenter);
            goCoinsText.rectTransform.sizeDelta = new Vector2(450f, 48f);

            Button goCheckpointBtn = CreateActionButton(gameOverContent, "CheckpointButton", "CHECKPOINT", LoadFirstSprite(IconPlayPath), ColGreen, font, new Vector2(426f, 72f));
            Button goRestartBtn = CreateActionButton(gameOverContent, "GameOverRestartButton", "RESTART", LoadFirstSprite(IconRestartPath), ColGold, font, new Vector2(426f, 72f));
            Button goMenuBtn = CreateActionButton(gameOverContent, "GameOverMenuButton", "MAIN MENU", LoadFirstSprite(IconHomePath), ColRed, font, new Vector2(426f, 72f));
            gameOverRt.gameObject.SetActive(false);

            // === H. VICTORY PANEL (Window_7) ===
            RectTransform victoryRt = CreateWindowFrame(
                canvasRoot.transform,
                "VictoryPanel",
                "Window_7",
                "VICTORY",
                new Vector2(810f, 660f),
                new Vector2(40f, 0f),
                0.449f,
                0.946f,
                font,
                false,
                out _,
                out RectTransform victoryContent);

            VerticalLayoutGroup vicLayout = victoryContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vicLayout.spacing = 18f;
            vicLayout.childAlignment = TextAnchor.MiddleCenter;
            vicLayout.childControlWidth = false;
            vicLayout.childControlHeight = false;
            vicLayout.childForceExpandWidth = false;
            vicLayout.childForceExpandHeight = false;

            GameObject vicBadge = new GameObject("VictoryBadge");
            vicBadge.transform.SetParent(victoryContent, false);
            RectTransform vicBadgeRt = vicBadge.AddComponent<RectTransform>();
            vicBadgeRt.sizeDelta = new Vector2(450f, 96f);
            HorizontalLayoutGroup vicRow = vicBadge.AddComponent<HorizontalLayoutGroup>();
            vicRow.spacing = 21f;
            vicRow.childAlignment = TextAnchor.MiddleCenter;
            vicRow.childControlWidth = false;
            vicRow.childControlHeight = false;

            Sprite starSprite = LoadFirstSprite(IconStarPath);
            CreateIconImage(vicBadge.transform, "StarLeft", starSprite, ColGold, new Vector2(45f, 45f));
            CreateIconImage(vicBadge.transform, "TrophyIcon", LoadFirstSprite(IconTrophyPath), ColGold, new Vector2(78f, 78f));
            CreateIconImage(vicBadge.transform, "StarRight", starSprite, ColGold, new Vector2(45f, 45f));

            Text vicCoinsText = CreatePixelText(victoryContent, "VictoryCoinsText", "COINS: 0", 30, ColGold, font, TextAnchor.MiddleCenter);
            vicCoinsText.rectTransform.sizeDelta = new Vector2(450f, 52f);

            Button vicRestartBtn = CreateActionButton(victoryContent, "VictoryRestartButton", "PLAY AGAIN", LoadFirstSprite(IconRestartPath), ColGreen, font, new Vector2(426f, 78f));
            Button vicMenuBtn = CreateActionButton(victoryContent, "VictoryMenuButton", "MAIN MENU", LoadFirstSprite(IconHomePath), ColCyan, font, new Vector2(426f, 78f));
            victoryRt.gameObject.SetActive(false);

            // Wire PlayerHUD
            SerializedObject so = new SerializedObject(hud);
            SetObject(so, "_healthBarFill", healthFillImg);
            SetObject(so, "_healthText", hpText);
            SetObject(so, "_staminaBarFill", staminaFillImg);
            SetObject(so, "_staminaText", staminaText);
            SetObject(so, "_levelBadgeText", lvBadgeTxt);
            SetObject(so, "_expBarFill", expFillImg);
            SetObject(so, "_expText", expText);
            SetObject(so, "_rockText", rockText);
            SetObject(so, "_scoreText", scoreText);
            SetObject(so, "_skillsButton", skillsBtn);
            SetObject(so, "_pauseButton", pauseBtn);
            SetObject(so, "_skillTreePanel", skillTreeUI);
            SetObject(so, "_toastContainer", toastRt.gameObject);
            SetObject(so, "_toastText", toastText);
            SetObject(so, "_dimBackdrop", backdropObj);

            SetObject(so, "_pausePanel", pausePanelRt.gameObject);
            SetObject(so, "_resumeButton", resumeBtn);
            SetObject(so, "_pauseCloseButton", pauseCloseBtn);
            SetObject(so, "_pauseRestartButton", pauseRestartBtn);
            SetObject(so, "_pauseSettingsButton", pauseSettingsBtn);
            SetObject(so, "_pauseMainMenuButton", pauseMenuBtn);

            SetObject(so, "_settingsPanel", settingsPanel);

            SetObject(so, "_gameOverPanel", gameOverRt.gameObject);
            SetObject(so, "_gameOverCoinsText", goCoinsText);
            SetObject(so, "_checkpointRespawnButton", goCheckpointBtn);
            SetObject(so, "_gameOverRestartButton", goRestartBtn);
            SetObject(so, "_gameOverMainMenuButton", goMenuBtn);

            SetObject(so, "_victoryPanel", victoryRt.gameObject);
            SetObject(so, "_victoryCoinsText", vicCoinsText);
            SetObject(so, "_victoryRestartButton", vicRestartBtn);
            SetObject(so, "_victoryMainMenuButton", vicMenuBtn);
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveTempPrefab(canvasRoot, PrefabGameplayUIPath);
        }

        private static void BuildSkillHotbarAndPanel(
            Transform canvasTransform,
            Font font,
            SkillTreePanelUI skillTreeUI,
            out GameObject skillPanelRoot)
        {
            string[] skillTitles = { "FIREBALL", "LIGHTNING", "MAGIC SHIELD", "EARTH SPIKES" };
            string[] skillKeys = { "1/Q", "2/E", "3/R", "4/F" };
            string[] iconPaths =
            {
                SkillIconFireballPath,
                SkillIconLightningPath,
                SkillIconShieldPath,
                SkillIconSpikesPath
            };
            Color[] accentColors =
            {
                new Color(1f, 0.52f, 0.25f, 1f),
                new Color(0.42f, 0.85f, 1f, 1f),
                new Color(0.45f, 0.95f, 0.65f, 1f),
                new Color(0.95f, 0.78f, 0.32f, 1f)
            };

            Sprite squareSprite = LoadFirstSprite(SquareBarPath);

            // 1. Thanh Hotbar 4 ô ở đáy màn hình (Full HD, icon 64x64 đúng bội số 2x của 32x32)
            GameObject hotbarObj = new GameObject("SkillHotbar");
            hotbarObj.transform.SetParent(canvasTransform, false);
            RectTransform hotbarRt = hotbarObj.AddComponent<RectTransform>();
            hotbarRt.anchorMin = new Vector2(0.5f, 0f);
            hotbarRt.anchorMax = new Vector2(0.5f, 0f);
            hotbarRt.pivot = new Vector2(0.5f, 0f);
            hotbarRt.sizeDelta = new Vector2(534f, 120f);
            hotbarRt.anchoredPosition = new Vector2(0f, 20f);

            HorizontalLayoutGroup hotbarLayout = hotbarObj.AddComponent<HorizontalLayoutGroup>();
            hotbarLayout.spacing = 18f;
            hotbarLayout.childAlignment = TextAnchor.MiddleCenter;
            hotbarLayout.childControlWidth = false;
            hotbarLayout.childControlHeight = false;
            hotbarLayout.childForceExpandWidth = false;
            hotbarLayout.childForceExpandHeight = false;

            Button[] hotbarButtons = new Button[4];
            Image[] hotbarIcons = new Image[4];
            Image[] hotbarCdFills = new Image[4];
            Text[] hotbarCdTexts = new Text[4];
            Text[] hotbarLvTexts = new Text[4];

            for (int i = 0; i < 4; i++)
            {
                Sprite iconSprite = LoadFirstSprite(iconPaths[i]);
                RectTransform slotRt = CreateBorderedBox(hotbarObj.transform, $"SkillSlot_{i}", new Vector2(120f, 120f), ColPanelDark, ColPanelBorder);
                Image bgImg = slotRt.GetComponent<Image>();
                bgImg.raycastTarget = true;

                Button slotBtn = slotRt.gameObject.AddComponent<Button>();
                slotBtn.targetGraphic = bgImg;
                ColorBlock slotColors = slotBtn.colors;
                slotColors.normalColor = ColPanelDark;
                slotColors.highlightedColor = new Color(0.20f, 0.24f, 0.38f, 1f);
                slotColors.pressedColor = new Color(0.08f, 0.10f, 0.16f, 1f);
                slotBtn.colors = slotColors;
                slotRt.gameObject.AddComponent<UIButtonFeedback>();

                Image iconImg = CreateIconImage(slotRt, "Icon", iconSprite, new Color(0.72f, 0.76f, 0.84f, 1f), new Vector2(64f, 64f));
                iconImg.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                iconImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                iconImg.rectTransform.anchoredPosition = new Vector2(0f, 2f);

                GameObject cdObj = new GameObject("CooldownFill");
                cdObj.transform.SetParent(slotRt, false);
                Image cdImg = cdObj.AddComponent<Image>();
                cdImg.sprite = squareSprite;
                cdImg.color = new Color(0.04f, 0.05f, 0.09f, 0.78f);
                cdImg.type = Image.Type.Filled;
                cdImg.fillMethod = Image.FillMethod.Vertical;
                cdImg.fillOrigin = (int)Image.OriginVertical.Bottom;
                cdImg.fillAmount = 0f;
                cdImg.raycastTarget = false;
                StretchFull(cdImg.rectTransform);

                Text keyBadge = CreatePixelText(slotRt, "KeyText", skillKeys[i], 15, ColTextMuted, font, TextAnchor.UpperLeft);
                keyBadge.rectTransform.anchorMin = new Vector2(0f, 1f);
                keyBadge.rectTransform.anchorMax = new Vector2(1f, 1f);
                keyBadge.rectTransform.pivot = new Vector2(0.5f, 1f);
                keyBadge.rectTransform.offsetMin = new Vector2(8f, -24f);
                keyBadge.rectTransform.offsetMax = new Vector2(-8f, -4f);

                Text lvBadge = CreatePixelText(slotRt, "LevelText", "LOCK", 15, ColGold, font, TextAnchor.LowerCenter);
                lvBadge.rectTransform.anchorMin = new Vector2(0f, 0f);
                lvBadge.rectTransform.anchorMax = new Vector2(1f, 0f);
                lvBadge.rectTransform.pivot = new Vector2(0.5f, 0f);
                lvBadge.rectTransform.offsetMin = new Vector2(4f, 4f);
                lvBadge.rectTransform.offsetMax = new Vector2(-4f, 26f);

                Text cdText = CreatePixelText(slotRt, "CooldownText", string.Empty, 28, ColTextWhite, font, TextAnchor.MiddleCenter);
                StretchFull(cdText.rectTransform);
                cdText.gameObject.SetActive(false);

                hotbarButtons[i] = slotBtn;
                hotbarIcons[i] = iconImg;
                hotbarCdFills[i] = cdImg;
                hotbarCdTexts[i] = cdText;
                hotbarLvTexts[i] = lvBadge;
            }

            // 2. Bảng Mở khóa & Nâng cấp Skill (mở bằng phím Tab hoặc nút SKILLS)
            RectTransform panelRt = CreateWindowFrame(
                canvasTransform,
                "SkillTreePanel",
                "Window_5",
                "SKILLS",
                new Vector2(1100f, 830f),
                new Vector2(44f, 0f),
                0.449f,
                0.946f,
                font,
                true,
                out Button closeBtn,
                out RectTransform content);

            skillPanelRoot = panelRt.gameObject;

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Text coinsHeader = CreatePixelText(content, "CoinsBalanceText", "COINS: 0", 28, ColGold, font, TextAnchor.MiddleCenter);
            coinsHeader.rectTransform.sizeDelta = new Vector2(740f, 42f);

            Text[] rowLvTexts = new Text[4];
            Text[] rowStatTexts = new Text[4];
            Button[] rowBtns = new Button[4];
            Text[] rowBtnTexts = new Text[4];

            for (int i = 0; i < 4; i++)
            {
                RectTransform rowRt = CreateBorderedBox(content, $"SkillRow_{i}", new Vector2(760f, 94f), new Color(0.08f, 0.10f, 0.165f, 0.96f), ColPanelBorder);

                RectTransform iconBox = CreateBorderedBox(rowRt, "IconBox", new Vector2(76f, 76f), ColPanelDark, accentColors[i]);
                iconBox.anchorMin = new Vector2(0f, 0.5f);
                iconBox.anchorMax = new Vector2(0f, 0.5f);
                iconBox.pivot = new Vector2(0f, 0.5f);
                iconBox.anchoredPosition = new Vector2(12f, 0f);
                CreateIconImage(iconBox, "Icon", LoadFirstSprite(iconPaths[i]), Color.white, new Vector2(64f, 64f));

                Text titleTxt = CreatePixelText(rowRt, "NameText", $"{skillTitles[i]} [{skillKeys[i]}]", 22, accentColors[i], font, TextAnchor.UpperLeft);
                titleTxt.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                titleTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
                titleTxt.rectTransform.offsetMin = new Vector2(102f, 4f);
                titleTxt.rectTransform.offsetMax = new Vector2(-350f, -10f);

                Text statTxt = CreatePixelText(rowRt, "StatText", "LOCKED", 17, ColTextMuted, font, TextAnchor.LowerLeft);
                statTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
                statTxt.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                statTxt.rectTransform.offsetMin = new Vector2(102f, 10f);
                statTxt.rectTransform.offsetMax = new Vector2(-350f, -4f);

                Text lvTxt = CreatePixelText(rowRt, "LevelText", "LOCKED", 20, ColGold, font, TextAnchor.MiddleCenter);
                lvTxt.rectTransform.anchorMin = new Vector2(1f, 0.5f);
                lvTxt.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                lvTxt.rectTransform.pivot = new Vector2(1f, 0.5f);
                lvTxt.rectTransform.sizeDelta = new Vector2(118f, 44f);
                lvTxt.rectTransform.anchoredPosition = new Vector2(-218f, 0f);

                Button upgBtn = CreateActionButton(rowRt, "UpgradeButton", "UNLOCK (10)", null, ColGreen, font, new Vector2(196f, 62f));
                RectTransform upgRt = upgBtn.GetComponent<RectTransform>();
                upgRt.anchorMin = new Vector2(1f, 0.5f);
                upgRt.anchorMax = new Vector2(1f, 0.5f);
                upgRt.pivot = new Vector2(1f, 0.5f);
                upgRt.anchoredPosition = new Vector2(-12f, 0f);
                Text upgTxt = upgBtn.transform.Find("Label")?.GetComponent<Text>();
                if (upgTxt != null) upgTxt.fontSize = 18;

                rowLvTexts[i] = lvTxt;
                rowStatTexts[i] = statTxt;
                rowBtns[i] = upgBtn;
                rowBtnTexts[i] = upgTxt;
            }

            Button doneBtn = CreateActionButton(content, "DoneButton", "DONE", LoadFirstSprite(IconPlayPath), ColGreen, font, new Vector2(310f, 68f));

            SerializedObject skillSo = new SerializedObject(skillTreeUI);
            SetObjectArray(skillSo, "_hotbarButtons", hotbarButtons);
            SetObjectArray(skillSo, "_hotbarIcons", hotbarIcons);
            SetObjectArray(skillSo, "_hotbarCooldownFills", hotbarCdFills);
            SetObjectArray(skillSo, "_hotbarCooldownTexts", hotbarCdTexts);
            SetObjectArray(skillSo, "_hotbarLevelTexts", hotbarLvTexts);
            SetObject(skillSo, "_panelRoot", skillPanelRoot);
            SetObject(skillSo, "_coinsText", coinsHeader);
            SetObject(skillSo, "_closeButton", closeBtn);
            SetObject(skillSo, "_doneButton", doneBtn);
            SetObjectArray(skillSo, "_rowLevelTexts", rowLvTexts);
            SetObjectArray(skillSo, "_rowStatTexts", rowStatTexts);
            SetObjectArray(skillSo, "_rowUpgradeButtons", rowBtns);
            SetObjectArray(skillSo, "_rowUpgradeButtonTexts", rowBtnTexts);
            SetObject(skillSo, "_iconFrameSprite", LoadFirstSprite("Assets/GameAssets/UI/1 Frames/Portrait_frame.png"));
            skillSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SettingsPanelUI CreateSettingsPanel(Transform parent, Font font)
        {
            RectTransform panelRt = CreateWindowFrame(
                parent,
                "SettingsPanel",
                "Window_5",
                "SETTINGS",
                new Vector2(880f, 660f),
                new Vector2(44f, 0f),
                0.449f,
                0.946f,
                font,
                true,
                out Button closeBtn,
                out RectTransform content);

            SettingsPanelUI settingsUI = panelRt.gameObject.AddComponent<SettingsPanelUI>();

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateVolumeSliderRow(content, "MasterRow", "MASTER", font, out Slider masterSlider, out Text masterVal);
            CreateVolumeSliderRow(content, "MusicRow", "MUSIC", font, out Slider musicSlider, out Text musicVal);
            CreateVolumeSliderRow(content, "SfxRow", "SFX", font, out Slider sfxSlider, out Text sfxVal);

            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(content, false);
            RectTransform spacerRt = spacer.AddComponent<RectTransform>();
            spacerRt.sizeDelta = new Vector2(150f, 6f);

            Button backBtn = CreateActionButton(content, "BackButton", "DONE", LoadFirstSprite(IconSpeakerPath), ColGreen, font, new Vector2(330f, 72f));

            SerializedObject so = new SerializedObject(settingsUI);
            SetObject(so, "_masterSlider", masterSlider);
            SetObject(so, "_musicSlider", musicSlider);
            SetObject(so, "_sfxSlider", sfxSlider);
            SetObject(so, "_masterValueText", masterVal);
            SetObject(so, "_musicValueText", musicVal);
            SetObject(so, "_sfxValueText", sfxVal);
            SetObject(so, "_closeButton", closeBtn);
            SetObject(so, "_backButton", backBtn);
            so.ApplyModifiedPropertiesWithoutUndo();

            return settingsUI;
        }

        private static RectTransform CreateWindowFrame(
            Transform parent,
            string name,
            string windowSpriteName,
            string title,
            Vector2 size,
            Vector2 anchoredPos,
            float headerXFrac,
            float redXAnchorX,
            Font font,
            bool showRedCloseHitbox,
            out Button cornerCloseBtn,
            out RectTransform contentArea)
        {
            GameObject winObj = new GameObject(name);
            winObj.transform.SetParent(parent, false);
            Image bg = winObj.AddComponent<Image>();
            bg.sprite = LoadSpriteByName(WindowSheetPath, windowSpriteName);
            bg.preserveAspect = false;

            RectTransform winRt = winObj.GetComponent<RectTransform>();
            winRt.anchorMin = new Vector2(0.5f, 0.5f);
            winRt.anchorMax = new Vector2(0.5f, 0.5f);
            winRt.pivot = new Vector2(0.5f, 0.5f);
            winRt.sizeDelta = size;
            winRt.anchoredPosition = anchoredPos;

            // Tiêu đề căn chuẩn giữa tấm thép phía trên
            Text headerText = CreatePixelText(winObj.transform, "HeaderTitle", title, 34, ColTextWhite, font, TextAnchor.MiddleCenter);
            RectTransform headerRt = headerText.rectTransform;
            headerRt.anchorMin = new Vector2(headerXFrac, 0.905f);
            headerRt.anchorMax = new Vector2(headerXFrac, 0.905f);
            headerRt.pivot = new Vector2(0.5f, 0.5f);
            headerRt.sizeDelta = new Vector2(size.x * 0.44f, 60f);
            headerRt.anchoredPosition = Vector2.zero;

            // Nút đóng (X) trùng khít hình tròn đỏ góc trên phải của Window sprite
            cornerCloseBtn = null;
            if (showRedCloseHitbox)
            {
                GameObject closeObj = new GameObject("CornerCloseButton");
                closeObj.transform.SetParent(winObj.transform, false);
                Image hitImg = closeObj.AddComponent<Image>();
                hitImg.color = new Color(1f, 1f, 1f, 0.001f);
                hitImg.raycastTarget = true;

                RectTransform closeRt = closeObj.GetComponent<RectTransform>();
                closeRt.anchorMin = new Vector2(redXAnchorX, 0.862f);
                closeRt.anchorMax = new Vector2(redXAnchorX, 0.862f);
                closeRt.pivot = new Vector2(0.5f, 0.5f);
                closeRt.sizeDelta = new Vector2(66f, 66f);
                closeRt.anchoredPosition = Vector2.zero;

                cornerCloseBtn = closeObj.AddComponent<Button>();
                cornerCloseBtn.targetGraphic = hitImg;
                closeObj.AddComponent<UIButtonFeedback>();
            }

            // Vùng nội dung nằm lọt lòng phần nền navy bên trong khung gỗ
            GameObject contentObj = new GameObject("ContentArea");
            contentObj.transform.SetParent(winObj.transform, false);
            contentArea = contentObj.AddComponent<RectTransform>();
            bool isWide = windowSpriteName != "Window_2";
            contentArea.anchorMin = isWide ? new Vector2(0.09f, 0.115f) : new Vector2(0.105f, 0.115f);
            contentArea.anchorMax = isWide ? new Vector2(0.81f, 0.815f) : new Vector2(0.775f, 0.815f);
            contentArea.offsetMin = Vector2.zero;
            contentArea.offsetMax = Vector2.zero;

            return winRt;
        }

        private static void CreateVolumeSliderRow(
            Transform parent,
            string rowName,
            string label,
            Font font,
            out Slider slider,
            out Text valueText)
        {
            GameObject row = new GameObject(rowName);
            row.transform.SetParent(parent, false);
            RectTransform rowRt = row.AddComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(585f, 66f);

            Text labelText = CreatePixelText(row.transform, "Label", label, 26, ColTextWhite, font, TextAnchor.MiddleLeft);
            labelText.rectTransform.anchorMin = new Vector2(0f, 0f);
            labelText.rectTransform.anchorMax = new Vector2(0f, 1f);
            labelText.rectTransform.pivot = new Vector2(0f, 0.5f);
            labelText.rectTransform.sizeDelta = new Vector2(162f, 0f);
            labelText.rectTransform.anchoredPosition = new Vector2(6f, 0f);

            // Slider Pixel Art
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(row.transform, false);
            RectTransform sliderRt = sliderObj.AddComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0f, 0.5f);
            sliderRt.anchorMax = new Vector2(0f, 0.5f);
            sliderRt.pivot = new Vector2(0f, 0.5f);
            sliderRt.sizeDelta = new Vector2(306f, 38f);
            sliderRt.anchoredPosition = new Vector2(171f, 0f);

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = LoadFirstSprite(BandOffPath);
            StretchFull(bgImg.rectTransform);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = new Vector2(8f, 6f);
            fillAreaRt.offsetMax = new Vector2(-8f, -6f);

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = LoadFirstSprite(BandOnPath);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;
            fillImg.color = new Color(0.45f, 0.85f, 1f, 1f);
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(14f, 0f);
            handleAreaRt.offsetMax = new Vector2(-14f, 0f);

            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleArea.transform, false);
            Image handleImg = handleObj.AddComponent<Image>();
            handleImg.sprite = LoadFirstSprite(SliderHandlePath);
            RectTransform handleRt = handleObj.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(36f, 36f);

            slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            valueText = CreatePixelText(row.transform, "ValueText", "100%", 25, ColGold, font, TextAnchor.MiddleRight);
            valueText.rectTransform.anchorMin = new Vector2(1f, 0f);
            valueText.rectTransform.anchorMax = new Vector2(1f, 1f);
            valueText.rectTransform.pivot = new Vector2(1f, 0.5f);
            valueText.rectTransform.sizeDelta = new Vector2(96f, 0f);
            valueText.rectTransform.anchoredPosition = new Vector2(-4f, 0f);
        }

        private static Button CreateActionButton(
            Transform parent,
            string name,
            string label,
            Sprite icon,
            Color accentColor,
            Font font,
            Vector2 size)
        {
            RectTransform boxRt = CreateBorderedBox(parent, name, size, ColPanelDark, ColPanelBorder);
            Image bgImg = boxRt.GetComponent<Image>();
            bgImg.raycastTarget = true;

            Button btn = boxRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            ColorBlock colors = btn.colors;
            colors.normalColor = ColPanelDark;
            colors.highlightedColor = new Color(0.180f, 0.216f, 0.345f, 1f);
            colors.pressedColor = new Color(0.075f, 0.090f, 0.149f, 1f);
            colors.selectedColor = colors.highlightedColor;
            btn.colors = colors;

            if (icon != null)
            {
                Image iconImg = CreateIconImage(boxRt, "Icon", icon, accentColor, new Vector2(34f, 34f));
                iconImg.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                iconImg.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                iconImg.rectTransform.pivot = new Vector2(0f, 0.5f);
                iconImg.rectTransform.anchoredPosition = new Vector2(22f, 0f);
            }

            Text txt = CreatePixelText(boxRt, "Label", label, 28, accentColor, font, TextAnchor.MiddleCenter);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = new Vector2(icon != null ? 62f : 14f, 3f);
            txt.rectTransform.offsetMax = new Vector2(-16f, -3f);

            boxRt.gameObject.AddComponent<UIButtonFeedback>();
            return btn;
        }

        private static Button CreateSquareIconButton(Transform parent, string name, Sprite icon, Color iconColor, Vector2 size)
        {
            RectTransform boxRt = CreateBorderedBox(parent, name, size, ColPanelDark, ColPanelBorder);
            Image bgImg = boxRt.GetComponent<Image>();
            bgImg.raycastTarget = true;

            Button btn = boxRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            ColorBlock colors = btn.colors;
            colors.normalColor = ColPanelDark;
            colors.highlightedColor = new Color(0.20f, 0.24f, 0.38f, 1f);
            colors.pressedColor = new Color(0.07f, 0.08f, 0.14f, 1f);
            btn.colors = colors;

            Image iconImg = CreateIconImage(boxRt, "Icon", icon, iconColor, new Vector2(size.x * 0.48f, size.y * 0.48f));
            iconImg.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            iconImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            iconImg.rectTransform.anchoredPosition = Vector2.zero;

            boxRt.gameObject.AddComponent<UIButtonFeedback>();
            return btn;
        }

        private static RectTransform CreateBorderedBox(Transform parent, string name, Vector2 size, Color fillColor, Color borderColor)
        {
            GameObject boxObj = new GameObject(name);
            boxObj.transform.SetParent(parent, false);
            Image img = boxObj.AddComponent<Image>();
            img.sprite = LoadFirstSprite(SquareBarPath);
            img.color = fillColor;
            img.raycastTarget = false;

            Outline outline = boxObj.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(3f, -3f);

            RectTransform rt = boxObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            return rt;
        }

        private static Image CreateIconImage(Transform parent, string name, Sprite sprite, Color color, Vector2 size)
        {
            GameObject iconObj = new GameObject(name);
            iconObj.transform.SetParent(parent, false);
            Image img = iconObj.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = size;
            return img;
        }

        private static Text CreatePixelText(Transform parent, string name, string content, int fontSize, Color color, Font font, TextAnchor alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            Text txt = textObj.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            if (font != null) txt.font = font;

            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = ColOutlineDark;
            outline.effectDistance = new Vector2(3f, -3f);

            return txt;
        }

        private static GameObject CreateCanvasRoot(string name, int sortingOrder)
        {
            GameObject root = new GameObject(name);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
            return root;
        }

        private static void InjectUIIntoScene(string scenePath, GameObject uiPrefab, string legacyPlaceholderName)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject uiSection = GameObject.Find("=== UI ===");
            if (uiSection == null)
            {
                uiSection = new GameObject("=== UI ===");
            }

            if (!string.IsNullOrEmpty(legacyPlaceholderName))
            {
                Transform oldPlaceholder = uiSection.transform.Find(legacyPlaceholderName);
                if (oldPlaceholder != null)
                {
                    Object.DestroyImmediate(oldPlaceholder.gameObject);
                }
            }

            if (uiPrefab != null)
            {
                for (int i = uiSection.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = uiSection.transform.GetChild(i);
                    if (child.name == uiPrefab.name)
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                }

                GameObject uiInstance = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab);
                uiInstance.transform.SetParent(uiSection.transform, false);
            }

            EnsureEventSystemInActiveScene();
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static GameObject SaveTempPrefab(GameObject temp, string assetPath)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, assetPath);
            Object.DestroyImmediate(temp);
            return saved;
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

        private static Font LoadPixelFont()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font != null) return font;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Sprite LoadFirstSprite(string assetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null) return null;

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    return sprite;
                }
            }
            return null;
        }

        private static Sprite LoadSpriteByName(string assetPath, string spriteName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null) return null;

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }
            return LoadFirstSprite(assetPath);
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchorBounds(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetObject(SerializedObject so, string propName, Object value)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static void SetObjectArray(SerializedObject so, string propName, Object[] values)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop == null || !prop.isArray) return;

            int len = values != null ? values.Length : 0;
            prop.arraySize = len;
            for (int i = 0; i < len; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
#endif
