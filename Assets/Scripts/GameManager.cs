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
using VContainer;

namespace PuzzleGame
{
    /// <summary>
    /// GameManager — VContainer DI consumer only. Not a Composition Root.
    /// All services injected via VContainer.
    /// </summary>
    public class GameManager : MonoBehaviour, IUpdateable
    {
        [Header("HUD (optional)")]
        [SerializeField] private Canvas    hudCanvas;
        [SerializeField] private TMPro.TextMeshProUGUI moveCountText;
        [SerializeField] private GameObject winPanel;

        [Header("UI Reference")]
        [SerializeField] private LevelSelectUI levelSelectUI;

        // VContainer injected services
        [Inject] private GameConfig gameConfig;
        [Inject] private AnimationConfig animConfig;
        [Inject] private LevelConfig levelConfig;
        [Inject] private AudioConfig audioConfig;

        // VContainer injected interfaces
        private IInputHandler _inputHandler;
        private IBottleValidator _validator;
        private IRendererService _rendererService;
        private IAnimationService _animationService;
        private IBottleSelectionService _selectionService;
        private IGameHistoryService _gameHistoryService;
        private IGameStateMachine _stateMachine;
        private IAudioService _audioService;
        private ILevelRepository _levelRepository;
        private ILevelProgressService _levelProgress;
        private Camera _camera;
        private ITweenService _tweenService;

        private InputHandlerService _inputHandlerService;
        private LevelSetupService _levelSetupService;
        private GameHistoryManagementService _historyManagementService;

        private LevelData _currentLevel;
        private IBottleView[] _bottles;

        private static readonly WaitForSeconds WinCheckDelay = new WaitForSeconds(0.5f);
        private static readonly Color CamDefaultBgColor = new Color(0.08f, 0.05f, 0.16f, 1f);

        [Inject]
        public void Construct(
            IInputHandler inputHandler,
            IBottleValidator validator,
            IRendererService rendererService,
            IAnimationService animationService,
            IBottleSelectionService selectionService,
            IGameHistoryService gameHistoryService,
            IGameStateMachine stateMachine,
            ILevelRepository levelRepository,
            ILevelProgressService levelProgress,
            IAudioService audioService,
            ITweenService tweenService)
        {
            BottleLogger.LogInfo("GameManager.Construct called by VContainer DI.");
            _inputHandler = inputHandler;
            _validator = validator;
            _rendererService = rendererService;
            _animationService = animationService;
            _selectionService = selectionService;
            _gameHistoryService = gameHistoryService;
            _stateMachine = stateMachine;
            _levelRepository = levelRepository;
            _levelProgress = levelProgress;
            _audioService = audioService;
            _tweenService = tweenService;
        }

        private void Awake()
        {
            BottleLogger.LogInfo("GameManager Awake.");
        }

        private void InitModularServices()
        {
            _inputHandlerService = new InputHandlerService(
                _inputHandler,
                _camera,
                _stateMachine,
                _animationService,
                _selectionService,
                _validator,
                gameConfig,
                animConfig,
                _audioService,
                onPourSucceeded: () => { _historyManagementService?.IncrementMoveCount(); StartCoroutine(DelayedWinCheck()); },
                onRecordUndoSnapshot: () => { _historyManagementService?.RecordUndoSnapshot(); });
        }

        private void Start()
        {
            if (_stateMachine == null)
            {
                BottleLogger.LogError(
                    "VContainer DI failed — GameInstaller (LifetimeScope) not found in scene.\n" +
                    "Fix: Add GameInstaller component to any GameObject, or use:\n" +
                    "  Tools > PuzzleGame > Open Editor > Scene > 'Setup Current Scene (Bottles + GameManager + DI)'");
                enabled = false;
                return;
            }

            BottleLogger.LogInfo("GameManager Start — initializing game systems.");

            _camera = Camera.main;
            InitAudio();
            InitModularServices();

            EventAggregator.Subscribe<LevelSelectedEvent>(OnLevelSelected);

            _stateMachine.TransitionTo(GameState.Menu);

            if (_levelRepository != null && levelSelectUI != null)
            {
                levelSelectUI.Initialize(_levelRepository, _levelProgress);
            }
            else
            {
                _stateMachine.TransitionTo(GameState.Playing);
            }

            CacheBottles();
            SetupBottles();
            InitHUD();
        }

        private void InitAudio()
        {
            if (audioConfig == null) return;
            // Audio init logic
        }

        private void OnDestroy()
        {
            EventAggregator.Unsubscribe<LevelSelectedEvent>(OnLevelSelected);
            EventAggregator.Clear();
        }

        private void Update()
        {
            _inputHandlerService?.ProcessInput();
        }

        public void CustomUpdate() => Update();

        public void OnUpdate(float deltaTime) => Update();

        private void CacheBottles()
        {
            _bottles = FindObjectsByType<BottleController>(FindObjectsInactive.Exclude)
                       .OrderBy(b => b.name)
                       .Cast<IBottleView>()
                       .ToArray();

            BottleLogger.LogInfo($"Found {_bottles.Length} bottles in scene.");

            if (_bottles.Length == 0)
                BottleLogger.LogWarning("No BottleController found — level will be empty.");

            // History management service — bir kez oluştur
            if (_historyManagementService == null)
            {
                _historyManagementService = new GameHistoryManagementService(_gameHistoryService, _bottles);
                _historyManagementService.SetMoveCountChangedCallback(OnMoveCountChanged);
            }

            _inputHandlerService?.SetBottles(_bottles);
        }

        private void SetupBottles()
        {
            if (_bottles == null || _bottles.Length == 0)
            {
                BottleLogger.LogError("No bottles found in scene, cannot setup level.");
                return;
            }

            _levelSetupService = new LevelSetupService(gameConfig, levelConfig, _currentLevel);
            _levelSetupService.SetupBottles(_bottles, _rendererService, _validator, _animationService);

            if (_camera != null)
            {
                _camera.backgroundColor = CamDefaultBgColor;
                _camera.clearFlags = CameraClearFlags.SolidColor;
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

            bool hasLiquid = false;
            bool allComplete = true;

            foreach (var view in _bottles)
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
                
                int stars = _currentLevel != null ? _currentLevel.CalculateStars(_historyManagementService?.CurrentMoveCount ?? 0) : 3;
                if (_currentLevel != null)
                {
                    _levelProgress?.RecordCompletion(_currentLevel.levelNumber, _historyManagementService?.CurrentMoveCount ?? 0, stars);
                }

                _audioService?.PlaySfx(AudioClipId.LevelComplete);

                if (winPanel != null) winPanel.SetActive(true);

                EventAggregator.Publish(new LevelCompletedEvent(_historyManagementService?.CurrentMoveCount ?? 0));
            }
        }

        public void Undo()
        {
            if (_stateMachine == null || !_stateMachine.IsInState(GameState.Playing)) return;
            _historyManagementService?.Undo();
        }

        public void OnLevelSelected(LevelSelectedEvent e)
        {
            _currentLevel = _levelRepository?.GetByNumber(e.LevelNumber);
            BottleLogger.LogInfo($"Level {e.LevelNumber} selected.");

            if (_currentLevel != null)
            {
                if (winPanel != null) winPanel.SetActive(false);

                var levelValidator = new LevelValidationService();
                if (!levelValidator.ValidateLevel(_currentLevel, _bottles?.Length ?? 0))
                {
                    BottleLogger.LogError($"Level {e.LevelNumber} failed validation.");
                    _stateMachine?.TransitionTo(GameState.Menu);
                    return;
                }

                _levelSetupService = new LevelSetupService(gameConfig, levelConfig, _currentLevel);

                _stateMachine?.TransitionTo(GameState.LevelLoading);

                _selectionService?.Deselect();
                
                // Reset history and move count properly instead of recreating services
                _historyManagementService?.ResetAll();

                SetupBottles();

                _stateMachine?.TransitionTo(GameState.Playing);
                _audioService?.PlaySfx(AudioClipId.LevelStart);
            }
        }

        private void UpdateHUD()
        {
            if (moveCountText != null && _historyManagementService != null)
                moveCountText.text = $"Hamle: {_historyManagementService.CurrentMoveCount}";
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
