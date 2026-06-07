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
    /// Listens to <see cref="CastCompletedEvent"/> and decides whether the level is solved.
    /// On win, transitions to <see cref="GameState.LevelComplete"/> or <see cref="GameState.OptionalCasting"/>
    /// depending on the level's optional targets.
    /// SRP: only "is the puzzle solved?" and the state transition that follows.
    /// </summary>
    public sealed class WinLoseEvaluator : IDisposable
    {
        private readonly IGameStateMachine _stateMachine;
        private readonly IMoldValidator _validator;
        private readonly IAudioService _audio;
        private readonly ILevelProgressService _progress;
        private readonly IGameHistoryManager _history;
        private readonly ITweenService _tween;
        private readonly IEventAggregator _events;
        private readonly IActiveMoldsProvider _pool;
        private readonly IAnalyticsService _analytics;
        private readonly System.Diagnostics.Stopwatch _levelStopwatch = new System.Diagnostics.Stopwatch();
        private LevelData _currentLevel;

        public WinLoseEvaluator(
            IGameStateMachine stateMachine,
            IMoldValidator validator,
            IAudioService audio,
            ILevelProgressService progress,
            IGameHistoryManager history,
            ITweenService tween,
            IEventAggregator events,
            IActiveMoldsProvider pool,
            IAnalyticsService analytics)
        {
            _stateMachine = stateMachine;
            _validator = validator;
            _audio = audio;
            _progress = progress;
            _history = history;
            _tween = tween;
            _events = events;
            _pool = pool;
            _analytics = analytics;

            _events.Subscribe<CastCompletedEvent>(OnCastCompleted);
            _events.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
        }

        public void Dispose()
        {
            _events.Unsubscribe<CastCompletedEvent>(OnCastCompleted);
            _events.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);
        }

        public void CompleteWithOptionalRewards()
        {
            if (!_stateMachine.IsInState(GameState.OptionalCasting)) return;
            FinalizeLevel();
        }

        private void OnLevelLoaded(LevelLoadedEvent e)
        {
            _currentLevel = e.Level;
            _levelStopwatch.Restart();
        }

        private void OnCastCompleted(CastCompletedEvent e)
        {
            _tween.Delay(0.5f).OnComplete(CheckWin).Start();
        }

        private bool _isWon = false;
        
        private void CheckWin()
        {
            if (_isWon) return;
            
            var molds = _pool.Molds;
            if (molds == null || molds.Length == 0) return;
            
            bool hasOre = false;
            bool allComplete = true;
            
            foreach (var view in molds)
            {
                if (view == null || view.IsEmpty) continue;
                hasOre = true;
                
                bool isComplete = _validator.IsComplete(view.State);
                if (isComplete && !view.IsCapped)
                {
                    view.AnimateCompletion();
                }
                
                if (!isComplete) allComplete = false;
            }
            
            if (!allComplete || !hasOre) return;
            
            _isWon = true;
            
            _audio.PlaySfx(AudioClipId.LevelComplete);
            
            if (_currentLevel != null && _currentLevel.optionalTargets != null && _currentLevel.optionalTargets.Count > 0)
            {
                _stateMachine.TransitionTo(GameState.OptionalCasting);
                _pool.ActivateOptionalMolds(_currentLevel);
                MoldLogger.LogInfo("All main crucibles complete — entered OptionalCasting.");
            }
            else
            {
                FinalizeLevel();
            }
        }

        private void FinalizeLevel()
        {
            _stateMachine.TransitionTo(GameState.LevelComplete);

            int moveCount = _history.CurrentMoveCount;
            int stars = _currentLevel != null ? _currentLevel.CalculateStars(moveCount) : 3;

            if (_currentLevel != null && _currentLevel.optionalTargets != null && _currentLevel.optionalTargets.Count > 0)
            {
                var molds = _pool.Molds;
                if (molds != null)
                {
                    foreach (var mold in molds)
                    {
                        if (mold != null && mold.GameObject.name.StartsWith("Optional_") && !mold.IsEmpty)
                        {
                            stars = Math.Min(3, stars + 1);
                            MoldLogger.LogInfo("Optional casting target filled — Perfect Forge bonus.");
                            break;
                        }
                    }
                }
            }

            if (_currentLevel != null)
            {
                _progress.RecordCompletion(_currentLevel.levelNumber, moveCount, stars);
            }

            _levelStopwatch.Stop();
            _analytics.Track(AnalyticsEvent.LevelCompleted, new Dictionary<string, object>
            {
                { "levelNumber", _currentLevel?.levelNumber ?? 0 },
                { "moveCount", moveCount },
                { "stars", stars },
                { "durationSec", (float)_levelStopwatch.Elapsed.TotalSeconds }
            });

            _events.Publish(new LevelCompletedEvent(moveCount));
        }
    }
}
