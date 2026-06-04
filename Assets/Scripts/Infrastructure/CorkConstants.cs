namespace PuzzleGame.Infrastructure
{
    /// <summary>
    /// Cork (Mold cap) geometry constants for procedural mesh generation.
    /// Moved from Domain (rendering concern).
    /// </summary>
    public static class CorkConstants
    {
        public const float Radius = 0.15f;
        public const float Height = 0.25f;
        public const int   Segments = 16;
        public const float YOffset = 0.05f;

        // Wood material color
        public const float WoodR = 0.45f;
        public const float WoodG = 0.28f;
        public const float WoodB = 0.16f;
    }
}
