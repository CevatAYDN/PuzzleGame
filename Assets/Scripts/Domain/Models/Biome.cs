namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Theme/biome identifier. L01-L25 = CrystalMines, L26-L50 = VolcanicForge.
    /// Used by art provider, world map, level select filtering.
    /// Domain enum — accessible from Application/Composition/Infrastructure.
    /// </summary>
    public enum Biome
    {
        CrystalMines,
        VolcanicForge
    }
}
