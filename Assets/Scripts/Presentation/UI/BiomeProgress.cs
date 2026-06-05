using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Calculates player progress within a biome. Pure POCO — testable, Unity-agnostic.
    /// Used by WorldMapController to display "12/25 completed" on each biome card.
    /// </summary>
    public static class BiomeProgress
    {
        public const int CrystalMinesTotal = 25;
        public const int VolcanicForgeTotal = 25;

        public static int GetTotalLevels(Biome biome)
        {
            return biome == Biome.CrystalMines ? CrystalMinesTotal : VolcanicForgeTotal;
        }

        public static int GetStartLevel(Biome biome)
        {
            return biome == Biome.CrystalMines ? 1 : LevelBiomeClassifier.VolcanicForgeStartLevel;
        }

        public static int GetEndLevel(Biome biome)
        {
            return biome == Biome.CrystalMines
                ? LevelBiomeClassifier.CrystalMinesEndLevel
                : 50;
        }

        public static int GetCompletedCount(ILevelProgressService progress, Biome biome)
        {
            if (progress == null) return 0;
            int start = GetStartLevel(biome);
            int end = GetEndLevel(biome);
            int count = 0;
            for (int i = start; i <= end; i++)
            {
                if (progress.IsCompleted(i)) count++;
            }
            return count;
        }

        public static int GetStarCount(ILevelProgressService progress, Biome biome)
        {
            if (progress == null) return 0;
            int start = GetStartLevel(biome);
            int end = GetEndLevel(biome);
            int stars = 0;
            for (int i = start; i <= end; i++)
            {
                stars += progress.GetStars(i);
            }
            return stars;
        }

        public static int GetMaxStars(Biome biome)
        {
            return GetTotalLevels(biome) * 3;
        }

        public static bool IsBiomeComplete(ILevelProgressService progress, Biome biome)
        {
            return GetCompletedCount(progress, biome) >= GetTotalLevels(biome);
        }
    }
}
