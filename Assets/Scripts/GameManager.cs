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

        // VContainer injected configurations
        private GameConfig gameConfig;
        private AnimationConfig animConfig;
        private LevelConfig levelConfig;
        private AudioConfig audioConfig;

        // VContainer injected services & interfaces
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
        private IBottleView[] _bottles;
        private BottleController[] _allBottlesPool;

        private static readonly WaitForSeconds WinCheckDelay = new WaitForSeconds(0.5f);
        private static readonly Color CamDefaultBgColor = new Color(0.08f, 0.05f, 0.16f, 1f);

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
            ILocalizationService localizationService)
        {
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
        }

        private void Awake()
        {
            BottleLogger.LogInfo("GameManager Awake.");
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

            EventAggregator.Subscribe<LevelSelectedEvent>(OnLevelSelected);
            EventAggregator.Subscribe<PourCompletedEvent>(OnPourCompleted);

            if (_historyManager != null)
            {
                _historyManager.OnMoveCountChanged += OnMoveCountChanged;
            }

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
            EventAggregator.Unsubscribe<PourCompletedEvent>(OnPourCompleted);
            
            if (_historyManager != null)
            {
                _historyManager.OnMoveCountChanged -= OnMoveCountChanged;
            }
        }

        private void Update()
        {
            _inputHandlerService?.ProcessInput();
        }

        public void CustomUpdate() => Update();

        public void OnUpdate(float deltaTime) => Update();

        private void CacheBottles()
        {
            _allBottlesPool = FindObjectsByType<BottleController>(FindObjectsInactive.Include)
                             .OrderBy(b => b.name)
                             .ToArray();

            BottleLogger.LogInfo($"Found {_allBottlesPool.Length} bottles in master pool.");

            if (_allBottlesPool.Length == 0)
                BottleLogger.LogWarning("No BottleController found in scene.");
        }

        private void SetupBottles()
        {
            if (_allBottlesPool == null || _allBottlesPool.Length == 0)
            {
                BottleLogger.LogError("No bottles cached in master pool, cannot setup level.");
                return;
            }

            int targetCount = _allBottlesPool.Length;
            if (_currentLevel != null)
            {
                targetCount = _currentLevel.bottleCount;
            }
            targetCount = Mathf.Clamp(targetCount, 2, _allBottlesPool.Length);

            for (int i = 0; i < _allBottlesPool.Length; i++)
            {
                if (_allBottlesPool[i] != null)
                {
                    _allBottlesPool[i].gameObject.SetActive(i < targetCount);
                }
            }

            _bottles = _allBottlesPool
                       .Where(b => b != null && b.gameObject.activeSelf)
                       .Cast<IBottleView>()
                       .ToArray();

            _historyManager?.Initialize(_bottles);
            _inputHandlerService?.SetBottles(_bottles);

            _levelSetupService?.SetupBottles(_bottles, _currentLevel, _rendererService, _validator, _animationService);

            if (_camera != null)
            {
                _camera.backgroundColor = CamDefaultBgColor;
                _camera.clearFlags = CameraClearFlags.SolidColor;
            }
        }

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
                
                int stars = _currentLevel != null ? _currentLevel.CalculateStars(_historyManager?.CurrentMoveCount ?? 0) : 3;
                if (_currentLevel != null)
                {
                    _levelProgress?.RecordCompletion(_currentLevel.levelNumber, _historyManager?.CurrentMoveCount ?? 0, stars);
                }

                _audioService?.PlaySfx(AudioClipId.LevelComplete);

                if (winPanel != null) winPanel.SetActive(true);

                EventAggregator.Publish(new LevelCompletedEvent(_historyManager?.CurrentMoveCount ?? 0));
            }
        }

        public void Undo()
        {
            if (_stateMachine == null || !_stateMachine.IsInState(GameState.Playing)) return;
            _historyManager?.Undo();
        }

        public void OnLevelSelected(LevelSelectedEvent e)
        {
            _currentLevel = _levelRepository?.GetByNumber(e.LevelNumber);
            BottleLogger.LogInfo($"Level {e.LevelNumber} selected.");

            if (_currentLevel != null)
            {
                if (winPanel != null) winPanel.SetActive(false);

                if (_levelValidationService != null && !_levelValidationService.ValidateLevel(_currentLevel, _bottles?.Length ?? 0))
                {
                    BottleLogger.LogError($"Level {e.LevelNumber} failed validation.");
                    _stateMachine?.TransitionTo(GameState.Menu);
                    return;
                }

                _stateMachine?.TransitionTo(GameState.LevelLoading);

                _selectionService?.Deselect();
                
                _historyManager?.ResetAll();

                SetupBottles();

                _stateMachine?.TransitionTo(GameState.Playing);
                _audioService?.PlaySfx(AudioClipId.LevelStart);
            }
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
