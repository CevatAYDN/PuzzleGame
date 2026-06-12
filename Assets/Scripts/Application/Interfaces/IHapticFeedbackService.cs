namespace PuzzleGame.Application.Interfaces
{
    public enum HapticIntensity
    {
        Light = 0,
        Medium = 1,
        Heavy = 2,
        Success = 3,
        Warning = 4,
        Error = 5,
        Selection = 6,
        ContinuousPour = 7,
        PourComplete = 8
    }

    public interface IHapticFeedbackService
    {
        bool IsEnabled { get; set; }
        void Trigger(HapticIntensity intensity);
    }
}
