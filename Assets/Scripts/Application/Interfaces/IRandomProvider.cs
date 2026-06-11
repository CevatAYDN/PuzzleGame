namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Abstraction for random number generation. Enables deterministic
    /// (seedable) randomness in tests and injectable RNG strategies.
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>Returns a non-negative random integer less than <paramref name="maxValue"/>.</summary>
        int Next(int maxValue);
    }
}
