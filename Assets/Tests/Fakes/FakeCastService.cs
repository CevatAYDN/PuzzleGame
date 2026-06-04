using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for ICastService.
    /// </summary>
    public class FakeCastService : ICastService
    {
        public bool TryCastResult { get; set; } = true;
        public int GetCastLayerCountResult { get; set; } = 1;

        public int TryCastCallCount { get; private set; }
        public int GetCastLayerCountCallCount { get; private set; }

        public IMoldView[] LastActiveMolds { get; private set; }
        public IMoldView LastSource { get; private set; }
        public IMoldView LastTarget { get; private set; }
        public LevelData LastLevelData { get; private set; }

        public bool TryCast(IMoldView source, IMoldView target, LevelData levelData, IMoldView[] activeMolds)
        {
            TryCastCallCount++;
            LastSource = source;
            LastTarget = target;
            LastLevelData = levelData;
            LastActiveMolds = activeMolds;
            return TryCastResult;
        }

        public int GetCastLayerCount(IMoldView source, IMoldView target, LevelData levelData)
        {
            GetCastLayerCountCallCount++;
            LastSource = source;
            LastTarget = target;
            LastLevelData = levelData;
            return GetCastLayerCountResult;
        }
    }
}
