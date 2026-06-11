using System.Collections.Generic;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// A single leaderboard entry: best score recorded for a level.
    /// </summary>
    public sealed class LeaderboardEntry
    {
        public int LevelIndex { get; }
        public int BestScore { get; }
        public int BestPourCount { get; }
        public long RecordedAtUnix { get; }

        public LeaderboardEntry(int levelIndex, int bestScore, int bestPourCount, long recordedAtUnix)
        {
            LevelIndex = levelIndex;
            BestScore = bestScore;
            BestPourCount = bestPourCount;
            RecordedAtUnix = recordedAtUnix;
        }
    }

    /// <summary>
    /// Tracks personal best scores per level. PlayerPrefs-backed.
    /// </summary>
    public interface ILeaderboardService
    {
        /// <summary>All recorded entries sorted by level index ascending.</summary>
        IReadOnlyList<LeaderboardEntry> GetAllEntries();

        /// <summary>Best entry for a specific level, or null if unplayed.</summary>
        LeaderboardEntry GetEntry(int levelIndex);

        /// <summary>Submit a new score for a level. Returns true if it's a new personal best.</summary>
        bool TrySubmitScore(int levelIndex, int score, int pourCount);

        /// <summary>Total score across all submitted levels.</summary>
        int TotalScore { get; }

        /// <summary>Number of levels with a recorded score.</summary>
        int LevelsCompleted { get; }

        /// <summary>Clear all leaderboard data.</summary>
        void ResetAll();
    }
}
