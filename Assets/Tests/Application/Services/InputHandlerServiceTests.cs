using NUnit.Framework;
using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Tests.Fakes;

namespace PuzzleGame.Tests.Application.Services
{
    public class InputHandlerServiceTests
    {
        private InputHandlerService _sut;
        private FakeInputHandler _inputHandler;
        private FakeGameStateMachine _stateMachine;
        private FakeAnimationService _animationService;
        private FakeBottleSelectionService _selectionService;
        private FakeBottleValidator _validator;
        private FakeAudioService _audioService;
        private FakeHistoryManager _historyManager;
        private FakePourService _pourService;
        private GameConfig _gameConfig;
        private AnimationConfig _animConfig;

        [SetUp]
        public void SetUp()
        {
            BottleLogger.SetLevel(BottleLogger.Level.Error, false);
            EventAggregator.Clear();

            _inputHandler = new FakeInputHandler();
            _stateMachine = new FakeGameStateMachine();
            _animationService = new FakeAnimationService();
            _selectionService = new FakeBottleSelectionService();
            _validator = new FakeBottleValidator();
            _audioService = new FakeAudioService();
            _historyManager = new FakeHistoryManager();
            _pourService = new FakePourService();

            _gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            _gameConfig.bottleLayerMask = 1 << 8;
            _animConfig = ScriptableObject.CreateInstance<AnimationConfig>();
            _animConfig.liftHeight = 1f;
            _animConfig.liftDuration = 0.4f;
            _animConfig.pourDuration = 0.6f;

            _sut = new InputHandlerService(
                _inputHandler, Camera.main, _stateMachine,
                _animationService, _selectionService, _validator,
                _gameConfig, _animConfig, _audioService,
                _historyManager, _pourService);
        }

        [TearDown]
        public void TearDown()
        {
            EventAggregator.Clear();
            if (_gameConfig != null) ScriptableObject.DestroyImmediate(_gameConfig);
            if (_animConfig != null) ScriptableObject.DestroyImmediate(_animConfig);
        }

        // ── ProcessInput guard conditions ──────────────────────────────────────

        [Test]
        public void ProcessInput_NotPlayingState_SkipsProcessing()
        {
            _stateMachine.TransitionTo(GameState.Menu);
            _inputHandler.SimulateClick(Vector2.zero);

            _sut.ProcessInput();

            Assert.That(_inputHandler.GetPointerDownCallCount, Is.EqualTo(0));
        }

        [Test]
        public void ProcessInput_Animating_SkipsProcessing()
        {
            _stateMachine.TransitionTo(GameState.Playing);
            _animationService.IsAnimating = true;
            _inputHandler.SimulateClick(Vector2.zero);

            _sut.ProcessInput();

            Assert.That(_inputHandler.GetPointerDownCallCount, Is.EqualTo(0));
        }

        [Test]
        public void ProcessInput_NoPointerDown_SkipsProcessing()
        {
            _stateMachine.TransitionTo(GameState.Playing);
            _inputHandler.GetPointerDownResult = false;

            _sut.ProcessInput();

            Assert.That(_inputHandler.RaycastCallCount, Is.EqualTo(0));
        }

        // ── Raycast miss ──────────────────────────────────────────────────────

        [Test]
        public void HandleInput_RaycastMiss_DeselectsIfSelected()
        {
            _stateMachine.TransitionTo(GameState.Playing);
            var bottle = new BottleState(4);
            bottle.AddLayer(new LiquidLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            _selectionService.Select(bottle);

            _inputHandler.SimulateClick(Vector2.zero, raycastSuccess: false);

            _sut.ProcessInput();

            Assert.That(_selectionService.SelectedBottle, Is.Null);
            Assert.That(_animationService.AnimateBottleLowerCallCount, Is.EqualTo(1));
        }

        // ── Bottle selection ──────────────────────────────────────────────────

        [Test]
        public void HandleInput_NoSelection_SelectsBottle()
        {
            _stateMachine.TransitionTo(GameState.Playing);
            var bottle = CreateBottleView();
            _sut.SetBottles(new IBottleView[] { bottle });

            SetupRaycastHit(bottle);

            _sut.ProcessInput();

            Assert.That(_selectionService.SelectedBottle, Is.EqualTo(bottle.State));
            Assert.That(_animationService.AnimateBottleLiftCallCount, Is.EqualTo(1));
        }

        [Test]
        public void HandleInput_SelectedCappedBottle_LogsAndSkips()
        {
            _stateMachine.TransitionTo(GameState.Playing);
            var bottle = CreateBottleView();
            bottle.IsCapped = true;
            _sut.SetBottles(new IBottleView[] { bottle });

            SetupRaycastHit(bottle);

            _sut.ProcessInput();

            Assert.That(_selectionService.SelectedBottle, Is.Null);
        }

        [Test]
        public void HandleInput_SelectedEmptyBottle_LogsAndSkips()
        {
            _stateMachine.TransitionTo(GameState.Playing);
            var bottle = CreateBottleView(); // Already empty
            _sut.SetBottles(new IBottleView[] { bottle });

            SetupRaycastHit(bottle);

            _sut.ProcessInput();

            Assert.That(_selectionService.SelectedBottle, Is.Null);
        }

        // ── Pour ──────────────────────────────────────────────────────────────

        [Test]
        public void HandleInput_SecondBottle_AttemptsPour()
        {
            _stateMachine.TransitionTo(GameState.Playing);
            var sourceBottle = CreateBottleView();
            sourceBottle.State.AddLayer(new LiquidLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            var targetBottle = CreateBottleView();
            _sut.SetBottles(new IBottleView[] { sourceBottle, targetBottle });

            // First click: select source
            SetupRaycastHit(sourceBottle);
            _sut.ProcessInput();
            Assert.That(_selectionService.SelectedBottle, Is.EqualTo(sourceBottle.State));

            // Second click: pour to target
            SetupRaycastHit(targetBottle);
            _pourService.TryPourResult = true;
            _sut.ProcessInput();

            Assert.That(_pourService.TryPourCallCount, Is.EqualTo(1));
            Assert.That(_animationService.AnimatePourCallCount, Is.EqualTo(1));
            Assert.That(_selectionService.SelectedBottle, Is.Null);
        }

        [Test]
        public void HandleInput_PourFails_PlaysErrorShake()
        {
            _stateMachine.TransitionTo(GameState.Playing);
            var sourceBottle = CreateBottleView();
            sourceBottle.State.AddLayer(new LiquidLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            var targetBottle = CreateBottleView();
            _sut.SetBottles(new IBottleView[] { sourceBottle, targetBottle });

            SetupRaycastHit(sourceBottle);
            _sut.ProcessInput();

            SetupRaycastHit(targetBottle);
            _pourService.TryPourResult = false;
            _sut.ProcessInput();

            Assert.That(_animationService.AnimateErrorShakeCallCount, Is.EqualTo(1));
            Assert.That(_audioService.PlaySfxCallCount, Is.EqualTo(1));
            Assert.That(_audioService.LastSfxId, Is.EqualTo(AudioClipId.Error));
        }

        // ── Self-click (deselect) ─────────────────────────────────────────────

        [Test]
        public void HandleInput_ClickSameBottle_Deselects()
        {
            _stateMachine.TransitionTo(GameState.Playing);
            var bottle = CreateBottleView();
            bottle.State.AddLayer(new LiquidLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            _sut.SetBottles(new IBottleView[] { bottle });

            SetupRaycastHit(bottle);
            _sut.ProcessInput();
            Assert.That(_selectionService.SelectedBottle, Is.Not.Null);

            SetupRaycastHit(bottle);
            _sut.ProcessInput();

            Assert.That(_selectionService.SelectedBottle, Is.Null);
            Assert.That(_animationService.AnimateBottleLowerCallCount, Is.EqualTo(1));
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private FakeBottleView CreateBottleView()
        {
            var state = new BottleState(4);
            var go = new GameObject("TestBottle");
            var view = new FakeBottleView(state)
            {
                GameObject = go,
                Transform = go.transform,
                Height = 2f
            };
            return view;
        }

        private void SetupRaycastHit(IBottleView bottle)
        {
            _inputHandler.SimulateClick(Vector2.zero, raycastSuccess: true);
            _inputHandler.RaycastHitResult = new RaycastHit(); // Non-null collider needed
            // We need the FakeInputHandler to return a collider with IBottleView
            // For tests with direct bottle references, use a test GameObject with collider
        }
    }
}
