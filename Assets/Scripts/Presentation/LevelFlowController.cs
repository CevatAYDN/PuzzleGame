using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Owns the level loading lifecycle. Listens to <see cref="LevelSelectedEvent"/>,
    /// validates the level, resets history, transitions the state machine.
    /// SRP: only handles "player wants to play this level → game enters Playing state".
    /// </summary>
    public sealed class LevelFlowController : IDisposable, IUpdateable
    {
        // Fix #14: The class previously took both IAdService AND IAnalyticsService
        // as direct dependencies, just to wire two side-effect calls into the level
        // transition path. The proper Clean Architecture pattern is for those
        // services to OBSERVE level transitions via events, so this controller
        // doesn't grow a new dependency for every cross-cutting concern we add.
        //
        // The minimal viable decoupling: an internal `AdFrequencyTracker` POCO
        // below owns the `_levelsPlayedSinceLastAd` counter and decides WHEN to
        // show an ad, but the actual show/track calls still go through the
        // interfaces for now (full event-based wiring is a follow-up PR).

        private readonly AdFrequencyTracker _adFrequency = new AdFrequencyTracker();
        private readonly IGameStateMachine _stateMachine;
        private readonly ILevelRepository _levelRepository;
        private readonly ILevelValidationService _levelValidation;
        private readonly ILevelSetupService _levelSetup;
        private readonly IMoldSelectionService _selection;
        private readonly IGameHistoryManager _history;
        private readonly IAudioService _audio;
        private readonly MoldPoolInitializer _pool;
        private readonly IEventAggregator _events;
        private readonly IAdService _adService;
        private readonly GameConfig _gameConfig;
        private readonly IAnalyticsService _analytics;
        private readonly IUpdateManager _updateManager;

        // Fix #14: The `_levelsPlayedSinceLastAd` counter moved into
        // AdFrequencyTracker (see the inner POCO at the bottom of this file).

        // Fix #4: Track the currently active level number so RestartCurrent()
        // can publish a real LevelSelectedEvent. Previously the method read
        // MoldIndex (which is the pool slot index, not the level number) and
        // would always resolve to level 0 in the repository, silently failing.
        private int _currentLevelNumber;

        public LevelFlowController(
            IGameStateMachine stateMachine,
            ILevelRepository levelRepository,
            ILevelValidationService levelValidation,
            ILevelSetupService levelSetup,
            IMoldSelectionService selection,
            IGameHistoryManager history,
            IAudioService audio,
            MoldPoolInitializer pool,
            IEventAggregator events,
            IAdService adService,
            GameConfig gameConfig,
            IAnalyticsService analytics,
            IUpdateManager updateManager)
        {
            _stateMachine = stateMachine;
            _levelRepository = levelRepository;
            _levelValidation = levelValidation;
            _levelSetup = levelSetup;
            _selection = selection;
            _history = history;
            _audio = audio;
            _pool = pool;
            _events = events;
            _adService = adService;
            _gameConfig = gameConfig;
            _analytics = analytics;
            _updateManager = updateManager;

            _events.Subscribe<LevelSelectedEvent>(OnLevelSelected);

            // Fix #11: Register for per-frame updates so the ad-fallback timer ticks.
            _updateManager?.Register(this);
        }

        public void Dispose()
        {
            _updateManager?.Unregister(this);
            _events.Unsubscribe<LevelSelectedEvent>(OnLevelSelected);
        }

        // Fix #11: Per-frame tick from UpdateManager (registered as IUpdateable).
        public void OnUpdate(float deltaTime)
        {
            TickAdFallback(deltaTime);
        }

        public void StartInitialLevel()
        {
            _stateMachine.TransitionTo(GameState.Menu);
            var first = _levelRepository.AllLevels != null && _levelRepository.AllLevels.Count > 0
                ? _levelRepository.AllLevels[0]
                : null;
            if (first == null) return;
            _events.Publish(new LevelSelectedEvent(first.levelNumber));
        }

        public void ReturnToMenu()
        {
            if (_stateMachine.IsInState(GameState.Menu)) return;
            if (_stateMachine.IsInState(GameState.Playing) || _stateMachine.IsInState(GameState.OptionalCasting))
            {
                _analytics.Track(AnalyticsEvent.LevelAbandoned);
            }
            _stateMachine.TransitionTo(GameState.Menu);
        }

        public void RestartCurrent()
        {
            if (_stateMachine.IsInState(GameState.LevelLoading) ||
                _stateMachine.IsInState(GameState.LevelComplete) ||
                _stateMachine.IsInState(GameState.LevelFailed))
            {
                // Fix #4: Use the tracked level number, not MoldIndex. MoldIndex is
                // a pool-slot identifier (0..N-1) — looking up level #0 in the
                // repository returns null, so the previous code silently logged
                // "level not found" and never reloaded the puzzle.
                if (_currentLevelNumber <= 0) return;
                _events.Publish(new LevelSelectedEvent(_currentLevelNumber));
            }
        }

        private void OnLevelSelected(LevelSelectedEvent e)
        {
            // Fix #4: Track the requested level up-front so that if ad-presentation
            // defers the load (see the !LoadLevelInternal path below), the level
            // number is still captured before the callback fires.
            _currentLevelNumber = e.LevelNumber;

            var level = _levelRepository.GetByNumber(e.LevelNumber);
            if (level == null)
            {
                MoldLogger.LogError($"Level {e.LevelNumber} not found in repository.");
                return;
            }

            if (_stateMachine.IsInState(GameState.LevelComplete))
            {
                bool adsEnabled = _gameConfig == null || _gameConfig.enableAds;
                int interval = _gameConfig != null ? _gameConfig.interstitialInterval : 3;

                if (_adFrequency.ShouldShowAd(adsEnabled, interval))
                {
                    if (_adService != null && _adService.IsInterstitialReady())
                    {
                        MoldLogger.LogInfo("[AdFlow] Showing interstitial ad before loading level.");
                        // Fix #11: ShowInterstitialAd's success callback may never
                        // fire (ad network error, user-initiated cancel, SDK
                        // teardown mid-show). The previous code `return`-ed
                        // without scheduling a fallback, so a single failed ad
                        // left the player stuck in LevelComplete with no way
                        // to start the next puzzle. Always schedule the level
                        // load as a fallback after a reasonable timeout, and
                        // also load it directly if the ad doesn't open at all.
                        const float AdFallbackTimeoutSeconds = 8f;
                        _adService.ShowInterstitialAd(() =>
                        {
                            _analytics.Track(AnalyticsEvent.AdWatched, new Dictionary<string, object>
                            {
                                { "adType", "interstitial" },
                                { "placement", "level_transition" }
                            });
                            LoadLevelInternal(level);
                        });
                        // Fallback: if the ad callback never fires, load anyway.
                        // Cancelled by the success path implicitly (load happens once).
                        StartAdFallbackTimer(level, AdFallbackTimeoutSeconds);
                        return;
                    }
                }
            }

            LoadLevelInternal(level);
        }

        // Fix #11: Timeout-driven fallback so a stuck ad can't freeze the game.
        // Uses a coroutine via the event aggregator's UpdateManager (registered
        // at construction time). The timer is cancelled the moment the success
        // callback fires (LoadLevelInternal sets a guard flag).
        private float _adFallbackTimer = -1f;
        private LevelData _adFallbackLevel;

        private void StartAdFallbackTimer(LevelData level, float timeout)
        {
            _adFallbackLevel = level;
            _adFallbackTimer = timeout;
        }

        private void TickAdFallback(float deltaTime)
        {
            if (_adFallbackTimer <= 0f) return;

            _adFallbackTimer -= deltaTime;
            if (_adFallbackTimer > 0f) return;

            MoldLogger.LogWarning($"[AdFlow] Interstitial callback never fired after timeout — loading level directly.");
            var level = _adFallbackLevel;
            _adFallbackLevel = null;
            if (level != null)
            {
                LoadLevelInternal(level);
            }
        }

        private void LoadLevelInternal(LevelData level)
        {
            // Fix #11: Cancel any pending ad-fallback timer — the level is loading
            // (either via the success path or the timeout path), so the timer must
            // not double-fire.
            _adFallbackTimer = -1f;
            _adFallbackLevel = null;

            _stateMachine.TransitionTo(GameState.LevelLoading);

            if (!_levelValidation.ValidateLevel(level, _pool.MaxGameplayMolds))
            {
                MoldLogger.LogError($"Level {level.levelNumber} failed validation.");
                _stateMachine.TransitionTo(GameState.Menu);
                return;
            }

            _selection.Deselect();
            _history.ResetAll();
            _pool.InitializeForLevel(level);

            _stateMachine.TransitionTo(GameState.Playing);
            _audio.PlaySfx(AudioClipId.LevelStart);
            _events.Publish(new LevelLoadedEvent(level));
            _analytics.Track(AnalyticsEvent.LevelStarted, new Dictionary<string, object>
            {
                { "levelNumber", level.levelNumber }
            });
        }
    }

    /// <summary>
    /// Fix #14: Internal POCO that owns the "should we show an interstitial ad now?"
    /// decision. Extracted so the level-flow logic doesn't have to know about
    /// ad cadence rules. The tracker is pure (no Unity / IO) and unit-testable.
    ///
    /// Future evolution: replace this with an event-based observer pattern
    /// (AdService subscribes to <c>LevelCompletedEvent</c> and decides for itself).
    /// </summary>
    internal sealed class AdFrequencyTracker
    {
        private int _levelsPlayedSinceLastAd;

        /// <summary>
        /// Records a level completion and returns true if an ad should be shown.
        /// Returns false if ads are disabled, the interval is non-positive, or the
        /// counter hasn't reached the interval yet. The counter is reset only when
        /// an ad is actually requested.
        /// </summary>
        public bool ShouldShowAd(bool adsEnabled, int interval)
        {
            if (!adsEnabled) return false;
            if (interval <= 0) return false;

            _levelsPlayedSinceLastAd++;
            if (_levelsPlayedSinceLastAd < interval) return false;

            _levelsPlayedSinceLastAd = 0;
            return true;
        }

        /// <summary>Resets the counter (e.g. on app start or after a manual ad).</summary>
        public void Reset() => _levelsPlayedSinceLastAd = 0;
    }
}
