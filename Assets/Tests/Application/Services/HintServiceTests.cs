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
        private EventAggregator _events;
        private FakeActiveMoldsProvider _molds;
        private LevelData _level;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _config.hintCost = 10;
            _config.maxHintPerLevel = 3;
            _wallet = new FakeCoinWallet(initialBalance: 100);
            _events = new EventAggregator();
            _molds = new FakeActiveMoldsProvider();
            _level = ScriptableObject.CreateInstance<LevelData>();
            _level.maxLayersPerMold = 4;
            _level.colorCount = 3;
            _level.MoldCount = 3;

            // Build a trivially solvable layout: 3 molds of 1 color each (effectively already sorted,
            // but the generator still produces a layout that the solver can examine).
            _molds.Molds = new PuzzleGame.Application.Interfaces.IMoldView[3];
            for (int i = 0; i < 3; i++)
            {
                var state = new PuzzleGame.Domain.Models.MoldState(4);
                state.AddLayer(new PuzzleGame.Domain.Models.OreLayer(
                    new PuzzleGame.Domain.Models.DomainColor(i / 3f, 1f - i / 3f, 0.5f), 0.25f));
                _molds.Molds[i] = new FakeMoldView(state);
            }

            _sut = new HintService(_wallet, _config, _molds, _events);
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
            var noConfig = new HintService(_wallet, null, _molds, _events);
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
            var noMolds = new HintService(_wallet, _config, new FakeActiveMoldsProvider(), _events);
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
            _config.maxHintPerLevel = 1;
            _sut = new HintService(_wallet, _config, _molds, _events);

            // Force the first hint to succeed by ensuring the layout is non-trivial
            Assert.That(_sut.TryGetHint(_level, out _, out _), Is.True, "First hint should be allowed.");

            bool second = _sut.TryGetHint(_level, out _, out _);
            Assert.That(second, Is.False, "Second hint should be blocked by the per-level limit.");
        }

        [Test]
        public void TryGetHint_DecrementsRemainingHints()
        {
            // Build a non-trivially-solvable layout: 2 full mixed molds
            var a = new PuzzleGame.Domain.Models.MoldState(4);
            a.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(1f, 0f, 0f), 0.25f));
            a.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(0f, 1f, 0f), 0.25f));
            var b = new PuzzleGame.Domain.Models.MoldState(4);
            b.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(0f, 1f, 0f), 0.25f));
            b.AddLayer(new PuzzleGame.Domain.Models.OreLayer(new PuzzleGame.Domain.Models.DomainColor(1f, 0f, 0f), 0.25f));
            _molds.Molds = new PuzzleGame.Application.Interfaces.IMoldView[] { new FakeMoldView(a), new FakeMoldView(b) };

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
    }
}
