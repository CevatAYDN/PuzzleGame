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
        private LevelData _currentLevel;

        private readonly StringBuilder _sb = new StringBuilder(64);

        [VContainer.Inject]
        public void Construct(
            IGameHistoryManager history,
            ILocalizationService localization,
            IEventAggregator events,
            ILevelRepository repository,
            ILevelProgressService progress,
            IGameStateMachine stateMachine)
        {
            _history = history;
            _localization = localization;
            _events = events;
            _repository = repository;
            _progress = progress;
            _stateMachine = stateMachine;
        }

        private void OnEnable()
        {
            if (_events == null) return;
            _events.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            _events.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
            _events.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
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
            UpdateMoveCount(0);
            UpdateLevelTitle();
            HideAllPanels();
        }

        private void OnLevelCompleted(LevelCompletedEvent e)
        {
            if (winPanel != null) winPanel.SetActive(true);
            if (winMoveCountText != null)
            {
                string movesLabel = _localization != null ? _localization.GetString("moves_text") : "Moves";
                _sb.Clear();
                _sb.Append(movesLabel).Append(": ").Append(e.MoveCount);
                winMoveCountText.SetText(_sb);
            }
            UpdateStars();
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
            string movesLabel = _localization != null ? _localization.GetString("moves_text") : "Moves";
            _sb.Clear();
            _sb.Append(movesLabel).Append(": ").Append(_history.CurrentMoveCount);
            moveCountText.SetText(_sb);
        }

        private void UpdateLevelTitle()
        {
            if (levelTitleText == null || _currentLevel == null) return;
            _sb.Clear();
            _sb.Append("Level ").Append(_currentLevel.levelNumber);
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
    }
}
