using PuzzleGame.Domain.Models;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Determines which biome a level belongs to. Pure POCO — testable, Unity-agnostic.
    /// GDD decision: L01-L25 = Crystal Mines (blue/purple/green), L26-L50 = Volcanic Forge (orange/red/yellow).
    /// </summary>
    public static class LevelBiomeClassifier
    {
        public const int CrystalMinesEndLevel = 25;
        public const int VolcanicForgeStartLevel = 26;

        public static Biome GetBiome(int levelNumber)
        {
            if (levelNumber <= 0) return Biome.CrystalMines;
            return levelNumber <= CrystalMinesEndLevel
                ? Biome.CrystalMines
                : Biome.VolcanicForge;
        }

        public static bool IsInCrystalMines(int levelNumber) =>
            levelNumber >= 1 && levelNumber <= CrystalMinesEndLevel;

        public static bool IsInVolcanicForge(int levelNumber) =>
            levelNumber >= VolcanicForgeStartLevel;
    }
}
