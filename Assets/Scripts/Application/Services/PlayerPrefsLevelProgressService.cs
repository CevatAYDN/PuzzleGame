using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using UnityEngine;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// PlayerPrefs-backed level progression service.
    /// Simple, local-only persistence — suitable for single-device play.
    /// Follows the same "only overwrite if better" policy as SecureFileLevelProgressService.
    /// </summary>
    public class PlayerPrefsLevelProgressService : ILevelProgressService
    {
        private const string StarsKeyFormat = "level_{0}_stars";
        private const string MovesKeyFormat = "level_{0}_moves";

        public bool IsUnlocked(int levelNumber)
        {
            if (levelNumber <= 1) return true;
            return IsCompleted(levelNumber - 1);
        }

        public int GetStars(int levelNumber) =>
            PlayerPrefs.GetInt(string.Format(StarsKeyFormat, levelNumber), 0);

        public int GetBestMoves(int levelNumber) =>
            PlayerPrefs.GetInt(string.Format(MovesKeyFormat, levelNumber), 0);

        public bool IsCompleted(int levelNumber) =>
            GetStars(levelNumber) > 0;

        public void RecordCompletion(int levelNumber, int moveCount, int stars)
        {
            if (levelNumber < 1 || moveCount < 1 || stars < 1) return;

            int prevStars = GetStars(levelNumber);
            int prevMoves = GetBestMoves(levelNumber);

            bool isBetter = stars > prevStars ||
                            (stars == prevStars && moveCount < prevMoves) ||
                            prevStars == 0;

            if (!isBetter)
            {
                BottleLogger.LogDebug($"Level {levelNumber} progress kept: existing {prevStars}★/{prevMoves} ≥ new {stars}★/{moveCount}");
                return;
            }

            PlayerPrefs.SetInt(string.Format(StarsKeyFormat, levelNumber), stars);
            PlayerPrefs.SetInt(string.Format(MovesKeyFormat, levelNumber), moveCount);
            PlayerPrefs.Save();

            EventAggregator.Publish(new LevelProgressChangedEvent(levelNumber, stars, moveCount));
        }

        public void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            BottleLogger.LogInfo("All level progress cleared (PlayerPrefs).");
        }
    }
}
