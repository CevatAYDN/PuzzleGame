using System;

namespace PuzzleGame.Domain.Models
{
    public readonly struct AchievementState : IEquatable<AchievementState>
    {
        public AchievementId Id { get; }
        public bool Unlocked { get; }
        public DateTime UnlockedAt { get; }
        public int Progress { get; }
        public int Target { get; }

        public AchievementState(AchievementId id, bool unlocked, DateTime unlockedAt, int progress, int target)
        {
            Id = id;
            Unlocked = unlocked;
            UnlockedAt = unlockedAt;
            Progress = progress;
            Target = target;
        }

        public AchievementState WithProgress(int progress)
        {
            bool nowUnlocked = Unlocked || progress >= Target;
            DateTime at = nowUnlocked && !Unlocked ? DateTime.UtcNow : UnlockedAt;
            return new AchievementState(Id, nowUnlocked, at, progress, Target);
        }

        public AchievementState Unlock(DateTime at) =>
            new AchievementState(Id, true, at, Target, Target);

        public bool Equals(AchievementState other) =>
            Id == other.Id && Unlocked == other.Unlocked && Progress == other.Progress;

        public override bool Equals(object obj) => obj is AchievementState other && Equals(other);
        public override int GetHashCode() => (int)Id ^ (Unlocked ? 1 : 0) ^ Progress.GetHashCode();
    }
}
