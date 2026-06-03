using System;
using PuzzleGame.Application.Configuration.FeatureSystem;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Handles chemical reactions between colors after pours.
    /// Supports Explosion, Transform, and Bubble reaction types.
    /// </summary>
    public interface IReactionService
    {
        /// <summary>
        /// Check for reactions in all bottles after a pour.
        /// Returns number of bottles that had reactions.
        /// </summary>
        /// <exception cref="ArgumentNullException">If bottles is null.</exception>
        int CheckReactions(IBottleView[] bottles, ReactionSystemData config);
    }
}
