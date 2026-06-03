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
        public List<ReactionResult> CheckReactionsResult { get; set; } = new List<ReactionResult>();
        public int CheckReactionsCallCount { get; private set; }
        public IBottleView[] LastBottles { get; private set; }
        public ReactionSystemData LastConfig { get; private set; }

        public List<ReactionResult> CheckReactions(IBottleView[] bottles, ReactionSystemData config)
        {
            CheckReactionsCallCount++;
            LastBottles = bottles;
            LastConfig = config;
            return CheckReactionsResult;
        }
    }
}
