using System;
using System.Collections.Generic;
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
        [SerializeField] private PuzzleGame.Presentation.UI.MainMenuController mainMenuController;
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
        private OnboardingFlowController _onboardingFlow;
        private MoldPoolInitializer _moldPoolInitializer;
        private HapticObserver _hapticObserver;
        private IAnalyticsService _analytics;
        private float _sessionStartTime;

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
            LevelFlowController levelFlow,
            OnboardingFlowController onboardingFlow,
            MoldPoolInitializer moldPoolInitializer,
            HapticObserver hapticObserver,
            IAnalyticsService analytics)
        {
            _gameConfig = gameConfig;
            _audioConfig = audioConfig;
            _shaderOptimizer = shaderOptimizer;
            _events = eventAggregator;
            _updateManager = updateManager;
            _inputHandlerService = inputHandlerService;
            _stateMachine = stateMachine;
            _levelFlow = levelFlow;
            _onboardingFlow = onboardingFlow;
            _moldPoolInitializer = moldPoolInitializer;
            _hapticObserver = hapticObserver;
            _analytics = analytics;

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
            
            _sessionStartTime = Time.realtimeSinceStartup;
            
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
            
            // Check if we are in play-test mode (no real Main Menu in scene, but Molds exist)
            bool isFallbackMenu = mainMenuController == null || mainMenuController.gameObject.name.Contains("[Fallback]");
            var moldsInScene = FindObjectsByType<MoldController>(FindObjectsInactive.Exclude);
            bool isPlayTest = isFallbackMenu && moldsInScene.Length > 0;
            
            if (isPlayTest)
            {
                MoldLogger.LogInfo("[PlayTest] Fallback Menu detected with Molds in scene. Initializing Play-Test mode directly, skipping onboarding.");
                if (_moldPoolInitializer != null)
                {
                    _moldPoolInitializer.InitializeForLevel(null);
                }
                _stateMachine?.TransitionTo(GameState.Playing);
            }
            else
            {
                if (_onboardingFlow != null)
                {
                    _onboardingFlow.OnCompletedFlow += OnOnboardingCompleted;
                    _onboardingFlow.Run();
                }
                else
                {
                    _stateMachine?.TransitionTo(GameState.Menu);
                }
            }
        }

        private void OnOnboardingCompleted()
        {
            if (_onboardingFlow != null)
            {
                _onboardingFlow.OnCompletedFlow -= OnOnboardingCompleted;
            }

            // Check if we are in play-test mode (no real Main Menu in scene, but Molds exist)
            bool isFallbackMenu = mainMenuController == null || mainMenuController.gameObject.name.Contains("[Fallback]");
            var moldsInScene = FindObjectsByType<MoldController>(FindObjectsInactive.Exclude);

            if (isFallbackMenu && moldsInScene.Length > 0)
            {
                MoldLogger.LogInfo("[PlayTest] Fallback Menu detected with Molds in scene. Initializing Play-Test mode.");
                
                // Initialize the mold pool for playtesting (with null level, which triggers play-test initialization)
                if (_moldPoolInitializer != null)
                {
                    _moldPoolInitializer.InitializeForLevel(null);
                }

                // Transition state machine directly to Playing so inputs work
                _stateMachine?.TransitionTo(GameState.Playing);
            }
            else
            {
                // Normal flow: transition to Menu state
                _stateMachine?.TransitionTo(GameState.Menu);
            }
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
            // MainMenuController handles its own visibility via GameStateChangedEvent subscription.
            // Kept here as fallback for cases where MainMenuController is not assigned in Inspector.
            if (mainMenuController == null && e.Current == GameState.Menu)
            {
                MoldLogger.LogWarning("MainMenuController not assigned — falling back to legacy LevelSelect-only flow.");
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (!pause) return;
            if (_analytics == null) return;
            float durationSec = Time.realtimeSinceStartup - _sessionStartTime;
            _analytics.Track(AnalyticsEvent.SessionEnd, new Dictionary<string, object>
            {
                { "durationSec", durationSec }
            });
        }

        private void OnDestroy()
        {
            _updateManager?.Unregister(this);
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            _events?.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            if (_onboardingFlow != null)
            {
                _onboardingFlow.OnCompletedFlow -= OnOnboardingCompleted;
            }
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
