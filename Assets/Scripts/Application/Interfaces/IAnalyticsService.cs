using System.Collections.Generic;

namespace PuzzleGame.Application.Interfaces
{
    public enum AnalyticsEvent
    {
        SessionStart,
        SessionEnd,
        LevelStarted,
        LevelCompleted,
        LevelFailed,
        LevelAbandoned,
        HintUsed,
        UndoUsed,
        CoinEarned,
        CoinSpent,
        AdWatched,
        DailyChallengeStarted,
        DailyChallengeCompleted,
        SettingsChanged,
        TutorialStarted,
        TutorialCompleted,
        Crash,
        ErrorShown
    }

    public interface IAnalyticsService
    {
        bool IsEnabled { get; set; }
        void Track(AnalyticsEvent evt, IReadOnlyDictionary<string, object> properties = null);
        void SetUserProperty(string key, string value);
        void Flush();
    }
}
