namespace PuzzleGame.Application
{
    /// <summary>
    /// Cork (Mold cap) geometry constants for procedural mesh generation.
    /// Shared between Presentation (rendering) and Infrastructure (services).
    /// </summary>
    public static class CorkConstants
    {
        public const float Radius = 0.15f;
        public const float Height = 0.05f;
        public const int   Segments = 32;
        public const float YOffset = -0.02f;

        // Graphite / dark stone color
        public const float WoodR = 0.22f;
        public const float WoodG = 0.22f;
        public const float WoodB = 0.22f;
    }
}
