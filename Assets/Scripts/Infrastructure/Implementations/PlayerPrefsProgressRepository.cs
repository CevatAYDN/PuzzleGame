using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    public class PlayerPrefsProgressRepository : IProgressRepository
    {
        private const string XpPrefKey = "PuzzleGame.Progress.TotalXp";
        private const string SeasonXpPrefKey = "PuzzleGame.Progress.SeasonXp";
        private const string ClaimedPrefPrefix = "PuzzleGame.Progress.Claimed.";

        public void LoadProgress(out int totalXp, out int seasonXp, HashSet<int> claimedTiers)
        {
            totalXp = PlayerPrefs.GetInt(XpPrefKey, 0);
            seasonXp = PlayerPrefs.GetInt(SeasonXpPrefKey, 0);
            for (int i = 0; i < 200; i++)
            {
                if (PlayerPrefs.GetInt(ClaimedPrefPrefix + i, 0) == 1)
                    claimedTiers.Add(i);
            }
        }

        public void SaveXp(int totalXp, int seasonXp)
        {
            PlayerPrefs.SetInt(XpPrefKey, totalXp);
            PlayerPrefs.SetInt(SeasonXpPrefKey, seasonXp);
            PlayerPrefs.Save();
        }

        public void SaveClaimedTier(int tierIndex)
        {
            PlayerPrefs.SetInt(ClaimedPrefPrefix + tierIndex, 1);
            PlayerPrefs.Save();
        }

        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(XpPrefKey);
            PlayerPrefs.DeleteKey(SeasonXpPrefKey);
            for (int i = 0; i < 200; i++)
                PlayerPrefs.DeleteKey(ClaimedPrefPrefix + i);
            PlayerPrefs.Save();
        }
    }
}
