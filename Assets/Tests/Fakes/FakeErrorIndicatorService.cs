using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    public class FakeErrorIndicatorService : IErrorIndicatorService
    {
        public int LastErrorMoldIndex { get; private set; } = -1;
        public string LastErrorReason { get; private set; }
        public int ShowErrorCallCount { get; private set; }
        public int ClearAllCallCount { get; private set; }

        public void ShowErrorOnMold(int moldIndex, string reason)
        {
            LastErrorMoldIndex = moldIndex;
            LastErrorReason = reason;
            ShowErrorCallCount++;
        }

        public void ClearAllIndicators()
        {
            ClearAllCallCount++;
        }
    }
}
