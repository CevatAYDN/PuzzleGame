using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for IMoldValidator. All behaviors settable via properties.
    /// </summary>
    public class FakeMoldValidator : IMoldValidator
    {
        public bool CanCastResult { get; set; } = true;
        public bool IsCompleteResult { get; set; }
        public bool CanBreakCorkResult { get; set; } = true;

        public int CanCastCallCount { get; private set; }
        public int IsCompleteCallCount { get; private set; }
        public int CanBreakCorkCallCount { get; private set; }

        public MoldState LastSource { get; private set; }
        public MoldState LastTarget { get; private set; }
        public MoldState LastCompleteCheck { get; private set; }
        public MoldState LastBreakCorkSource { get; private set; }
        public MoldState LastBreakCorkTarget { get; private set; }

        public bool CanMultiCast(MoldState[] sources, MoldState target) => CanCastResult;

        public bool CanCast(MoldState source, MoldState target)
        {
            CanCastCallCount++;
            LastSource = source;
            LastTarget = target;
            return CanCastResult;
        }

        public bool CanBreakCork(MoldState source, MoldState target)
        {
            CanBreakCorkCallCount++;
            LastBreakCorkSource = source;
            LastBreakCorkTarget = target;
            return CanBreakCorkResult;
        }

        public bool IsComplete(MoldState state)
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
            CanCastCallCount = 0;
            IsCompleteCallCount = 0;
            CanBreakCorkCallCount = 0;
        }
    }
}
