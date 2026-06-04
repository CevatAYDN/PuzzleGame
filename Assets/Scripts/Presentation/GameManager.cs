using System;
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
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Presentation.UI;
using VContainer;
// IUpdateable/IUpdateManager now live in Application.Interfaces (Fix #11)

namespace PuzzleGame
{
    /// <summary>
    /// GameManager — VContainer DI consumer only. Not a Composition Root.
    /// All services injected via VContainer.
    /// Mold pool initialization delegated to MoldPoolInitializer (SRP).
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

        private IMoldValidator _validator;
        private IRendererService _rendererService;
        private IAnimationService _animationService;
        private IMoldSelectionService _selectionService;
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
        private MoldPoolInitializer _poolInitializer;
        private IShaderOptimizer _shaderOptimizer;
        private IEventAggregator _eventAggregator;
        private IUpdateManager _updateManager;
        private ITweenService _tweenService;

        private bool _isInitialized;
        private readonly System.Text.StringBuilder _hudSb = new System.Text.StringBuilder(64);

        [Inject]
        public void Construct(
            GameConfig gameConfig,
            AnimationConfig animConfig,
            LevelConfig levelConfig,
            AudioConfig audioConfig,
            IMoldValidator validator,
            IRendererService rendererService,
            IAnimationService animationService,
            IMoldSelectionService selectionService,
            IGameStateMachine stateMachine,
            ILevelRepository levelRepository,
            ILevelProgressService levelProgress,
            IAudioService audioService,
            IInputHandlerService inputHandlerService,
            ILevelSetupService levelSetupService,
            ILevelValidationService levelValidationService,
            IGameHistoryManager historyManager,
            ILocalizationService localizationService,
            IShaderOptimizer shaderOptimizer,
            IEventAggregator eventAggregator,
            IUpdateManager updateManager,
            ITweenService tweenService)
        {
            if (validator == null)        throw new ArgumentNullException(nameof(validator));
            if (stateMachine == null)     throw new ArgumentNullException(nameof(stateMachine));
            if (levelRepository == null)  throw new ArgumentNullException(nameof(levelRepository));
            if (audioService == null)     throw new ArgumentNullException(nameof(audioService));
            if (historyManager == null)   throw new ArgumentNullException(nameof(historyManager));
            if (inputHandlerService == null) throw new ArgumentNullException(nameof(inputHandlerService));

            MoldLogger.LogInfo("GameManager.Construct called by VContainer DI.");
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
            _eventAggregator = eventAggregator;
            _updateManager = updateManager;
            _tweenService = tweenService;

            _isInitialized = true;
        }

        private void Start()
        {
            if (!_isInitialized)
            {
                const string errorMsg = "VContainer DI failed — GameInstaller (LifetimeScope) not found or not configured.\n" +
                                         "Fix: Tools > PuzzleGame > Open Editor > Scene tab > 'Setup Current Scene (GameManager + DI)'";
                MoldLogger.LogError(errorMsg);
                if (diErrorPanel != null) diErrorPanel.SetActive(true);
                enabled = false;
                return;
            }

            MoldLogger.LogInfo("GameManager Start — initializing game systems.");

            // Lock target frame rate to device screen refresh rate for AAA mobile smoothness (Unity 6 API)
            double refreshRate = Screen.currentResolution.refreshRateRatio.value;
            UnityEngine.Application.targetFrameRate = refreshRate > 0 ? (int)Math.Round(refreshRate) : 60;
            MoldLogger.LogInfo($"Target frame rate set to: {UnityEngine.Application.targetFrameRate} FPS");

            _shaderOptimizer?.Initialize(gameConfig.applyMobileShaderDefaults);
            _camera = Camera.main;
            InitAudio();

            SceneManager.sceneUnloaded += OnSceneUnloaded;
            _eventAggregator.Subscribe<LevelSelectedEvent>(OnLevelSelected);
            _eventAggregator.Subscribe<CastCompletedEvent>(OnCastCompleted);

            _historyManager.OnMoveCountChanged += OnMoveCountChanged;

            _stateMachine.TransitionTo(GameState.Menu);

            if (levelSelectUI != null)
            {
                levelSelectUI.Initialize(_levelRepository, _levelProgress);
            }
            else
            {
                _stateMachine.TransitionTo(GameState.Playing);
                bool hasSceneMolds = FindObjectsByType<MoldController>(FindObjectsInactive.Include).Length > 0;
                if (!hasSceneMolds && _currentLevel == null && _levelRepository != null && _levelRepository.AllLevels != null && _levelRepository.AllLevels.Count > 0)
                {
                    _currentLevel = _levelRepository.AllLevels[0];
                }
            }

            _poolInitializer = new MoldPoolInitializer(
                _levelSetupService, _rendererService, _validator, _animationService,
                _inputHandlerService, _historyManager, _updateManager, _camera);

            _poolInitializer.InitializeForLevel(_currentLevel);
            InitHUD();
            _updateManager?.Register(this);
        }

        private void InitAudio()
        {
            if (audioConfig == null)
            {
                MoldLogger.LogWarning("AudioConfig is null — audio init skipped.");
                return;
            }
        }

        private void OnDestroy()
        {
            _updateManager?.Unregister(this);

            if (_animationService is System.IDisposable disposable)
            {
                disposable.Dispose();
            }

            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            _eventAggregator.Unsubscribe<LevelSelectedEvent>(OnLevelSelected);
            _eventAggregator.Unsubscribe<CastCompletedEvent>(OnCastCompleted);

            if (_historyManager != null)
            {
                _historyManager.OnMoveCountChanged -= OnMoveCountChanged;
            }

            _currentLevel = null;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            MoldLogger.LogDebug("Scene unloaded — cleaning up subscriptions and particle pools.");
            _eventAggregator.Clear();
            if (_animationService is System.IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private Rect _errorRect;
        private Rect _errorLabelRect;
        private GUIStyle _errorStyle;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private void OnGUI()
        {
            if (!_isInitialized)
            {
                if (_errorStyle == null || _lastScreenWidth != Screen.width || _lastScreenHeight != Screen.height)
                {
                    _lastScreenWidth = Screen.width;
                    _lastScreenHeight = Screen.height;
                    _errorRect = new Rect(20, 20, Screen.width - 40, Screen.height - 40);
                    _errorLabelRect = new Rect(40, 60, Screen.width - 80, Screen.height - 120);
                    _errorStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 20,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true
                    };
                }

                GUI.Box(_errorRect, "VCONTAINER DI FAILURE");
                GUI.Label(_errorLabelRect, "VContainer DI failed — GameManager (LifetimeScope) not found or not configured.\n\n" +
                                     "Fix: Tools > PuzzleGame > Open Editor > Scene tab > 'Setup Current Scene (GameManager + DI)'", _errorStyle);
            }
        }

        // Fix #10: MonoBehaviour.Update() removed to avoid double-firing ProcessInput.
        // Input is now processed exclusively via IUpdateable.OnUpdate() → UpdateManager.
        // If UpdateManager is not present in scene, GameManager calls ProcessInput directly
        // from Start() by registering itself — see Start().
        public void OnUpdate(float deltaTime)
        {
            _inputHandlerService.ProcessInput();
        }

        private void OnCastCompleted(CastCompletedEvent e)
        {
            _tweenService.Delay(0.5f)
                .OnComplete(CheckWinCondition)
                .Start();
        }

        private void CheckWinCondition()
        {
            var Molds = _poolInitializer?.Molds;
            if (Molds == null || Molds.Length == 0) return;

            bool hasOre = false;
            bool allComplete = true;

            foreach (var view in Molds)
            {
                if (view == null || view.IsEmpty) continue;
                hasOre = true;

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

            if (allComplete && hasOre)
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

                _eventAggregator.Publish(new LevelCompletedEvent(moveCount));
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
            MoldLogger.LogInfo($"Level {e.LevelNumber} selected.");

            if (_currentLevel == null)
            {
                MoldLogger.LogError($"Level {e.LevelNumber} not found in repository.");
                return;
            }

            if (winPanel != null) winPanel.SetActive(false);

            if (!_levelValidationService.ValidateLevel(_currentLevel, _poolInitializer?.Molds?.Length ?? 0))
            {
                MoldLogger.LogError($"Level {e.LevelNumber} failed validation.");
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
                _hudSb.Clear();
                _hudSb.Append(movesLabel).Append(": ").Append(_historyManager.CurrentMoveCount);
                moveCountText.SetText(_hudSb);
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
