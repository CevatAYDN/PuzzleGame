using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Events;
using PuzzleGame.Logging;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// PlayerPrefs-backed progress. Keys: "level_{n}_stars" / "level_{n}_moves".
    /// Level 1 always unlocked. Level N unlocked iff Level N-1 completed.
    /// </summary>
    public class PlayerPrefsLevelProgressService : ILevelProgressService
    {
        private const string StarsKeyPrefix = "level_{0}_stars";
        private const string MovesKeyPrefix = "level_{0}_moves";

        public bool IsUnlocked(int levelNumber)
        {
            if (levelNumber <= 1) return true;
            return IsCompleted(levelNumber - 1);
        }

        public int GetStars(int levelNumber)
        {
            return PlayerPrefs.GetInt(FormatKey(StarsKeyPrefix, levelNumber), 0);
        }

        public int GetBestMoves(int levelNumber)
        {
            return PlayerPrefs.GetInt(FormatKey(MovesKeyPrefix, levelNumber), 0);
        }

        public bool IsCompleted(int levelNumber)
        {
            return GetStars(levelNumber) > 0;
        }

        public void RecordCompletion(int levelNumber, int moveCount, int stars)
        {
            if (levelNumber < 1 || moveCount < 1 || stars < 1) return;

            int prevStars = GetStars(levelNumber);
            int prevMoves = GetBestMoves(levelNumber);

            // Overwrite only if better (more stars, or same stars + fewer moves)
            bool isBetter = stars > prevStars ||
                            (stars == prevStars && moveCount < prevMoves) ||
                            prevStars == 0;

            if (!isBetter)
            {
                BottleLogger.LogDebug($"Level {levelNumber} progress kept: existing {prevStars}★/{prevMoves} ≥ new {stars}★/{moveCount}");
                return;
            }

            PlayerPrefs.SetInt(FormatKey(StarsKeyPrefix, levelNumber), stars);
            PlayerPrefs.SetInt(FormatKey(MovesKeyPrefix, levelNumber), moveCount);
            PlayerPrefs.Save();

            EventAggregator.Publish(new Events.LevelProgressChangedEvent(levelNumber, stars, moveCount));
        }

        public void ResetAll()
        {
            // Wipe all level_* keys
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            BottleLogger.LogInfo("All level progress reset.");
        }

        private static string FormatKey(string prefix, int levelNumber)
            => string.Format(prefix, levelNumber);
    }
}
