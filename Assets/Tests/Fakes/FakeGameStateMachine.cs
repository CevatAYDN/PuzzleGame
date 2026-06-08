using System;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for IGameStateMachine.
    /// </summary>
    public class FakeGameStateMachine : IGameStateMachine
    {
        public GameState Current { get; set; } = GameState.Menu;
        public GameState Previous { get; set; } = GameState.Menu;

#pragma warning disable CS0618
        public event Action<GameState, GameState> OnStateChanged;
#pragma warning restore CS0618

        public int TransitionToCallCount { get; private set; }
        public GameState LastTransitionTo { get; private set; }
        public bool TransitionResult { get; set; } = true;

        public bool IsInState(GameState state) => Current == state;

        public bool TransitionTo(GameState next)
        {
            TransitionToCallCount++;
            LastTransitionTo = next;
            if (!TransitionResult) return false;

            var old = Current;
            Previous = old;
            Current = next;
#pragma warning disable CS0618
            OnStateChanged?.Invoke(old, next);
#pragma warning restore CS0618
            return true;
        }

        public bool RevertToPrevious()
        {
            return TransitionTo(Previous);
        }

        public void RegisterTransitionRule(GameState from, GameState to, Func<bool> guard)
        {
            // No-op for tests
        }

        public bool CanTransitionTo(GameState next)
        {
            return TransitionResult;
        }
    }
}
