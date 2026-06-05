using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Presentation;
using PuzzleGame.Presentation.UI;
using VContainer;

namespace PuzzleGame
{
    /// <summary>
    /// Composition root consumer — receives DI injections and initializes game-wide singletons.
    /// Per-feature logic lives in dedicated controllers: <see cref="LevelFlowController"/>,
    /// <see cref="WinLoseEvaluator"/>, <see cref="HudPresenter"/>. This class is intentionally
    /// slim: target FPS, audio boot, scene-unload cleanup, and DI failure reporting only.
    /// </summary>
    public class GameManager : MonoBehaviour, IUpdateable
    {
        [Header("UI References")]
        [SerializeField] private LevelSelectUI levelSelectUI;
        [SerializeField] private HudPresenter hudPresenter;

        [Header("DI Failure UI")]
        [SerializeField] private GameObject diErrorPanel;

        [Header("Error Reporting")]
        [SerializeField] private string diErrorMessage =
            "VContainer DI failed — GameInstaller (LifetimeScope) not found or not configured.\n" +
            "Fix: Tools > PuzzleGame > Open Editor > Scene tab > 'Setup Current Scene (GameManager + DI)'";

        private GameConfig _gameConfig;
        private AudioConfig _audioConfig;
        private IShaderOptimizer _shaderOptimizer;
        private IEventAggregator _events;
        private IUpdateManager _updateManager;
        private IInputHandlerService _inputHandlerService;
        private IGameStateMachine _stateMachine;
        private LevelFlowController _levelFlow;

        private bool _isInitialized;

        [Inject]
        public void Construct(
            GameConfig gameConfig,
            AudioConfig audioConfig,
            IShaderOptimizer shaderOptimizer,
            IEventAggregator eventAggregator,
            IUpdateManager updateManager,
            IInputHandlerService inputHandlerService,
            IGameStateMachine stateMachine,
            LevelFlowController levelFlow)
        {
            _gameConfig = gameConfig;
            _audioConfig = audioConfig;
            _shaderOptimizer = shaderOptimizer;
            _events = eventAggregator;
            _updateManager = updateManager;
            _inputHandlerService = inputHandlerService;
            _stateMachine = stateMachine;
            _levelFlow = levelFlow;

            _isInitialized = true;
        }

        private void Start()
        {
            if (!_isInitialized)
            {
                MoldLogger.LogError(diErrorMessage);
                if (diErrorPanel != null) diErrorPanel.SetActive(true);
                if (hudPresenter != null) hudPresenter.ShowDIError(diErrorMessage);
                enabled = false;
                return;
            }

            MoldLogger.LogInfo("GameManager Start — initializing game systems.");

            double refreshRate = Screen.currentResolution.refreshRateRatio.value;
            UnityEngine.Application.targetFrameRate = refreshRate > 0
                ? (int)Math.Round(refreshRate)
                : 60;
            MoldLogger.LogInfo($"Target frame rate set to: {UnityEngine.Application.targetFrameRate} FPS");

            _shaderOptimizer?.Initialize(_gameConfig != null && _gameConfig.applyMobileShaderDefaults);
            InitAudio();

            SceneManager.sceneUnloaded += OnSceneUnloaded;
            _events.Subscribe<GameStateChangedEvent>(OnGameStateChanged);

            _updateManager?.Register(this);
        }

        private void InitAudio()
        {
            if (_audioConfig == null)
            {
                MoldLogger.LogWarning("AudioConfig is null — audio init skipped.");
            }
        }

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            if (e.Current == GameState.Menu)
            {
                if (levelSelectUI != null) levelSelectUI.gameObject.SetActive(true);
            }
            else if (levelSelectUI != null)
            {
                levelSelectUI.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _updateManager?.Unregister(this);
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            _events?.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            MoldLogger.LogDebug("Scene unloaded — clearing event aggregator subscriptions.");
            _events?.Clear();
        }

        public void OnUpdate(float deltaTime)
        {
            _inputHandlerService?.ProcessInput();
        }
    }
}
