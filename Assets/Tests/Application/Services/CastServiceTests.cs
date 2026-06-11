using NUnit.Framework;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Configuration.FeatureSystem;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using PuzzleGame.Tests.Fakes;
using PuzzleGame.Application.Interfaces;
using System.Collections.Generic;

namespace PuzzleGame.Tests.Application.Services
{
    public class CastServiceTests
    {
        private CastService _sut;
        private FakeMoldValidator _validator;
        private FakeHistoryManager _historyManager;
        private FakeReactionService _reactionService;
        private EventAggregator _eventAggregator;
        private FakeErrorIndicatorService _errorIndicator;

        private MoldState CreateMold(int maxLayers = 4)
        {
            return new MoldState(maxLayers);
        }

        private FakeMoldView CreateView(MoldState state)
        {
            var go = new UnityEngine.GameObject("TestMold");
            return new FakeMoldView(state) { GameObject = go, Transform = go.transform };
        }

        [SetUp]
        public void SetUp()
        {
            MoldLogger.SetLevel(MoldLogger.Level.Error, false);
            _eventAggregator = new EventAggregator();
            _validator = new FakeMoldValidator();
            _historyManager = new FakeHistoryManager();
            _reactionService = new FakeReactionService();
            _errorIndicator = new FakeErrorIndicatorService();
            _sut = new CastService(_validator, _historyManager, _reactionService, _eventAggregator, _errorIndicator);
        }

        [TearDown]
        public void TearDown()
        {
            _eventAggregator?.Clear();
        }

        // ── Null / Edge cases ─────────────────────────────────────────────────

        [Test]
        public void TryCast_NullSource_ThrowsArgumentNullException()
        {
            var target = CreateView(CreateMold());
            var levelData = CreateLevelData(enableMultiLayer: false);
            var activeMolds = new IMoldView[] { target };

            Assert.Throws<System.ArgumentNullException>(() => _sut.TryCast(null, target, levelData, activeMolds));
        }

        [Test]
        public void TryCast_NullTarget_ThrowsArgumentNullException()
        {
            var source = CreateView(CreateMold());
            var levelData = CreateLevelData(enableMultiLayer: false);
            var activeMolds = new IMoldView[] { source };

            Assert.Throws<System.ArgumentNullException>(() => _sut.TryCast(source, null, levelData, activeMolds));
        }

        [Test]
        public void TryCast_NullLevelData_ThrowsArgumentNullException()
        {
            var source = CreateView(CreateMold());
            var target = CreateView(CreateMold());
            var activeMolds = new IMoldView[] { source, target };

            Assert.Throws<System.ArgumentNullException>(() => _sut.TryCast(source, target, null, activeMolds));
        }

        // ── Single layer Cast ─────────────────────────────────────────────────

        [Test]
        public void TrySingleLayerCast_Success_ReturnsTrue()
        {
            var source = CreateView(CreateMold());
            source.State.AddLayer(new OreLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            var target = CreateView(CreateMold());
            var levelData = CreateLevelData(enableMultiLayer: false);

            _validator.CanCastResult = true;

            bool result = _sut.TryCast(source, target, levelData, new IMoldView[] { source, target });

            Assert.That(result, Is.True);
            Assert.That(source.State.IsEmpty, Is.True);
            Assert.That(target.State.LayerCount, Is.EqualTo(1));
            Assert.That(_historyManager.RecordUndoSnapshotCallCount, Is.EqualTo(1));
            Assert.That(_historyManager.IncrementMoveCountCallCount, Is.EqualTo(1));
        }

        [Test]
        public void TrySingleLayerCast_ValidationFails_ReturnsFalse()
        {
            var source = CreateView(CreateMold());
            source.State.AddLayer(new OreLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            var target = CreateView(CreateMold());
            target.State.AddLayer(new OreLayer(new DomainColor(0.2f, 0.2f, 1f), 1f));
            var levelData = CreateLevelData(enableMultiLayer: false);

            _validator.CanCastResult = false;

            bool result = _sut.TryCast(source, target, levelData, new IMoldView[] { source, target });

            Assert.That(result, Is.False);
            Assert.That(source.State.LayerCount, Is.EqualTo(1));
            Assert.That(target.State.LayerCount, Is.EqualTo(1));
            Assert.That(_historyManager.RecordUndoSnapshotCallCount, Is.EqualTo(0));
        }

        [Test]
        public void TrySingleLayerCast_EmptySource_ReturnsFalse()
        {
            var source = CreateView(CreateMold());
            var target = CreateView(CreateMold());
            var levelData = CreateLevelData(enableMultiLayer: false);

            _validator.CanCastResult = true;

            bool result = _sut.TryCast(source, target, levelData, new IMoldView[] { source, target });

            Assert.That(result, Is.False);
        }

        // ── Multi-layer Cast ──────────────────────────────────────────────────

        [Test]
        public void TryMultiLayerCast_TwoConsecutiveSameColors_CastsBoth()
        {
            var source = CreateView(CreateMold());
            source.State.AddLayer(new OreLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            source.State.AddLayer(new OreLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            var target = CreateView(CreateMold());
            var levelData = CreateLevelData(enableMultiLayer: true);

            _validator.CanCastResult = true;

            bool result = _sut.TryCast(source, target, levelData, new IMoldView[] { source, target });

            Assert.That(result, Is.True);
            Assert.That(source.State.IsEmpty, Is.True);
            Assert.That(target.State.LayerCount, Is.EqualTo(2));
            Assert.That(_historyManager.IncrementMoveCountCallCount, Is.EqualTo(1));
        }

        [Test]
        public void TryMultiLayerCast_DifferentColors_CastsNone()
        {
            var source = CreateView(CreateMold());
            source.State.AddLayer(new OreLayer(new DomainColor(1f, 0.2f, 0.2f), 1f)); // Red
            source.State.AddLayer(new OreLayer(new DomainColor(0.2f, 0.2f, 1f), 1f)); // Blue
            var target = CreateView(CreateMold());
            var levelData = CreateLevelData(enableMultiLayer: true);

            // Only top color counts
            _validator.CanCastResult = true;

            int count = _sut.GetCastLayerCount(source, target, levelData);
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void GetCastLayerCount_SingleLayer_ReturnsOne()
        {
            var source = CreateView(CreateMold());
            source.State.AddLayer(new OreLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            var target = CreateView(CreateMold());
            var levelData = CreateLevelData(enableMultiLayer: false);

            _validator.CanCastResult = true;

            int count = _sut.GetCastLayerCount(source, target, levelData);
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void GetCastLayerCount_FullTarget_ReturnsZero()
        {
            var source = CreateView(CreateMold());
            source.State.AddLayer(new OreLayer(new DomainColor(1f, 0.2f, 0.2f), 1f));
            var target = CreateView(CreateMold());
            for (int i = 0; i < 4; i++)
                target.State.AddLayer(new OreLayer(new DomainColor(0.5f, 0.5f, 0.5f), 0.5f));
            var levelData = CreateLevelData(enableMultiLayer: true);

            int count = _sut.GetCastLayerCount(source, target, levelData);
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void TryCast_ReactionExplodesTarget_UndoRestoresState()
        {
            // Set up real services for reaction integration test
            var eventAggregator = new EventAggregator();
            var colorAdapter = new PuzzleGame.Infrastructure.ColorAdapter();
            var reactionService = new ReactionService(colorAdapter, eventAggregator);
            var historyManager = new GameHistoryManager();
            var validator = new PuzzleGame.Domain.Services.MoldValidationService(PuzzleGame.Domain.ForgeConstants.ColorMatchEpsilon);
            var castService = new CastService(validator, historyManager, reactionService, eventAggregator, null);

            var levelData = CreateLevelData(enableMultiLayer: false);
            levelData.enableReactionSystem = true;
            levelData.reactionConfig = new ReactionSystemData
            {
                enableReactions = true,
                reactionRules = new List<ReactionRule>
                {
                    new ReactionRule
                    {
                        colorA = OreColor.Red,
                        colorB = OreColor.Blue,
                        resultColor = OreColor.Purple,
                        reactionType = ReactionRule.ReactionType.Explode
                    }
                }
            };
            castService.SetLevelData(levelData);

            // Mold A (source): Has Blue layer (top)
            var sourceState = CreateMold();
            sourceState.AddLayer(new OreLayer(OreColor.Blue.ToDefaultDomainColor(), 1.0f, OreColor.Blue));
            var sourceView = CreateView(sourceState);

            // Mold B (target): Has Red layer (top)
            var targetState = CreateMold();
            targetState.AddLayer(new OreLayer(OreColor.Red.ToDefaultDomainColor(), 1.0f, OreColor.Red));
            var targetView = CreateView(targetState);

            var activeMolds = new IMoldView[] { sourceView, targetView };
            historyManager.Initialize(activeMolds);

            // Verify initial states
            Assert.That(sourceState.LayerCount, Is.EqualTo(1));
            Assert.That(targetState.LayerCount, Is.EqualTo(1));
            Assert.That(targetState.TopLayer.Value.ColorType, Is.EqualTo(OreColor.Red));

            // Pour Blue from source to target
            bool castSuccess = castService.TryCast(sourceView, targetView, levelData, activeMolds);
            Assert.That(castSuccess, Is.True);

            // Target had Red, we poured Blue. Red + Blue = Explode. Target mold should explode and be cleared!
            // Source should be empty (since it poured its only layer).
            Assert.That(sourceState.IsEmpty, Is.True);
            Assert.That(targetState.IsEmpty, Is.True);

            // Verify Undo restores pre-cast state
            historyManager.Undo();

            Assert.That(sourceState.LayerCount, Is.EqualTo(1));
            Assert.That(sourceState.TopLayer.Value.ColorType, Is.EqualTo(OreColor.Blue));
            Assert.That(targetState.LayerCount, Is.EqualTo(1));
            Assert.That(targetState.TopLayer.Value.ColorType, Is.EqualTo(OreColor.Red));
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static LevelData CreateLevelData(bool enableMultiLayer,
            int minConsecutive = 2)
        {
            var data = UnityEngine.ScriptableObject.CreateInstance<LevelData>();
            data.levelNumber = 1;
            data.enableMultiLayerCast = enableMultiLayer;
            data.multiLayerCastConfig = new MultiLayerCastData
            {
                CastAllMatching = true,
                CastConsecutiveOnly = true,
                minConsecutiveForCast = minConsecutive
            };
            return data;
        }
    }
}
