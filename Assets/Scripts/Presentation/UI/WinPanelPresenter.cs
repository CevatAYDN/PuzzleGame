using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Events;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Handles the Level Completion screen logic exclusively.
    /// Pushes focus to 'Next Level' button for controller navigation.
    /// </summary>
    public class WinPanelPresenter : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject _winPanelRoot;
        [SerializeField] private TextMeshProUGUI _winMoveCountText;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _replayButton;
        [SerializeField] private Button _mainMenuButton;
        
        [Header("Stars")]
        [SerializeField] private Image[] _starImages = new Image[3];
        [SerializeField] private Sprite _starOn;
        [SerializeField] private Sprite _starOff;

        [Header("Progression")]
        [SerializeField] private TextMeshProUGUI _winXpGainedText;
        [SerializeField] private TextMeshProUGUI _winLeaderboardText;
        [SerializeField] private TextMeshProUGUI _winSeasonXpText;
        [SerializeField] private Slider _winSeasonSlider;

        private IEventAggregator _events;
        private ILocalizationService _localization;
        private ILevelRepository _repository;
        private ILeaderboardService _leaderboard;
        private IProgressService _progressSeason;
        private IGameStateMachine _stateMachine;
        
        private LevelData _currentLevel;
        private readonly StringBuilder _sb = new StringBuilder(64);

        [VContainer.Inject]
        public void Construct(
            IEventAggregator events,
            ILocalizationService localization,
            ILevelRepository repository,
            ILeaderboardService leaderboard,
            IProgressService progressSeason,
            IGameStateMachine stateMachine)
        {
            _events = events;
            _localization = localization;
            _repository = repository;
            _leaderboard = leaderboard;
            _progressSeason = progressSeason;
            _stateMachine = stateMachine;
        }

        private void OnEnable()
        {
            if (_events != null)
            {
                _events.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
                _events.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
            }
            
            if (_nextLevelButton != null) _nextLevelButton.onClick.AddListener(OnNextLevel);
            if (_replayButton != null) _replayButton.onClick.AddListener(OnReplay);
            if (_mainMenuButton != null) _mainMenuButton.onClick.AddListener(OnMainMenu);
        }

        private void OnDisable()
        {
            if (_events != null)
            {
                _events.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
                _events.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);
            }
        }

        private void OnLevelLoaded(LevelLoadedEvent e)
        {
            _currentLevel = e.Level;
            if (_winPanelRoot != null) _winPanelRoot.SetActive(false);
        }

        private void OnLevelCompleted(LevelCompletedEvent e)
        {
            if (_winPanelRoot != null) _winPanelRoot.SetActive(true);
            
            // Controller Focus
            if (_nextLevelButton != null)
            {
                UINavigationManager.Instance.PushFocus(_nextLevelButton.gameObject);
            }

            if (_winMoveCountText != null)
            {
                string movesLabel = _localization != null ? _localization.GetString("moves_text") : "Moves";
                _sb.Clear();
                _sb.Append(movesLabel).Append(": ").Append(e.MoveCount);
                _winMoveCountText.SetText(_sb);
            }

            UpdateStars(e.MoveCount);
            UpdateProgression();
        }

        private void UpdateStars(int moveCount)
        {
            if (_starImages == null || _currentLevel == null) return;
            int stars = _currentLevel.CalculateStars(moveCount);
            for (int i = 0; i < _starImages.Length; i++)
            {
                if (_starImages[i] == null) continue;
                _starImages[i].sprite = i < stars ? _starOn : _starOff;
                _starImages[i].color = i < stars ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
        }

        private void UpdateProgression()
        {
            if (_currentLevel == null) return;
            int levelNum = _currentLevel.levelNumber;

            // XP gained
            int xpGained = (_progressSeason?.TotalXp ?? 0);
            if (_progressSeason != null)
            {
                int beforeXp = xpGained > 50 ? xpGained - 50 : 0;
                int xpDelta = xpGained - beforeXp;
                if (_winXpGainedText != null) _winXpGainedText.text = $"+{xpDelta} XP";
            }

            // Leaderboard
            if (_leaderboard != null && _winLeaderboardText != null)
            {
                var entry = _leaderboard.GetEntry(levelNum);
                if (entry != null)
                {
                    string scoreLabel = _localization?.GetString("best_score_label") ?? "Best";
                    _sb.Clear();
                    _sb.Append(scoreLabel).Append(": ").Append(entry.BestScore);
                    _winLeaderboardText.SetText(_sb);
                }
                else
                {
                    _winLeaderboardText.text = "";
                }
            }

            // Season progress
            if (_progressSeason != null && _winSeasonXpText != null)
            {
                if (_progressSeason.IsSeasonActive)
                {
                    int seasonXp = _progressSeason.SeasonXp;
                    int nextTierXp = _progressSeason.SeasonXpToNextTier;
                    int tier = _progressSeason.CurrentTierIndex;

                    if (tier >= 0)
                    {
                        _winSeasonXpText.text = $"{_localization?.GetString("season_tier_label") ?? "Tier"} {tier + 1} | {seasonXp} XP";
                    }
                    else
                    {
                        _winSeasonXpText.text = $"{seasonXp} {_localization?.GetString("season_xp_label") ?? "Season XP"}";
                    }

                    if (_winSeasonSlider != null)
                    {
                        _winSeasonSlider.value = Mathf.Clamp01((float)seasonXp / Mathf.Max(1, seasonXp + nextTierXp));
                    }
                }
                else
                {
                    _winSeasonXpText.text = "";
                    if (_winSeasonSlider != null) _winSeasonSlider.value = 0f;
                }
            }
        }

        private void OnNextLevel()
        {
            UINavigationManager.Instance.PopFocus();
            if (_currentLevel == null) return;
            int next = _currentLevel.levelNumber + 1;
            if (_repository.GetByNumber(next) == null) next = 1;
            _events?.Publish(new LevelSelectedEvent(next));
        }

        private void OnReplay()
        {
            UINavigationManager.Instance.PopFocus();
            _events?.Publish(new LevelSelectedEvent(_currentLevel?.levelNumber ?? 1));
        }

        private void OnMainMenu()
        {
            UINavigationManager.Instance.PopFocus();
            _stateMachine?.TransitionTo(GameState.Menu);
        }
    }
}
