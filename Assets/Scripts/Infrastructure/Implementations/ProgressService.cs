using System;
using System.Collections.Generic;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    public sealed class ProgressService : IProgressService
    {
        private const string LogTag = "[Progress]";
        private const string XpPrefKey = "PuzzleGame.Progress.TotalXp";
        private const string SeasonXpPrefKey = "PuzzleGame.Progress.SeasonXp";
        private const string ClaimedPrefPrefix = "PuzzleGame.Progress.Claimed.";

        private readonly SeasonConfig _config;
        private readonly IProgressRepository _repository;
        private int _totalXp;
        private int _seasonXp;
        private readonly HashSet<int> _claimedTiers = new HashSet<int>();

        public int TotalXp => _totalXp;
        public int PlayerLevel => _totalXp / Math.Max(1, _config.xpPerPlayerLevel);
        public int SeasonXp => _seasonXp;
        public int CurrentTierIndex => ComputeCurrentTier();
        public int SeasonXpToNextTier => ComputeXpToNextTier();
        public bool IsSeasonActive => ComputeSeasonActive();
        public SeasonDef ActiveSeason => GetActiveSeason();

        public ProgressService(SeasonConfig config, IProgressRepository repository)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            LoadState();
        }

        public void AddLevelXp(int levelIndex, int stars, bool wasEfficient)
        {
            var xp = _config.xpPerLevelComplete;
            xp += Mathf.Clamp(stars, 0, 3) * _config.xpPerStar;
            if (wasEfficient) xp += _config.xpEfficiencyBonus;
            AddXp(xp);
            MoldLogger.LogInfo($"{LogTag} Level {levelIndex} complete (+{xp} XP)");
        }

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            _totalXp += amount;
            _seasonXp += amount;
            _repository.SaveXp(_totalXp, _seasonXp);
            MoldLogger.LogInfo($"{LogTag} +{amount} XP (total: {_totalXp})");
        }

        public IReadOnlyList<SeasonDef> GetAllSeasons()
        {
            return _config.seasons.AsReadOnly();
        }

        public IReadOnlyCollection<int> GetClaimedTierIds()
        {
            return _claimedTiers;
        }

        public bool ClaimTierReward(int tierIndex, ICoinWallet wallet)
        {
            if (!CanClaimTier(tierIndex)) return false;

            var season = ActiveSeason;
            if (season == null || tierIndex < 0 || tierIndex >= season.rewards.Count) return false;

            var reward = season.rewards[tierIndex];
            switch (reward.rewardType)
            {
                case RewardType.Coins:
                    var coinAmount = int.TryParse(reward.rewardValue, out var c) ? c : 0;
                    if (coinAmount > 0) wallet?.Add(coinAmount, $"season_tier_{tierIndex}");
                    break;
                case RewardType.Hint:
                case RewardType.PowerUp:
                    MoldLogger.LogInfo($"{LogTag} Reward type {reward.rewardType} value={reward.rewardValue} — UI should handle.");
                    break;
                case RewardType.CosmeticItem:
                    if (!string.IsNullOrEmpty(reward.cosmeticItemId))
                        MoldLogger.LogInfo($"{LogTag} Cosmetic reward: {reward.cosmeticItemId} — unlock via shop.");
                    break;
            }

            _claimedTiers.Add(tierIndex);
            _repository.SaveClaimedTier(tierIndex);
            MoldLogger.LogInfo($"{LogTag} Claimed tier {tierIndex} reward.");
            return true;
        }

        public bool CanClaimTier(int tierIndex)
        {
            if (!IsSeasonActive) return false;
            if (_claimedTiers.Contains(tierIndex)) return false;
            var season = ActiveSeason;
            if (season == null || tierIndex < 0 || tierIndex >= season.rewards.Count) return false;
            var xpNeeded = ComputeTierXpThreshold(tierIndex, season);
            return _seasonXp >= xpNeeded;
        }

        public void ResetAll()
        {
            _totalXp = 0;
            _seasonXp = 0;
            _claimedTiers.Clear();
            _repository.ResetProgress();
            MoldLogger.LogInfo($"{LogTag} All progress reset.");
        }

        private void LoadState()
        {
            _repository.LoadProgress(out _totalXp, out _seasonXp, _claimedTiers);
            MoldLogger.LogInfo($"{LogTag} Loaded: {_totalXp} XP, {_claimedTiers.Count} tiers claimed.");
        }

        private SeasonDef GetActiveSeason()
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var s in _config.seasons)
            {
                if (string.IsNullOrEmpty(s.seasonId)) continue;
                if (IsSeasonActiveForDate(s, now)) return s;
            }
            return null;
        }

        private bool ComputeSeasonActive()
        {
            return GetActiveSeason() != null;
        }

        private static bool IsSeasonActiveForDate(SeasonDef season, DateTimeOffset now)
        {
            if (!string.IsNullOrEmpty(season.startDateIso))
            {
                if (!DateTimeOffset.TryParse(season.startDateIso, out var start)) return false;
                if (now < start) return false;
            }
            if (!string.IsNullOrEmpty(season.endDateIso))
            {
                if (!DateTimeOffset.TryParse(season.endDateIso, out var end)) return false;
                if (now > end) return false;
            }
            return true;
        }

        private int ComputeCurrentTier()
        {
            var season = ActiveSeason;
            if (season == null) return -1;
            for (int i = season.rewards.Count - 1; i >= 0; i--)
            {
                if (_seasonXp >= ComputeTierXpThreshold(i, season))
                    return i;
            }
            return 0;
        }

        private int ComputeXpToNextTier()
        {
            var season = ActiveSeason;
            if (season == null) return 0;
            var next = CurrentTierIndex + 1;
            if (next >= season.rewards.Count) return 0;
            var needed = ComputeTierXpThreshold(next, season);
            return Math.Max(0, needed - _seasonXp);
        }

        private static int ComputeTierXpThreshold(int tierIndex, SeasonDef season)
        {
            return season.baseXp + tierIndex * season.xpPerTier;
        }
    }
}
