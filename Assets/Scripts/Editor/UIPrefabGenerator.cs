using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Presentation.UI;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Generates UI prefab assets for all game screens, fixing council blocker B2
    /// (SceneBuilderUI uses AddComponent instead of prefab Instantiate).
    ///
    /// Run via: Tools > PuzzleGame > Generate UI Prefabs
    ///
    /// Each prefab includes:
    ///   - RectTransform (full-screen anchors)
    ///   - CanvasGroup (for fade transitions)
    ///   - The controller component with key SerializeField references wired
    /// </summary>
    internal static class UIPrefabGenerator
    {
        private static string _currentPrefabRoot = "Assets/Resources/Prefabs";
        private static UIStyleConfig _config;

        private static void LoadConfig()
        {
            if (_config != null) return;
            string[] guids = AssetDatabase.FindAssets("t:UIStyleConfig");
            if (guids.Length == 0) return;
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _config = AssetDatabase.LoadAssetAtPath<UIStyleConfig>(path);
        }


        // ── Generation entry point ──────────────────────────────────────────

        public static void GenerateAll()
        {
            _currentPrefabRoot = "Assets/Resources/Prefabs";
            EnsurePillSprite();
            EnsureConfig();
            LoadConfig();
            GenerateAllCore();
            HideAllExceptMainMenu();
        }

        public static void GenerateAllColorBlind()
        {
            _currentPrefabRoot = "Assets/Resources/Prefabs/ColorBlind";
            EnsurePillSprite();
            EnsureConfig();
            LoadConfig();
            
            // Apply colorblind-friendly palette (Protanopia/Deuteranopia friendly)
            UnityEngine.ColorUtility.TryParseHtmlString("#1f77b4", out _config.colorPrimary); // Accessible Blue
            UnityEngine.ColorUtility.TryParseHtmlString("#ff7f0e", out _config.colorSecondary); // Accessible Orange
            UnityEngine.ColorUtility.TryParseHtmlString("#d62728", out _config.colorError); // Accessible Red
            UnityEngine.ColorUtility.TryParseHtmlString("#ffbb78", out _config.colorGold); // Accessible Yellow/Gold
            
            try
            {
                GenerateAllCore();
            }
            finally
            {
                // Restore default colors
                UnityEngine.ColorUtility.TryParseHtmlString("#34d399", out _config.colorPrimary);
                UnityEngine.ColorUtility.TryParseHtmlString("#a855f7", out _config.colorSecondary);
                UnityEngine.ColorUtility.TryParseHtmlString("#ef4444", out _config.colorError);
                UnityEngine.ColorUtility.TryParseHtmlString("#fbbf24", out _config.colorGold);
            }
        }

        private static void HideAllExceptMainMenu()
        {
            if (_testCanvas == null) return;
            foreach (Transform child in _testCanvas.transform)
            {
                if (child.name != "MainMenuController")
                    child.gameObject.SetActive(false);
            }
        }

        private static void GenerateAllCore()
        {
            SetupUITestScene();

            GenerateMainMenu();
            GenerateHud();
            GenerateWorldMap();
            GenerateSettings();
            GenerateSettingsPrivacy();
            GenerateSettingsSound();
            GenerateDailyChallenge();
            GenerateShop();
            GeneratePowerUp();
            GenerateAchievementNotification();
            GenerateAgeGate();
            GenerateConsentModal();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UIPrefabGenerator] All prefabs generated under {_currentPrefabRoot}/");
        }

        // ── Individual prefab generators ─────────────────────────────────────

        private static Canvas _testCanvas;

        private static void SetupUITestScene()
        {
            // Create or open UITest scene
            string scenePath = "Assets/Scenes/UITest.unity";
            
            if (!System.IO.Directory.Exists("Assets/Scenes"))
                System.IO.Directory.CreateDirectory("Assets/Scenes");

            UnityEngine.SceneManagement.Scene scene;
            if (!System.IO.File.Exists(scenePath))
            {
                scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
                scene.name = "UITest";
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            }
            else
            {
                scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            }
            
            // Create EventSystem if needed
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                
                // Add the correct Input Module for the New Input System
                var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null)
                {
                    eventSystemGo.AddComponent(inputModuleType);
                }
                else
                {
                    eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            _testCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (_testCanvas == null)
            {
                var canvasGo = new GameObject("MasterCanvas");
                _testCanvas = canvasGo.AddComponent<Canvas>();
                _testCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
        }

        private static void GenerateMainMenu()
        {
            var root = CreateRoot("MainMenuController", out var content);
            var ctrl = root.AddComponent<MainMenuController>();

            // --- HEADER (Coins & Gems) ---
            var headerPanel = new GameObject("HeaderPanel", typeof(RectTransform));
            headerPanel.transform.SetParent(content.transform, false);
            var hLayout = headerPanel.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hLayout.childControlWidth = true; hLayout.childControlHeight = true;
            hLayout.spacing = 400; // Push to sides
            hLayout.childAlignment = UnityEngine.TextAnchor.UpperCenter;
            
            var headerElement = headerPanel.AddComponent<UnityEngine.UI.LayoutElement>();
            headerElement.minHeight = 100;
            headerElement.minWidth = 1000;

            CreateLabelAndAssign(headerPanel, "CoinText", "  1,250", ctrl, "coinText");
            CreateLabelAndAssign(headerPanel, "GemText", "  45", ctrl, "streakText"); // Using streakText as gem text temporarily

            CreateSpacer(content, 0.5f); // Spacer 1

            // --- TITLE ---
            var titleSpace = new GameObject("TitleSpace", typeof(RectTransform));
            titleSpace.transform.SetParent(content.transform, false);
            var titleLe = titleSpace.AddComponent<UnityEngine.UI.LayoutElement>();
            titleLe.minHeight = 250;
            titleLe.minWidth = 1000;
            
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(titleSpace.transform, false);
            var title = AddTextComponent(titleGo, "ORESORTER", 120);
            var colorProp = title.GetType().GetProperty("color");
            if (colorProp != null) colorProp.SetValue(title, _config.colorPrimary);
            
            var subtitleGo = new GameObject("Subtitle", typeof(RectTransform));
            subtitleGo.transform.SetParent(titleSpace.transform, false);
            var subtitle = AddTextComponent(subtitleGo, "\n\nILLUMINATED EARTH", 40);
            if (colorProp != null) colorProp.SetValue(subtitle, UnityEngine.Color.gray);

            CreateSpacer(content, 0.5f); // Spacer 2

            // --- BUTTONS ---
            SetField(ctrl, "playButton", CreateButton(content, "PlayButton", "PLAY", _config.colorPrimary));
            SetField(ctrl, "dailyChallengeButton", CreateButton(content, "DailyChallengeButton", "DAILY CHALLENGE", _config.colorSecondary));
            SetField(ctrl, "settingsButton", CreateButton(content, "ShopButton", "SHOP", _config.colorPanel)); // Fake shop mapped to settings for now
            SetField(ctrl, "privacyButton", CreateButton(content, "SettingsButton", "SETTINGS", _config.colorPanel));
            SetField(ctrl, "soundButton", CreateButton(content, "SoundButton", "SOUND ON", _config.colorPanel));

            CreateSpacer(content, 1f); // Spacer 3

            // --- STREAK PANEL ---
            var streakPanel = new GameObject("StreakPanel", typeof(RectTransform));
            streakPanel.transform.SetParent(content.transform, false);
            var sBg = streakPanel.AddComponent<UnityEngine.UI.Image>();
            sBg.color = _config.colorPanel;
            if (_config.buttonSprite != null) { sBg.sprite = _config.buttonSprite; sBg.type = UnityEngine.UI.Image.Type.Sliced; }
            var sLayout = streakPanel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            sLayout.padding = new RectOffset(40, 40, 40, 40);
            sLayout.childAlignment = UnityEngine.TextAnchor.MiddleCenter;
            sLayout.spacing = 20;
            var streakLe = streakPanel.AddComponent<UnityEngine.UI.LayoutElement>();
            streakLe.minHeight = 250;
            streakLe.minWidth = 900;

            var streakTitle = AddTextComponent(CreateEmptyChild(streakPanel, "StreakTitle"), "DAILY STREAK", 40);
            
            var streakCircles = new GameObject("Circles", typeof(RectTransform));
            streakCircles.transform.SetParent(streakPanel.transform, false);
            var scLayout = streakCircles.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            scLayout.spacing = 20;
            scLayout.childAlignment = UnityEngine.TextAnchor.MiddleCenter;
            for(int i=1; i<=7; i++) {
                var c = new GameObject("Day"+i, typeof(RectTransform));
                c.transform.SetParent(streakCircles.transform, false);
                var crect = c.AddComponent<UnityEngine.UI.LayoutElement>();
                crect.minWidth = 80; crect.minHeight = 80;
                var cImg = c.AddComponent<UnityEngine.UI.Image>();
                cImg.color = i <= 4 ? _config.colorPrimary : _config.colorBackground;
                if (_config.buttonSprite != null) { cImg.sprite = _config.buttonSprite; cImg.type = UnityEngine.UI.Image.Type.Sliced; }
            }

            CreateSpacer(content, 0.5f); // Spacer 4

            // --- BOTTOM NAV ---
            var navSpace = new GameObject("NavSpace", typeof(RectTransform));
            navSpace.transform.SetParent(content.transform, false);
            var navLe = navSpace.AddComponent<UnityEngine.UI.LayoutElement>();
            navLe.minHeight = 150;
            navLe.minWidth = 1000;
            var navLayout = navSpace.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            navLayout.spacing = 100;
            navLayout.childAlignment = UnityEngine.TextAnchor.MiddleCenter;
            
            for(int i=0; i<4; i++) {
                var c = new GameObject("NavIcon"+i, typeof(RectTransform));
                c.transform.SetParent(navSpace.transform, false);
                var crect = c.AddComponent<UnityEngine.UI.LayoutElement>();
                crect.minWidth = 100; crect.minHeight = 100;
                var cImg = c.AddComponent<UnityEngine.UI.Image>();
                cImg.color = i == 0 ? _config.colorPrimary : UnityEngine.Color.clear; // Highlight first
                if (_config.buttonSprite != null) { cImg.sprite = _config.buttonSprite; cImg.type = UnityEngine.UI.Image.Type.Sliced; }
            }

            // Bind the missing required quit button so compiler doesn't complain
            var quitButtonRoot = new GameObject("HiddenQuit");
            quitButtonRoot.transform.SetParent(content.transform, false);
            quitButtonRoot.SetActive(false);
            SetField(ctrl, "quitButton", quitButtonRoot.AddComponent<UnityEngine.UI.Button>());

            var badgeGo = new GameObject("DailyChallengeBadge", typeof(RectTransform));
            badgeGo.transform.SetParent(content.transform, false);
            SetField(ctrl, "dailyChallengeBadge", badgeGo);
            badgeGo.SetActive(false);

            // --- SUB-PANELS (Managed Visibility) ---
            SetField(ctrl, "worldMapPanel", CreateEmptyChild(root, "WorldMapPanel"));
            SetField(ctrl, "levelSelectPanel", CreateEmptyChild(root, "LevelSelectPanel"));
            SetField(ctrl, "dailyChallengePanel", CreateEmptyChild(root, "DailyChallengePanel"));
            SetField(ctrl, "settingsPanel", CreateEmptyChild(root, "SettingsPanel"));
            SetField(ctrl, "soundPanel", CreateEmptyChild(root, "SoundPanel"));

            // --- TRANSITION CANVAS GROUP ---
            SetField(ctrl, "rootCanvasGroup", root.GetComponent<CanvasGroup>());
            
            Save(root, "MainMenuController.prefab");
        }

        private static void GenerateHud()
        {
            GenerateHUDWidget();
            GeneratePausePanel();
            GenerateWinPanel();
            GenerateGlobalHud();
        }

        private static void GenerateHUDWidget()
        {
            var root = CreateRoot("HUDWidgetPresenter");
            var hud = root.AddComponent<HUDWidgetPresenter>();

            CreateLabelAndAssign(root, "MoveCountText", "Moves: 0", hud, "_moveCountText");
            CreateLabelAndAssign(root, "LevelTitleText", "Level 1", hud, "_levelTitleText");

            SetField(hud, "_undoButton", CreateButton(root, "UndoButton", "UNDO"));
            SetField(hud, "_hintButton", CreateButton(root, "HintButton", "HINT"));

            Save(root, "HUDWidgetPresenter.prefab");
        }

        private static void GeneratePausePanel()
        {
            var root = CreateRoot("PausePanelPresenter");
            var pause = root.AddComponent<PausePanelPresenter>();

            var pauseRoot = CreateEmptyChild(root, "PausePanelRoot");
            SetField(pause, "_pausePanelRoot", pauseRoot);
            
            SetField(pause, "_resumeButton", CreateButton(pauseRoot, "ResumeButton", "RESUME"));
            SetField(pause, "_pauseMainMenuButton", CreateButton(pauseRoot, "PauseMainMenuButton", "MENU"));
            SetField(pause, "_pauseRestartButton", CreateButton(pauseRoot, "PauseRestartButton", "RESTART"));

            Save(root, "PausePanelPresenter.prefab");
        }

        private static void GenerateWinPanel()
        {
            var root = CreateRoot("WinPanelPresenter");
            var win = root.AddComponent<WinPanelPresenter>();

            var winRoot = CreateEmptyChild(root, "WinPanelRoot");
            SetField(win, "_winPanelRoot", winRoot);

            CreateLabelAndAssign(winRoot, "WinMoveCountText", "Moves: 0", win, "_winMoveCountText");
            SetField(win, "_nextLevelButton", CreateButton(winRoot, "NextLevelButton", "NEXT"));
            SetField(win, "_replayButton", CreateButton(winRoot, "ReplayButton", "REPLAY"));
            SetField(win, "_mainMenuButton", CreateButton(winRoot, "MainMenuButton", "MENU"));

            var starContainer = CreateEmptyChild(winRoot, "StarContainer");
            var starImages = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var star = new GameObject($"Star{i}", typeof(RectTransform), typeof(Image));
                star.transform.SetParent(starContainer.transform, false);
                starImages[i] = star.GetComponent<Image>();
            }
            SetField(win, "_starImages", starImages);

            CreateLabelAndAssign(winRoot, "WinXpGainedText", "+0 XP", win, "_winXpGainedText");
            CreateLabelAndAssign(winRoot, "WinLeaderboardText", "", win, "_winLeaderboardText");
            CreateLabelAndAssign(winRoot, "WinSeasonXpText", "", win, "_winSeasonXpText");
            SetField(win, "_winSeasonSlider", CreateSlider(winRoot, "WinSeasonSlider"));

            Save(root, "WinPanelPresenter.prefab");
        }

        private static void GenerateGlobalHud()
        {
            var root = CreateRoot("HudPresenter");
            var hud = root.AddComponent<HudPresenter>();

            SetField(hud, "_loadingPanel", CreateEmptyChild(root, "LoadingPanel"));
            SetField(hud, "_diErrorPanel", CreateEmptyChild(root, "DIErrorPanel"));
            CreateLabelAndAssign(root, "DIErrorText", "DI Error", hud, "_diErrorText");

            Save(root, "HudPresenter.prefab");
        }

        private static void GenerateWorldMap()
        {
            var root = CreateRoot("WorldMapController");
            var ctrl = root.AddComponent<WorldMapController>();

            SetField(ctrl, "backButton", CreateButton(root, "BackButton", "BACK"));

            var crystal = CreateEmptyChild(root, "CrystalMinesCard");
            crystal.AddComponent<BiomeCardView>();

            var volcanic = CreateEmptyChild(root, "VolcanicForgeCard");
            volcanic.AddComponent<BiomeCardView>();

            Save(root, "WorldMapController.prefab");
        }

        private static void GenerateSettings()
        {
            var root = CreateRoot("SettingsController");
            var ctrl = root.AddComponent<SettingsController>();

            SetField(ctrl, "_languageDropdown", CreateDropdown(root, "LanguageDropdown"));
            CreateLabelAndAssign(root, "LanguageLabel", "Language", ctrl, "_languageLabel");
            SetField(ctrl, "_hapticToggle", CreateToggle(root, "HapticToggle"));
            CreateLabelAndAssign(root, "HapticLabel", "Haptic", ctrl, "_hapticLabel");
            SetField(ctrl, "_soundButton", CreateButton(root, "SoundButton", "SOUND"));
            SetField(ctrl, "_privacyButton", CreateButton(root, "PrivacyButton", "PRIVACY"));
            SetField(ctrl, "_backButton", CreateButton(root, "BackButton", "BACK"));
            SetField(ctrl, "_soundPanel", CreateEmptyChild(root, "SoundPanel"));
            SetField(ctrl, "_privacyPanel", CreateEmptyChild(root, "PrivacyPanel"));

            Save(root, "SettingsController.prefab");
        }

        private static void GenerateSettingsPrivacy()
        {
            var root = CreateRoot("SettingsPrivacyController");
            var ctrl = root.AddComponent<SettingsPrivacyController>();

            SetField(ctrl, "_analyticsToggle", CreateToggle(root, "AnalyticsToggle"));
            SetField(ctrl, "_personalizedAdsToggle", CreateToggle(root, "PersonalizedAdsToggle"));
            SetField(ctrl, "_resetConsentButton", CreateButton(root, "ResetConsentButton", "RESET CONSENT"));
            SetField(ctrl, "_deleteDataButton", CreateButton(root, "DeleteDataButton", "DELETE DATA"));
            SetField(ctrl, "_privacyPolicyButton", CreateButton(root, "PrivacyPolicyButton", "PRIVACY POLICY"));
            SetField(ctrl, "_termsButton", CreateButton(root, "TermsButton", "TERMS"));
            SetField(ctrl, "_ageVerifyButton", CreateButton(root, "AgeVerifyButton", "AGE VERIFY"));
            SetField(ctrl, "_confirmPanel", CreateEmptyChild(root, "ConfirmPanel"));
            CreateLegacyTextAndAssign(root, "ConfirmMessage", "Are you sure?", ctrl, "_confirmMessage");
            SetField(ctrl, "_confirmYesButton", CreateButton(root, "ConfirmYesButton", "YES"));
            SetField(ctrl, "_confirmNoButton", CreateButton(root, "ConfirmNoButton", "NO"));
            SetField(ctrl, "_backButton", CreateButton(root, "BackButton", "BACK"));

            Save(root, "SettingsPrivacyController.prefab");
        }

        private static void GenerateSettingsSound()
        {
            var root = CreateRoot("SettingsSoundController");
            root.AddComponent<SettingsSoundController>();
            // No SerializeField to wire up — just the component shell.
            Save(root, "SettingsSoundController.prefab");
        }

        private static void GenerateDailyChallenge()
        {
            var root = CreateRoot("DailyChallengeController");
            var ctrl = root.AddComponent<DailyChallengeController>();

            CreateLabelAndAssign(root, "TitleText", "Daily Challenge", ctrl, "titleText");
            CreateLabelAndAssign(root, "StreakCountText", "0", ctrl, "streakCountText");
            CreateLabelAndAssign(root, "LongestStreakText", "0", ctrl, "longestStreakText");
            CreateLabelAndAssign(root, "CountdownText", "--:--:--", ctrl, "countdownText");
            CreateLabelAndAssign(root, "StatusText", "Ready", ctrl, "statusText");
            SetField(ctrl, "completedBadge", CreateEmptyChild(root, "CompletedBadge"));
            SetField(ctrl, "claimableBadge", CreateEmptyChild(root, "ClaimableBadge"));
            SetField(ctrl, "playButton", CreateButton(root, "PlayButton", "PLAY"));
            SetField(ctrl, "backButton", CreateButton(root, "BackButton", "BACK"));

            Save(root, "DailyChallengeController.prefab");
        }

        private static void GenerateShop()
        {
            var root = CreateRoot("ShopUI");
            var shop = root.AddComponent<ShopUI>();

            CreateLabelAndAssign(root, "CoinBalanceText", "0", shop, "coinBalanceText");
            SetField(shop, "closeButton", CreateButton(root, "CloseButton", "CLOSE"));

            var container = CreateEmptyChild(root, "ItemContainer");
            SetField(shop, "itemContainer", container.GetComponent<RectTransform>());
            var scroll = root.AddComponent<ScrollRect>();
            scroll.content = (RectTransform)shop.GetType().GetField("itemContainer",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).GetValue(shop);
            SetField(shop, "scrollRect", scroll);

            var itemPrefab = CreateButton(root, "ShopItemPrefab_Template", "Item");
            itemPrefab.gameObject.SetActive(false);
            SetField(shop, "shopItemPrefab", (GameObject)itemPrefab.gameObject);

            var detail = CreateEmptyChild(root, "DetailPanel");
            SetField(shop, "detailPanel", detail);
            CreateLabelAndAssign(detail, "DetailNameText", "Item Name", shop, "detailNameText");
            CreateLabelAndAssign(detail, "DetailDescriptionText", "Description", shop, "detailDescriptionText");
            CreateLabelAndAssign(detail, "DetailPriceText", "0", shop, "detailPriceText");
            SetField(shop, "purchaseButton", CreateButton(detail, "PurchaseButton", "BUY"));
            SetField(shop, "equipButton", CreateButton(detail, "EquipButton", "EQUIP"));
            SetField(shop, "detailPreviewImage", detail.AddComponent<Image>());

            Save(root, "ShopUI.prefab");
        }

        private static void GeneratePowerUp()
        {
            var root = CreateRoot("PowerUpUI");
            var ctrl = root.AddComponent<PowerUpUI>();
            SetField(ctrl, "blocker", root.GetComponent<CanvasGroup>());

            var slotContainer = CreateEmptyChild(root, "PowerUpSlots");
            var slots = new PowerUpSlotView[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = CreateEmptyChild(slotContainer, $"Slot{i}");
                slot.AddComponent<Image>();
                slots[i] = slot.AddComponent<PowerUpSlotView>();
            }
            SetField(ctrl, "powerUpSlots", slots);

            Save(root, "PowerUpUI.prefab");
        }

        private static void GenerateAchievementNotification()
        {
            var root = CreateRoot("AchievementNotificationUI");
            root.AddComponent<AchievementNotificationUI>();
            Save(root, "AchievementNotificationUI.prefab");
        }

        private static void GenerateAgeGate()
        {
            var root = CreateRoot("AgeGateModal");
            root.AddComponent<AgeGateModal>();
            Save(root, "AgeGateModal.prefab");
        }

        private static void GenerateConsentModal()
        {
            var root = CreateRoot("ConsentModal");
            root.AddComponent<ConsentModal>();
            Save(root, "ConsentModal.prefab");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void CreateSpacer(GameObject parent, float flexibleHeight)
        {
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(parent.transform, false);
            var le = spacer.AddComponent<UnityEngine.UI.LayoutElement>();
            le.flexibleHeight = flexibleHeight;
        }

        private static void EnsurePillSprite()
        {
            string SpritePath = "Assets/Resources/Sprites/PillSprite.png";
            if (System.IO.File.Exists(SpritePath)) return;

            int width = 256;
            int height = 128;
            float radius = height / 2f;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float cx = x + 0.5f;
                    float cy = y + 0.5f;
                    float dist = 0f;
                    if (cx < radius) dist = Mathf.Sqrt((cx - radius) * (cx - radius) + (cy - radius) * (cy - radius));
                    else if (cx > width - radius) dist = Mathf.Sqrt((cx - (width - radius)) * (cx - (width - radius)) + (cy - radius) * (cy - radius));
                    else dist = Mathf.Abs(cy - radius);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            string dir = "Assets/Resources/Sprites";
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.Refresh();

            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.spriteBorder = new Vector4(radius - 1, radius - 1, radius - 1, radius - 1);
                importer.spritePixelsPerUnit = 20f;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:UIStyleConfig");
            if (guids.Length == 0) return;
            var config = AssetDatabase.LoadAssetAtPath<UIStyleConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
            config.buttonWidth = 800f;
            config.buttonHeight = 140f;
            config.titleFontSize = 80;
            config.bodyFontSize = 50;
            config.safeAreaBottom = 34f;
            config.elementGap = 40f;
            config.containerPadding = 24f;
            
            UnityEngine.ColorUtility.TryParseHtmlString("#091421", out config.colorBackground);
            UnityEngine.ColorUtility.TryParseHtmlString("#34d399", out config.colorPrimary);
            UnityEngine.ColorUtility.TryParseHtmlString("#a855f7", out config.colorSecondary);
            UnityEngine.ColorUtility.TryParseHtmlString("#ef4444", out config.colorError);
            UnityEngine.ColorUtility.TryParseHtmlString("#fbbf24", out config.colorGold);
            UnityEngine.ColorUtility.TryParseHtmlString("#1f293799", out config.colorPanel);
            
            config.buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/PillSprite.png");
            
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }


        private static bool _tmprChecked;
        private static Type _textMeshProType;
        private static Type _tmpDropdownType;

        /// <summary>Resolve TMPro types at runtime to avoid compile-time dependency.</summary>
        private static void EnsureTMProTypes()
        {
            if (_tmprChecked) return;
            _tmprChecked = true;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (_textMeshProType == null)
                    _textMeshProType = asm.GetType("TMPro.TextMeshProUGUI");
                if (_tmpDropdownType == null)
                    _tmpDropdownType = asm.GetType("TMPro.TMP_Dropdown");
                if (_textMeshProType != null && _tmpDropdownType != null) break;
            }
        }

        /// <summary>Add a TextMeshProUGUI (preferred) or legacy Text component.</summary>
        private static Component AddTextComponent(GameObject go, string text, int fontSize = 18)
        {
            EnsureTMProTypes();
            if (_textMeshProType != null)
            {
                var tmp = go.AddComponent(_textMeshProType);
                _textMeshProType.GetProperty("text")?.SetValue(tmp, text);
                _textMeshProType.GetProperty("fontSize")?.SetValue(tmp, (float)fontSize);
                _textMeshProType.GetProperty("color")?.SetValue(tmp, Color.white);
                
                // Try to load default font to make text visible
                var settingsType = _textMeshProType.Assembly.GetType("TMPro.TMP_Settings");
                if (settingsType != null)
                {
                    var defaultFontProp = settingsType.GetProperty("defaultFontAsset", BindingFlags.Public | BindingFlags.Static);
                    if (defaultFontProp != null)
                    {
                        var defaultFont = defaultFontProp.GetValue(null);
                        if (defaultFont != null)
                        {
                            _textMeshProType.GetProperty("font")?.SetValue(tmp, defaultFont);
                        }
                    }
                }

                // alignment = Center
                var alignType = _textMeshProType.Assembly.GetType("TMPro.TextAlignmentOptions");
                if (alignType != null)
                {
                    var center = Enum.Parse(alignType, "Center");
                    _textMeshProType.GetProperty("alignment")?.SetValue(tmp, center);
                }
                return (Component)tmp;
            }
            // Fallback to legacy text
            var legacy = go.AddComponent<Text>();
            legacy.text = text;
            legacy.fontSize = fontSize;
            legacy.color = Color.white;
            legacy.alignment = TextAnchor.MiddleCenter;
            return legacy;
        }

        private static void SetField(object controller, string fieldName, object value)
        {
            var field = controller.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(controller, value);
        }

        
        private static GameObject CreateRoot(string name)
        {
            return CreateRoot(name, out _);
        }

        private static GameObject CreateRoot(string name, out GameObject contentPanel)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            if (_testCanvas != null)
                go.transform.SetParent(_testCanvas.transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = go.AddComponent<UnityEngine.UI.Image>();
            bg.color = _config.colorBackground;

            var safeArea = new GameObject("SafeArea", typeof(RectTransform));
            safeArea.transform.SetParent(go.transform, false);
            var safeRect = safeArea.GetComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = new Vector2(_config.containerPadding, _config.safeAreaBottom);
            safeRect.offsetMax = new Vector2(-_config.containerPadding, -_config.containerPadding);

            contentPanel = new GameObject("ContentPanel", typeof(RectTransform));
            contentPanel.transform.SetParent(safeArea.transform, false);
            var contentRect = contentPanel.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            var panelBg = contentPanel.AddComponent<UnityEngine.UI.Image>();
            panelBg.color = _config.colorPanel;
            if (_config.buttonSprite != null)
            {
                panelBg.sprite = _config.buttonSprite;
                panelBg.type = UnityEngine.UI.Image.Type.Sliced;
            }

            var layout = contentPanel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.childAlignment = UnityEngine.TextAnchor.MiddleCenter;
            layout.padding = new RectOffset((int)_config.containerPadding, (int)_config.containerPadding, (int)_config.containerPadding, (int)_config.containerPadding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = _config.elementGap;

            return go;
        }

        private static Button CreateButton(GameObject parent, string name, string label, Color? overrideColor = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(_config.buttonWidth, _config.buttonHeight);

            var image = go.AddComponent<Image>();
            image.color = _config.colorPrimary;
            image.type = Image.Type.Sliced;
            image.sprite = _config.buttonSprite;

            var shadow = go.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(_config.colorPrimary.r, _config.colorPrimary.g, _config.colorPrimary.b, 0.5f);
            shadow.effectDistance = new Vector2(0, -15f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minHeight = _config.buttonHeight;
            layoutElement.minWidth = _config.buttonWidth;
            var colors = button.colors;
            colors.normalColor = overrideColor ?? _config.colorPrimary;
            colors.highlightedColor = Color.Lerp(overrideColor ?? _config.colorPrimary, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(overrideColor ?? _config.colorPrimary, Color.black, 0.2f);
            colors.selectedColor = Color.Lerp(_config.colorPrimary, Color.white, 0.2f);
            button.colors = colors;

            go.AddComponent<UIButtonJuice>();

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelCmp = AddTextComponent(labelGo, label);
            if (labelCmp != null)
            {
                var fontSizeProp = labelCmp.GetType().GetProperty("fontSize");
                if (fontSizeProp != null) fontSizeProp.SetValue(labelCmp, (float)_config.bodyFontSize);
            }

            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }

        private static void CreateLabelAndAssign(GameObject parent, string name, string text, object controller, string fieldName)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var cmp = AddTextComponent(go, text);
            SetField(controller, fieldName, cmp);
        }

        private static void CreateLegacyTextAndAssign(GameObject parent, string name, string text, object controller, string fieldName)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.color = Color.white;
            SetField(controller, fieldName, t);
        }

        private static Component CreateDropdown(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            // Try to add TMP_Dropdown, fall back to legacy Dropdown.
            // TMP_Dropdown does NOT inherit from UI.Dropdown, so we return Component.
            EnsureTMProTypes();
            Component cmp;
            if (_tmpDropdownType != null)
                cmp = (Component)go.AddComponent(_tmpDropdownType);
            else
                cmp = go.AddComponent<Dropdown>();

            // Add a label child
            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(go.transform, false);
            AddTextComponent(label, "Option");

            return cmp;
        }

        private static Toggle CreateToggle(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bg;

            // Checkmark child
            var check = new GameObject("Checkmark", typeof(RectTransform));
            check.transform.SetParent(go.transform, false);
            var checkImage = check.AddComponent<Image>();
            checkImage.color = new Color(0.2f, 0.8f, 0.4f, 1f);
            toggle.graphic = checkImage;
            toggle.isOn = true;

            return toggle;
        }

        private static Slider CreateSlider(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var slider = go.AddComponent<Slider>();
            slider.targetGraphic = bg;

            // Fill area
            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(go.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.8f, 0.4f, 1f);
            slider.fillRect = fill.GetComponent<RectTransform>();

            return slider;
        }

        private static GameObject CreateEmptyChild(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

        private static void Save(GameObject root, string fileName)
        {
            SetLayerRecursively(root, 5); // UI Layer
            string path = $"{_currentPrefabRoot}/{fileName}";

            var dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            // Connect to prefab in scene
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
            if (prefabAsset == null)
            {
                Debug.LogWarning($"[UIPrefabGenerator] Failed to save prefab: {path}");
            }
        }
    }
}


