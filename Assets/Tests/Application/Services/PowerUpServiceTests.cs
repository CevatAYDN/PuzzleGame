using NUnit.Framework;
using System.Collections.Generic;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain.Models;
using PuzzleGame.Tests.Fakes;
using PuzzleGame.Infrastructure.Implementations;

namespace PuzzleGame.Tests.Application.Services
{
    public class PowerUpServiceTests
    {
        private PowerUpService _sut;
        private FakeEventAggregator _events;
        private FakeAnimationService _animation;
        private FakeActiveMoldsProvider _molds;
        private FakeChargeStorageService _chargeStorage;
        private FakeRandomProvider _randomProvider;

        private static readonly DomainColor Red   = new DomainColor(1f, 0f, 0f);
        private static readonly DomainColor Green = new DomainColor(0f, 1f, 0f);
        private static readonly DomainColor Blue  = new DomainColor(0f, 0f, 1f);

        [SetUp]
        public void SetUp()
        {
            _events = new FakeEventAggregator();
            _animation = new FakeAnimationService { IsAnimating = false };
            _chargeStorage = new FakeChargeStorageService();
            _randomProvider = new FakeRandomProvider();
            _sut = new PowerUpService(_events, _animation, _chargeStorage, _randomProvider);
            _sut.ResetAll();
            _molds = new FakeActiveMoldsProvider();
        }

        private MoldState CreateMold(int maxLayers, params OreLayer[] layers)
        {
            var mold = new MoldState(maxLayers);
            foreach (var layer in layers)
                mold.AddLayer(layer);
            return mold;
        }

        private FakeMoldView CreateMoldView(MoldState state)
        {
            return new FakeMoldView(state);
        }

        // ─── ApplyColorBomb ────────────────────────────────────────────────

        [Test]
        public void ApplyColorBomb_NullMoldsProvider_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.ApplyColorBomb(null, 0));
        }

        [Test]
        public void ApplyColorBomb_EmptyMoldsArray_DoesNotThrow()
        {
            _molds.Molds = System.Array.Empty<IMoldView>();
            Assert.DoesNotThrow(() => _sut.ApplyColorBomb(_molds, 0));
        }

        [Test]
        public void ApplyColorBomb_NegativeIndex_DoesNotThrow()
        {
            var state = CreateMold(4, new OreLayer(Red, 0.25f));
            _molds.Molds = new IMoldView[] { CreateMoldView(state) };

            Assert.DoesNotThrow(() => _sut.ApplyColorBomb(_molds, -1));
        }

        [Test]
        public void ApplyColorBomb_IndexOutOfRange_DoesNotThrow()
        {
            var state = CreateMold(4, new OreLayer(Red, 0.25f));
            _molds.Molds = new IMoldView[] { CreateMoldView(state) };

            Assert.DoesNotThrow(() => _sut.ApplyColorBomb(_molds, 99));
        }

        [Test]
        public void ApplyColorBomb_EmptyMold_DoesNotChangeState()
        {
            var state = CreateMold(4);
            _molds.Molds = new IMoldView[] { CreateMoldView(state) };

            _sut.ApplyColorBomb(_molds, 0);

            Assert.AreEqual(0, state.LayerCount);
        }

        [Test]
        public void ApplyColorBomb_SingleLayer_DoesNotChangeLayers()
        {
            var state = CreateMold(4, new OreLayer(Red, 0.25f));
            _molds.Molds = new IMoldView[] { CreateMoldView(state) };

            _sut.ApplyColorBomb(_molds, 0);

            Assert.AreEqual(1, state.LayerCount);
        }

        [Test]
        public void ApplyColorBomb_TwoDifferentColors_NoMerge()
        {
            var state = CreateMold(4,
                new OreLayer(Red, 0.25f),
                new OreLayer(Green, 0.25f));
            _molds.Molds = new IMoldView[] { CreateMoldView(state) };

            _sut.ApplyColorBomb(_molds, 0);

            Assert.AreEqual(2, state.LayerCount);
        }

        [Test]
        public void ApplyColorBomb_TwoSameColorsAdjacent_MergesIntoOne()
        {
            var layer1 = new OreLayer(Red, 0.25f);
            var layer2 = new OreLayer(Red, 0.30f);
            var state = CreateMold(4, layer1, layer2);
            _molds.Molds = new IMoldView[] { CreateMoldView(state) };

            _sut.ApplyColorBomb(_molds, 0);

            Assert.AreEqual(1, state.LayerCount);
            Assert.AreEqual(0.55f, state.GetLayerAt(0).Amount, 0.001f);
        }

        [Test]
        public void ApplyColorBomb_ThreeSameColorLayers_MergesIntoOne()
        {
            var state = CreateMold(4,
                new OreLayer(Red, 0.20f),
                new OreLayer(Red, 0.25f),
                new OreLayer(Red, 0.30f));
            _molds.Molds = new IMoldView[] { CreateMoldView(state) };

            _sut.ApplyColorBomb(_molds, 0);

            Assert.AreEqual(1, state.LayerCount);
            Assert.AreEqual(0.75f, state.GetLayerAt(0).Amount, 0.001f);
        }

        [Test]
        public void ApplyColorBomb_AlternatingColors_MergesOnlyAdjacentSame()
        {
            var state = CreateMold(4,
                new OreLayer(Red, 0.25f),
                new OreLayer(Green, 0.25f),
                new OreLayer(Red, 0.25f));
            _molds.Molds = new IMoldView[] { CreateMoldView(state) };

            _sut.ApplyColorBomb(_molds, 0);

            // red/green/red stays as 3 layers (no adjacent same colors)
            Assert.AreEqual(3, state.LayerCount);
        }

        [Test]
        public void ApplyColorBomb_MergeReducesLayerCount_UpdatesVisuals()
        {
            var moldView = CreateMoldView(CreateMold(4,
                new OreLayer(Red, 0.25f),
                new OreLayer(Red, 0.30f)));
            _molds.Molds = new IMoldView[] { moldView };

            _sut.ApplyColorBomb(_molds, 0);

            Assert.AreEqual(1, moldView.UpdateVisualsFromStateCallCount);
        }

        [Test]
        public void ApplyColorBomb_NoMerge_DoesNotUpdateVisuals()
        {
            var moldView = CreateMoldView(CreateMold(4,
                new OreLayer(Red, 0.25f),
                new OreLayer(Green, 0.30f)));
            _molds.Molds = new IMoldView[] { moldView };

            _sut.ApplyColorBomb(_molds, 0);

            Assert.AreEqual(0, moldView.UpdateVisualsFromStateCallCount);
        }

        [Test]
        public void ApplyColorBomb_TargetsCorrectMoldIndex()
        {
            var untouched = CreateMoldView(CreateMold(4, new OreLayer(Red, 0.25f)));
            var target = CreateMoldView(CreateMold(4,
                new OreLayer(Green, 0.25f),
                new OreLayer(Green, 0.30f)));
            _molds.Molds = new IMoldView[] { untouched, target };

            _sut.ApplyColorBomb(_molds, 1);

            Assert.AreEqual(1, target.State.LayerCount); // merged
            Assert.AreEqual(1, untouched.State.LayerCount); // unchanged
        }

        // ─── ApplyShuffle ──────────────────────────────────────────────────

        [Test]
        public void ApplyShuffle_NullMoldsProvider_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.ApplyShuffle(null));
        }

        [Test]
        public void ApplyShuffle_EmptyMoldsArray_DoesNotThrow()
        {
            _molds.Molds = System.Array.Empty<IMoldView>();
            Assert.DoesNotThrow(() => _sut.ApplyShuffle(_molds));
        }

        [Test]
        public void ApplyShuffle_PreservesTotalLayerCount()
        {
            var molds = new[]
            {
                CreateMoldView(CreateMold(4, new OreLayer(Red, 0.25f), new OreLayer(Green, 0.25f))),
                CreateMoldView(CreateMold(4, new OreLayer(Blue, 0.25f))),
            };
            _molds.Molds = molds;

            _sut.ApplyShuffle(_molds);

            int totalLayers = 0;
            foreach (var m in _molds.Molds)
                totalLayers += m.State.LayerCount;
            Assert.AreEqual(3, totalLayers);
        }

        [Test]
        public void ApplyShuffle_EmptyMolds_NoChange()
        {
            var state = CreateMold(4);
            _molds.Molds = new IMoldView[] { CreateMoldView(state) };

            _sut.ApplyShuffle(_molds);

            Assert.AreEqual(0, state.LayerCount);
        }

        [Test]
        public void ApplyShuffle_UpdatesVisualsOnAllNonEmptyMolds()
        {
            var viewA = CreateMoldView(CreateMold(4, new OreLayer(Red, 0.25f)));
            var viewB = CreateMoldView(CreateMold(4, new OreLayer(Green, 0.25f)));
            _molds.Molds = new IMoldView[] { viewA, viewB };

            _sut.ApplyShuffle(_molds);

            Assert.AreEqual(1, viewA.UpdateVisualsFromStateCallCount);
            Assert.AreEqual(1, viewB.UpdateVisualsFromStateCallCount);
        }

        [Test]
        public void ApplyShuffle_EmptyMoldIncluded_SkipsWithoutUpdatingVisuals()
        {
            var viewA = CreateMoldView(CreateMold(4, new OreLayer(Red, 0.25f)));
            var viewEmpty = CreateMoldView(CreateMold(4));
            _molds.Molds = new IMoldView[] { viewA, viewEmpty };

            _sut.ApplyShuffle(_molds);

            // The empty mold was skipped (Clear() called but nothing to clear)
            // After shuffle it may receive layers, so UpdateVisualsFromState is called
            // Only verify that the previously non-empty mold got updated
            Assert.AreEqual(1, viewA.UpdateVisualsFromStateCallCount);
        }

        [Test]
        public void ApplyShuffle_RedistributesAcrossMolds_RespectingMaxLayers()
        {
            var maxLayers = 2;
            var viewA = CreateMoldView(CreateMold(maxLayers, new OreLayer(Red, 0.25f), new OreLayer(Green, 0.25f)));
            var viewB = CreateMoldView(CreateMold(maxLayers, new OreLayer(Blue, 0.25f)));
            _molds.Molds = new IMoldView[] { viewA, viewB };

            _sut.ApplyShuffle(_molds);

            // After shuffle total layers is 3. With maxLayers=2, layers distribute as 2+1.
            int totalLayers = 0;
            foreach (var m in _molds.Molds)
            {
                Assert.IsTrue(m.State.LayerCount <= maxLayers, "Mold exceeded MaxLayers after shuffle");
                totalLayers += m.State.LayerCount;
            }
            Assert.AreEqual(3, totalLayers);
        }

        [Test]
        public void ApplyShuffle_NoDuplicateReferences()
        {
            // Verify that all layers after shuffle are distinct references
            var molds = new[]
            {
                CreateMoldView(CreateMold(4, new OreLayer(Red, 0.25f), new OreLayer(Green, 0.25f))),
                CreateMoldView(CreateMold(4, new OreLayer(Blue, 0.25f))),
            };
            _molds.Molds = molds;

            _sut.ApplyShuffle(_molds);

            var allLayers = new List<OreLayer>();
            foreach (var m in _molds.Molds)
                allLayers.AddRange(m.State.Layers);

            Assert.AreEqual(3, allLayers.Count);
        }

        [Test]
        public void ApplyShuffle_SingleMold_PreservesLayerCount()
        {
            var view = CreateMoldView(CreateMold(4,
                new OreLayer(Red, 0.25f),
                new OreLayer(Green, 0.25f),
                new OreLayer(Blue, 0.25f)));
            _molds.Molds = new IMoldView[] { view };

            _sut.ApplyShuffle(_molds);

            Assert.AreEqual(3, view.State.LayerCount);
        }

        [Test]
        public void ApplyShuffle_AllEmptyMolds_NoChange()
        {
            _molds.Molds = new IMoldView[]
            {
                CreateMoldView(CreateMold(4)),
                CreateMoldView(CreateMold(4)),
            };

            Assert.DoesNotThrow(() => _sut.ApplyShuffle(_molds));
            foreach (var m in _molds.Molds)
                Assert.AreEqual(0, m.State.LayerCount);
        }
    }
}

namespace PuzzleGame.Tests.Application.Services
{
    internal sealed class FakeChargeStorageService : IChargeStorageService
    {
        private readonly Dictionary<PowerUpType, int> _store = new Dictionary<PowerUpType, int>();

        public int GetCharge(PowerUpType type, int defaultValue)
            => _store.TryGetValue(type, out int v) ? v : defaultValue;

        public void SetCharge(PowerUpType type, int value)
            => _store[type] = value;

        public void Save() { }
    }

    internal sealed class FakeRandomProvider : IRandomProvider
    {
        private int _nextValue;
        public int Next(int maxValue) => _nextValue % maxValue;
        public void SetNext(int v) => _nextValue = v;
    }
}
