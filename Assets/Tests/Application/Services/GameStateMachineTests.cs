using NUnit.Framework;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Events;

namespace PuzzleGame.Tests.Application.Services
{
    public class GameStateMachineTests
    {
        private GameStateMachine _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new GameStateMachine();
        }

        [TearDown]
        public void Teardown()
        {
            EventAggregator.Clear();
        }

        [Test]
        public void InitialState_IsBoot()
        {
            Assert.That(_sut.Current, Is.EqualTo(GameState.Boot));
        }

        [Test]
        public void Transition_BootToMenu_Succeeds()
        {
            bool result = _sut.TransitionTo(GameState.Menu);
            Assert.That(result, Is.True);
            Assert.That(_sut.Current, Is.EqualTo(GameState.Menu));
            Assert.That(_sut.Previous, Is.EqualTo(GameState.Boot));
        }

        [Test]
        public void Transition_SameState_ReturnsFalse()
        {
            _sut.TransitionTo(GameState.Menu);
            bool result = _sut.TransitionTo(GameState.Menu);
            Assert.That(result, Is.False);
        }

        [Test]
        public void TransitionTo_NoExplicitRule_DefaultAllowsTransition()
        {
            // Boot → Playing: kural tanımlı değil ama default guard true döner
            bool result = _sut.TransitionTo(GameState.Playing);
            Assert.That(result, Is.True); // default guard: true
        }

        [Test]
        public void IsInState_AfterTransition_True()
        {
            _sut.TransitionTo(GameState.Menu);
            Assert.That(_sut.IsInState(GameState.Menu), Is.True);
            Assert.That(_sut.IsInState(GameState.Boot), Is.False);
        }

        [Test]
        public void RevertToPrevious_RestoresPreviousState()
        {
            _sut.TransitionTo(GameState.Menu);
            _sut.TransitionTo(GameState.LevelLoading);
            bool reverted = _sut.RevertToPrevious();

            Assert.That(reverted, Is.True);
            Assert.That(_sut.Current, Is.EqualTo(GameState.Menu));
        }

        [Test]
        public void RevertToPrevious_WithNoPrevious_StillBoot()
        {
            // Initially _previous == Boot == _current, revert should be no-op
            bool reverted = _sut.RevertToPrevious();
            Assert.That(reverted, Is.False);
            Assert.That(_sut.Current, Is.EqualTo(GameState.Boot));
        }

        [Test]
        public void OnStateChanged_EventFires()
        {
            GameState? firedPrev = null;
            GameState? firedCurr = null;
            _sut.OnStateChanged += (prev, curr) => { firedPrev = prev; firedCurr = curr; };

            _sut.TransitionTo(GameState.Menu);

            Assert.That(firedPrev, Is.EqualTo(GameState.Boot));
            Assert.That(firedCurr, Is.EqualTo(GameState.Menu));
        }

        [Test]
        public void EventAggregator_EventPublished_OnTransition()
        {
            GameStateChangedEvent? received = null;
            EventAggregator.Subscribe<GameStateChangedEvent>(e => received = e);

            _sut.TransitionTo(GameState.Menu);

            Assert.That(received.HasValue, Is.True);
            Assert.That(received.Value.Previous, Is.EqualTo(GameState.Boot));
            Assert.That(received.Value.Current, Is.EqualTo(GameState.Menu));
        }

        [Test]
        public void RegisterTransitionRule_CustomGuard_BlocksTransition()
        {
            _sut.RegisterTransitionRule(GameState.Menu, GameState.LevelLoading, () => false);

            _sut.TransitionTo(GameState.Menu);
            bool result = _sut.TransitionTo(GameState.LevelLoading);

            Assert.That(result, Is.False);
            Assert.That(_sut.Current, Is.EqualTo(GameState.Menu));
        }

        [Test]
        public void FullFlow_BootToMenuToPlaying_Works()
        {
            Assert.That(_sut.TransitionTo(GameState.Menu), Is.True);
            Assert.That(_sut.TransitionTo(GameState.LevelLoading), Is.True);
            Assert.That(_sut.TransitionTo(GameState.Playing), Is.True);
            Assert.That(_sut.IsInState(GameState.Playing), Is.True);
        }
    }
}
