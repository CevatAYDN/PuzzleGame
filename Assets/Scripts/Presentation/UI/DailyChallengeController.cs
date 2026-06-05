using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using VContainer;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Daily Challenge entry screen — reached from main menu's "Daily" button.
    /// Shows today's challenge status, current streak, longest streak, and UTC-midnight countdown.
    /// "Play" navigates into the daily challenge level (placeholder: emits event for GameManager to consume).
    /// "Back" returns to main menu.
    /// SRP: only owns daily challenge UI state and countdown rendering.
    /// </summary>
    public class DailyChallengeController : MonoBehaviour
    {
        private const string LogTag = "[DailyChallenge]";

        [Header("Display")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI streakCountText;
        [SerializeField] private TextMeshProUGUI longestStreakText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject completedBadge;
        [SerializeField] private GameObject claimableBadge;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button backButton;

        private IEventAggregator _events;
        private IDailyChallengeService _daily;
        private IStreakService _streak;

        [Inject]
        public void Construct(IEventAggregator events, IDailyChallengeService daily, IStreakService streak)
        {
            _events = events;
            _daily = daily;
            _streak = streak;
        }

        private void OnEnable()
        {
            if (_events != null) _events.Subscribe<ShowDailyChallengeRequestEvent>(OnShow);
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnDisable()
        {
            if (_events != null) _events.Unsubscribe<ShowDailyChallengeRequestEvent>(OnShow);
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy || countdownText == null) return;
            var remaining = DailyChallengeCountdown.GetTimeUntilNextReset();
            countdownText.text = DailyChallengeCountdown.FormatCountdown(remaining);
        }

        private void OnShow(ShowDailyChallengeRequestEvent e)
        {
            MoldLogger.LogInfo($"{LogTag} Showing daily challenge panel.");
            gameObject.SetActive(true);
            RefreshUI();
        }

        private void OnBackClicked()
        {
            MoldLogger.LogInfo($"{LogTag} Back clicked — returning to main menu.");
            gameObject.SetActive(false);
            _events?.Publish(new HideDailyChallengeRequestEvent());
        }

        private void OnPlayClicked()
        {
            var state = _daily?.GetTodayChallenge() ?? default;
            if (!state.HasChallenge || state.Completed)
            {
                MoldLogger.LogWarning($"{LogTag} Play clicked but challenge not available or already completed.");
                return;
            }
            MoldLogger.LogInfo($"{LogTag} Play clicked — entering daily challenge (seed={state.Seed}).");
            _events?.Publish(new DailyChallengeStartedEvent { Seed = state.Seed });
            gameObject.SetActive(false);
        }

        private void RefreshUI()
        {
            var state = _daily?.GetTodayChallenge() ?? default;

            if (titleText != null) titleText.text = "Daily Challenge";
            if (streakCountText != null) streakCountText.text = _streak != null ? _streak.CurrentStreak.ToString() : "0";
            if (longestStreakText != null) longestStreakText.text = _streak != null ? _streak.LongestStreak.ToString() : "0";

            if (statusText != null)
            {
                statusText.text = !state.HasChallenge
                    ? "Loading..."
                    : state.Completed
                        ? "Completed — see you tomorrow!"
                        : "Today's challenge awaits";
            }
            if (completedBadge != null) completedBadge.SetActive(state.Completed);
            if (claimableBadge != null) claimableBadge.SetActive(state.HasChallenge && !state.Completed);
            if (playButton != null) playButton.interactable = state.HasChallenge && !state.Completed;
        }
    }

    /// <summary>
    /// Published by MainMenuController when "Daily Challenge" is clicked.
    /// DailyChallengeController subscribes to navigate to the daily challenge screen.
    /// </summary>
    public class ShowDailyChallengeRequestEvent { }

    /// <summary>
    /// Published by DailyChallengeController when "Back" is clicked.
    /// MainMenuController subscribes to reactivate its root panel.
    /// </summary>
    public class HideDailyChallengeRequestEvent { }

    /// <summary>
    /// Published by DailyChallengeController when "Play" is clicked.
    /// GameManager/LevelFlowController subscribes to load the daily challenge level with the given seed.
    /// </summary>
    public class DailyChallengeStartedEvent
    {
        public int Seed { get; set; }
    }
}
