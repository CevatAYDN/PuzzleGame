namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Pure Domain representation of a visual pattern to be used alongside <c>DomainColor</c>.
    /// Provides color-blind accessibility by allowing liquid layers to be distinguished by pattern
    /// as well as color.
    /// </summary>
    public enum DomainPattern
    {
        None = 0,
        Solid = 1,
        Stripes = 2,
        Dots = 3,
        Waves = 4,
        Crosshatch = 5,
        Zigzag = 6,
        Checkered = 7,
        Diamonds = 8,
        Rings = 9,
        Stars = 10,
        Triangles = 11,
        Squares = 12
    }
}
