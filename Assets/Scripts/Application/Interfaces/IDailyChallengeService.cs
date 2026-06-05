using System;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Application.Interfaces
{
    public struct DailyChallengeState
    {
        public bool HasChallenge;
        public int Seed;
        public DateTime IssuedAtUtc;
        public bool Completed;
    }

    public interface IDailyChallengeService
    {
        DailyChallengeState GetTodayChallenge();
        void MarkCompleted();
        void Reset();
    }

    public interface IStreakService
    {
        int CurrentStreak { get; }
        int LongestStreak { get; }
        DateTime? LastClaimedUtc { get; }
        bool IsClaimableToday { get; }
        bool TryClaim();
    }
}
