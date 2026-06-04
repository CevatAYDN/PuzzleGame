using System;
using PuzzleGame.Application.Configuration.FeatureSystem;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Handles chemical reactions between colors after Casts.
    /// Supports Explosion, Transform, and Bubble reaction types.
    /// </summary>
    public interface IReactionService
    {
        /// <summary>
        /// Check for reactions in all Molds after a Cast.
        /// Returns number of Molds that had reactions.
        /// </summary>
        /// <exception cref="ArgumentNullException">If Molds is null.</exception>
        int CheckReactions(IMoldView[] Molds, ReactionSystemData config);
    }
}
