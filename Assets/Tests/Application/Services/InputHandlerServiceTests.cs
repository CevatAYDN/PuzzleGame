using NUnit.Framework;
using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Infrastructure.Implementations;
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
        private MoldInputRouter _router;
        private EventAggregator _eventAggregator;
        private FakeInputHandler _inputHandler;
        private FakeGameStateMachine _stateMachine;
        private FakeAnimationService _animationService;
        private FakeMoldSelectionService _selectionService;
        private FakeMoldValidator _validator;
        private FakeAudioService _audioService;
        private FakeHistoryManager _historyManager;
        private FakeCastService _CastService;
        private FakeHapticFeedbackService _hapticService;
        private GameConfig _gameConfig;
        private AnimationConfig _animConfig;
        private FakeActiveMoldsProvider _activeMoldsProvider;

        [SetUp]
        public void SetUp()
        {
            MoldLogger.SetLevel(MoldLogger.Level.Error, false);
            _eventAggregator = new EventAggregator();

            _inputHandler = new FakeInputHandler();
            _stateMachine = new FakeGameStateMachine();
            _animationService = new FakeAnimationService();
            _selectionService = new FakeMoldSelectionService();
            _validator = new FakeMoldValidator();
            _audioService = new FakeAudioService();
            _historyManager = new FakeHistoryManager();
            _CastService = new FakeCastService();
            _hapticService = new FakeHapticFeedbackService();

            _gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            _gameConfig.MoldLayerMask = 1 << 8;
            _animConfig = ScriptableObject.CreateInstance<AnimationConfig>();
            _animConfig.liftHeight = 1f;
            _animConfig.liftDuration = 0.4f;
            _animConfig.CastDuration = 0.6f;

            // Sprint #11: InputHandlerService is now a thin facade composed
            // of 3 focused services (MoldInputRouter, MoldLookupCache,
            // InputHandlerDefaults). The test exercises the full facade
            // surface (ProcessInput, SetMolds) which delegates to the router
            // and cache respectively.
            var lookup = new MoldLookupCache();
            var defaults = new InputHandlerDefaults();
            _activeMoldsProvider = new FakeActiveMoldsProvider();
            _router = new MoldInputRouter(
                _inputHandler, Camera.main, _stateMachine,
                _animationService, _selectionService, _validator,
                _gameConfig, _animConfig, _audioService,
                _historyManager, _CastService, lookup, defaults, _activeMoldsProvider,
                _hapticService,
                new NoOpAnalyticsService(),
                multiPourService: null);
            _sut = new InputHandlerService(_router, lookup, defaults);
        }

        [TearDown]
        public void TearDown()
        {
            _eventAggregator?.Clear();
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
            var Mold = CreateMoldView();
            Mold.State.AddLayer(new OreLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            SetMolds(new[] { Mold });
            _selectionService.Select(Mold.State);

            _inputHandler.SimulateClick(Vector2.zero, raycastSuccess: false);

            _sut.ProcessInput();

            Assert.That(_selectionService.SelectedMold, Is.Null);
            Assert.That(_animationService.AnimateMoldLowerCallCount, Is.EqualTo(1));
        }

        // ── Mold selection ──────────────────────────────────────────────────

        [Test]
        public void HandleInput_NoSelection_SelectsMold()
        {
            _stateMachine.TransitionTo(GameState.Playing);
            var Mold = CreateMoldView();
            Mold.State.AddLayer(new OreLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            SetMolds(new IMoldView[] { Mold });

            SetupRaycastHit(Mold);

            _sut.ProcessInput();

            Assert.That(_selectionService.SelectedMold, Is.EqualTo(Mold.State));
            Assert.That(_animationService.AnimateMoldLiftCallCount, Is.EqualTo(1));
        }

        [Test]
        public void HandleInput_SelectedCappedMold_LogsAndSkips()
        {
            _stateMachine.TransitionTo(GameState.Playing);
            var Mold = CreateMoldView();
            Mold.IsCapped = true;
            SetMolds(new IMoldView[] { Mold });

            SetupRaycastHit(Mold);

            _sut.ProcessInput();

            Assert.That(_selectionService.SelectedMold, Is.Null);
        }

        [Test]
        public void HandleInput_SelectedEmptyMold_LogsAndSkips()
        {
            _stateMachine.TransitionTo(GameState.Playing);
            var Mold = CreateMoldView(); // Already empty
            SetMolds(new IMoldView[] { Mold });

            SetupRaycastHit(Mold);

            _sut.ProcessInput();

            Assert.That(_selectionService.SelectedMold, Is.Null);
        }

        // ── Cast ──────────────────────────────────────────────────────────────

        [Test]
        public void HandleInput_SecondMold_AttemptsCast()
        {
            _stateMachine.TransitionTo(GameState.Playing);
            var sourceMold = CreateMoldView();
            sourceMold.State.AddLayer(new OreLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            var targetMold = CreateMoldView();
            SetMolds(new IMoldView[] { sourceMold, targetMold });

            // First click: select source
            SetupRaycastHit(sourceMold);
            _sut.ProcessInput();
            Assert.That(_selectionService.SelectedMold, Is.EqualTo(sourceMold.State));

            // Second click: Cast to target
            SetupRaycastHit(targetMold);
            _CastService.TryCastResult = true;
            _sut.ProcessInput();

            Assert.That(_CastService.TryCastCallCount, Is.EqualTo(1));
            Assert.That(_animationService.AnimateCastCallCount, Is.EqualTo(1));
            Assert.That(_selectionService.SelectedMold, Is.Null);
        }

        [Test]
        public void HandleInput_CastFails_PlaysErrorShake()
        {
            _stateMachine.TransitionTo(GameState.Playing);
            var sourceMold = CreateMoldView();
            sourceMold.State.AddLayer(new OreLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            var targetMold = CreateMoldView();
            SetMolds(new IMoldView[] { sourceMold, targetMold });

            SetupRaycastHit(sourceMold);
            _sut.ProcessInput();

            SetupRaycastHit(targetMold);
            _CastService.TryCastResult = false;
            _sut.ProcessInput();

            Assert.That(_animationService.AnimateErrorShakeCallCount, Is.EqualTo(1));
            Assert.That(_audioService.PlaySfxCallCount, Is.EqualTo(1));
            Assert.That(_audioService.LastSfxId, Is.EqualTo(AudioClipId.Error));
        }

        // ── Self-click (deselect) ─────────────────────────────────────────────

        [Test]
        public void HandleInput_ClickSameMold_Deselects()
        {
            _stateMachine.TransitionTo(GameState.Playing);
            var Mold = CreateMoldView();
            Mold.State.AddLayer(new OreLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            SetMolds(new IMoldView[] { Mold });

            SetupRaycastHit(Mold);
            _sut.ProcessInput();
            Assert.That(_selectionService.SelectedMold, Is.Not.Null);

            SetupRaycastHit(Mold);
            _sut.ProcessInput();

            Assert.That(_selectionService.SelectedMold, Is.Null);
            Assert.That(_animationService.AnimateMoldLowerCallCount, Is.EqualTo(1));
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void SetMolds(IMoldView[] molds)
        {
            _sut.SetMolds(molds);
            _activeMoldsProvider.Molds = molds;
        }

        private FakeMoldView CreateMoldView()
        {
            var state = new MoldState(4);
            var go = new GameObject("TestMold");
            go.AddComponent<BoxCollider>();
            var view = new FakeMoldView(state)
            {
                GameObject = go,
                Transform = go.transform,
                Height = 2f
            };
            return view;
        }

        private void SetupRaycastHit(IMoldView Mold)
        {
            Collider collider = null;
            if (Mold != null && Mold.GameObject != null)
            {
                collider = Mold.GameObject.GetComponent<Collider>();
                if (collider == null)
                {
                    collider = Mold.GameObject.AddComponent<BoxCollider>();
                }
            }

            _inputHandler.SimulateClick(Vector2.zero, raycastSuccess: true, collider: collider);
            _inputHandler.RaycastColliderResult = collider;

            // Reset the per-frame guard so the next ProcessInput is not silently dropped.
            // Multiple clicks within the same test frame must all be processed.
            _router?.ResetFrameGuardForTests();
        }
    }
}
