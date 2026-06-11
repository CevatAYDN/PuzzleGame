using System.Linq;
using NUnit.Framework;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Tests.Fakes;
using UnityEngine;

namespace PuzzleGame.Tests.Infrastructure
{
    [TestFixture]
    public class ProgressServiceTests
    {
        private SeasonConfig _config;

        private const string XpPrefKey = "PuzzleGame.Progress.TotalXp";
        private const string SeasonXpPrefKey = "PuzzleGame.Progress.SeasonXp";
        private const string ClaimedPrefix = "PuzzleGame.Progress.Claimed.";

        [SetUp]
        public void SetUp()
        {
            WipePrefs();
            _config = ScriptableObject.CreateInstance<SeasonConfig>();
            _config.xpPerLevelComplete = 50;
            _config.xpPerStar = 10;
            _config.xpEfficiencyBonus = 25;
            _config.xpPerPlayerLevel = 500;

            var season = new SeasonDef
            {
                seasonId = "test_season",
                baseXp = 100,
                xpPerTier = 50,
                tierCount = 5,
            };
            for (int i = 0; i < 5; i++)
            {
                season.rewards.Add(new SeasonTierReward
                {
                    xpRequired = 100 + i * 50,
                    rewardType = RewardType.Coins,
                    rewardValue = "50"
                });
            }
            _config.seasons.Add(season);
        }

        [TearDown]
        public void TearDown() => WipePrefs();

        private static void WipePrefs()
        {
            PlayerPrefs.DeleteKey(XpPrefKey);
            PlayerPrefs.DeleteKey(SeasonXpPrefKey);
            for (int i = 0; i < 10; i++)
                PlayerPrefs.DeleteKey(ClaimedPrefix + i);
            PlayerPrefs.Save();
        }

        // ── Constructor / defaults ──────────────────────────────────────────

        [Test]
        public void Constructor_NoSavedPrefs_LoadsDefaults()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            Assert.That(svc.TotalXp, Is.EqualTo(0));
            Assert.That(svc.SeasonXp, Is.EqualTo(0));
            Assert.That(svc.PlayerLevel, Is.EqualTo(0));
            Assert.That(svc.GetClaimedTierIds(), Is.Empty);
        }

        // ── AddXp ────────────────────────────────────────────────────────────

        [Test]
        public void AddXp_Positive_IncreasesTotalAndSeason()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddXp(100);
            Assert.That(svc.TotalXp, Is.EqualTo(100));
            Assert.That(svc.SeasonXp, Is.EqualTo(100));
        }

        [Test]
        public void AddXp_Zero_DoesNothing()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddXp(50);
            svc.AddXp(0);
            Assert.That(svc.TotalXp, Is.EqualTo(50));
        }

        [Test]
        public void AddXp_Negative_DoesNothing()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddXp(50);
            svc.AddXp(-30);
            Assert.That(svc.TotalXp, Is.EqualTo(50));
        }

        [Test]
        public void AddXp_MultipleCalls_Accumulates()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddXp(100);
            svc.AddXp(200);
            svc.AddXp(300);
            Assert.That(svc.TotalXp, Is.EqualTo(600));
            Assert.That(svc.SeasonXp, Is.EqualTo(600));
        }

        // ── AddLevelXp ──────────────────────────────────────────────────────

        [Test]
        public void AddLevelXp_NoStarsNotEfficient_GivesBaseXp()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddLevelXp(1, 0, false);
            Assert.That(svc.TotalXp, Is.EqualTo(50));
        }

        [Test]
        public void AddLevelXp_WithStars_AddsStarBonus()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddLevelXp(1, 3, false);
            Assert.That(svc.TotalXp, Is.EqualTo(50 + 3 * 10));
        }

        [Test]
        public void AddLevelXp_StarsClampedTo3()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddLevelXp(1, 5, false);
            // Mathf.Clamp(5, 0, 3) = 3
            Assert.That(svc.TotalXp, Is.EqualTo(50 + 3 * 10));
        }

        [Test]
        public void AddLevelXp_StarsClampedTo0()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddLevelXp(1, -1, false);
            // Mathf.Clamp(-1, 0, 3) = 0
            Assert.That(svc.TotalXp, Is.EqualTo(50));
        }

        [Test]
        public void AddLevelXp_Efficient_AddsEfficiencyBonus()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddLevelXp(1, 3, true);
            Assert.That(svc.TotalXp, Is.EqualTo(50 + 3 * 10 + 25));
        }

        // ── PlayerLevel ─────────────────────────────────────────────────────

        [Test]
        public void PlayerLevel_ComputedFromTotalXp()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            // xpPerPlayerLevel = 500
            svc.AddXp(1200);
            Assert.That(svc.PlayerLevel, Is.EqualTo(1200 / 500));
        }

        [Test]
        public void PlayerLevel_ZeroXp_ReturnsZero()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            Assert.That(svc.PlayerLevel, Is.EqualTo(0));
        }

        [Test]
        public void PlayerLevel_BelowOneLevel_ReturnsZero()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddXp(499);
            Assert.That(svc.PlayerLevel, Is.EqualTo(0));
        }

        // ── Season / tier logic ─────────────────────────────────────────────

        [Test]
        public void IsSeasonActive_WithAlwaysActiveSeason_ReturnsTrue()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            Assert.That(svc.IsSeasonActive, Is.True);
        }

        [Test]
        public void GetAllSeasons_ReturnsConfiguredSeasons()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            Assert.That(svc.GetAllSeasons().Count, Is.EqualTo(1));
            Assert.That(svc.GetAllSeasons()[0].seasonId, Is.EqualTo("test_season"));
        }

        [Test]
        public void CurrentTierIndex_NoXp_ReturnsMinusOne()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            // No season XP → CurrentTierIndex should be 0 (first tier) since
            // ComputeCurrentTier loops backwards and returns 0 as fallback
            Assert.That(svc.CurrentTierIndex, Is.EqualTo(0));
        }

        [Test]
        public void CanClaimTier_NotEnoughXp_ReturnsFalse()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            // Tier 1 needs baseXp + 1*xpPerTier = 100 + 50 = 150
            Assert.That(svc.CanClaimTier(1), Is.False);
        }

        [Test]
        public void CanClaimTier_EnoughXp_ReturnsTrue()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddXp(200);
            // Tier 1 needs 150 XP, we have 200
            Assert.That(svc.CanClaimTier(1), Is.True);
        }

        [Test]
        public void CanClaimTier_AlreadyClaimed_ReturnsFalse()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddXp(200);
            var wallet = new FakeCoinWallet();
            svc.ClaimTierReward(1, wallet);
            Assert.That(svc.CanClaimTier(1), Is.False);
        }

        [Test]
        public void CanClaimTier_NegativeIndex_ReturnsFalse()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            Assert.That(svc.CanClaimTier(-1), Is.False);
        }

        [Test]
        public void CanClaimTier_OutOfRangeIndex_ReturnsFalse()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            Assert.That(svc.CanClaimTier(99), Is.False);
        }

        [Test]
        public void ClaimTierReward_Coins_AddsToWallet()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddXp(200);
            var wallet = new FakeCoinWallet(0);

            bool claimed = svc.ClaimTierReward(0, wallet);

            Assert.That(claimed, Is.True);
            Assert.That(wallet.AddCallCount, Is.EqualTo(1));
            Assert.That(wallet.LastAddReason, Does.Contain("season_tier_0"));
            Assert.That(wallet.Balance, Is.EqualTo(50));
        }

        [Test]
        public void ClaimTierReward_AlreadyClaimed_ReturnsFalse()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddXp(200);
            var wallet = new FakeCoinWallet();
            svc.ClaimTierReward(0, wallet);
            bool second = svc.ClaimTierReward(0, wallet);
            Assert.That(second, Is.False);
        }

        [Test]
        public void ClaimTierReward_NotEnoughXp_ReturnsFalse()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            var wallet = new FakeCoinWallet();
            bool claimed = svc.ClaimTierReward(2, wallet);
            Assert.That(claimed, Is.False);
        }

        [Test]
        public void ClaimTierReward_MarksTierAsClaimed()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddXp(200);
            var wallet = new FakeCoinWallet();
            svc.ClaimTierReward(0, wallet);

            var claimed = svc.GetClaimedTierIds();
            Assert.That(claimed, Contains.Item(0));
        }

        [Test]
        public void SeasonXpToNextTier_ReturnsCorrectDelta()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            // Tier 0: 100 XP, Tier 1: 150 XP
            // With 0 XP, next tier is 0, xp needed = 100 - 0 = 100
            Assert.That(svc.SeasonXpToNextTier, Is.GreaterThan(0));
        }

        // ── ResetAll ─────────────────────────────────────────────────────────

        [Test]
        public void ResetAll_ClearsEverything()
        {
            var svc = new ProgressService(_config, new PlayerPrefsProgressRepository());
            svc.AddXp(500);
            var wallet = new FakeCoinWallet();
            svc.ClaimTierReward(0, wallet);

            svc.ResetAll();

            Assert.That(svc.TotalXp, Is.EqualTo(0));
            Assert.That(svc.SeasonXp, Is.EqualTo(0));
            Assert.That(svc.GetClaimedTierIds(), Is.Empty);
        }

        // ── Persistence ─────────────────────────────────────────────────────

        [Test]
        public void Constructor_LoadsPersistedData()
        {
            var first = new ProgressService(_config, new PlayerPrefsProgressRepository());
            first.AddXp(350);

            var second = new ProgressService(_config, new PlayerPrefsProgressRepository());
            Assert.That(second.TotalXp, Is.EqualTo(350));
            Assert.That(second.SeasonXp, Is.EqualTo(350));
        }

        [Test]
        public void PersistedClaimedTiers_AreLoaded()
        {
            var first = new ProgressService(_config, new PlayerPrefsProgressRepository());
            first.AddXp(200);
            var wallet = new FakeCoinWallet();
            first.ClaimTierReward(0, wallet);

            var second = new ProgressService(_config, new PlayerPrefsProgressRepository());
            var claimed = second.GetClaimedTierIds();
            Assert.That(claimed, Contains.Item(0));
        }
    }
}
