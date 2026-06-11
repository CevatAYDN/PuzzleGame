using System;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    public class FakeStreakService : IStreakService
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public DateTime? LastClaimedUtc { get; set; }
        public bool IsClaimableToday { get; set; } = true;
        public bool TryClaimReturnValue { get; set; } = true;

        public bool TryClaim()
        {
            if (!IsClaimableToday) return false;
            CurrentStreak++;
            if (CurrentStreak > LongestStreak) LongestStreak = CurrentStreak;
            LastClaimedUtc = DateTime.UtcNow;
            return TryClaimReturnValue;
        }
    }
}
