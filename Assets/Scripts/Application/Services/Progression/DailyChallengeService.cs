using System;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Generates a deterministic seed for "today" (UTC) so the same daily challenge
    /// is shared across all installs on a given date.
    /// </summary>
    public sealed class DailyChallengeService : IDailyChallengeService
    {
        private const string LogTag = "[DailyChallenge]";
        private const string SeedKey = "PuzzleGame.Daily.Seed";
        private const string IssuedKey = "PuzzleGame.Daily.IssuedAt";
        private const string CompletedKey = "PuzzleGame.Daily.Completed";
        private const string RewardClaimedKey = "PuzzleGame.Daily.RewardClaimed";

        private readonly GameConfig _config;
        private readonly ICoinWallet _wallet;
        private readonly IStreakService _streak;

        public DailyChallengeService(GameConfig config, ICoinWallet wallet, IStreakService streak)
        {
            _config = config;
            _wallet = wallet;
            _streak = streak;
        }

        public DailyChallengeState GetTodayChallenge()
        {
            var today = DateTime.UtcNow.Date;
            var storedUnix = PlayerPrefs.GetString(IssuedKey, string.Empty);
            
            long issuedUnix = 0;
            bool hasStored = !string.IsNullOrEmpty(storedUnix) && long.TryParse(storedUnix, out issuedUnix);
            var issued = hasStored ? DateTimeOffset.FromUnixTimeSeconds(issuedUnix).UtcDateTime.Date : (DateTime?)null;
            
            if (!issued.HasValue || issued.Value != today)
            {
                int newSeed = unchecked((int)today.ToBinary());
                PlayerPrefs.SetString(IssuedKey, new DateTimeOffset(today).ToUnixTimeSeconds().ToString());
                PlayerPrefs.SetInt(SeedKey, newSeed);
                PlayerPrefs.SetInt(CompletedKey, 0);
                PlayerPrefs.Save();
                MoldLogger.LogInfo($"{LogTag} New daily challenge issued (seed={newSeed}).");
                return new DailyChallengeState
                {
                    HasChallenge = true,
                    Seed = newSeed,
                    IssuedAtUtc = today,
                    Completed = false
                };
            }

            return new DailyChallengeState
            {
                HasChallenge = true,
                Seed = PlayerPrefs.GetInt(SeedKey, 0),
                IssuedAtUtc = issued.Value,
                Completed = PlayerPrefs.GetInt(CompletedKey, 0) == 1
            };
        }

        public void MarkCompleted()
        {
            bool alreadyClaimed = PlayerPrefs.GetInt(RewardClaimedKey, 0) == 1;
            PlayerPrefs.SetInt(CompletedKey, 1);

            if (!alreadyClaimed)
            {
                int baseReward = _config != null ? _config.dailyChallengeCoinReward : 50;
                int streak = _streak?.CurrentStreak ?? 0;
                int multiplier = streak >= 30 ? 3 : streak >= 7 ? 2 : 1;
                int reward = baseReward * multiplier;
                _wallet?.Add(reward, "daily_challenge");
                PlayerPrefs.SetInt(RewardClaimedKey, 1);
                MoldLogger.LogInfo($"{LogTag} Reward claimed: {reward} coins (streak={streak}, x{multiplier}).");
            }
        }

        public void Reset()
        {
            PlayerPrefs.DeleteKey(SeedKey);
            PlayerPrefs.DeleteKey(IssuedKey);
            PlayerPrefs.DeleteKey(CompletedKey);
            PlayerPrefs.DeleteKey(RewardClaimedKey);
        }
    }
}
