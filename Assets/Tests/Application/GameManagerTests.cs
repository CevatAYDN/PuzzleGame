using NUnit.Framework;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Configuration.FeatureSystem;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Tests.Fakes;

namespace PuzzleGame.Tests.Application
{
    public class GameManagerTests
    {
        private FakeHistoryManager _historyManager;
        private FakeBottleSelectionService _selectionService;
        private FakeAnimationService _animationService;
        private FakeAudioService _audioService;
        private FakePourService _pourService;
        private FakeBottleValidator _validator;
        private FakeGameStateMachine _stateMachine;

        [SetUp]
        public void SetUp()
        {
            BottleLogger.SetLevel(BottleLogger.Level.Error, false);
            EventAggregator.Clear();

            _historyManager = new FakeHistoryManager();
            _selectionService = new FakeBottleSelectionService();
            _animationService = new FakeAnimationService();
            _audioService = new FakeAudioService();
            _pourService = new FakePourService();
            _validator = new FakeBottleValidator();
            _stateMachine = new FakeGameStateMachine();
        }

        [TearDown]
        public void TearDown()
        {
            EventAggregator.Clear();
        }

        // ── PourService ───────────────────────────────────────────────────────

        [Test]
        public void PourService_SingleLayer_Success()
        {
            var source = CreateBottleWithLayer(new DomainColor(1f, 0.2f, 0.2f));
            var target = CreateEmptyBottle();
            var levelData = CreateLevelData(enableMultiLayer: false);

            var sut = new PourService(_validator, _historyManager, reactionService: new FakeReactionService());
            _validator.CanPourResult = true;

            bool result = sut.TryPour(source, target, levelData);

            Assert.That(result, Is.True);
            Assert.That(source.State.IsEmpty, Is.True);
            Assert.That(target.State.LayerCount, Is.EqualTo(1));
        }

        [Test]
        public void PourService_MultiLayer_TwoSameColors_PoursBoth()
        {
            var source = new BottleState(4);
            source.AddLayer(new LiquidLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            source.AddLayer(new LiquidLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            var target = new BottleState(4);
            var levelData = CreateLevelData(enableMultiLayer: true);

            var sv = new FakeBottleView(source) { GameObject = new UnityEngine.GameObject("S"), Transform = new UnityEngine.GameObject("ST").transform };
            var tv = new FakeBottleView(target) { GameObject = new UnityEngine.GameObject("T"), Transform = new UnityEngine.GameObject("TT").transform };

            var sut = new PourService(_validator, _historyManager, new FakeReactionService());

            int count = sut.GetPourLayerCount(sv, tv, levelData);
            Assert.That(count, Is.EqualTo(2));
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static FakeBottleView CreateBottleWithLayer(DomainColor color)
        {
            var state = new BottleState(4);
            state.AddLayer(new LiquidLayer(color, 1f));
            var go = new UnityEngine.GameObject("TestBottle");
            return new FakeBottleView(state) { GameObject = go, Transform = go.transform };
        }

        private static FakeBottleView CreateEmptyBottle()
        {
            var state = new BottleState(4);
            var go = new UnityEngine.GameObject("TestBottle");
            return new FakeBottleView(state) { GameObject = go, Transform = go.transform };
        }

        private static LevelData CreateLevelData(bool enableMultiLayer = false)
        {
            return new LevelData
            {
                levelNumber = 1,
                enableMultiLayerPour = enableMultiLayer,
                multiLayerPourConfig = new PuzzleGame.Application.Configuration.FeatureSystem.MultiLayerPourData
                {
                    pourAllMatching = true,
                    pourConsecutiveOnly = true,
                    minConsecutiveForPour = 2
                }
            };
        }
    }
}
