using System.Collections.Generic;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for ILevelProgressService. Per-level answers are
    /// settable via dictionaries; RecordCompletion/ResetAll calls are recorded.
    /// </summary>
    public class FakeLevelProgressService : ILevelProgressService
    {
        public Dictionary<int, bool> UnlockedByLevel { get; } = new Dictionary<int, bool>();
        public Dictionary<int, int> StarsByLevel { get; } = new Dictionary<int, int>();
        public Dictionary<int, int> BestMovesByLevel { get; } = new Dictionary<int, int>();
        public Dictionary<int, bool> CompletedByLevel { get; } = new Dictionary<int, bool>();

        public int RecordCompletionCallCount { get; private set; }
        public int LastRecordedLevel { get; private set; }
        public int LastRecordedMoves { get; private set; }
        public int LastRecordedStars { get; private set; }
        public int ResetAllCallCount { get; private set; }

        /// <summary>When true, RecordCompletion also updates the dictionaries to reflect the call.</summary>
        public bool ApplyRecordCompletion { get; set; } = true;

        public bool IsUnlocked(int levelNumber)
        {
            return UnlockedByLevel.TryGetValue(levelNumber, out var v) && v;
        }

        public int GetStars(int levelNumber)
        {
            return StarsByLevel.TryGetValue(levelNumber, out var v) ? v : 0;
        }

        public int GetBestMoves(int levelNumber)
        {
            return BestMovesByLevel.TryGetValue(levelNumber, out var v) ? v : 0;
        }

        public bool IsCompleted(int levelNumber)
        {
            return CompletedByLevel.TryGetValue(levelNumber, out var v) && v;
        }

        public void RecordCompletion(int levelNumber, int moveCount, int stars)
        {
            RecordCompletionCallCount++;
            LastRecordedLevel = levelNumber;
            LastRecordedMoves = moveCount;
            LastRecordedStars = stars;

            if (!ApplyRecordCompletion) return;

            CompletedByLevel[levelNumber] = true;
            if (!BestMovesByLevel.TryGetValue(levelNumber, out var prev) || moveCount < prev)
            {
                BestMovesByLevel[levelNumber] = moveCount;
            }
            if (!StarsByLevel.TryGetValue(levelNumber, out var prevStars) || stars > prevStars)
            {
                StarsByLevel[levelNumber] = stars;
            }
        }

        public void ResetAll()
        {
            ResetAllCallCount++;
            UnlockedByLevel.Clear();
            StarsByLevel.Clear();
            BestMovesByLevel.Clear();
            CompletedByLevel.Clear();
        }
    }
}
