using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Secure file-backed level progression service using GameSaveManager.
    /// Employs HMAC-SHA256 signature verification and atomic writing.
    /// </summary>
    public class SecureFileLevelProgressService : ILevelProgressService
    {
        private readonly IEventAggregator _eventAggregator;

        public SecureFileLevelProgressService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator ?? throw new System.ArgumentNullException(nameof(eventAggregator));
        }

        public bool IsUnlocked(int levelNumber)
        {
            if (levelNumber <= 1) return true;
            return IsCompleted(levelNumber - 1);
        }

        public int GetStars(int levelNumber)
        {
            var level = GameSaveManager.LoadLevel(levelNumber);
            return level != null ? level.Value.stars : 0;
        }

        public int GetBestMoves(int levelNumber)
        {
            var level = GameSaveManager.LoadLevel(levelNumber);
            return level != null ? level.Value.moveCount : 0;
        }

        public bool IsCompleted(int levelNumber)
        {
            var level = GameSaveManager.LoadLevel(levelNumber);
            return level != null ? level.Value.isCompleted : false;
        }

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
                BottleLogger.LogDebug($"Level {levelNumber} secure progress kept: existing {prevStars}★/{prevMoves} ≥ new {stars}★/{moveCount}");
                return;
            }

            // Record completion securely. Passing an empty array of bottles since the level is completed.
            GameSaveManager.Save(levelNumber, moveCount, System.Array.Empty<IBottleView>(), isCompleted: true, stars: stars);

            _eventAggregator.Publish(new Events.LevelProgressChangedEvent(levelNumber, stars, moveCount));
        }

        public void ResetAll()
        {
            GameSaveManager.DeleteAll();
            BottleLogger.LogInfo("All level progress secure save files deleted.");
        }
    }
}
