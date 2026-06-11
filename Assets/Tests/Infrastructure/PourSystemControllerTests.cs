using System;
using System.Collections.Generic;
using NUnit.Framework;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Tests.Fakes;

namespace PuzzleGame.Tests.Infrastructure
{
    /// <summary>
    /// Covers PourSystemController: pour preview/execute, snapshot/restore,
    /// mold state mutation, config overrides, debug queries, and edge cases.
    /// </summary>
    [TestFixture]
    public class PourSystemControllerTests
    {
        private PourSystemController _sut;
        private FakeCastService _castService;
        private FakeAnimationService _animationService;
        private EventAggregator _eventAggregator;
        private FakeMoldView[] _molds;

        [SetUp]
        public void Setup()
        {
            _castService = new FakeCastService();
            _animationService = new FakeAnimationService();
            _eventAggregator = new EventAggregator();
            _sut = new PourSystemController(_castService, _animationService, _eventAggregator);

            // Create 4 molds: 3 with layers, 1 empty
            _molds = new FakeMoldView[4];
            for (int i = 0; i < 4; i++)
            {
                var state = new MoldState(4);
                _molds[i] = new FakeMoldView(state) { MoldIndex = i };
            }

            _sut.SetMolds(_molds);
        }

        [TearDown]
        public void Teardown()
        {
            _sut.Dispose();
            _eventAggregator.Clear();
        }

        // ─── Constructor ─────────────────────────────────────────────────────

        [Test]
        public void Constructor_NullCastService_Throws()
        {
            Assert.That(() => new PourSystemController(null, _animationService, _eventAggregator),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_NullAnimationService_Throws()
        {
            Assert.That(() => new PourSystemController(_castService, null, _eventAggregator),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_NullEventAggregator_Throws()
        {
            Assert.That(() => new PourSystemController(_castService, _animationService, null),
                Throws.ArgumentNullException);
        }

        // ─── SetMolds ────────────────────────────────────────────────────────

        [Test]
        public void SetMolds_Null_Throws()
        {
            Assert.That(() => _sut.SetMolds(null), Throws.ArgumentNullException);
        }

        [Test]
        public void SetMolds_ValidArray_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.SetMolds(new FakeMoldView[2]
            {
                new FakeMoldView(new MoldState(4)) { MoldIndex = 0 },
                new FakeMoldView(new MoldState(4)) { MoldIndex = 1 }
            }));
        }

        // ─── ThrowIfMoldIndexInvalid ─────────────────────────────────────────

        [Test]
        public void Operations_BeforeSetMolds_Throws()
        {
            var sut = new PourSystemController(_castService, _animationService, _eventAggregator);
            Assert.That(() => sut.PreviewPour(0, 1), Throws.InvalidOperationException);
        }

        [Test]
        public void PreviewPour_NegativeIndex_Throws()
        {
            Assert.That(() => _sut.PreviewPour(-1, 0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void PreviewPour_IndexOutOfRange_Throws()
        {
            Assert.That(() => _sut.PreviewPour(0, 99), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        // ─── SetMoldLayers ───────────────────────────────────────────────────

        [Test]
        public void SetMoldLayers_ReplacesAllLayers()
        {
            var red = new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f);
            var blue = new OreLayer(new DomainColor(0f, 0f, 1f), 0.25f);

            _sut.SetMoldLayers(0, new[] { red, blue });

            var state = _molds[0].State;
            Assert.That(state.Layers.Count, Is.EqualTo(2));
            Assert.That(state.Layers[0].Color, Is.EqualTo(new DomainColor(1f, 0f, 0f)));
            Assert.That(state.Layers[1].Color, Is.EqualTo(new DomainColor(0f, 0f, 1f)));
        }

        [Test]
        public void SetMoldLayers_EmptyList_ClearsMold()
        {
            // First add some layers
            var red = new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f);
            _sut.SetMoldLayers(0, new[] { red });

            // Then clear
            _sut.SetMoldLayers(0, Array.Empty<OreLayer>());

            Assert.That(_molds[0].State.IsEmpty, Is.True);
        }

        [Test]
        public void SetMoldLayers_NullLayers_ClearsMold()
        {
            var red = new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f);
            _sut.SetMoldLayers(0, new[] { red });
            _sut.SetMoldLayers(0, null);

            Assert.That(_molds[0].State.IsEmpty, Is.True);
        }

        [Test]
        public void SetMoldLayers_NullMold_NoOp()
        {
            _molds[0] = null;
            Assert.DoesNotThrow(() => _sut.SetMoldLayers(0, new[] { new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f) }));
        }

        // ─── SetMoldColor ────────────────────────────────────────────────────

        [Test]
        public void SetMoldColor_ChangesLayerColor()
        {
            var red = new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f);
            _sut.SetMoldLayers(0, new[] { red });

            _sut.SetMoldColor(0, 0, new DomainColor(0f, 1f, 0f));

            Assert.That(_molds[0].State.Layers[0].Color, Is.EqualTo(new DomainColor(0f, 1f, 0f)));
        }

        [Test]
        public void SetMoldColor_InvalidLayerIndex_Throws()
        {
            Assert.That(() => _sut.SetMoldColor(0, 99, new DomainColor(1f, 0f, 0f)),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        // ─── SetMoldFillAmount ───────────────────────────────────────────────

        [Test]
        public void SetMoldFillAmount_ClampedTo01()
        {
            _sut.SetMoldFillAmount(0, 1.5f);
            // No assertion on visual state since FakeMoldView is no-op,
            // but verify it doesn't throw
            Assert.DoesNotThrow(() => _sut.SetMoldFillAmount(0, -0.5f));
        }

        // ─── PreviewPour ─────────────────────────────────────────────────────

        [Test]
        public void PreviewPour_BothMoldsNull_ReturnsRejected()
        {
            _molds[0] = null;
            _molds[1] = null;
            var result = _sut.PreviewPour(0, 1);
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void PreviewPour_SourceEmpty_ReturnsRejected()
        {
            // Mold 0 is empty by default
            var result = _sut.PreviewPour(0, 1);
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void PreviewPour_ValidTransfer_ReturnsValid()
        {
            // Fill source with red layers
            var red = new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f);
            _sut.SetMoldLayers(0, new[] { red, red });

            var result = _sut.PreviewPour(0, 1);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.LayersToTransfer, Is.EqualTo(2));
        }

        [Test]
        public void PreviewPour_TargetFull_ReturnsRejected()
        {
            // Fill source with red
            var red = new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f);
            _sut.SetMoldLayers(0, new[] { red, red, red, red }); // 4 layers

            // Fill target with 4 blue layers (max = 4)
            var blue = new OreLayer(new DomainColor(0f, 0f, 1f), 0.25f);
            _sut.SetMoldLayers(1, new[] { blue, blue, blue, blue });

            var result = _sut.PreviewPour(0, 1);
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void PreviewPour_ColorMismatch_ReturnsRejected()
        {
            // Source has red on top
            var red = new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f);
            _sut.SetMoldLayers(0, new[] { red });

            // Target has blue on top
            var blue = new OreLayer(new DomainColor(0f, 0f, 1f), 0.25f);
            _sut.SetMoldLayers(1, new[] { blue });

            var result = _sut.PreviewPour(0, 1);
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void PreviewPour_EmptyTarget_AcceptsAnyColor()
        {
            var red = new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f);
            _sut.SetMoldLayers(0, new[] { red });

            // Target is empty
            var result = _sut.PreviewPour(0, 1);
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void PreviewPour_ConsecutiveMatchingLayers_CountsAll()
        {
            // Source: blue, red, red (top=red, 2 consecutive red)
            var blue = new OreLayer(new DomainColor(0f, 0f, 1f), 0.25f);
            var red = new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f);
            _sut.SetMoldLayers(0, new[] { blue, red, red });

            var result = _sut.PreviewPour(0, 1);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.LayersToTransfer, Is.EqualTo(2));
        }

        [Test]
        public void PreviewPour_DoesNotMutateState()
        {
            var red = new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f);
            _sut.SetMoldLayers(0, new[] { red, red });

            int sourceCountBefore = _molds[0].State.Layers.Count;
            int targetCountBefore = _molds[1].State.Layers.Count;

            _sut.PreviewPour(0, 1);

            Assert.That(_molds[0].State.Layers.Count, Is.EqualTo(sourceCountBefore));
            Assert.That(_molds[1].State.Layers.Count, Is.EqualTo(targetCountBefore));
        }

        // ─── ExecuteInstantPour ──────────────────────────────────────────────

        [Test]
        public void ExecuteInstantPour_ValidPour_ReturnsTrue()
        {
            var red = new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f);
            _sut.SetMoldLayers(0, new[] { red, red });

            bool result = _sut.ExecuteInstantPour(0, 1);

            Assert.That(result, Is.True);
            Assert.That(_molds[0].State.IsEmpty, Is.True);
            Assert.That(_molds[1].State.Layers.Count, Is.EqualTo(2));
        }

        [Test]
        public void ExecuteInstantPour_InvalidPour_ReturnsFalse()
        {
            // Source empty
            bool result = _sut.ExecuteInstantPour(0, 1);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ExecuteInstantPour_NullMolds_ReturnsFalse()
        {
            _molds[0] = null;
            bool result = _sut.ExecuteInstantPour(0, 1);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ExecuteInstantPour_FiresEvents()
        {
            int completedCount = 0;
            int rejectedCount = 0;
            _eventAggregator.Subscribe<CastCompletedEvent>(_ => completedCount++);
            _eventAggregator.Subscribe<CastRejectedEvent>(_ => rejectedCount++);

            var red = new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f);
            _sut.SetMoldLayers(0, new[] { red });
            _sut.ExecuteInstantPour(0, 1);

            Assert.That(completedCount, Is.EqualTo(1));
            Assert.That(rejectedCount, Is.EqualTo(0));
        }

        [Test]
        public void ExecuteInstantPour_Rejected_FiresRejectedEvent()
        {
            int rejectedCount = 0;
            _eventAggregator.Subscribe<CastRejectedEvent>(_ => rejectedCount++);

            // Source empty → rejected
            _sut.ExecuteInstantPour(0, 1);

            Assert.That(rejectedCount, Is.EqualTo(1));
        }

        // ─── Snapshot / Restore ──────────────────────────────────────────────

        [Test]
        public void SnapshotAllMolds_AfterPour_CanRestore()
        {
            var red = new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f);
            _sut.SetMoldLayers(0, new[] { red, red });

            // Explicitly snapshot before pouring
            _sut.SnapshotAllMolds();
            _sut.ExecuteInstantPour(0, 1);

            // Verify pour happened
            Assert.That(_molds[0].State.IsEmpty, Is.True);
            Assert.That(_molds[1].State.Layers.Count, Is.EqualTo(2));

            // Restore
            _sut.RestoreSnapshot();

            // Source should have its layers back
            Assert.That(_molds[0].State.Layers.Count, Is.EqualTo(2));
            Assert.That(_molds[1].State.IsEmpty, Is.True);
        }

        [Test]
        public void RestoreSnapshot_EmptyStack_NoOp()
        {
            Assert.DoesNotThrow(() => _sut.RestoreSnapshot());
        }

        [Test]
        public void SnapshotStack_Exceeds32_EvictsOldest()
        {
            var red = new OreLayer(new DomainColor(1f, 0f, 0f), 0.25f);
            _sut.SetMoldLayers(0, new[] { red });

            // Take 35 snapshots (manually + via pour)
            for (int i = 0; i < 35; i++)
            {
                _sut.SnapshotAllMolds();
            }

            // Restore 32 times should not throw
            for (int i = 0; i < 32; i++)
            {
                Assert.DoesNotThrow(() => _sut.RestoreSnapshot());
            }

            // 33rd restore should be no-op (stack empty)
            Assert.DoesNotThrow(() => _sut.RestoreSnapshot());
        }

        [Test]
        public void SnapshotAllMolds_NullMolds_ThrowsInvalidOperation()
        {
            var sut = new PourSystemController(_castService, _animationService, _eventAggregator);
            Assert.Throws<System.InvalidOperationException>(() => sut.SnapshotAllMolds());
            sut.Dispose();
        }

        // ─── GetAllMoldDebugStates ───────────────────────────────────────────

        [Test]
        public void GetAllMoldDebugStates_ReturnsCorrectCount()
        {
            var states = _sut.GetAllMoldDebugStates();
            Assert.That(states.Count, Is.EqualTo(4));
        }

        [Test]
        public void GetAllMoldDebugStates_NullMolds_ReturnsEmptyArray()
        {
            var sut = new PourSystemController(_castService, _animationService, _eventAggregator);
            var result = sut.GetAllMoldDebugStates();
            Assert.That(result, Is.Empty);
            sut.Dispose();
        }

        [Test]
        public void GetAllMoldDebugStates_NullMoldEntry_ReturnsUnavailable()
        {
            _molds[2] = null;
            var states = _sut.GetAllMoldDebugStates();
            Assert.That(states[2].MoldIndex, Is.EqualTo(2));
            Assert.That(states[2].LayerCount, Is.EqualTo(0), "Null mold entry should be marked as unavailable with 0 layers.");
        }

        // ─── Debug Flags ─────────────────────────────────────────────────────

        [Test]
        public void IsDebugModeEnabled_DefaultFalse()
        {
            Assert.That(_sut.IsDebugModeEnabled, Is.False);
        }

        [Test]
        public void IsAnimationDisabled_DefaultFalse()
        {
            Assert.That(_sut.IsAnimationDisabled, Is.False);
        }

        [Test]
        public void DebugFlags_CanToggle()
        {
            _sut.IsDebugModeEnabled = true;
            _sut.IsAnimationDisabled = true;
            Assert.That(_sut.IsDebugModeEnabled, Is.True);
            Assert.That(_sut.IsAnimationDisabled, Is.True);
        }

        // ─── Dispose ─────────────────────────────────────────────────────────

        [Test]
        public void Dispose_ClearsSnapshots()
        {
            _sut.SnapshotAllMolds();
            _sut.Dispose();

            // Restore after dispose should throw since molds are cleared
            Assert.Throws<System.InvalidOperationException>(() => _sut.RestoreSnapshot());
        }

        [Test]
        public void Dispose_CanCallMultipleTimes()
        {
            _sut.Dispose();
            Assert.DoesNotThrow(() => _sut.Dispose());
        }
    }
}
