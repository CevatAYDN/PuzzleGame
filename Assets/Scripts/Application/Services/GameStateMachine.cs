using System;
using System.Collections.Generic;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Events;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Pure POCO state machine. Test edilebilir, Unity bağımlılığı yok.
    /// EventAggregator üzerinden global event publish eder.
    /// </summary>
    public class GameStateMachine : IGameStateMachine
    {
        private readonly Dictionary<(GameState, GameState), Func<bool>> _rules
            = new Dictionary<(GameState, GameState), Func<bool>>();

        private GameState _current = GameState.Boot;
        private GameState _previous = GameState.Boot;

        public GameState Current => _current;
        public GameState Previous => _previous;
        private readonly IEventAggregator _eventAggregator;

        public event Action<GameState, GameState> OnStateChanged;

        public GameStateMachine(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            // Default guard'lar — her kural CanTransitionTo() içinde override edilebilir
            // Boot → her yere
            _rules[(GameState.Boot, GameState.Menu)] = () => true;
            // Menu → Playing/LevelLoading
            _rules[(GameState.Menu, GameState.LevelLoading)] = () => true;
            // LevelLoading → Playing
            _rules[(GameState.LevelLoading, GameState.Playing)] = () => true;
            // Playing → Paused/LevelComplete/LevelFailed
            _rules[(GameState.Playing, GameState.Paused)] = () => true;
            _rules[(GameState.Playing, GameState.LevelComplete)] = () => true;
            _rules[(GameState.Playing, GameState.LevelFailed)] = () => true;
            // Paused → Playing (resume)
            _rules[(GameState.Paused, GameState.Playing)] = () => true;
            // LevelComplete/Failed → Menu/LevelLoading
            _rules[(GameState.LevelComplete, GameState.Menu)] = () => true;
            _rules[(GameState.LevelComplete, GameState.LevelLoading)] = () => true;
            _rules[(GameState.LevelFailed, GameState.LevelLoading)] = () => true;
            _rules[(GameState.LevelFailed, GameState.Menu)] = () => true;
            // Any → Menu (escape)
            _rules[(GameState.Playing, GameState.Menu)] = () => true;
            // GameOver → Menu
            _rules[(GameState.GameOver, GameState.Menu)] = () => true;
        }

        public bool IsInState(GameState state) => _current == state;

        public bool CanTransitionTo(GameState next)
        {
            if (_current == next) return false;
            if (_rules.TryGetValue((_current, next), out var guard))
                return guard();
            return false;
        }

        public bool TransitionTo(GameState next)
        {
            if (_current == next) return false;
            if (!CanTransitionTo(next)) return false;

            var prev = _current;
            _previous = prev;
            _current = next;

            OnStateChanged?.Invoke(prev, next);
            _eventAggregator.Publish(new GameStateChangedEvent(prev, next));
            return true;
        }

        public bool RevertToPrevious()
        {
            return TransitionTo(_previous);
        }

        public void RegisterTransitionRule(GameState from, GameState to, Func<bool> guard)
        {
            _rules[(from, to)] = guard ?? (() => true);
        }
    }
}
