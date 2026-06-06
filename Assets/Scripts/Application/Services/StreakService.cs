using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
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
