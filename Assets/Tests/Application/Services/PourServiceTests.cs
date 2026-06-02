using NUnit.Framework;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Models.FeatureSystem;
using PuzzleGame.Events;
using PuzzleGame.Logging;
using PuzzleGame.Tests.Fakes;

namespace PuzzleGame.Tests.Application.Services
{
    public class PourServiceTests
    {
        private PourService _sut;
        private FakeBottleValidator _validator;
        private FakeHistoryManager _historyManager;
        private FakeReactionService _reactionService;

        private BottleState CreateBottle(int maxLayers = 4)
        {
            return new BottleState(maxLayers);
        }

        private FakeBottleView CreateView(BottleState state)
        {
            var go = new UnityEngine.GameObject("TestBottle");
            return new FakeBottleView(state) { GameObject = go, Transform = go.transform };
        }

        [SetUp]
        public void SetUp()
        {
            BottleLogger.SetLevel(BottleLogger.Level.Error, false);
            EventAggregator.Clear();
            _validator = new FakeBottleValidator();
            _historyManager = new FakeHistoryManager();
            _reactionService = new FakeReactionService();
            _sut = new PourService(_validator, _historyManager, _reactionService);
        }

        [TearDown]
        public void TearDown()
        {
            EventAggregator.Clear();
        }

        // ── Null / Edge cases ─────────────────────────────────────────────────

        [Test]
        public void TryPour_NullSource_ReturnsFalse()
        {
            var target = CreateView(CreateBottle());
            var levelData = CreateLevelData(enableMultiLayer: false);

            Assert.That(_sut.TryPour(null, target, levelData), Is.False);
        }

        [Test]
        public void TryPour_NullTarget_ReturnsFalse()
        {
            var source = CreateView(CreateBottle());
            var levelData = CreateLevelData(enableMultiLayer: false);

            Assert.That(_sut.TryPour(source, null, levelData), Is.False);
        }

        [Test]
        public void TryPour_NullLevelData_DoesNotThrow()
        {
            var source = CreateView(CreateBottle());
            var target = CreateView(CreateBottle());

            Assert.DoesNotThrow(() => _sut.TryPour(source, target, null));
        }

        // ── Single layer pour ─────────────────────────────────────────────────

        [Test]
        public void TrySingleLayerPour_Success_ReturnsTrue()
        {
            var source = CreateView(CreateBottle());
            source.State.AddLayer(new LiquidLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            var target = CreateView(CreateBottle());
            var levelData = CreateLevelData(enableMultiLayer: false);

            _validator.CanPourResult = true;

            bool result = _sut.TryPour(source, target, levelData);

            Assert.That(result, Is.True);
            Assert.That(source.State.IsEmpty, Is.True);
            Assert.That(target.State.LayerCount, Is.EqualTo(1));
            Assert.That(_historyManager.RecordUndoSnapshotCallCount, Is.EqualTo(1));
            Assert.That(_historyManager.IncrementMoveCountCallCount, Is.EqualTo(1));
        }

        [Test]
        public void TrySingleLayerPour_ValidationFails_ReturnsFalse()
        {
            var source = CreateView(CreateBottle());
            source.State.AddLayer(new LiquidLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            var target = CreateView(CreateBottle());
            target.State.AddLayer(new LiquidLayer(new DomainColor(0.2f, 0.2f, 1f), 1f));
            var levelData = CreateLevelData(enableMultiLayer: false);

            _validator.CanPourResult = false;

            bool result = _sut.TryPour(source, target, levelData);

            Assert.That(result, Is.False);
            Assert.That(source.State.LayerCount, Is.EqualTo(1));
            Assert.That(target.State.LayerCount, Is.EqualTo(1));
            Assert.That(_historyManager.RecordUndoSnapshotCallCount, Is.EqualTo(0));
        }

        [Test]
        public void TrySingleLayerPour_EmptySource_ReturnsFalse()
        {
            var source = CreateView(CreateBottle());
            var target = CreateView(CreateBottle());
            var levelData = CreateLevelData(enableMultiLayer: false);

            _validator.CanPourResult = true;

            bool result = _sut.TryPour(source, target, levelData);

            Assert.That(result, Is.False);
        }

        // ── Multi-layer pour ──────────────────────────────────────────────────

        [Test]
        public void TryMultiLayerPour_TwoConsecutiveSameColors_PoursBoth()
        {
            var source = CreateView(CreateBottle());
            source.State.AddLayer(new LiquidLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            source.State.AddLayer(new LiquidLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            var target = CreateView(CreateBottle());
            var levelData = CreateLevelData(enableMultiLayer: true);

            _validator.CanPourResult = true;

            bool result = _sut.TryPour(source, target, levelData);

            Assert.That(result, Is.True);
            Assert.That(source.State.IsEmpty, Is.True);
            Assert.That(target.State.LayerCount, Is.EqualTo(2));
            Assert.That(_historyManager.IncrementMoveCountCallCount, Is.EqualTo(1));
        }

        [Test]
        public void TryMultiLayerPour_DifferentColors_PoursNone()
        {
            var source = CreateView(CreateBottle());
            source.State.AddLayer(new LiquidLayer(new DomainColor(1f, 0.2f, 0.2f), 1f)); // Red
            source.State.AddLayer(new LiquidLayer(new DomainColor(0.2f, 0.2f, 1f), 1f)); // Blue
            var target = CreateView(CreateBottle());
            var levelData = CreateLevelData(enableMultiLayer: true);

            // Only top color counts
            _validator.CanPourResult = true;

            int count = _sut.GetPourLayerCount(source, target, levelData);
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void GetPourLayerCount_SingleLayer_ReturnsOne()
        {
            var source = CreateView(CreateBottle());
            source.State.AddLayer(new LiquidLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            var target = CreateView(CreateBottle());
            var levelData = CreateLevelData(enableMultiLayer: false);

            _validator.CanPourResult = true;

            int count = _sut.GetPourLayerCount(source, target, levelData);
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void GetPourLayerCount_FullTarget_ReturnsZero()
        {
            var source = CreateView(CreateBottle());
            source.State.AddLayer(new LiquidLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            var target = CreateView(CreateBottle());
            for (int i = 0; i < 4; i++)
                target.State.AddLayer(new LiquidLayer(new DomainColor(0.5f, 0.5f, 0.5f), 0.5f));
            var levelData = CreateLevelData(enableMultiLayer: true);

            int count = _sut.GetPourLayerCount(source, target, levelData);
            Assert.That(count, Is.EqualTo(0));
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static LevelData CreateLevelData(bool enableMultiLayer,
            int minConsecutive = 2)
        {
            return new LevelData
            {
                levelNumber = 1,
                enableMultiLayerPour = enableMultiLayer,
                multiLayerPourConfig = new MultiLayerPourData
                {
                    pourAllMatching = true,
                    pourConsecutiveOnly = true,
                    minConsecutiveForPour = minConsecutive
                }
            };
        }
    }
}
