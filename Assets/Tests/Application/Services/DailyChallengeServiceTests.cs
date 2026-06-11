using System;
using NUnit.Framework;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Tests.Fakes;
using UnityEngine;

namespace PuzzleGame.Tests.Application.Services
{
    public class DailyChallengeServiceTests
    {
        private const string SeedKey = "PuzzleGame.Daily.Seed";
        private const string IssuedKey = "PuzzleGame.Daily.IssuedAt";
        private const string CompletedKey = "PuzzleGame.Daily.Completed";

        private DailyChallengeService _sut;
        private GameConfig _config;
        private FakeCoinWallet _wallet;
        private FakeStreakService _streak;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(SeedKey);
            PlayerPrefs.DeleteKey(IssuedKey);
            PlayerPrefs.DeleteKey(CompletedKey);
            _config = ScriptableObject.CreateInstance<GameConfig>();
            _wallet = new FakeCoinWallet(0);
            _streak = new FakeStreakService();
            _sut = new DailyChallengeService(_config, _wallet, _streak);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(SeedKey);
            PlayerPrefs.DeleteKey(IssuedKey);
            PlayerPrefs.DeleteKey(CompletedKey);
            if (_config != null) UnityEngine.Object.DestroyImmediate(_config);
        }

        // ── First call: issue new challenge ───────────────────────────────

        [Test]
        public void GetTodayChallenge_FirstCall_IssuesChallenge()
        {
            var state = _sut.GetTodayChallenge();

            Assert.That(state.HasChallenge, Is.True);
        }

        [Test]
        public void GetTodayChallenge_FirstCall_IssuedAtUtcIsTodayUtc()
        {
            var state = _sut.GetTodayChallenge();

            Assert.That(state.IssuedAtUtc, Is.EqualTo(DateTime.UtcNow.Date));
        }

        [Test]
        public void GetTodayChallenge_FirstCall_SeedIsBasedOnTodayBinary()
        {
            var state = _sut.GetTodayChallenge();
            var expectedSeed = unchecked((int)DateTime.UtcNow.Date.ToBinary());

            Assert.That(state.Seed, Is.EqualTo(expectedSeed));
        }

        [Test]
        public void GetTodayChallenge_FirstCall_NotCompleted()
        {
            var state = _sut.GetTodayChallenge();

            Assert.That(state.Completed, Is.False);
        }

        [Test]
        public void GetTodayChallenge_FirstCall_PersistsSeedAndIssuedAt()
        {
            _sut.GetTodayChallenge();

            Assert.That(PlayerPrefs.HasKey(SeedKey), Is.True);
            Assert.That(PlayerPrefs.HasKey(IssuedKey), Is.True);
        }

        // ── Same-day idempotency ──────────────────────────────────────────

        [Test]
        public void GetTodayChallenge_SecondCallSameDay_ReturnsSameSeed()
        {
            var first = _sut.GetTodayChallenge();
            var second = _sut.GetTodayChallenge();

            Assert.That(second.Seed, Is.EqualTo(first.Seed));
            Assert.That(second.IssuedAtUtc, Is.EqualTo(first.IssuedAtUtc));
        }

        [Test]
        public void GetTodayChallenge_SecondCallSameDay_DoesNotReseedPlayerPrefs()
        {
            _sut.GetTodayChallenge();
            int seedAfterFirst = PlayerPrefs.GetInt(SeedKey, -1);
            string issuedAfterFirst = PlayerPrefs.GetString(IssuedKey, string.Empty);

            _sut.GetTodayChallenge();
            int seedAfterSecond = PlayerPrefs.GetInt(SeedKey, -1);
            string issuedAfterSecond = PlayerPrefs.GetString(IssuedKey, string.Empty);

            Assert.That(seedAfterSecond, Is.EqualTo(seedAfterFirst));
            Assert.That(issuedAfterSecond, Is.EqualTo(issuedAfterFirst));
        }

        // ── MarkCompleted ────────────────────────────────────────────────

        [Test]
        public void MarkCompleted_SetsCompletedFlag()
        {
            _sut.GetTodayChallenge();
            _sut.MarkCompleted();

            var state = _sut.GetTodayChallenge();
            Assert.That(state.Completed, Is.True);
        }

        [Test]
        public void MarkCompleted_PersistsAcrossInstances()
        {
            _sut.GetTodayChallenge();
            _sut.MarkCompleted();

            var second = new DailyChallengeService(_config, _wallet, _streak);
            var state = second.GetTodayChallenge();
            Assert.That(state.Completed, Is.True);
        }

        [Test]
        public void MarkCompleted_DoesNotChangeSeed()
        {
            _sut.GetTodayChallenge();
            int seedBefore = PlayerPrefs.GetInt(SeedKey, -1);
            _sut.MarkCompleted();
            int seedAfter = PlayerPrefs.GetInt(SeedKey, -1);

            Assert.That(seedAfter, Is.EqualTo(seedBefore));
        }

        // ── Reset ────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsAllKeys()
        {
            _sut.GetTodayChallenge();
            _sut.MarkCompleted();

            _sut.Reset();

            Assert.That(PlayerPrefs.HasKey(SeedKey), Is.False);
            Assert.That(PlayerPrefs.HasKey(IssuedKey), Is.False);
            Assert.That(PlayerPrefs.HasKey(CompletedKey), Is.False);
        }

        [Test]
        public void Reset_AfterReset_NextCallIssuesNewChallenge()
        {
            _sut.GetTodayChallenge();
            int originalSeed = PlayerPrefs.GetInt(SeedKey, 0);

            _sut.Reset();
            var fresh = _sut.GetTodayChallenge();

            // Same day, but the call must still issue (no issued key) and produce same deterministic seed.
            Assert.That(fresh.Seed, Is.EqualTo(originalSeed));
            Assert.That(fresh.Completed, Is.False);
        }
    }
}
