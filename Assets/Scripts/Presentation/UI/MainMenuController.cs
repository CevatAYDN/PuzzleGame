using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Models;
using VContainer;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Main menu screen — entry point after onboarding/boot.
    /// Shows coin balance, daily streak, and entry buttons (Play, Daily, Settings, Privacy, Sound).
    /// When "Play" is clicked, publishes a ShowWorldMapRequestEvent that LevelSelectUI listens for.
    /// SRP: only owns main-menu UI state and navigation. Sub-panels are separate components.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private const string LogTag = "[MainMenu]";
        private const float SubPanelFadeDuration = 0.25f;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button dailyChallengeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button privacyButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private Button quitButton;

        [Header("Coin/Streak Display")]
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private GameObject dailyChallengeBadge;

        [Header("Sub-Panels (managed visibility)")]
        [SerializeField] private GameObject worldMapPanel;
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private GameObject dailyChallengePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject soundPanel;

        [Header("Transition (optional CanvasGroup for fade)")]
        [SerializeField] private CanvasGroup rootCanvasGroup;

        private IEventAggregator _events;
        private ICoinWallet _coinWallet;
        private IStreakService _streak;
        private IDailyChallengeService _daily;
        private ITweenService _tween;

        [Inject]
        public void Construct(
            IEventAggregator events,
            ICoinWallet coinWallet,
            IStreakService streak,
            IDailyChallengeService daily,
            ITweenService tween)
        {
            _events = events;
            _coinWallet = coinWallet;
            _streak = streak;
            _daily = daily;
            _tween = tween;
        }

        private void OnEnable()
        {
            if (_events != null)
            {
                _events.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
                _events.Subscribe<ShowWorldMapRequestEvent>(OnShowWorldMap);
                _events.Subscribe<ShowLevelSelectRequestEvent>(OnShowLevelSelect);
                _events.Subscribe<HideLevelSelectRequestEvent>(OnHideLevelSelect);
                _events.Subscribe<HideWorldMapRequestEvent>(OnHideWorldMap);
                _events.Subscribe<ShowDailyChallengeRequestEvent>(OnShowDailyChallenge);
                _events.Subscribe<HideDailyChallengeRequestEvent>(OnHideDailyChallenge);
                _events.Subscribe<ShowSoundPanelRequestEvent>(OnShowSoundPanel);
                _events.Subscribe<HideSoundPanelRequestEvent>(OnHideSoundPanel);
            }
            if (_coinWallet != null) _coinWallet.OnBalanceChanged += OnCoinBalanceChanged;
        }

        private void OnDisable()
        {
            if (_events != null)
            {
                _events.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
                _events.Unsubscribe<ShowWorldMapRequestEvent>(OnShowWorldMap);
                _events.Unsubscribe<ShowLevelSelectRequestEvent>(OnShowLevelSelect);
                _events.Unsubscribe<HideLevelSelectRequestEvent>(OnHideLevelSelect);
                _events.Unsubscribe<HideWorldMapRequestEvent>(OnHideWorldMap);
                _events.Unsubscribe<ShowDailyChallengeRequestEvent>(OnShowDailyChallenge);
                _events.Unsubscribe<HideDailyChallengeRequestEvent>(OnHideDailyChallenge);
                _events.Unsubscribe<ShowSoundPanelRequestEvent>(OnShowSoundPanel);
                _events.Unsubscribe<HideSoundPanelRequestEvent>(OnHideSoundPanel);
            }
            if (_coinWallet != null) _coinWallet.OnBalanceChanged -= OnCoinBalanceChanged;
        }

        private void Start()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (dailyChallengeButton != null) dailyChallengeButton.onClick.AddListener(OnDailyClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (privacyButton != null) privacyButton.onClick.AddListener(OnPrivacyClicked);
            if (soundButton != null) soundButton.onClick.AddListener(OnSoundClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

            RefreshDisplays();
            HideAllSubPanels();
            SetRootAlpha(1f); // visible by default; fade-in only on first Menu transition
        }

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            bool shouldShow = e.Current == GameState.Menu;
            if (shouldShow)
            {
                gameObject.SetActive(true);
                RefreshDisplays();
                HideAllSubPanels();
                FadeRoot(toAlpha: 1f);
            }
            else
            {
                FadeRoot(toAlpha: 0f, onComplete: () => gameObject.SetActive(false));
            }
        }

        private void OnShowWorldMap(ShowWorldMapRequestEvent e)
        {
            if (worldMapPanel != null) worldMapPanel.SetActive(true);
            FadeRoot(0f);
        }

        private void OnHideWorldMap(HideWorldMapRequestEvent e)
        {
            if (worldMapPanel != null) worldMapPanel.SetActive(false);
            FadeRoot(1f);
        }

        private void OnShowLevelSelect(ShowLevelSelectRequestEvent e)
        {
            if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
            FadeRoot(0f);
        }

        private void OnHideLevelSelect(HideLevelSelectRequestEvent e)
        {
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
            FadeRoot(1f);
        }

        private void OnShowDailyChallenge(ShowDailyChallengeRequestEvent e)
        {
            if (dailyChallengePanel != null) dailyChallengePanel.SetActive(true);
            FadeRoot(0f);
        }

        private void OnHideDailyChallenge(HideDailyChallengeRequestEvent e)
        {
            if (dailyChallengePanel != null) dailyChallengePanel.SetActive(false);
            FadeRoot(1f);
        }

        private void OnShowSoundPanel(ShowSoundPanelRequestEvent e)
        {
            if (soundPanel != null) soundPanel.SetActive(true);
            FadeRoot(0f);
        }

        private void OnHideSoundPanel(HideSoundPanelRequestEvent e)
        {
            if (soundPanel != null) soundPanel.SetActive(false);
            FadeRoot(1f);
        }

        private void OnPlayClicked()
        {
            MoldLogger.LogInfo($"{LogTag} Play clicked — showing world map.");
            _events?.Publish(new ShowWorldMapRequestEvent());
        }

        private void OnDailyClicked()
        {
            MoldLogger.LogInfo($"{LogTag} Daily challenge clicked.");
            _events?.Publish(new ShowDailyChallengeRequestEvent());
        }

        private void OnSettingsClicked()
        {
            MoldLogger.LogInfo($"{LogTag} Settings clicked.");
            if (settingsPanel != null) settingsPanel.SetActive(true);
            FadeRoot(0f);
        }

        private void OnPrivacyClicked()
        {
            MoldLogger.LogInfo($"{LogTag} Privacy clicked.");
            if (settingsPanel != null) settingsPanel.SetActive(true);
            FadeRoot(0f);
        }

        private void OnSoundClicked()
        {
            MoldLogger.LogInfo($"{LogTag} Sound clicked.");
            _events?.Publish(new ShowSoundPanelRequestEvent());
        }

        private void OnQuitClicked()
        {
            MoldLogger.LogInfo($"{LogTag} Quit clicked.");
            UnityEngine.Application.Quit();
        }

        private void RefreshDisplays()
        {
            UpdateCoinDisplay();
            UpdateStreakDisplay();
            UpdateDailyBadge();
        }

        private void UpdateCoinDisplay()
        {
            if (coinText == null || _coinWallet == null) return;
            coinText.text = _coinWallet.Balance.ToString();
        }

        private void UpdateStreakDisplay()
        {
            if (streakText == null || _streak == null) return;
            streakText.text = _streak.CurrentStreak.ToString();
        }

        private void UpdateDailyBadge()
        {
            if (dailyChallengeBadge == null || _daily == null) return;
            var state = _daily.GetTodayChallenge();
            dailyChallengeBadge.SetActive(state.HasChallenge && !state.Completed);
        }

        private void OnCoinBalanceChanged(int newBalance) => UpdateCoinDisplay();

        private void HideAllSubPanels()
        {
            if (worldMapPanel != null) worldMapPanel.SetActive(false);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
            if (dailyChallengePanel != null) dailyChallengePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (soundPanel != null) soundPanel.SetActive(false);
        }

        // ──────────────────────────────────────────────
        //  Fade helpers — use ITweenService if available, else snap.
        // ──────────────────────────────────────────────

        private void SetRootAlpha(float alpha)
        {
            if (rootCanvasGroup == null) return;
            rootCanvasGroup.alpha = alpha;
        }

        private void FadeRoot(float toAlpha, Action onComplete = null)
        {
            if (rootCanvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }
            if (_tween == null)
            {
                rootCanvasGroup.alpha = toAlpha;
                onComplete?.Invoke();
                return;
            }
            float from = rootCanvasGroup.alpha;
            if (Mathf.Approximately(from, toAlpha))
            {
                onComplete?.Invoke();
                return;
            }
            _tween.TweenCustom(rootCanvasGroup, from, toAlpha, SubPanelFadeDuration, (target, t) =>
            {
                if (target is CanvasGroup cg) cg.alpha = Mathf.Lerp(from, toAlpha, t);
            }).OnComplete(() => onComplete?.Invoke());
        }
    }

    /// <summary>
    /// Published by MainMenuController when "Play" is clicked.
    /// WorldMapController subscribes to show itself (then user picks a biome).
    /// Decouples main menu from world map.
    /// </summary>
    public class ShowWorldMapRequestEvent { }

    /// <summary>
    /// Published by WorldMapController when a biome card is clicked.
    /// LevelSelectUI subscribes to show itself, filtered to the selected biome.
    /// </summary>
    public class ShowLevelSelectRequestEvent
    {
        public Biome BiomeFilter { get; set; } = Biome.CrystalMines;
    }

    /// <summary>
    /// Published by LevelSelectUI when "Back" is clicked.
    /// MainMenuController subscribes to reactivate its root panel.
    /// </summary>
    public class HideLevelSelectRequestEvent { }

    /// <summary>
    /// Published by MainMenuController when "Sound" is clicked.
    /// SettingsSoundController subscribes to show itself.
    /// </summary>
    public class ShowSoundPanelRequestEvent { }
}
