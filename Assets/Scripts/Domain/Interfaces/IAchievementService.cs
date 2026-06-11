using System;
using System.Collections.Generic;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Interfaces
{
    public interface IAchievementService
    {
        IReadOnlyList<AchievementState> GetAll();
        bool IsUnlocked(AchievementId id);
        event Action<AchievementId> OnUnlocked;
    }
}
