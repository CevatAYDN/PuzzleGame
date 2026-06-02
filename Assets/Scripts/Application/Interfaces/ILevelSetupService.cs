using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Infrastructure.Interfaces;

namespace PuzzleGame.Application.Interfaces
{
    public interface ILevelSetupService
    {
        List<List<LiquidLayer>> GenerateLevelAssignments(IBottleView[] bottles, LevelData currentLevel);
        void SetupBottles(IBottleView[] bottles,
                           LevelData currentLevel,
                           IRendererService rendererService,
                           IBottleValidator validator,
                           IAnimationService animationService);
    }
}
