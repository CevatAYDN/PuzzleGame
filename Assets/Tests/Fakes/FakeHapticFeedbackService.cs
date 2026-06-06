using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    public class FakeHapticFeedbackService : IHapticFeedbackService
    {
        public bool IsEnabled { get; set; } = true;
        public HapticIntensity LastTriggeredIntensity { get; private set; }
        public int TriggerCallCount { get; private set; }

        public void Trigger(HapticIntensity intensity)
        {
            LastTriggeredIntensity = intensity;
            TriggerCallCount++;
        }

        public void Reset()
        {
            TriggerCallCount = 0;
        }
    }
}
