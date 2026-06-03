using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Secure file-backed level progression service using ISaveManager.
    /// Employs HMAC-SHA256 signature verification and atomic writing.
    /// Fix Critical #1: Now uses injected ISaveManager instead of static GameSaveManager.
    /// </summary>
    public class SecureFileLevelProgressService : ILevelProgressService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISaveManager _saveManager;

        public SecureFileLevelProgressService(IEventAggregator eventAggregator, ISaveManager saveManager)
        {
            _eventAggregator = eventAggregator ?? throw new System.ArgumentNullException(nameof(eventAggregator));
            _saveManager = saveManager ?? throw new System.ArgumentNullException(nameof(saveManager));
        }

        public bool IsUnlocked(int levelNumber)
        {
            if (levelNumber <= 1) return true;
            return IsCompleted(levelNumber - 1);
        }

        public int GetStars(int levelNumber)
        {
            var level = _saveManager.LoadLevel(levelNumber);
            return level.HasValue ? level.Value.Stars : 0;
        }

        public int GetBestMoves(int levelNumber)
        {
            var level = _saveManager.LoadLevel(levelNumber);
            return level.HasValue ? level.Value.MoveCount : 0;
        }

        public bool IsCompleted(int levelNumber)
        {
            var level = _saveManager.LoadLevel(levelNumber);
            return level.HasValue && level.Value.IsCompleted;
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
                BottleLogger.LogDebug($"Level {levelNumber} secure progress kept: existing {prevStars}\u2605/{prevMoves} \u2265 new {stars}\u2605/{moveCount}");
                return;
            }

            _saveManager.Save(levelNumber, moveCount, System.Array.Empty<IBottleView>(), isCompleted: true, stars: stars);
            _eventAggregator.Publish(new LevelProgressChangedEvent(levelNumber, stars, moveCount));
        }

        public void ResetAll()
        {
            _saveManager.DeleteAll();
            BottleLogger.LogInfo("All level progress secure save files deleted.");
        }
    }
}
