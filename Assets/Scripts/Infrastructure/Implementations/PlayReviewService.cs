using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Mock implementation of In-App Review service.
    /// In a real project, this wraps Google.Play.Review.ReviewManager.
    /// </summary>
    public class PlayReviewService : IReviewService
    {
        private const string LogTag = "[PlayReview]";
        private int _levelsCompletedSinceLastPrompt;

        public void RequestInAppReview()
        {
            MoldLogger.LogInfo($"{LogTag} Requesting In-App Review from OS.");
            // Reset counter after asking
            _levelsCompletedSinceLastPrompt = 0;
        }

        public bool ShouldPromptForReview()
        {
            // Dummy logic: prompt every 10 levels
            _levelsCompletedSinceLastPrompt++;
            return _levelsCompletedSinceLastPrompt >= 10;
        }
    }
}
