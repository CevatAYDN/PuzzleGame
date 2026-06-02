using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Application
{
    public class GameHistoryManagementServiceTests
    {
        private FakeGameHistoryService _historyService;
        private GameHistoryManagementService _sut;
        private int _callbackMoveCount;

        [SetUp]
        public void SetUp()
        {
            _historyService = new FakeGameHistoryService();
            _callbackMoveCount = -1;
            _sut = new GameHistoryManagementService(_historyService, new IBottleView[0]);
            _sut.SetMoveCountChangedCallback(mc => _callbackMoveCount = mc);
        }

        [Test]
        public void Constructor_WithValidDependencies_InitializesWithZeroMoves()
        {
            Assert.AreEqual(0, _sut.CurrentMoveCount);
        }

        [Test]
        public void Constructor_WithNullHistory_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new GameHistoryManagementService(null, new IBottleView[0]));
        }

        [Test]
        public void IncrementMoveCount_IncrementsByOne()
        {
            _sut.IncrementMoveCount();
            Assert.AreEqual(1, _sut.CurrentMoveCount);
        }

        [Test]
        public void IncrementMoveCount_TriggersCallback()
        {
            _sut.IncrementMoveCount();
            Assert.AreEqual(1, _callbackMoveCount);
        }

        [Test]
        public void ResetMoveCount_ResetsToZero()
        {
            _sut.IncrementMoveCount();
            _sut.IncrementMoveCount();
            _sut.ResetMoveCount();
            Assert.AreEqual(0, _sut.CurrentMoveCount);
            Assert.AreEqual(0, _callbackMoveCount);
        }

        [Test]
        public void Undo_WhenCanUndoIsFalse_DoesNothing()
        {
            _historyService.CanUndoValue = false;
            _sut.IncrementMoveCount();
            int before = _sut.CurrentMoveCount;

            _sut.Undo();

            Assert.AreEqual(before, _sut.CurrentMoveCount);
        }

        [Test]
        public void Undo_WhenCanUndoIsTrue_DecrementsMoveCount()
        {
            _historyService.CanUndoValue = true;
            _historyService.LastSnapshotValue = new List<LiquidLayer>[0];
            _sut.IncrementMoveCount();
            _sut.IncrementMoveCount();

            _sut.Undo();

            Assert.AreEqual(1, _sut.CurrentMoveCount);
        }

        [Test]
        public void Undo_DoesNotGoBelowZero()
        {
            _historyService.CanUndoValue = true;
            _historyService.LastSnapshotValue = new List<LiquidLayer>[0];

            _sut.Undo();

            Assert.AreEqual(0, _sut.CurrentMoveCount);
        }

        [Test]
        public void Undo_TriggersMoveCountCallback()
        {
            _historyService.CanUndoValue = true;
            _historyService.LastSnapshotValue = new List<LiquidLayer>[0];
            _sut.IncrementMoveCount();

            _sut.Undo();

            Assert.AreEqual(0, _callbackMoveCount);
        }

        [Test]
        public void CanUndo_DelegatesToHistoryService()
        {
            _historyService.CanUndoValue = true;
            Assert.IsTrue(_sut.CanUndo);

            _historyService.CanUndoValue = false;
            Assert.IsFalse(_sut.CanUndo);
        }

        [Test]
        public void CanUndo_WithNullHistory_ReturnsFalse()
        {
            var sut = new GameHistoryManagementService(null, new IBottleView[0]);
            Assert.IsFalse(sut.CanUndo);
        }

        [Test]
        public void RecordUndoSnapshot_WithEmptyBottles_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.RecordUndoSnapshot());
        }

        [Test]
        public void RecordUndoSnapshot_WithNullBottles_DoesNotThrow()
        {
            var sut = new GameHistoryManagementService(_historyService, null);
            Assert.DoesNotThrow(() => sut.RecordUndoSnapshot());
        }
    }

    /// <summary>
    /// Manuel mock: NSubstitute assembly reference gerektirmeden test yazılabilir.
    /// </summary>
    internal class FakeGameHistoryService : IGameHistoryService
    {
        public bool CanUndoValue { get; set; }
        public List<LiquidLayer>[] LastSnapshotValue { get; set; } = new List<LiquidLayer>[0];
        public int RecordSnapshotCallCount { get; private set; }
        public int UndoCallCount { get; private set; }

        public bool CanUndo => CanUndoValue;
        public List<LiquidLayer>[] LastSnapshot => LastSnapshotValue;

        public void RecordSnapshot(BottleState[] states)
        {
            RecordSnapshotCallCount++;
        }

        public void Undo()
        {
            UndoCallCount++;
        }

        public void Clear()
        {
            LastSnapshotValue = new List<LiquidLayer>[0];
        }
    }
}