using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Interfaces;
// IRendererService now in PuzzleGame.Application.Interfaces

namespace PuzzleGame.Application.Interfaces
{
    public interface ILevelSetupService
    {
        List<List<OreLayer>> GenerateLevelAssignments(IMoldView[] Molds, LevelData currentLevel);
        void SetupMolds(IMoldView[] Molds,
                           LevelData currentLevel,
                           IRendererService rendererService,
                           IMoldValidator validator,
                           IAnimationService animationService);
    }
}
