using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// HUD presenter — owns all in-game UI text/panel binding.
    /// Subscribes to <see cref="LevelCompletedEvent"/> for win panel,
    /// <see cref="GameStateChangedEvent"/> for pause overlay,
    /// and <see cref="IGameHistoryManager.OnMoveCountChanged"/> for move counter.
    /// </summary>
    public sealed class HudPresenter : MonoBehaviour, IDisposable
    {
        [Header("HUD")]
        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private TextMeshProUGUI moveCountText;
        [SerializeField] private TextMeshProUGUI bestMovesText;
        [SerializeField] private TextMeshProUGUI levelTitleText;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button hintButton;

        [Header("Panels")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject diErrorPanel;

        [Header("Star Display")]
        [SerializeField] private Image[] starImages = new Image[3];
        [SerializeField] private Sprite starOn;
        [SerializeField] private Sprite starOff;

        [Header("Win Panel")]
        [SerializeField] private TextMeshProUGUI winMoveCountText;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Win Panel - Progression")]
        [SerializeField] private TextMeshProUGUI winXpGainedText;
        [SerializeField] private TextMeshProUGUI winLeaderboardText;
        [SerializeField] private TextMeshProUGUI winSeasonXpText;
        [SerializeField] private Slider winSeasonSlider;

        [Header("Pause Panel")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseMainMenuButton;
        [SerializeField] private Button pauseRestartButton;

        [Header("Error Panel")]
        [SerializeField] private TextMeshProUGUI diErrorText;

        private IGameHistoryManager _history;
        private ILocalizationService _localization;
        private IEventAggregator _events;
        private ILevelRepository _repository;
        private ILevelProgressService _progress;
        private IGameStateMachine _stateMachine;
        private IUndoService _undoService;
        private IHintService _hintService;
        private IAnimationService _animationService;
        private ILeaderboardService _leaderboard;
        private IProgressService _progressSeason;
        private IActiveMoldsProvider _moldsProvider;
        private LevelData _currentLevel;

        private readonly StringBuilder _sb = new StringBuilder(64);
        // Fix #13: Cache last move count to avoid redundant SetText calls
        private int _lastMoveCount = -1;
        // Y20: also cache the level title + win move count so re-publishing the
        // same level-completed event does not regenerate TMP meshes.
        private int _lastLevelNumber = -1;
        private int _lastWinMoveCount = -1;

        [VContainer.Inject]
        public void Construct(
            IGameHistoryManager history,
            ILocalizationService localization,
            IEventAggregator events,
            ILevelRepository repository,
            ILevelProgressService progress,
            IGameStateMachine stateMachine,
            IUndoService undoService,
            IHintService hintService,
            IAnimationService animationService,
            ILeaderboardService leaderboard,
            IProgressService progressSeason,
            IActiveMoldsProvider moldsProvider)
        {
            _history = history;
            _localization = localization;
            _events = events;
            _repository = repository;
            _progress = progress;
            _stateMachine = stateMachine;
            _undoService = undoService;
            _hintService = hintService;
            _animationService = animationService;
            _leaderboard = leaderboard;
            _progressSeason = progressSeason;
            _moldsProvider = moldsProvider;
        }

        private void OnEnable()
        {
            if (_events == null) return;
            _events.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            _events.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
            _events.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            _events.Subscribe<HintHighlightEvent>(OnHintHighlight);
        }

        private void OnDisable()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_events == null) return;
            _events.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            _events.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);
            _events.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            _events.Unsubscribe<HintHighlightEvent>(OnHintHighlight);
            _events = null;
        }

        private void Start()
        {
            if (_history != null) _history.OnMoveCountChanged += UpdateMoveCount;
            HideAllPanels();
            UpdateMoveCount(0);

            if (nextLevelButton != null) nextLevelButton.onClick.AddListener(OnNextLevel);
            if (replayButton != null) replayButton.onClick.AddListener(OnReplay);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
            if (pauseMainMenuButton != null) pauseMainMenuButton.onClick.AddListener(OnMainMenu);
            if (pauseRestartButton != null) pauseRestartButton.onClick.AddListener(OnReplay);
            if (undoButton != null) undoButton.onClick.AddListener(OnUndoClicked);
            if (hintButton != null) hintButton.onClick.AddListener(OnHintClicked);
        }

        private void OnDestroy()
        {
            if (_history != null) _history.OnMoveCountChanged -= UpdateMoveCount;
        }

        public void ShowDIError(string message)
        {
            if (diErrorPanel != null) diErrorPanel.SetActive(true);
            if (diErrorText != null) diErrorText.text = message ?? "VContainer DI failed.";
        }

        public void SetCurrentLevel(LevelData level) => _currentLevel = level;

        private void OnLevelLoaded(LevelLoadedEvent e)
        {
            _currentLevel = e.Level;
            _lastMoveCount = -1; // reset cache on level load
            UpdateMoveCount(0);
            UpdateLevelTitle();
            HideAllPanels();
        }

        private void OnLevelCompleted(LevelCompletedEvent e)
        {
            if (winPanel != null) winPanel.SetActive(true);
            if (winMoveCountText != null && e.MoveCount != _lastWinMoveCount)
            {
                _lastWinMoveCount = e.MoveCount;
                string movesLabel = _localization != null ? _localization.GetString("moves_text") : "Moves";
                _sb.Clear();
                _sb.Append(movesLabel).Append(": ").Append(e.MoveCount);
                winMoveCountText.SetText(_sb);
            }
            UpdateStars();
            UpdateWinProgression();
        }

        private void UpdateWinProgression()
        {
            if (_currentLevel == null) return;

            int stars = _currentLevel.CalculateStars(_history?.CurrentMoveCount ?? 999);
            bool wasEfficient = stars >= 3;
            int levelNum = _currentLevel.levelNumber;

            // XP gained
            int xpGained = (_progressSeason?.TotalXp ?? 0);
            if (_progressSeason != null)
            {
                int beforeXp = xpGained > 50 ? xpGained - 50 : 0;
                int xpDelta = xpGained - beforeXp;
                if (winXpGainedText != null)
                    winXpGainedText.text = $"+{xpDelta} XP";
            }

            // Leaderboard
            if (_leaderboard != null && winLeaderboardText != null)
            {
                var entry = _leaderboard.GetEntry(levelNum);
                if (entry != null)
                {
                    string scoreLabel = _localization?.GetString("best_score_label") ?? "Best";
                    _sb.Clear();
                    _sb.Append(scoreLabel).Append(": ").Append(entry.BestScore);
                    winLeaderboardText.SetText(_sb);
                }
                else
                {
                    winLeaderboardText.text = "";
                }
            }

            // Season progress
            if (_progressSeason != null && winSeasonXpText != null)
            {
                if (_progressSeason.IsSeasonActive)
                {
                    int seasonXp = _progressSeason.SeasonXp;
                    int nextTierXp = _progressSeason.SeasonXpToNextTier;
                    int tier = _progressSeason.CurrentTierIndex;

                    if (tier >= 0)
                    {
                        winSeasonXpText.text = $"{_localization?.GetString("season_tier_label") ?? "Tier"} {tier + 1} | {seasonXp} XP";
                    }
                    else
                    {
                        winSeasonXpText.text = $"{seasonXp} {_localization?.GetString("season_xp_label") ?? "Season XP"}";
                    }

                    if (winSeasonSlider != null)
                    {
                        winSeasonSlider.value = Mathf.Clamp01((float)seasonXp / Mathf.Max(1, seasonXp + nextTierXp));
                    }
                }
                else
                {
                    winSeasonXpText.text = "";
                    if (winSeasonSlider != null) winSeasonSlider.value = 0f;
                }
            }
        }

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            if (pausePanel == null) return;
            pausePanel.SetActive(e.Current == GameState.Paused);
            if (loadingPanel != null)
                loadingPanel.SetActive(e.Current == GameState.LevelLoading);
        }

        private void UpdateMoveCount(int count)
        {
            if (moveCountText == null || _history == null) return;
            // Fix #13: Skip update if value hasn't changed to avoid redundant SetText calls
            // TextMeshPro SetText regenerates mesh every call, so only call when needed
            if (count == _lastMoveCount) return;
            _lastMoveCount = count;

            string movesLabel = _localization != null ? _localization.GetString("moves_text") : "Moves";
            _sb.Clear();
            _sb.Append(movesLabel).Append(": ").Append(_history.CurrentMoveCount);
            moveCountText.SetText(_sb);
        }

        private void UpdateLevelTitle()
        {
            if (levelTitleText == null || _currentLevel == null) return;
            if (_currentLevel.levelNumber == _lastLevelNumber) return;
            _lastLevelNumber = _currentLevel.levelNumber;
            _sb.Clear();
            string levelLabel = _localization != null ? _localization.GetString("level_title") : "Level";
            _sb.Append(levelLabel).Append(" ").Append(_currentLevel.levelNumber);
            levelTitleText.SetText(_sb);
        }

        private void UpdateStars()
        {
            if (starImages == null || _currentLevel == null) return;
            int stars = _currentLevel.CalculateStars(_history?.CurrentMoveCount ?? 999);
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] == null) continue;
                starImages[i].sprite = i < stars ? starOn : starOff;
                starImages[i].color = i < stars ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
        }

        private void HideAllPanels()
        {
            if (winPanel != null) winPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }

        private void OnNextLevel()
        {
            if (_currentLevel == null) return;
            int next = _currentLevel.levelNumber + 1;
            if (_repository.GetByNumber(next) == null) next = 1;
            _events?.Publish(new LevelSelectedEvent(next));
        }

        private void OnReplay() => _events?.Publish(new LevelSelectedEvent(_currentLevel?.levelNumber ?? 1));

        private void OnMainMenu() => _stateMachine?.TransitionTo(GameState.Menu);

        private void OnResume() => _stateMachine?.TransitionTo(GameState.Playing);

        private void OnUndoClicked()
        {
            if (_animationService != null && _animationService.IsAnimating)
            {
                MoldLogger.LogWarning("[HudPresenter] Undo click ignored: Animation is active.");
                return;
            }
            if (_undoService != null)
            {
                _undoService.TryUndo();
            }
        }

        private void OnHintClicked()
        {
            if (_animationService != null && _animationService.IsAnimating)
            {
                MoldLogger.LogWarning("[HudPresenter] Hint click ignored: Animation is active.");
                return;
            }
            if (_hintService != null && _currentLevel != null)
            {
                if (_hintService.TryGetHint(_currentLevel, out int srcIndex, out int dstIndex))
                {
                    _events?.Publish(new HintHighlightEvent(srcIndex, dstIndex));
                    MoldLogger.LogInfo($"[HudPresenter] Hint suggested: {srcIndex} -> {dstIndex}");
                }
            }
        }

        private void OnHintHighlight(HintHighlightEvent e)
        {
            var molds = _moldsProvider?.Molds;
            if (molds == null) return;

            if (e.SourceIndex >= 0 && e.SourceIndex < molds.Length)
                molds[e.SourceIndex].SetSelectionHighlight(true);
            if (e.TargetIndex >= 0 && e.TargetIndex < molds.Length && e.TargetIndex != e.SourceIndex)
                molds[e.TargetIndex].SetSelectionHighlight(true);

            // Fix Phase 3: Replaced Coroutine with PrimeTween for delay
            PrimeTween.Tween.Delay(1.5f).OnComplete(() =>
            {
                if (e.SourceIndex >= 0 && e.SourceIndex < molds.Length)
                    molds[e.SourceIndex].SetSelectionHighlight(false);
                if (e.TargetIndex >= 0 && e.TargetIndex < molds.Length && e.TargetIndex != e.SourceIndex)
                    molds[e.TargetIndex].SetSelectionHighlight(false);
            });
        }
    }
}
