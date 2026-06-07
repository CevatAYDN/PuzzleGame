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

        private readonly GameConfig _config;

        public DailyChallengeService(GameConfig config)
        {
            _config = config;
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
            PlayerPrefs.SetInt(CompletedKey, 1);
            // Fix #M5: Removed explicit Save() call. Unity auto-saves on Quit/Dispose.
            // Explicit Save() on every completion causes disk I/O overhead on low-end devices.
            // For explicit flush needed, call PlayerPrefs.Save() from Application.quitting callback.
        }

        public void Reset()
        {
            PlayerPrefs.DeleteKey(SeedKey);
            PlayerPrefs.DeleteKey(IssuedKey);
            PlayerPrefs.DeleteKey(CompletedKey);
        }
    }
}
