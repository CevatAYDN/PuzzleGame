using System;

namespace PuzzleGame.Application.Interfaces
{
    public enum TutorialStep
    {
        Inactive = 0,
        Welcome = 1,
        TapToSelect = 2,
        TapToCast = 3,
        LevelComplete = 4
    }

    public interface ITutorialService
    {
        TutorialStep CurrentStep { get; }
        bool IsActive { get; }
        string CurrentMessageKey { get; }

        event Action<TutorialStep> OnStepChanged;
        event Action OnTutorialCompleted;

        void Begin();
        void Skip();
        void Reset();
    }
}
