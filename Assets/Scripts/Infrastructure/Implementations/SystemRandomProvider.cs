using System;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Wraps <see cref="System.Random"/> as an <see cref="IRandomProvider"/>.
    /// Uses a shared instance for production use.
    /// </summary>
    public sealed class SystemRandomProvider : IRandomProvider
    {
        private readonly Random _rng;

        public SystemRandomProvider() : this(new Random()) { }

        public SystemRandomProvider(Random rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        public int Next(int maxValue) => _rng.Next(maxValue);
    }
}
