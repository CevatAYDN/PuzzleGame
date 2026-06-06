using NUnit.Framework;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Services;
using PuzzleGame.Tests.Fakes;
using UnityEngine;

namespace PuzzleGame.Tests.Application.Services
{
    public class UndoServiceTests
    {
        private UndoService _sut;
        private FakeCoinWallet _wallet;
        private FakeHistoryManager _history;
        private EconomyConfig _config;
        private EventAggregator _events;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _config.undoCost = 5;
            _config.maxUndoPerLevel = 4;
            _wallet = new FakeCoinWallet(initialBalance: 100);
            _history = new FakeHistoryManager { CanUndo = true };
            _events = new EventAggregator();

            _sut = new UndoService(_wallet, _history, _config, _events);
        }

        [TearDown]
        public void TearDown()
        {
            _events?.Clear();
            if (_config != null) Object.DestroyImmediate(_config);
        }

        // ── Cost / Remaining properties ──────────────────────────────────

        [Test]
        public void Cost_ReflectsConfig()
        {
            Assert.That(_sut.Cost, Is.EqualTo(5));
        }

        [Test]
        public void Cost_WhenConfigIsNull_ReturnsZero()
        {
            var noConfig = new UndoService(_wallet, _history, null, _events);
            Assert.That(noConfig.Cost, Is.EqualTo(0));
        }

        [Test]
        public void RemainingUndosForCurrentLevel_StartsAtMax()
        {
            Assert.That(_sut.RemainingUndosForCurrentLevel, Is.EqualTo(4));
        }

        // ── TryUndo: preconditions ───────────────────────────────────────

        [Test]
        public void TryUndo_EmptyHistory_ReturnsFalse()
        {
            _history.CanUndo = false;
            bool ok = _sut.TryUndo();

            Assert.That(ok, Is.False);
        }

        [Test]
        public void TryUndo_LimitReached_ReturnsFalseWithoutCallingHistory()
        {
            _config.maxUndoPerLevel = 1;
            _sut = new UndoService(_wallet, _history, _config, _events);

            Assert.That(_sut.TryUndo(), Is.True);
            int undoCallsAfterFirst = _history.UndoCallCount;
            int spendCallsAfterFirst = _wallet.TrySpendCallCount;

            bool second = _sut.TryUndo();
            Assert.That(second, Is.False);
            Assert.That(_history.UndoCallCount, Is.EqualTo(undoCallsAfterFirst), "History.Undo must not be called when limit reached.");
            Assert.That(_wallet.TrySpendCallCount, Is.EqualTo(spendCallsAfterFirst), "Wallet must not be charged when undo is blocked by limit.");
        }

        [Test]
        public void TryUndo_InsufficientCoins_ReturnsFalseWithoutCallingHistory()
        {
            _wallet.CanAffordOverride = 0;
            int undoCallsBefore = _history.UndoCallCount;
            int spendCallsBefore = _wallet.TrySpendCallCount;

            bool ok = _sut.TryUndo();

            Assert.That(ok, Is.False);
            Assert.That(_history.UndoCallCount, Is.EqualTo(undoCallsBefore), "History.Undo must not be called when wallet rejects the spend.");
            Assert.That(_wallet.TrySpendCallCount, Is.EqualTo(spendCallsBefore + 1), "Wallet.TrySpend must be attempted even when the call is expected to fail.");
        }

        // ── TryUndo: success path ────────────────────────────────────────

        [Test]
        public void TryUndo_OnSuccess_CallsHistoryUndo()
        {
            _sut.TryUndo();

            Assert.That(_history.UndoCallCount, Is.EqualTo(1));
        }

        [Test]
        public void TryUndo_OnSuccess_ChargesWalletWithCorrectCostAndReason()
        {
            _sut.TryUndo();

            Assert.That(_wallet.TrySpendCallCount, Is.EqualTo(1));
            Assert.That(_wallet.LastSpendAmount, Is.EqualTo(5));
            Assert.That(_wallet.LastSpendReason, Is.EqualTo("undo"));
            Assert.That(_wallet.Balance, Is.EqualTo(95));
        }

        [Test]
        public void TryUndo_OnSuccess_DecrementsRemainingUndos()
        {
            int before = _sut.RemainingUndosForCurrentLevel;
            _sut.TryUndo();
            int after = _sut.RemainingUndosForCurrentLevel;

            Assert.That(after, Is.EqualTo(before - 1));
        }

        // ── Reset on level change ────────────────────────────────────────

        [Test]
        public void TryUndo_LevelSelectedEvent_ResetsCounter()
        {
            _config.maxUndoPerLevel = 1;
            _sut = new UndoService(_wallet, _history, _config, _events);

            Assert.That(_sut.TryUndo(), Is.True);
            Assert.That(_sut.RemainingUndosForCurrentLevel, Is.EqualTo(0));

            _config.maxUndoPerLevel = 6;
            _events.Publish(new LevelSelectedEvent(1));

            Assert.That(_sut.RemainingUndosForCurrentLevel, Is.EqualTo(6));
        }
    }
}
