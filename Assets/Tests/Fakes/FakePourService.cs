using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for IPourService.
    /// </summary>
    public class FakePourService : IPourService
    {
        public bool TryPourResult { get; set; } = true;
        public int GetPourLayerCountResult { get; set; } = 1;

        public int TryPourCallCount { get; private set; }
        public int GetPourLayerCountCallCount { get; private set; }

        public IBottleView[] LastActiveBottles { get; private set; }
        public IBottleView LastSource { get; private set; }
        public IBottleView LastTarget { get; private set; }
        public LevelData LastLevelData { get; private set; }

        public bool TryPour(IBottleView source, IBottleView target, LevelData levelData, IBottleView[] activeBottles)
        {
            TryPourCallCount++;
            LastSource = source;
            LastTarget = target;
            LastLevelData = levelData;
            LastActiveBottles = activeBottles;
            return TryPourResult;
        }

        public int GetPourLayerCount(IBottleView source, IBottleView target, LevelData levelData)
        {
            GetPourLayerCountCallCount++;
            LastSource = source;
            LastTarget = target;
            LastLevelData = levelData;
            return GetPourLayerCountResult;
        }
    }
}
