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
        private PlayTestBootstrap _playTestBootstrap;
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
            PlayTestBootstrap playTestBootstrap,
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
            _playTestBootstrap = playTestBootstrap;
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

            if (_playTestBootstrap != null)
            {
                _playTestBootstrap.Initialize(_moldPoolInitializer, _stateMachine);
                if (_playTestBootstrap.TryEnterPlayTestMode(IsFallbackMenuActive()))
                {
                    return;
                }
            }

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

        private void OnOnboardingCompleted()
        {
            if (_onboardingFlow != null)
            {
                _onboardingFlow.OnCompletedFlow -= OnOnboardingCompleted;
            }

            bool enteredPlayTest = false;
            if (_playTestBootstrap != null)
            {
                _playTestBootstrap.Initialize(_moldPoolInitializer, _stateMachine);
                enteredPlayTest = _playTestBootstrap.TryEnterPlayTestMode(IsFallbackMenuActive());
            }

            if (!enteredPlayTest)
            {
                _stateMachine?.TransitionTo(GameState.Menu);
            }
        }

        /// <summary>
        /// True when the assigned MainMenuController is missing or has been synthesised
        /// by DI as a fallback (i.e. no real menu prefab is in the scene).
        /// </summary>
        private bool IsFallbackMenuActive()
        {
            if (mainMenuController == null) return true;
            return mainMenuController is IFallbackMarker fm && fm.IsFallback;
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
            if (pause)
            {
                if (_analytics == null) return;
                float durationSec = Time.realtimeSinceStartup - _sessionStartTime;
                _analytics.Track(AnalyticsEvent.SessionEnd, new Dictionary<string, object>
                {
                    { "durationSec", durationSec }
                });
            }
            else
            {
                // Foreground dönüşü: eğer oyun Paused ise resume et. Sahne sahibi
                // tüketiciler (WinLoseEvaluator vb.) zaten kendi state'lerini
                // GameStateChangedEvent üzerinden yönetir; bu sadece state-machine
                // tarafındaki deadlock'u önler.
                if (_stateMachine != null && _stateMachine.IsInState(GameState.Paused))
                {
                    _stateMachine.TransitionTo(GameState.Playing);
                }
            }
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

            // Fix #M7: Clear static MoldCorkController caches to prevent memory leaks
            // across scene loads. This cleans up the shared mesh/material cache.
            MoldCorkController.ClearCache();

            _moldPoolInitializer?.Dispose();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            // Fix #3: REMOVED `_events?.Clear()`. EventAggregator is registered as
            // Lifetime.Singleton in GameInstaller (DontDestroyOnLoad scope). Many
            // long-lived subscribers (LevelFlowController, WinLoseEvaluator, etc.)
            // are also singletons and live across scene transitions. Clearing the
            // aggregator on every scene unload silently drops their subscriptions
            // — the next LevelSelectedEvent after a scene change would never be
            // handled, and the game would appear frozen.
            //
            // Per-scope cleanup should be the responsibility of the LifetimeScope
            // itself (VContainer handles it when the scope is disposed). If scene-
            // scoped subscribers are needed in the future, introduce a separate
            // IScopedEventAggregator rather than nuking the global one here.
            MoldLogger.LogDebug("Scene unloaded — leaving EventAggregator subscriptions intact.");
        }

        public void OnUpdate(float deltaTime)
        {
            _inputHandlerService?.ProcessInput();
        }
    }
}
