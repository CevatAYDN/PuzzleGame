using NUnit.Framework;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Services;
using PuzzleGame.Tests.Fakes;
using UnityEngine;

namespace PuzzleGame.Tests.Application.Services
{
    public class HintServiceTests
    {
        private HintService _sut;
        private FakeCoinWallet _wallet;
        private EconomyConfig _config;
        private FakeEventAggregator _events;
        private FakeActiveMoldsProvider _molds;
        private LevelData _level;
        private FakeAnimationService _animation;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _config.hintCost = 10;
            _config.maxHintPerLevel = 3;
            _wallet = new FakeCoinWallet(initialBalance: 100);
            _events = new FakeEventAggregator();
            _molds = new FakeActiveMoldsProvider();
            _level = ScriptableObject.CreateInstance<LevelData>();
            _level.maxLayersPerMold = 4;
            _level.colorCount = 3;
            _level.MoldCount = 3;
            _animation = new FakeAnimationService();

            // Build a solvable layout: 2 mixed-color molds where the solver can find moves.
            // Mold A: red (bottom), green (top)  →  Mold B: empty
            // Mold C: green (bottom), red (top)  →  Mold D: empty
            // Solver can suggest: cast green from A to B, or red from C to D.
            var a = new PuzzleGame.Domain.Models.MoldState(4);
            a.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(1f, 0f, 0f), 0.25f));
            a.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(0f, 1f, 0f), 0.25f));
            var b = new PuzzleGame.Domain.Models.MoldState(4);
            var c = new PuzzleGame.Domain.Models.MoldState(4);
            c.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(0f, 1f, 0f), 0.25f));
            c.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(1f, 0f, 0f), 0.25f));
            _molds.Molds = new PuzzleGame.Application.Interfaces.IMoldView[]
            {
                new FakeMoldView(a), new FakeMoldView(b), new FakeMoldView(c)
            };

            _sut = new HintService(_wallet, _config, _molds, _events, _animation);
        }

        [TearDown]
        public void TearDown()
        {
            _events?.Clear();
            if (_config != null) Object.DestroyImmediate(_config);
            if (_level != null) Object.DestroyImmediate(_level);
        }

        // ── Cost / Remaining properties ──────────────────────────────────

        [Test]
        public void Cost_ReflectsConfig()
        {
            Assert.That(_sut.Cost, Is.EqualTo(10));
        }

        [Test]
        public void Cost_WhenConfigIsNull_ReturnsZero()
        {
            var noConfig = new HintService(_wallet, null, _molds, _events, _animation);
            Assert.That(noConfig.Cost, Is.EqualTo(0));
        }

        [Test]
        public void RemainingHintsForCurrentLevel_StartsAtMax()
        {
            Assert.That(_sut.RemainingHintsForCurrentLevel, Is.EqualTo(3));
        }

        // ── TryGetHint: null/invalid preconditions ────────────────────────

        [Test]
        public void TryGetHint_NullLevel_ReturnsFalse()
        {
            bool ok = _sut.TryGetHint(null, out int src, out int dst);

            Assert.That(ok, Is.False);
            Assert.That(src, Is.EqualTo(-1));
            Assert.That(dst, Is.EqualTo(-1));
        }

        [Test]
        public void TryGetHint_NullMoldsProvider_ReturnsFalse()
        {
            var noMolds = new HintService(_wallet, _config, new FakeActiveMoldsProvider(), _events, _animation);
            bool ok = noMolds.TryGetHint(_level, out _, out _);

            Assert.That(ok, Is.False);
        }

        [Test]
        public void TryGetHint_LessThanTwoMolds_ReturnsFalse()
        {
            _molds.Molds = new PuzzleGame.Application.Interfaces.IMoldView[1];
            bool ok = _sut.TryGetHint(_level, out _, out _);

            Assert.That(ok, Is.False);
        }

        [Test]
        public void TryGetHint_WhenAnimating_ReturnsFalseWithoutCallingWallet()
        {
            _animation.IsAnimating = true;
            int spendCallsBefore = _wallet.TrySpendCallCount;

            bool ok = _sut.TryGetHint(_level, out _, out _);

            Assert.That(ok, Is.False);
            Assert.That(_wallet.TrySpendCallCount, Is.EqualTo(spendCallsBefore));
        }

        // ── TryGetHint: cost / limit guards ───────────────────────────────

        [Test]
        public void TryGetHint_InsufficientCoins_ReturnsFalseWithoutCharging()
        {
            _wallet.CanAffordOverride = 0; // force insufficient

            int spendCountBefore = _wallet.TrySpendCallCount;
            int balanceBefore = _wallet.Balance;
            bool ok = _sut.TryGetHint(_level, out _, out _);

            Assert.That(ok, Is.False);
            Assert.That(_wallet.Balance, Is.EqualTo(balanceBefore));
            Assert.That(_wallet.TrySpendCallCount, Is.EqualTo(spendCountBefore), "Wallet must not be charged when hint is rejected.");
        }

        [Test]
        public void TryGetHint_LimitReached_ReturnsFalse()
        {
            // This test verifies the per-level limit is enforced.
            // We don't need a solvable layout - we just need to verify the limit check happens.
            
            // Set max hints to 0 to immediately hit the limit
            _config.maxHintPerLevel = 0;
            _sut = new HintService(_wallet, _config, _molds, _events, _animation);

            // Even with coins available, should return false due to limit
            bool result = _sut.TryGetHint(_level, out _, out _);
            Assert.That(result, Is.False, "Hint should be blocked when maxHintPerLevel is 0.");
            
            // Verify RemainingHintsForCurrentLevel reflects the limit
            Assert.That(_sut.RemainingHintsForCurrentLevel, Is.EqualTo(0));
        }

        [Test]
        public void TryGetHint_DecrementsRemainingHints()
        {
            // Build a solvable layout: 3 molds with mixed colors + 1 empty mold
            // Mold 0: red, red, green (3 layers)  Mold 1: green, green, red (3 layers)
            // Mold 2: red, green (2 layers)        Mold 3: empty
            // Solver path: cast green from 0→3, red from 1→3, green from 1→2, etc.
            var a = new PuzzleGame.Domain.Models.MoldState(4);
            a.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(1f, 0f, 0f), 0.25f));
            a.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(1f, 0f, 0f), 0.25f));
            a.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(0f, 1f, 0f), 0.25f));
            var b = new PuzzleGame.Domain.Models.MoldState(4);
            b.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(0f, 1f, 0f), 0.25f));
            b.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(0f, 1f, 0f), 0.25f));
            b.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(1f, 0f, 0f), 0.25f));
            var c = new PuzzleGame.Domain.Models.MoldState(4);
            c.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(1f, 0f, 0f), 0.25f));
            c.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(0f, 1f, 0f), 0.25f));
            var d = new PuzzleGame.Domain.Models.MoldState(4); // empty
            _molds.Molds = new PuzzleGame.Application.Interfaces.IMoldView[]
            {
                new FakeMoldView(a), new FakeMoldView(b), new FakeMoldView(c), new FakeMoldView(d)
            };

            int before = _sut.RemainingHintsForCurrentLevel;
            _sut.TryGetHint(_level, out _, out _);
            int after = _sut.RemainingHintsForCurrentLevel;

            Assert.That(after, Is.EqualTo(before - 1));
        }

        // ── TryGetHint: persistence + reset ──────────────────────────────

        [Test]
        public void TryGetHint_LevelSelectedEvent_ResetsCounter()
        {
            // First level — exhaust the per-level budget
            _config.maxHintPerLevel = 0;
            Assert.That(_sut.TryGetHint(_level, out _, out _), Is.False);

            // Bump the budget, then publish the level-selected event to reset.
            _config.maxHintPerLevel = 5;
            _events.Publish(new PuzzleGame.Application.Events.LevelSelectedEvent(1));

            int afterReset = _sut.RemainingHintsForCurrentLevel;
            Assert.That(afterReset, Is.EqualTo(5));
        }

        [Test]
        public void TryGetHint_OnSuccess_PublishesHintHighlightEvent()
        {
            // Build a solvable layout: 4 molds with mixed colors + 1 empty
            var a = new PuzzleGame.Domain.Models.MoldState(4);
            a.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(1f, 0f, 0f), 0.25f));
            a.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(1f, 0f, 0f), 0.25f));
            a.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(0f, 1f, 0f), 0.25f));
            var b = new PuzzleGame.Domain.Models.MoldState(4);
            b.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(0f, 1f, 0f), 0.25f));
            b.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(0f, 1f, 0f), 0.25f));
            b.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(1f, 0f, 0f), 0.25f));
            var c = new PuzzleGame.Domain.Models.MoldState(4);
            c.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(1f, 0f, 0f), 0.25f));
            c.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(0f, 1f, 0f), 0.25f));
            var d = new PuzzleGame.Domain.Models.MoldState(4);
            _molds.Molds = new PuzzleGame.Application.Interfaces.IMoldView[]
            {
                new FakeMoldView(a), new FakeMoldView(b), new FakeMoldView(c), new FakeMoldView(d)
            };

            bool ok = _sut.TryGetHint(_level, out int srcIdx, out int dstIdx);

            Assert.That(ok, Is.True);
            Assert.That(srcIdx, Is.InRange(0, 2));
            Assert.That(dstIdx, Is.EqualTo(3));

            // Verify HintHighlightEvent was published with correct indices
            var published = _events.LastOf<HintHighlightEvent>();
            Assert.That(published, Is.Not.Null);
            Assert.That(published.SourceIndex, Is.EqualTo(srcIdx));
            Assert.That(published.TargetIndex, Is.EqualTo(dstIdx));
        }

        [Test]
        public void TryGetHint_OnFailure_DoesNotPublishHintHighlightEvent()
        {
            // Force failure: insufficient coins
            _wallet.CanAffordOverride = 0;

            bool ok = _sut.TryGetHint(_level, out _, out _);

            Assert.That(ok, Is.False);
            Assert.That(_events.CountOf<HintHighlightEvent>(), Is.EqualTo(0));
        }
    }
}
