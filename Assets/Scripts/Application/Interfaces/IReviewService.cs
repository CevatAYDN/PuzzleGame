using System;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Service for requesting app ratings/reviews from the user.
    /// Used for ASO (App Store Optimization) to prompt ratings at positive moments.
    /// </summary>
    public interface IReviewService
    {
        /// <summary>
        /// Requests the in-app review dialog to be shown.
        /// The OS may decide whether to actually show it based on its own quotas.
        /// </summary>
        void RequestInAppReview();
        
        /// <summary>
        /// Checks if the user should be prompted based on internal heuristics (e.g., levels completed, time played).
        /// </summary>
        bool ShouldPromptForReview();
    }
}
