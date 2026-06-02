using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Services;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Infrastructure.Interfaces;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Infrastructure;
using PuzzleGame.Infrastructure.Pool;
using PuzzleGame.Events;
using PuzzleGame.Logging;
using PuzzleGame.Configuration;
using PuzzleGame.Application.Animation;
using PuzzleGame.Application.UI;

namespace PuzzleGame
{
    public class GameManager : MonoBehaviour, IUpdateable
    {
        [Header("Configuration (assign via Resources or Inspector)")]
        [SerializeField] private GameConfig     gameConfig;
        [SerializeField] private AnimationConfig animConfig;
        [SerializeField] private LevelConfig    levelConfig;
        [SerializeField] private AudioConfig   audioConfig;
        [SerializeField] private LevelData[]   levelCatalog;

        [Header("HUD (optional)")]
        [SerializeField] private Canvas    hudCanvas;
        [SerializeField] private TMPro.TextMeshProUGUI moveCountText;
        [SerializeField] private GameObject winPanel;

        [Header("UI Reference")]
        [SerializeField] private LevelSelectUI levelSelectUI;

        private IBottleValidator      _validator;
        private IRendererService      _rendererService;
        private IAnimationService     _animationService;
        private IBottleSelectionService _selectionService;
        private IInputHandler         _inputHandler;
        private IGameHistoryService   _gameHistoryService;
        private IGameStateMachine     _stateMachine;
        private IAudioService         _audioService;
        private ILevelRepository      _levelRepository;
        private ILevelProgressService _levelProgress;
        private LevelData             _currentLevel;

        private BottleController[] _bottles;
        private Camera             _mainCam;

        private int _moveCount;
        
        // Services for separation of concerns
        private InputHandlerService _inputHandlerService;
        private LevelSetupService _levelSetupService;
        private GameHistoryManagementService _historyManagementService;

        private Action<BottleState> _onBottleSelectedHandler;
        private Action<BottleState> _onBottleDeselectedHandler;

        private static readonly WaitForSeconds WinCheckDelay = new WaitForSeconds(0.5f);
        private static readonly Color CamDefaultBgColor = new Color(0.08f, 0.05f, 0.16f, 1f);

        private static readonly Color[] DefaultPalette = new Color[]
        {
            new Color(0.95f, 0.20f, 0.25f),
            new Color(0.20f, 0.55f, 0.95f),
            new Color(0.30f, 0.85f, 0.35f),
            new Color(0.98f, 0.80f, 0.15f),
            new Color(0.70f, 0.30f, 0.90f),
            new Color(0.95f, 0.50f, 0.15f),
        };

        private void Awake()
        {
            BottleLogger.LogInfo("GameManager Awake — composing services.");
            ComposeServices();
        }

        private void Start()
        {
            BottleLogger.LogInfo("GameManager Start — setting up scene.");
            _stateMachine.TransitionTo(GameState.Menu);
            CacheBottles();
            SetupBottles();
            InitHUD();
            // Auto-advance to LevelLoading → Playing (will be replaced by level select flow later)
            _stateMachine.TransitionTo(GameState.LevelLoading);
            _stateMachine.TransitionTo(GameState.Playing);
        }

        private void OnEnable()
        {
            UpdateManager.Instance?.Register(this);
        }

        private void OnDisable()
        {
            if (UpdateManager.Instance != null)
                UpdateManager.Instance.Unregister(this);
        }

        private void OnDestroy()
        {
            if (_selectionService != null)
            {
                _selectionService.OnBottleSelected   -= _onBottleSelectedHandler;
                _selectionService.OnBottleDeselected -= _onBottleDeselectedHandler;
            }
            EventAggregator.Clear();
            PoolManager.Instance.Cleanup();
            
            // Cleanup additional services
            _inputHandlerService = null;
            _levelSetupService = null;
            _historyManagementService = null;
        }

        public void OnUpdate(float deltaTime)
        {
            _inputHandlerService.ProcessInput();
        }

        private void ComposeServices()
        {
            if (gameConfig == null)
            {
                gameConfig = Resources.Load<GameConfig>("Data/GameConfig");
                if (gameConfig == null)
                {
                    gameConfig = ScriptableObject.CreateInstance<GameConfig>();
                    BottleLogger.LogWarning("GameConfig not found — using defaults.");
                }
            }

            if (animConfig == null)
            {
                animConfig = Resources.Load<AnimationConfig>("Data/AnimationConfig");
                if (animConfig == null)
                {
                    animConfig = ScriptableObject.CreateInstance<AnimationConfig>();
                    BottleLogger.LogWarning("AnimationConfig not found — using defaults.");
                }
            }

            _mainCam = Camera.main ?? FindAnyObjectByType<Camera>();
            if (_mainCam == null)
            {
                BottleLogger.LogError("No camera found — input will be disabled.");
            }
            else
            {
                _inputHandler = new InputHandler(_mainCam);
            }

            _validator        = new BottleValidationService(gameConfig.colorMatchTolerance);
            _rendererService  = new RendererService();
            var tweenService = CreateTweenService();
            _animationService = new AnimationService(animConfig, tweenService);
            _selectionService = new BottleSelectionService();
            _gameHistoryService = new GameHistoryService();
            _stateMachine = new GameStateMachine();

            // Audio (optional — no AudioConfig = null service, no crash)
            if (audioConfig == null)
            {
                audioConfig = Resources.Load<AudioConfig>("Data/AudioConfig");
            }
            if (audioConfig != null)
            {
                var audioTween = CreateTweenService();
                _audioService = new AudioService(audioConfig, audioTween);
            }

            // Wire audio to state machine changes
            if (_audioService != null)
            {
                _stateMachine.OnStateChanged += (prev, curr) =>
                {
                    if (curr == Domain.Models.GameState.LevelComplete)
                        _audioService.PlaySfx(AudioClipId.LevelComplete);
                    else if (curr == Domain.Models.GameState.Playing)
                        _audioService.PlaySfx(AudioClipId.LevelStart);
                };
            }

            // Level repository + progress
            _levelRepository = new ScriptableObjectLevelRepository(levelCatalog ?? System.Array.Empty<LevelData>());
            _levelProgress = new PlayerPrefsLevelProgressService();

            // Dynamic LevelSelectUI discovery and initialization
            if (levelSelectUI == null)
            {
                levelSelectUI = FindAnyObjectByType<LevelSelectUI>();
            }
            if (levelSelectUI != null)
            {
                levelSelectUI.Initialize(_levelRepository, _levelProgress);
            }

            // Initialize new modular services
            _inputHandlerService = new InputHandlerService(
                _inputHandler,
                _mainCam,
                _stateMachine,
                _animationService,
                _selectionService,
                _validator,
                gameConfig,
                animConfig,
                () => _moveCount++,
                RecordUndoSnapshot);
                
            _levelSetupService = new LevelSetupService(gameConfig, levelConfig, _currentLevel);
            
            _historyManagementService = new GameHistoryManagementService(_gameHistoryService, _bottles);
            _historyManagementService.SetUpdateHUDCallback(UpdateHUD);
            _historyManagementService.SetMoveCount(_moveCount);

            // Listen for level selection (from LevelSelectUI)
            EventAggregator.Subscribe<LevelSelectedEvent>(OnLevelSelected);

            _onBottleSelectedHandler = b => EventAggregator.Publish(
                new BottleSelectedEvent(b));
            _onBottleDeselectedHandler = b => EventAggregator.Publish(
                new BottleDeselectedEvent(b));

            _selectionService.OnBottleSelected   += _onBottleSelectedHandler;
            _selectionService.OnBottleDeselected += _onBottleDeselectedHandler;

            BottleLogger.LogDebug("All services composed.");
        }

        private static DomainColor GetBottleColorId(BottleState state)
        {
            return state.IsEmpty || state.Layers.Count == 0
                ? new DomainColor(0, 0, 0, 0)
                : state.Layers[0].Color;
        }

        private void CacheBottles()
        {
            _bottles = FindObjectsByType<BottleController>(FindObjectsInactive.Exclude)
                       .OrderBy(b => b.name)
                       .ToArray();

            BottleLogger.LogInfo($"Found {_bottles.Length} bottles in scene.");

            if (_bottles.Length == 0)
                BottleLogger.LogWarning("No BottleController found — level will be empty.");
                
            // Set bottles in input handler service if it exists
            if (_inputHandlerService != null)
            {
                _inputHandlerService.SetBottles(_bottles);
            }
        }

        private void SetupBottles()
        {
            if (_bottles == null || _bottles.Length == 0) 
            {
                BottleLogger.LogError("No bottles found in scene, cannot setup level.");
                return;
            }

            _levelSetupService.SetupBottles(_bottles, _rendererService, _validator, _animationService);

            if (_mainCam != null)
            {
                _mainCam.backgroundColor = CamDefaultBgColor;
                _mainCam.clearFlags      = CameraClearFlags.SolidColor;
            }
        }

        private void RecordUndoSnapshot()
        {
            if (_historyManagementService != null)
            {
                _historyManagementService.RecordUndoSnapshot();
            }
        }

        private IEnumerator DelayedWinCheck()
        {
            yield return WinCheckDelay;
            CheckWinCondition();
        }

        private void CheckWinCondition()
        {
            if (_bottles == null || _bottles.Length == 0) return;

            bool hasLiquid   = false;
            bool allComplete = true;

            foreach (var bottle in _bottles)
            {
                if (bottle == null || bottle.IsEmpty()) continue;
                hasLiquid = true;

                bool isComplete = _validator.IsComplete(bottle.State);
                if (isComplete && !bottle.IsCapped)
                {
                    bottle.AnimateCompletion();
                }

                if (!isComplete)
                {
                    allComplete = false;
                }
            }

            if (hasLiquid && allComplete)
            {
                _stateMachine.TransitionTo(GameState.LevelComplete);
                BottleLogger.LogInfo($"Level complete in {_moveCount} moves.");
                EventAggregator.Publish(new LevelCompletedEvent(_moveCount));
                if (_currentLevel != null && _levelProgress != null)
                {
                    int stars = _currentLevel.CalculateStars(_moveCount);
                    _levelProgress.RecordCompletion(_currentLevel.levelNumber, _moveCount, stars);
                }
                if (winPanel != null) winPanel.SetActive(true);
            }
        }

        private void InitHUD()
        {
            if (winPanel != null) winPanel.SetActive(false);
            UpdateHUD();
        }

        private void UpdateHUD()
        {
            if (moveCountText != null)
                moveCountText.text = $"Hamle: {_moveCount}";
        }
        
        private void UpdateHUD(int moveCount)
        {
            _moveCount = moveCount;
            UpdateHUD();
        }



        public void Undo()
        {
            if (_stateMachine == null || !_stateMachine.IsInState(GameState.Playing)) return;
            if (_historyManagementService != null)
            {
                _historyManagementService.Undo();
                _moveCount = _historyManagementService.GetCurrentMoveCount();
            }
        }

        public void RestartGame()
        {
            BottleLogger.LogInfo("Restarting game.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Factory: prefers PrimeTween (zero-allocation), falls back to coroutine tweens.
        /// </summary>
        private static ITweenService CreateTweenService()
        {
#if PRIME_TWEEN_INSTALLED
            BottleLogger.LogInfo("Using PrimeTween for animations (zero-allocation).");
            return new PrimeTweenService();
#else
            BottleLogger.LogInfo("PrimeTween not available — using CoroutineTweenService.");
            return new CoroutineTweenService();
#endif
        }

        private void OnLevelSelected(LevelSelectedEvent e)
        {
            _currentLevel = _levelRepository?.GetByNumber(e.LevelNumber);
            BottleLogger.LogInfo($"Level {e.LevelNumber} selected: {(_currentLevel != null ? "found" : "not found")}.");
            
            if (_currentLevel != null)
            {
                // Validate the level before loading
                var levelValidator = new LevelValidationService();
                if (!levelValidator.ValidateLevel(_currentLevel, _bottles?.Length ?? 0))
                {
                    BottleLogger.LogError($"Level {e.LevelNumber} failed validation, aborting load.");
                    // Optionally transition to an error state or stay in menu
                    _stateMachine?.TransitionTo(GameState.Menu);
                    return;
                }

                // Update level setup service with the new level
                _levelSetupService = new LevelSetupService(gameConfig, levelConfig, _currentLevel);
                
                _stateMachine?.TransitionTo(GameState.LevelLoading);
                
                if (_selectionService != null) _selectionService.Deselect();
                _moveCount = 0;
                UpdateHUD();
                _gameHistoryService = new GameHistoryService(); // geçmişi sıfırla
                _historyManagementService = new GameHistoryManagementService(_gameHistoryService, _bottles);
                _historyManagementService.SetUpdateHUDCallback(UpdateHUD);
                _historyManagementService.SetMoveCount(_moveCount);
                
                SetupBottles();
                
                _stateMachine?.TransitionTo(GameState.Playing);
            }
            else
            {
                BottleLogger.LogError($"Level {e.LevelNumber} not found in repository, cannot load.");
                _stateMachine?.TransitionTo(GameState.Menu);
            }
        }
    }
}