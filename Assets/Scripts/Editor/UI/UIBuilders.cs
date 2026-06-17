using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Presentation.UI;

namespace PuzzleGame.Editor.UI
{
    public class MainMenuBuilder : UIBuilderBase
    {
        protected override void BuildCore()
        {
            var root = CreateRoot("MainMenuController", out var content);
            var ctrl = root.AddComponent<MainMenuController>();

            var headerPanel = new GameObject("HeaderPanel", typeof(RectTransform));
            headerPanel.transform.SetParent(content.transform, false);
            var hLayout = headerPanel.AddComponent<HorizontalLayoutGroup>();
            hLayout.childControlWidth = true; hLayout.childControlHeight = true;
            hLayout.spacing = 400; hLayout.childAlignment = TextAnchor.UpperCenter;
            
            var headerElement = headerPanel.AddComponent<LayoutElement>();
            headerElement.minHeight = 100; headerElement.minWidth = 1000;

            CreateLabelAndAssign(headerPanel, "CoinText", "ui_label_coins", ctrl, "coinText");
            CreateLabelAndAssign(headerPanel, "GemText", "ui_label_gems", ctrl, "streakText");

            CreateSpacer(content, 0.5f);

            var titleSpace = new GameObject("TitleSpace", typeof(RectTransform));
            titleSpace.transform.SetParent(content.transform, false);
            var titleLe = titleSpace.AddComponent<LayoutElement>();
            titleLe.minHeight = 250; titleLe.minWidth = 1000;
            
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(titleSpace.transform, false);
            var title = AddTextComponent(titleGo, "ui_game_title", 120);
            var colorProp = title.GetType().GetProperty("color");
            if (colorProp != null) colorProp.SetValue(title, Context.Config.colorPrimary);
            
            var subtitleGo = new GameObject("Subtitle", typeof(RectTransform));
            subtitleGo.transform.SetParent(titleSpace.transform, false);
            var subtitle = AddTextComponent(subtitleGo, "ui_game_subtitle", 40);
            if (colorProp != null) colorProp.SetValue(subtitle, Color.gray);

            CreateSpacer(content, 0.5f);

            SetField(ctrl, "playButton", CreateButton(content, "PlayButton", "ui_btn_play", Context.Config.colorPrimary));
            SetField(ctrl, "dailyChallengeButton", CreateButton(content, "DailyChallengeButton", "ui_btn_daily_challenge", Context.Config.colorSecondary));
            SetField(ctrl, "settingsButton", CreateButton(content, "SettingsButton", "ui_btn_settings", Context.Config.colorPanel));
            SetField(ctrl, "privacyButton", CreateButton(content, "PrivacyButton", "ui_btn_privacy", Context.Config.colorPanel));
            SetField(ctrl, "soundButton", CreateButton(content, "SoundButton", "ui_btn_sound", Context.Config.colorPanel));

            CreateSpacer(content, 1f);

            var streakPanel = new GameObject("StreakPanel", typeof(RectTransform));
            streakPanel.transform.SetParent(content.transform, false);
            var sBg = streakPanel.AddComponent<Image>();
            sBg.color = Context.Config.colorPanel;
            if (Context.Config.buttonSprite != null) { sBg.sprite = Context.Config.buttonSprite; sBg.type = Image.Type.Sliced; }
            var sLayout = streakPanel.AddComponent<VerticalLayoutGroup>();
            sLayout.padding = new RectOffset(40, 40, 40, 40);
            sLayout.childAlignment = TextAnchor.MiddleCenter; sLayout.spacing = 20;
            var streakLe = streakPanel.AddComponent<LayoutElement>();
            streakLe.minHeight = 250; streakLe.minWidth = 900;

            AddTextComponent(CreateEmptyChild(streakPanel, "StreakTitle"), "ui_label_daily_streak", 40);
            
            var streakCircles = new GameObject("Circles", typeof(RectTransform));
            streakCircles.transform.SetParent(streakPanel.transform, false);
            var scLayout = streakCircles.AddComponent<HorizontalLayoutGroup>();
            scLayout.spacing = 20; scLayout.childAlignment = TextAnchor.MiddleCenter;
            for(int i=1; i<=7; i++) {
                var c = new GameObject("Day"+i, typeof(RectTransform));
                c.transform.SetParent(streakCircles.transform, false);
                var crect = c.AddComponent<LayoutElement>();
                crect.minWidth = 80; crect.minHeight = 80;
                var cImg = c.AddComponent<Image>();
                cImg.color = i <= 4 ? Context.Config.colorPrimary : Context.Config.colorBackground;
                if (Context.Config.buttonSprite != null) { cImg.sprite = Context.Config.buttonSprite; cImg.type = Image.Type.Sliced; }
            }

            CreateSpacer(content, 0.5f);

            var quitButtonRoot = new GameObject("HiddenQuit");
            quitButtonRoot.transform.SetParent(content.transform, false);
            quitButtonRoot.SetActive(false);
            SetField(ctrl, "quitButton", quitButtonRoot.AddComponent<Button>());

            var badgeGo = new GameObject("DailyChallengeBadge", typeof(RectTransform));
            badgeGo.transform.SetParent(content.transform, false);
            SetField(ctrl, "dailyChallengeBadge", badgeGo);
            badgeGo.SetActive(false);

            SetField(ctrl, "worldMapPanel", CreateEmptyChild(root, "WorldMapPanel"));
            SetField(ctrl, "levelSelectPanel", CreateEmptyChild(root, "LevelSelectPanel"));
            SetField(ctrl, "dailyChallengePanel", CreateEmptyChild(root, "DailyChallengePanel"));
            SetField(ctrl, "settingsPanel", CreateEmptyChild(root, "SettingsPanel"));
            SetField(ctrl, "soundPanel", CreateEmptyChild(root, "SoundPanel"));
            SetField(ctrl, "rootCanvasGroup", root.GetComponent<CanvasGroup>());
            
            Save(root, "MainMenuController.prefab");
        }
    }

    public class WorldMapBuilder : UIBuilderBase
    {
        protected override void BuildCore()
        {
            var root = CreateRoot("WorldMapController");
            root.AddComponent<Canvas>(); // Isolate from parent CanvasGroup alpha
            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var ctrl = root.AddComponent<WorldMapController>();

            SetField(ctrl, "backButton", CreateButton(root, "BackButton", "ui_btn_back"));

            var crystal = BuildBiomeCard(root, "CrystalMinesCard");
            var volcanic = BuildBiomeCard(root, "VolcanicForgeCard");

            SetField(ctrl, "crystalMinesCard", crystal);
            SetField(ctrl, "volcanicForgeCard", volcanic);

            Save(root, "WorldMapController.prefab");
        }

        private BiomeCardView BuildBiomeCard(GameObject root, string cardName)
        {
            var card = CreateEmptyChild(root, cardName);
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.sizeDelta = new Vector2(400, 300);
            rect.anchoredPosition = new Vector2(100, 0);

            var bg = card.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.25f, 0.3f, 1f);

            var btn = card.AddComponent<Button>();
            btn.targetGraphic = bg;

            var nameGo = new GameObject("BiomeName", typeof(RectTransform));
            nameGo.transform.SetParent(card.transform, false);
            var nameText = AddTextComponent(nameGo, "biome_name", 36);

            var progressGo = new GameObject("ProgressText", typeof(RectTransform));
            progressGo.transform.SetParent(card.transform, false);
            var progressText = AddTextComponent(progressGo, "0/0", 24);

            var starGo = new GameObject("StarProgress", typeof(RectTransform));
            starGo.transform.SetParent(card.transform, false);
            var starText = AddTextComponent(starGo, "\u2605 0/0", 24);

            var badge = CreateEmptyChild(card, "CompletedBadge");
            badge.SetActive(false);

            var view = card.GetComponent<BiomeCardView>();
            if (view == null) view = card.AddComponent<BiomeCardView>();

            // Assign view's serialized fields via reflection
            SetField(view, "biomeNameText", nameText);
            SetField(view, "progressText", progressText);
            SetField(view, "starProgressText", starText);
            SetField(view, "backgroundImage", bg);
            SetField(view, "completedBadge", badge);
            SetField(view, "button", btn);

            return view;
        }
    }

    public class SettingsBuilder : UIBuilderBase
    {
        protected override void BuildCore()
        {
            var root = CreateRoot("SettingsController");
            root.AddComponent<Canvas>(); // Isolate from parent CanvasGroup alpha
            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var ctrl = root.AddComponent<SettingsController>();

            SetField(ctrl, "_languageDropdown", CreateDropdown(root, "LanguageDropdown"));
            CreateLabelAndAssign(root, "LanguageLabel", "ui_label_language", ctrl, "_languageLabel");
            SetField(ctrl, "_hapticToggle", CreateToggle(root, "HapticToggle"));
            CreateLabelAndAssign(root, "HapticLabel", "ui_label_haptic", ctrl, "_hapticLabel");
            SetField(ctrl, "_soundButton", CreateButton(root, "SoundButton", "ui_btn_sound"));
            SetField(ctrl, "_privacyButton", CreateButton(root, "PrivacyButton", "ui_btn_privacy"));
            SetField(ctrl, "_backButton", CreateButton(root, "BackButton", "ui_btn_back"));
            SetField(ctrl, "_soundPanel", CreateEmptyChild(root, "SoundPanel"));
            SetField(ctrl, "_privacyPanel", CreateEmptyChild(root, "PrivacyPanel"));

            Save(root, "SettingsController.prefab");
        }
    }

    public class SettingsPrivacyBuilder : UIBuilderBase
    {
        protected override void BuildCore()
        {
            var root = CreateRoot("SettingsPrivacyController");
            root.AddComponent<Canvas>(); // Isolate from parent CanvasGroup alpha
            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var ctrl = root.AddComponent<SettingsPrivacyController>();

            SetField(ctrl, "_analyticsToggle", CreateToggle(root, "AnalyticsToggle"));
            SetField(ctrl, "_personalizedAdsToggle", CreateToggle(root, "PersonalizedAdsToggle"));
            SetField(ctrl, "_resetConsentButton", CreateButton(root, "ResetConsentButton", "ui_btn_reset_consent"));
            SetField(ctrl, "_deleteDataButton", CreateButton(root, "DeleteDataButton", "ui_btn_delete_data"));
            SetField(ctrl, "_privacyPolicyButton", CreateButton(root, "PrivacyPolicyButton", "ui_btn_privacy_policy"));
            SetField(ctrl, "_termsButton", CreateButton(root, "TermsButton", "ui_btn_terms"));
            SetField(ctrl, "_ageVerifyButton", CreateButton(root, "AgeVerifyButton", "ui_btn_age_verify"));
            SetField(ctrl, "_confirmPanel", CreateEmptyChild(root, "ConfirmPanel"));
            CreateLegacyTextAndAssign(root, "ConfirmMessage", "ui_msg_confirm", ctrl, "_confirmMessage");
            SetField(ctrl, "_confirmYesButton", CreateButton(root, "ConfirmYesButton", "ui_btn_yes"));
            SetField(ctrl, "_confirmNoButton", CreateButton(root, "ConfirmNoButton", "ui_btn_no"));
            SetField(ctrl, "_backButton", CreateButton(root, "BackButton", "ui_btn_back"));

            Save(root, "SettingsPrivacyController.prefab");
        }
    }

    public class DailyChallengeBuilder : UIBuilderBase
    {
        protected override void BuildCore()
        {
            var root = CreateRoot("DailyChallengeController");
            root.AddComponent<Canvas>(); // Isolate from parent CanvasGroup alpha
            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var ctrl = root.AddComponent<DailyChallengeController>();

            CreateLabelAndAssign(root, "TitleText", "ui_title_daily_challenge", ctrl, "titleText");
            CreateLabelAndAssign(root, "StreakCountText", "0", ctrl, "streakCountText");
            CreateLabelAndAssign(root, "LongestStreakText", "0", ctrl, "longestStreakText");
            CreateLabelAndAssign(root, "CountdownText", "--:--:--", ctrl, "countdownText");
            CreateLabelAndAssign(root, "StatusText", "ui_status_ready", ctrl, "statusText");
            SetField(ctrl, "completedBadge", CreateEmptyChild(root, "CompletedBadge"));
            SetField(ctrl, "claimableBadge", CreateEmptyChild(root, "ClaimableBadge"));
            SetField(ctrl, "playButton", CreateButton(root, "PlayButton", "ui_btn_play"));
            SetField(ctrl, "backButton", CreateButton(root, "BackButton", "ui_btn_back"));

            Save(root, "DailyChallengeController.prefab");
        }
    }

    public class SettingsSoundBuilder : UIBuilderBase
    {
        protected override void BuildCore()
        {
            var root = CreateRoot("SettingsSoundController");
            root.AddComponent<Canvas>(); // Isolate from parent CanvasGroup alpha
            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var ctrl = root.AddComponent<SettingsSoundController>();

            SetField(ctrl, "_musicToggle", CreateToggle(root, "MusicToggle"));
            SetField(ctrl, "_sfxToggle", CreateToggle(root, "SfxToggle"));
            SetField(ctrl, "_musicVolumeSlider", CreateSlider(root, "MusicVolumeSlider"));
            SetField(ctrl, "_sfxVolumeSlider", CreateSlider(root, "SfxVolumeSlider"));
            SetField(ctrl, "_backButton", CreateButton(root, "BackButton", "ui_btn_back"));

            Save(root, "SettingsSoundController.prefab");
        }
    }

    public class LevelSelectUIBuilder : UIBuilderBase
    {
        protected override void BuildCore()
        {
            var root = CreateRoot("LevelSelectController");
            root.AddComponent<Canvas>(); // Isolate from parent CanvasGroup alpha
            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var ctrl = root.AddComponent<LevelSelectUI>();

            var scrollGo = CreateEmptyChild(root, "ScrollArea");
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var viewport = CreateEmptyChild(scrollGo, "Viewport");
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewport.AddComponent<Image>(); // Viewport requires Image for masking
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scrollRect.viewport = viewportRect;

            var content = CreateEmptyChild(viewport, "Content");
            var contentRect = content.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;

            var buttonTemplate = CreateEmptyChild(content, "LevelButton_Template");
            buttonTemplate.AddComponent<LayoutElement>().minHeight = 100;
            buttonTemplate.SetActive(false);
            var btnBg = buttonTemplate.AddComponent<Image>();
            btnBg.color = new Color(0.2f, 0.3f, 0.4f, 1f);
            var btnButton = buttonTemplate.AddComponent<Button>();
            btnButton.targetGraphic = btnBg;
            var btnLabel = new GameObject("Label", typeof(RectTransform));
            btnLabel.transform.SetParent(buttonTemplate.transform, false);
            AddTextComponent(btnLabel, "1", 36);

            SetField(ctrl, "backButton", CreateButton(root, "BackButton", "ui_btn_back"));
            SetField(ctrl, "buttonContainer", content.transform);
            SetField(ctrl, "contentRect", contentRect);
            SetField(ctrl, "scrollRect", scrollRect);
            SetField(ctrl, "levelButtonPrefab", buttonTemplate);
            SetField(ctrl, "columns", 3);
            SetField(ctrl, "buttonWidth", 100f);
            SetField(ctrl, "buttonHeight", 100f);
            SetField(ctrl, "spacing", 10f);

            Save(root, "LevelSelectController.prefab");
        }
    }

    public class ShopBuilder : UIBuilderBase
    {
        protected override void BuildCore()
        {
            var root = CreateRoot("ShopUI");
            root.AddComponent<Canvas>(); // Isolate from parent CanvasGroup alpha
            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var shop = root.AddComponent<ShopUI>();

            CreateLabelAndAssign(root, "CoinBalanceText", "0", shop, "coinBalanceText");
            SetField(shop, "closeButton", CreateButton(root, "CloseButton", "ui_btn_close"));

            var container = CreateEmptyChild(root, "ItemContainer");
            SetField(shop, "itemContainer", container.GetComponent<RectTransform>());
            var scroll = root.AddComponent<ScrollRect>();
            scroll.content = (RectTransform)shop.GetType().GetField("itemContainer",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).GetValue(shop);
            SetField(shop, "scrollRect", scroll);

            var itemPrefab = CreateButton(root, "ShopItemPrefab_Template", "ui_item");
            itemPrefab.gameObject.SetActive(false);
            SetField(shop, "shopItemPrefab", (GameObject)itemPrefab.gameObject);

            var detail = CreateEmptyChild(root, "DetailPanel");
            SetField(shop, "detailPanel", detail);
            CreateLabelAndAssign(detail, "DetailNameText", "ui_item_name", shop, "detailNameText");
            CreateLabelAndAssign(detail, "DetailDescriptionText", "ui_item_desc", shop, "detailDescriptionText");
            CreateLabelAndAssign(detail, "DetailPriceText", "0", shop, "detailPriceText");
            SetField(shop, "purchaseButton", CreateButton(detail, "PurchaseButton", "ui_btn_buy"));
            SetField(shop, "equipButton", CreateButton(detail, "EquipButton", "ui_btn_equip"));
            SetField(shop, "detailPreviewImage", detail.AddComponent<Image>());

            Save(root, "ShopUI.prefab");
        }
    }
}
