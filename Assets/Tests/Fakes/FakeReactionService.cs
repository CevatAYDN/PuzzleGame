using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Configuration.FeatureSystem;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for IReactionService.
    /// </summary>
    public class FakeReactionService : IReactionService
    {
        public int CheckReactionsResult { get; set; }
        public int CheckReactionsCallCount { get; private set; }
        public IMoldView[] LastMolds { get; private set; }
        public ReactionSystemData LastConfig { get; private set; }

        public int CheckReactions(IMoldView[] Molds, ReactionSystemData config)
        {
            CheckReactionsCallCount++;
            LastMolds = Molds;
            LastConfig = config;
            return CheckReactionsResult;
        }
    }
}
