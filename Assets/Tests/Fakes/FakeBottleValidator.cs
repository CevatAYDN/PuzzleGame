using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for IBottleValidator. All behaviors settable via properties.
    /// </summary>
    public class FakeBottleValidator : IBottleValidator
    {
        public bool CanPourResult { get; set; } = true;
        public bool IsCompleteResult { get; set; }

        public int CanPourCallCount { get; private set; }
        public int IsCompleteCallCount { get; private set; }

        public BottleState LastSource { get; private set; }
        public BottleState LastTarget { get; private set; }
        public BottleState LastCompleteCheck { get; private set; }

        public bool CanPour(BottleState source, BottleState target)
        {
            CanPourCallCount++;
            LastSource = source;
            LastTarget = target;
            return CanPourResult;
        }

        public bool IsComplete(BottleState state)
        {
            IsCompleteCallCount++;
            LastCompleteCheck = state;
            return IsCompleteResult;
        }

        public bool ColorsMatch(DomainColor a, DomainColor b)
        {
            return a.Equals(b);
        }

        public void ResetCounters()
        {
            CanPourCallCount = 0;
            IsCompleteCallCount = 0;
        }
    }
}
