using System;
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
    public sealed class LevelFlowController : IDisposable
    {
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

        private int _levelsPlayedSinceLastAd;

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
            GameConfig gameConfig)
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

            _events.Subscribe<LevelSelectedEvent>(OnLevelSelected);
        }

        public void Dispose()
        {
            _events.Unsubscribe<LevelSelectedEvent>(OnLevelSelected);
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
            _stateMachine.TransitionTo(GameState.Menu);
        }

        public void RestartCurrent()
        {
            if (_stateMachine.IsInState(GameState.LevelLoading) ||
                _stateMachine.IsInState(GameState.LevelComplete) ||
                _stateMachine.IsInState(GameState.LevelFailed))
            {
                var current = _pool.Molds;
                if (current == null || current.Length == 0) return;
                int level = current[0].MoldIndex;
                _events.Publish(new LevelSelectedEvent(level));
            }
        }

        private void OnLevelSelected(LevelSelectedEvent e)
        {
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

                if (adsEnabled)
                {
                    _levelsPlayedSinceLastAd++;
                    if (_levelsPlayedSinceLastAd >= interval)
                    {
                        _levelsPlayedSinceLastAd = 0;
                        if (_adService != null && _adService.IsInterstitialReady())
                        {
                            MoldLogger.LogInfo("[AdFlow] Showing interstitial ad before loading level.");
                            _adService.ShowInterstitialAd(() => LoadLevelInternal(level));
                            return;
                        }
                    }
                }
            }

            LoadLevelInternal(level);
        }

        private void LoadLevelInternal(LevelData level)
        {
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
        }
    }
}
