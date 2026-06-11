using System;
using System.Linq;
using NUnit.Framework;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain.Models;
using PuzzleGame.Tests.Fakes;
using UnityEngine;

namespace PuzzleGame.Tests.Application.Services
{
    [TestFixture]
    public class AchievementServiceTests
    {
        private const string PrefsPrefix = "PuzzleGame.Achievement.";
        private FakeCoinWallet _wallet;
        private EventAggregator _events;
        private AchievementService _sut;

        [SetUp]
        public void Setup()
        {
            _events = new EventAggregator();
            _wallet = new FakeCoinWallet(0);
            WipePrefs();
            _sut = new AchievementService(_events, _wallet);
        }

        [TearDown]
        public void Teardown()
        {
            _sut.Dispose();
            _events.Clear();
            WipePrefs();
        }

        private static void WipePrefs()
        {
            foreach (AchievementId id in Enum.GetValues(typeof(AchievementId)))
            {
                PlayerPrefs.DeleteKey(PrefsPrefix + id + "_progress");
                PlayerPrefs.DeleteKey(PrefsPrefix + id + "_unlocked");
                PlayerPrefs.DeleteKey(PrefsPrefix + id + "_at");
            }
            PlayerPrefs.Save();
        }

        [Test]
        public void Constructor_LoadsDefaultStates()
        {
            var all = _sut.GetAll();
            Assert.That(all.Count, Is.EqualTo(Enum.GetValues(typeof(AchievementId)).Length));
            foreach (var state in all)
            {
                Assert.That(state.Unlocked, Is.False);
                Assert.That(state.Progress, Is.EqualTo(0));
            }
        }

        [Test]
        public void IncrementProgress_IncreasesValue()
        {
            _sut.IncrementProgress(AchievementId.FirstLevel);
            var state = _sut.GetAll().First(s => s.Id == AchievementId.FirstLevel);
            Assert.That(state.Progress, Is.EqualTo(1));
        }

        [Test]
        public void IncrementProgress_TriggersUnlockAndGrantsReward()
        {
            bool wasUnlockedCalled = false;
            _sut.OnUnlocked += id =>
            {
                if (id == AchievementId.FirstLevel)
                    wasUnlockedCalled = true;
            };

            // FirstLevel target is 1
            _sut.IncrementProgress(AchievementId.FirstLevel);

            var state = _sut.GetAll().First(s => s.Id == AchievementId.FirstLevel);
            Assert.That(state.Unlocked, Is.True);
            Assert.That(wasUnlockedCalled, Is.True);
            
            // FirstLevel reward is 25 coins
            Assert.That(_wallet.Balance, Is.EqualTo(25));
            Assert.That(_wallet.LastAddReason, Is.EqualTo("achievement_FirstLevel"));
        }

        [Test]
        public void IncrementProgress_AlreadyUnlocked_NoOp()
        {
            _sut.IncrementProgress(AchievementId.FirstLevel);
            Assert.That(_wallet.Balance, Is.EqualTo(25));

            // Try to increment again
            _sut.IncrementProgress(AchievementId.FirstLevel);
            var state = _sut.GetAll().First(s => s.Id == AchievementId.FirstLevel);
            Assert.That(state.Progress, Is.EqualTo(1));
            Assert.That(_wallet.Balance, Is.EqualTo(25)); // No extra coins
        }

        [Test]
        public void EventSubscription_OnLevelCompleted_IncrementsProgression()
        {
            // FirstLevel target is 1, TenLevels target is 10
            _events.Publish(new LevelCompletedEvent(1, 3, 45f));

            var firstState = _sut.GetAll().First(s => s.Id == AchievementId.FirstLevel);
            var tenState = _sut.GetAll().First(s => s.Id == AchievementId.TenLevels);

            Assert.That(firstState.Unlocked, Is.True);
            Assert.That(tenState.Progress, Is.EqualTo(1));
        }

        [Test]
        public void EventSubscription_OnPowerUpActivated_IncrementsPowerUpAchievements()
        {
            _events.Publish(new PowerUpActivatedEvent(PowerUpType.ExtraMold, 0));

            var state = _sut.GetAll().First(s => s.Id == AchievementId.FirstPowerUp);
            Assert.That(state.Progress, Is.EqualTo(1));
            Assert.That(state.Unlocked, Is.True);
        }

        [Test]
        public void TrackStreakClaimed_SetsStreakAchievementProgress()
        {
            _sut.TrackStreakClaimed(5);

            var streak7 = _sut.GetAll().First(s => s.Id == AchievementId.DailyStreak7);
            var streak30 = _sut.GetAll().First(s => s.Id == AchievementId.DailyStreak30);

            Assert.That(streak7.Progress, Is.EqualTo(5));
            Assert.That(streak30.Progress, Is.EqualTo(5));
            Assert.That(streak7.Unlocked, Is.False);
        }

        [Test]
        public void TrackPerfectForge_IncrementsPerfectForge()
        {
            _sut.TrackPerfectForge();
            var state = _sut.GetAll().First(s => s.Id == AchievementId.PerfectForge);
            Assert.That(state.Progress, Is.EqualTo(1));
        }

        [Test]
        public void TrackColorMix_IncrementsColorMixer()
        {
            _sut.TrackColorMix(0);
            var state = _sut.GetAll().First(s => s.Id == AchievementId.ColorMixer);
            Assert.That(state.Progress, Is.EqualTo(1));
        }
    }
}
