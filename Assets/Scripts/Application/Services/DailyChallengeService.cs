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
            var stored = PlayerPrefs.GetString(IssuedKey, string.Empty);
            if (!DateTime.TryParse(stored, null, System.Globalization.DateTimeStyles.RoundtripKind, out var issued) || issued.Date != today)
            {
                int newSeed = unchecked((int)today.ToBinary());
                PlayerPrefs.SetString(IssuedKey, today.ToString("o"));
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
                IssuedAtUtc = issued,
                Completed = PlayerPrefs.GetInt(CompletedKey, 0) == 1
            };
        }

        public void MarkCompleted()
        {
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
        }

        public void Reset()
        {
            PlayerPrefs.DeleteKey(SeedKey);
            PlayerPrefs.DeleteKey(IssuedKey);
            PlayerPrefs.DeleteKey(CompletedKey);
        }
    }

    /// <summary>
    /// Tracks consecutive day logins for daily-reward mechanics.
    /// </summary>
    public sealed class StreakService : IStreakService
    {
        private const string LogTag = "[Streak]";
        private const string CurrentKey = "PuzzleGame.Streak.Current";
        private const string LongestKey = "PuzzleGame.Streak.Longest";
        private const string LastClaimedKey = "PuzzleGame.Streak.LastClaimed";

        public int CurrentStreak => PlayerPrefs.GetInt(CurrentKey, 0);
        public int LongestStreak => PlayerPrefs.GetInt(LongestKey, 0);

        public DateTime? LastClaimedUtc
        {
            get
            {
                var raw = PlayerPrefs.GetString(LastClaimedKey, string.Empty);
                if (string.IsNullOrEmpty(raw)) return null;
                return DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                    ? dt.ToUniversalTime()
                    : (DateTime?)null;
            }
        }

        public bool IsClaimableToday
        {
            get
            {
                var last = LastClaimedUtc;
                if (!last.HasValue) return true;
                return last.Value.Date < DateTime.UtcNow.Date;
            }
        }

        public bool TryClaim()
        {
            if (!IsClaimableToday) return false;
            var last = LastClaimedUtc;
            var today = DateTime.UtcNow.Date;
            int current = CurrentStreak;

            if (last.HasValue && (today - last.Value.Date).TotalDays <= 1)
                current += 1;
            else
                current = 1;

            int longest = System.Math.Max(LongestStreak, current);
            PlayerPrefs.SetInt(CurrentKey, current);
            PlayerPrefs.SetInt(LongestKey, longest);
            PlayerPrefs.SetString(LastClaimedKey, DateTime.UtcNow.ToString("o"));
            PlayerPrefs.Save();
            MoldLogger.LogInfo($"{LogTag} Claimed. Current={current}, Longest={longest}.");
            return true;
        }
    }
}
