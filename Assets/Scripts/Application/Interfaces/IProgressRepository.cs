using System.Collections.Generic;

namespace PuzzleGame.Application.Interfaces
{
    public interface IProgressRepository
    {
        void LoadProgress(out int totalXp, out int seasonXp, HashSet<int> claimedTiers);
        void SaveXp(int totalXp, int seasonXp);
        void SaveClaimedTier(int tierIndex);
        void ResetProgress();
    }
}
