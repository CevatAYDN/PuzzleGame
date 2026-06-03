using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Tests.Fakes;

namespace PuzzleGame.Tests.Application.Services
{
    public class GameHistoryManagerTests
    {
        private FakeBottleView[] _bottles;
        private GameHistoryManager _sut;
        private int _callbackMoveCount;

        [SetUp]
        public void SetUp()
        {
            _sut = new GameHistoryManager();
            _callbackMoveCount = -1;
            _sut.OnMoveCountChanged += mc => _callbackMoveCount = mc;

            // Setup a couple of fake bottles
            _bottles = new FakeBottleView[]
            {
                new FakeBottleView(new BottleState(4)),
                new FakeBottleView(new BottleState(4))
            };
            _sut.Initialize(_bottles);
        }

        [Test]
        public void Constructor_InitializesWithZeroMoves()
        {
            Assert.AreEqual(0, _sut.CurrentMoveCount);
            Assert.IsFalse(_sut.CanUndo);
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
        public void ResetAll_ResetsToZero()
        {
            _sut.IncrementMoveCount();
            _sut.IncrementMoveCount();
            _sut.ResetAll();
            Assert.AreEqual(0, _sut.CurrentMoveCount);
            Assert.AreEqual(0, _callbackMoveCount);
            Assert.IsFalse(_sut.CanUndo);
        }

        [Test]
        public void Undo_WhenCanUndoIsFalse_DoesNothing()
        {
            _sut.IncrementMoveCount();
            int before = _sut.CurrentMoveCount;

            _sut.Undo();

            Assert.AreEqual(before, _sut.CurrentMoveCount);
        }

        [Test]
        public void RecordUndoSnapshot_AllowsUndoAndRestoresState()
        {
            // Initial state: Bottle 0 has color pink (R=1), Bottle 1 is empty
            var colorPink = new DomainColor(1f, 0f, 0f, 1f);
            _bottles[0].State.AddLayer(new LiquidLayer(colorPink, 0.25f));

            _sut.RecordUndoSnapshot();
            _sut.IncrementMoveCount();

            // Change state
            _bottles[0].State.Clear();
            _bottles[1].State.AddLayer(new LiquidLayer(colorPink, 0.25f));

            Assert.IsTrue(_sut.CanUndo);

            _sut.Undo();

            // Should restore Bottle 0's pink layer and clear Bottle 1
            Assert.IsFalse(_bottles[0].State.IsEmpty);
            Assert.IsTrue(_bottles[1].State.IsEmpty);
            Assert.AreEqual(0, _sut.CurrentMoveCount);
        }

        [Test]
        public void Undo_DoesNotGoBelowZero()
        {
            _sut.RecordUndoSnapshot();
            Assert.IsTrue(_sut.CanUndo);

            _sut.Undo();

            Assert.AreEqual(0, _sut.CurrentMoveCount);
        }
    }
}
