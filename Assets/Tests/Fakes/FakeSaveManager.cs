using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    public class FakeSaveManager : ISaveManager
    {
        private readonly Dictionary<int, GameSaveData> _saves = new Dictionary<int, GameSaveData>();
        
        public bool HasSaveData => _saves.Count > 0;

        public bool Save(int levelIndex, int moveCount, IMoldView[] Molds, bool isCompleted, int stars)
        {
            _saves[levelIndex] = new GameSaveData
            {
                LevelIndex = levelIndex,
                MoveCount = moveCount,
                IsCompleted = isCompleted,
                Stars = stars,
                SavedAtUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            return true;
        }

        public GameSaveData? LoadLevel(int levelIndex)
        {
            if (_saves.TryGetValue(levelIndex, out var data))
            {
                return data;
            }
            return null;
        }

        public int LoadLastPlayedLevel()
        {
            int last = 0;
            long latestTime = 0;
            foreach (var kvp in _saves)
            {
                if (kvp.Value.SavedAtUnix > latestTime)
                {
                    latestTime = kvp.Value.SavedAtUnix;
                    last = kvp.Key;
                }
            }
            return last;
        }

        public void DeleteAll()
        {
            _saves.Clear();
        }

        public bool VerifyIntegrity()
        {
            return true;
        }
    }
}
