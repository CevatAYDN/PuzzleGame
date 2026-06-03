using NUnit.Framework;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Events;
using PuzzleGame.Tests.Fakes;
using UnityEngine;

namespace PuzzleGame.Tests.Application.Services
{
    public class SecureFileLevelProgressServiceTests
    {
        private SecureFileLevelProgressService _sut;
        private EventAggregator _eventAggregator;

        [SetUp]
        public void Setup()
        {
            _eventAggregator = new EventAggregator();
            _sut = new SecureFileLevelProgressService(_eventAggregator, new FakeSaveManager());
            _sut.ResetAll();
        }

        [TearDown]
        public void Teardown()
        {
            _sut?.ResetAll();
            _eventAggregator?.Clear();
        }

        [Test]
        public void IsUnlocked_FirstLevel_AlwaysTrue()
        {
            Assert.That(_sut.IsUnlocked(1), Is.True);
        }

        [Test]
        public void IsUnlocked_Level2_WithoutCompletion_False()
        {
            Assert.That(_sut.IsUnlocked(2), Is.False);
        }

        [Test]
        public void IsUnlocked_AfterLevel1Complete_True()
        {
            _sut.RecordCompletion(1, 10, 2);
            Assert.That(_sut.IsUnlocked(2), Is.True);
        }

        [Test]
        public void RecordCompletion_StoresBestStars()
        {
            _sut.RecordCompletion(1, 10, 2);
            Assert.That(_sut.GetStars(1), Is.EqualTo(2));
        }

        [Test]
        public void RecordCompletion_PoorerRun_DoesNotOverwrite()
        {
            _sut.RecordCompletion(1, 10, 3);
            _sut.RecordCompletion(1, 20, 1);

            Assert.That(_sut.GetStars(1), Is.EqualTo(3));
            Assert.That(_sut.GetBestMoves(1), Is.EqualTo(10));
        }

        [Test]
        public void RecordCompletion_BetterStars_Overwrites()
        {
            _sut.RecordCompletion(1, 20, 1);
            _sut.RecordCompletion(1, 15, 3);

            Assert.That(_sut.GetStars(1), Is.EqualTo(3));
            Assert.That(_sut.GetBestMoves(1), Is.EqualTo(15));
        }

        [Test]
        public void RecordCompletion_SameStarsFewerMoves_Overwrites()
        {
            _sut.RecordCompletion(1, 20, 2);
            _sut.RecordCompletion(1, 15, 2);

            Assert.That(_sut.GetBestMoves(1), Is.EqualTo(15));
        }

        [Test]
        public void RecordCompletion_SameStarsMoreMoves_DoesNotOverwrite()
        {
            _sut.RecordCompletion(1, 10, 2);
            _sut.RecordCompletion(1, 20, 2);

            Assert.That(_sut.GetBestMoves(1), Is.EqualTo(10));
        }

        [Test]
        public void IsCompleted_AfterRecord_True()
        {
            _sut.RecordCompletion(1, 10, 1);
            Assert.That(_sut.IsCompleted(1), Is.True);
        }

        [Test]
        public void IsCompleted_BeforeRecord_False()
        {
            Assert.That(_sut.IsCompleted(1), Is.False);
        }

        [Test]
        public void ResetAll_ClearsProgress()
        {
            _sut.RecordCompletion(1, 10, 3);
            _sut.ResetAll();
            Assert.That(_sut.GetStars(1), Is.EqualTo(0));
        }

        [Test]
        public void RecordCompletion_PublishesEvent()
        {
            LevelProgressChangedEvent? received = null;
            _eventAggregator.Subscribe<LevelProgressChangedEvent>(e => received = e);

            _sut.RecordCompletion(1, 10, 3);

            Assert.That(received.HasValue, Is.True);
            Assert.That(received.Value.LevelNumber, Is.EqualTo(1));
            Assert.That(received.Value.Stars, Is.EqualTo(3));
            Assert.That(received.Value.Moves, Is.EqualTo(10));
        }

        [Test]
        public void RecordCompletion_InvalidInput_Ignored()
        {
            _sut.RecordCompletion(0, 10, 3);
            _sut.RecordCompletion(1, 0, 3);
            _sut.RecordCompletion(1, 10, 0);

            Assert.That(_sut.GetStars(1), Is.EqualTo(0));
        }
    }
}
