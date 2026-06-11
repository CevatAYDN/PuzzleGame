using System.Linq;
using NUnit.Framework;
using PuzzleGame.Infrastructure.Implementations;
using UnityEngine;

namespace PuzzleGame.Tests.Infrastructure
{
    [TestFixture]
    public class LeaderboardServiceTests
    {
        private const string ScorePrefix = "PuzzleGame.Leaderboard.Score.";
        private const string PourPrefix = "PuzzleGame.Leaderboard.Pour.";
        private const string TimePrefix = "PuzzleGame.Leaderboard.Time.";

        [SetUp]
        public void SetUp() => WipePrefs();
        [TearDown]
        public void TearDown() => WipePrefs();

        private static void WipePrefs()
        {
            for (int i = 0; i < 10; i++)
            {
                PlayerPrefs.DeleteKey(ScorePrefix + i);
                PlayerPrefs.DeleteKey(PourPrefix + i);
                PlayerPrefs.DeleteKey(TimePrefix + i);
            }
            PlayerPrefs.Save();
        }

        // ── Constructor / empty state ──────────────────────────────────────

        [Test]
        public void Constructor_NoSavedPrefs_LoadsEmpty()
        {
            var svc = new LeaderboardService();
            Assert.That(svc.GetAllEntries(), Is.Empty);
            Assert.That(svc.TotalScore, Is.EqualTo(0));
            Assert.That(svc.LevelsCompleted, Is.EqualTo(0));
        }

        // ── TrySubmitScore ─────────────────────────────────────────────────

        [Test]
        public void TrySubmitScore_ZeroScore_ReturnsFalse()
        {
            var svc = new LeaderboardService();
            Assert.That(svc.TrySubmitScore(1, 0, 5), Is.False);
        }

        [Test]
        public void TrySubmitScore_NegativeScore_ReturnsFalse()
        {
            var svc = new LeaderboardService();
            Assert.That(svc.TrySubmitScore(1, -10, 5), Is.False);
        }

        [Test]
        public void TrySubmitScore_FirstSubmit_ReturnsTrue()
        {
            var svc = new LeaderboardService();
            Assert.That(svc.TrySubmitScore(1, 100, 5), Is.True);
        }

        [Test]
        public void TrySubmitScore_BetterScore_ReturnsTrue()
        {
            var svc = new LeaderboardService();
            svc.TrySubmitScore(1, 100, 5);
            Assert.That(svc.TrySubmitScore(1, 200, 3), Is.True);
        }

        [Test]
        public void TrySubmitScore_BetterScore_UpdatesEntry()
        {
            var svc = new LeaderboardService();
            svc.TrySubmitScore(1, 100, 5);
            svc.TrySubmitScore(1, 200, 3);
            var entry = svc.GetEntry(1);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.BestScore, Is.EqualTo(200));
            Assert.That(entry.BestPourCount, Is.EqualTo(3));
        }

        [Test]
        public void TrySubmitScore_WorseScore_ReturnsFalse()
        {
            var svc = new LeaderboardService();
            svc.TrySubmitScore(1, 200, 3);
            Assert.That(svc.TrySubmitScore(1, 100, 5), Is.False);
        }

        [Test]
        public void TrySubmitScore_WorseScore_DoesNotChangeEntry()
        {
            var svc = new LeaderboardService();
            svc.TrySubmitScore(1, 200, 3);
            svc.TrySubmitScore(1, 100, 5);
            var entry = svc.GetEntry(1);
            Assert.That(entry.BestScore, Is.EqualTo(200));
            Assert.That(entry.BestPourCount, Is.EqualTo(3));
        }

        [Test]
        public void TrySubmitScore_SameScore_ReturnsFalse()
        {
            var svc = new LeaderboardService();
            svc.TrySubmitScore(1, 100, 5);
            Assert.That(svc.TrySubmitScore(1, 100, 3), Is.False);
        }

        [Test]
        public void TrySubmitScore_MultipleLevels_AccumulatesTotalScore()
        {
            var svc = new LeaderboardService();
            svc.TrySubmitScore(1, 100, 5);
            svc.TrySubmitScore(2, 200, 4);
            svc.TrySubmitScore(3, 300, 3);
            Assert.That(svc.TotalScore, Is.EqualTo(600));
            Assert.That(svc.LevelsCompleted, Is.EqualTo(3));
        }

        [Test]
        public void TrySubmitScore_UpdatesLevelsCompleted()
        {
            var svc = new LeaderboardService();
            svc.TrySubmitScore(1, 100, 5);
            Assert.That(svc.LevelsCompleted, Is.EqualTo(1));
            svc.TrySubmitScore(2, 200, 3);
            Assert.That(svc.LevelsCompleted, Is.EqualTo(2));
        }

        // ── GetEntry ────────────────────────────────────────────────────────

        [Test]
        public void GetEntry_ExistingLevel_ReturnsEntry()
        {
            var svc = new LeaderboardService();
            svc.TrySubmitScore(42, 500, 7);
            var entry = svc.GetEntry(42);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.LevelIndex, Is.EqualTo(42));
            Assert.That(entry.BestScore, Is.EqualTo(500));
            Assert.That(entry.BestPourCount, Is.EqualTo(7));
        }

        [Test]
        public void GetEntry_NonExistent_ReturnsNull()
        {
            var svc = new LeaderboardService();
            Assert.That(svc.GetEntry(99), Is.Null);
        }

        // ── GetAllEntries ────────────────────────────────────────────────

        [Test]
        public void GetAllEntries_ReturnsSortedByLevelIndex()
        {
            var svc = new LeaderboardService();
            svc.TrySubmitScore(3, 100, 5);
            svc.TrySubmitScore(1, 200, 3);
            svc.TrySubmitScore(2, 300, 4);

            var entries = svc.GetAllEntries();
            Assert.That(entries.Select(e => e.LevelIndex), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        // ── ResetAll ─────────────────────────────────────────────────────────

        [Test]
        public void ResetAll_ClearsEverything()
        {
            var svc = new LeaderboardService();
            svc.TrySubmitScore(1, 100, 5);
            svc.TrySubmitScore(2, 200, 3);

            svc.ResetAll();

            Assert.That(svc.GetAllEntries(), Is.Empty);
            Assert.That(svc.TotalScore, Is.EqualTo(0));
            Assert.That(svc.LevelsCompleted, Is.EqualTo(0));
        }

        // ── Persistence ──────────────────────────────────────────────────────

        [Test]
        public void Constructor_LoadsPersistedData()
        {
            var first = new LeaderboardService();
            first.TrySubmitScore(1, 150, 4);
            first.TrySubmitScore(2, 250, 2);

            var second = new LeaderboardService();
            var entries = second.GetAllEntries();
            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(second.TotalScore, Is.EqualTo(400));
            Assert.That(second.LevelsCompleted, Is.EqualTo(2));
        }
    }
}
