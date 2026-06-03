using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Services;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Infrastructure.Interfaces;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Infrastructure;
using PuzzleGame.Infrastructure.Pool;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Application.UI;
using VContainer;

namespace PuzzleGame
{
    /// <summary>
    /// GameManager — VContainer DI consumer only. Not a Composition Root.
    /// All services injected via VContainer.
    /// Bottle pool initialization delegated to BottlePoolInitializer (SRP).
    /// </summary>
    public class GameManager : MonoBehaviour, IUpdateable
    {
        [Header("HUD (optional)")]
        [SerializeField] private Canvas    hudCanvas;
        [SerializeField] private TMPro.TextMeshProUGUI moveCountText;
        [SerializeField] private GameObject winPanel;
        [Header("DI Failure UI")]
        [SerializeField] private GameObject diErrorPanel;

        [Header("UI Reference")]
        [SerializeField] private LevelSelectUI levelSelectUI;

        private GameConfig gameConfig;
        private AnimationConfig animConfig;
        private LevelConfig levelConfig;
        private AudioConfig audioConfig;

        private IBottleValidator _validator;
        private IRendererService _rendererService;
        private IAnimationService _animationService;
        private IBottleSelectionService _selectionService;
        private IGameStateMachine _stateMachine;
        private IAudioService _audioService;
        private ILevelRepository _levelRepository;
        private ILevelProgressService _levelProgress;

        private IInputHandlerService _inputHandlerService;
        private ILevelSetupService _levelSetupService;
        private ILevelValidationService _levelValidationService;
        private IGameHistoryManager _historyManager;
        private ILocalizationService _localizationService;

        private Camera _camera;
        private LevelData _currentLevel;
        private BottlePoolInitializer _poolInitializer;
        private IShaderOptimizer _shaderOptimizer;

        private static readonly WaitForSeconds WinCheckDelay =
            new WaitForSeconds(BottleConstants.WinCheckDelaySeconds);

        private bool _isInitialized;

        [Inject]
        public void Construct(
            GameConfig gameConfig,
            AnimationConfig animConfig,
            LevelConfig levelConfig,
            AudioConfig audioConfig,
            IBottleValidator validator,
            IRendererService rendererService,
            IAnimationService animationService,
            IBottleSelectionService selectionService,
            IGameStateMachine stateMachine,
            ILevelRepository levelRepository,
            ILevelProgressService levelProgress,
            IAudioService audioService,
            IInputHandlerService inputHandlerService,
            ILevelSetupService levelSetupService,
            ILevelValidationService levelValidationService,
            IGameHistoryManager historyManager,
            ILocalizationService localizationService,
            IShaderOptimizer shaderOptimizer)
        {
            if (validator == null)        throw new ArgumentNullException(nameof(validator));
            if (stateMachine == null)     throw new ArgumentNullException(nameof(stateMachine));
            if (levelRepository == null)  throw new ArgumentNullException(nameof(levelRepository));
            if (audioService == null)     throw new ArgumentNullException(nameof(audioService));
            if (historyManager == null)   throw new ArgumentNullException(nameof(historyManager));
            if (inputHandlerService == null) throw new ArgumentNullException(nameof(inputHandlerService));

            BottleLogger.LogInfo("GameManager.Construct called by VContainer DI.");
            this.gameConfig = gameConfig;
            this.animConfig = animConfig;
            this.levelConfig = levelConfig;
            this.audioConfig = audioConfig;
            _localizationService = localizationService;
            _validator = validator;
            _rendererService = rendererService;
            _animationService = animationService;
            _selectionService = selectionService;
            _stateMachine = stateMachine;
            _levelRepository = levelRepository;
            _levelProgress = levelProgress;
            _audioService = audioService;
            _inputHandlerService = inputHandlerService;
            _levelSetupService = levelSetupService;
            _levelValidationService = levelValidationService;
            _historyManager = historyManager;
            _shaderOptimizer = shaderOptimizer;

            _isInitialized = true;
        }

        private void Start()
        {
            if (!_isInitialized)
            {
                const string errorMsg = "VContainer DI failed — GameInstaller (LifetimeScope) not found or not configured.\n" +
                                         "Fix: Tools > PuzzleGame > Open Editor > Scene tab > 'Setup Current Scene (GameManager + DI)'";
                BottleLogger.LogError(errorMsg);
                if (diErrorPanel != null) diErrorPanel.SetActive(true);
                enabled = false;
                return;
            }

            BottleLogger.LogInfo("GameManager Start — initializing game systems.");

            _shaderOptimizer?.Initialize(gameConfig.applyMobileShaderDefaults);
            _camera = Camera.main;
            InitAudio();

            SceneManager.sceneUnloaded += OnSceneUnloaded;
            EventAggregator.Subscribe<LevelSelectedEvent>(OnLevelSelected);
            EventAggregator.Subscribe<PourCompletedEvent>(OnPourCompleted);

            _historyManager.OnMoveCountChanged += OnMoveCountChanged;

            _stateMachine.TransitionTo(GameState.Menu);

            if (levelSelectUI != null)
            {
                levelSelectUI.Initialize(_levelRepository, _levelProgress);
            }
            else
            {
                _stateMachine.TransitionTo(GameState.Playing);
            }

            _poolInitializer = new BottlePoolInitializer(
                _levelSetupService, _rendererService, _validator, _animationService,
                _inputHandlerService, _historyManager, _camera);
            _poolInitializer.InitializeForLevel(_currentLevel);
            InitHUD();
        }

        private void InitAudio()
        {
            if (audioConfig == null)
            {
                BottleLogger.LogWarning("AudioConfig is null — audio init skipped.");
                return;
            }
        }

        private void OnDestroy()
        {
            if (_animationService is System.IDisposable disposable)
            {
                disposable.Dispose();
            }

            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            EventAggregator.Unsubscribe<LevelSelectedEvent>(OnLevelSelected);
            EventAggregator.Unsubscribe<PourCompletedEvent>(OnPourCompleted);

            if (_historyManager != null)
            {
                _historyManager.OnMoveCountChanged -= OnMoveCountChanged;
            }

            _currentLevel = null;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            BottleLogger.LogDebug("Scene unloaded — cleaning up subscriptions and particle pools.");
            EventAggregator.Clear();
            if (_animationService is System.IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private void OnGUI()
        {
            if (!_isInitialized)
            {
                var rect = new Rect(20, 20, Screen.width - 40, Screen.height - 40);
                GUI.Box(rect, "VCONTAINER DI FAILURE");
                var labelRect = new Rect(40, 60, Screen.width - 80, Screen.height - 120);
                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                GUI.Label(labelRect, "VContainer DI failed — GameManager (LifetimeScope) not found or not configured.\n\n" +
                                     "Fix: Tools > PuzzleGame > Open Editor > Scene tab > 'Setup Current Scene (GameManager + DI)'", style);
            }
        }

        private void Update()
        {
            _inputHandlerService.ProcessInput();
        }

        public void CustomUpdate() => Update();

        public void OnUpdate(float deltaTime) => Update();

        private void OnPourCompleted(PourCompletedEvent e)
        {
            StartCoroutine(DelayedWinCheck());
        }

        private IEnumerator DelayedWinCheck()
        {
            yield return WinCheckDelay;
            CheckWinCondition();
        }

        private void CheckWinCondition()
        {
            var bottles = _poolInitializer?.Bottles;
            if (bottles == null || bottles.Length == 0) return;

            bool hasLiquid = false;
            bool allComplete = true;

            foreach (var view in bottles)
            {
                if (view == null || view.IsEmpty) continue;
                hasLiquid = true;

                bool isComplete = _validator.IsComplete(view.State);
                if (isComplete && !view.IsCapped)
                {
                    view.AnimateCompletion();
                }

                if (!isComplete)
                {
                    allComplete = false;
                }
            }

            if (allComplete && hasLiquid)
            {
                _stateMachine.TransitionTo(GameState.LevelComplete);

                int moveCount = _historyManager.CurrentMoveCount;
                int stars = _currentLevel != null ? _currentLevel.CalculateStars(moveCount) : 3;
                if (_currentLevel != null)
                {
                    _levelProgress?.RecordCompletion(_currentLevel.levelNumber, moveCount, stars);
                }

                _audioService.PlaySfx(AudioClipId.LevelComplete);

                if (winPanel != null) winPanel.SetActive(true);

                EventAggregator.Publish(new LevelCompletedEvent(moveCount));
            }
        }

        public void Undo()
        {
            if (!_stateMachine.IsInState(GameState.Playing)) return;
            _historyManager.Undo();
        }

        public void OnLevelSelected(LevelSelectedEvent e)
        {
            _currentLevel = _levelRepository.GetByNumber(e.LevelNumber);
            BottleLogger.LogInfo($"Level {e.LevelNumber} selected.");

            if (_currentLevel == null)
            {
                BottleLogger.LogError($"Level {e.LevelNumber} not found in repository.");
                return;
            }

            if (winPanel != null) winPanel.SetActive(false);

            if (!_levelValidationService.ValidateLevel(_currentLevel, _poolInitializer?.Bottles?.Length ?? 0))
            {
                BottleLogger.LogError($"Level {e.LevelNumber} failed validation.");
                _stateMachine.TransitionTo(GameState.Menu);
                return;
            }

            _stateMachine.TransitionTo(GameState.LevelLoading);

            _selectionService.Deselect();

            _historyManager.ResetAll();

            _poolInitializer.InitializeForLevel(_currentLevel);

            _stateMachine.TransitionTo(GameState.Playing);
            _audioService.PlaySfx(AudioClipId.LevelStart);
        }

        private void UpdateHUD()
        {
            if (moveCountText != null && _historyManager != null)
            {
                string movesLabel = _localizationService != null ? _localizationService.GetString("moves_text") : "Hamle";
                moveCountText.text = $"{movesLabel}: {_historyManager.CurrentMoveCount}";
            }
        }

        private void OnMoveCountChanged(int moveCount)
        {
            UpdateHUD();
        }

        private void InitHUD()
        {
            UpdateHUD();
            if (winPanel != null) winPanel.SetActive(false);
        }
    }
}
