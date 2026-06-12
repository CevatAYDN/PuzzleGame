using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

#if HAS_FIREBASE_ANALYTICS
using Firebase.Analytics;
#endif

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Firebase Analytics adapter implementing the IAnalyticsService contract.
    /// When com.google.firebase.analytics is not installed, methods are no-ops
    /// that still respect IsEnabled and debug logging so other systems can run
    /// in editor/CI without the package.
    /// </summary>
    public class FirebaseAnalyticsService : IAnalyticsService
    {
        private const string LogTag = "[Analytics-Firebase]";

        public bool IsEnabled { get; set; } = true;

#if HAS_FIREBASE_ANALYTICS
        private bool _initialized;

        public FirebaseAnalyticsService()
        {
            try
            {
                Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
                {
                    _initialized = task.Result == Firebase.DependencyStatus.Available;
                    MoldLogger.LogInfo($"{LogTag} Init. Available={_initialized}");
                });
            }
            catch (System.Exception e)
            {
                MoldLogger.LogError($"{LogTag} Init failed: {e.Message}");
            }
        }
#endif

        public void Track(AnalyticsEvent evt, IReadOnlyDictionary<string, object> properties = null)
        {
            if (!IsEnabled) return;

#if HAS_FIREBASE_ANALYTICS
            if (!_initialized) return;
            var parameters = new List<Parameter>();
            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    parameters.Add(new Parameter(kvp.Key, kvp.Value?.ToString() ?? string.Empty));
                }
            }
            FirebaseAnalytics.LogEvent(MapEventName(evt), parameters.ToArray());
#else
            if (MoldLogger.IsDebugEnabled)
            {
                MoldLogger.LogDebug($"{LogTag} {evt} (properties: {properties?.Count ?? 0}) [SDK not installed]");
            }
#endif
        }

        public void SetUserProperty(string key, string value)
        {
            if (!IsEnabled) return;
#if HAS_FIREBASE_ANALYTICS
            if (!_initialized) return;
            FirebaseAnalytics.SetUserProperty(key, value);
#else
            MoldLogger.LogDebug($"{LogTag} UserProp {key}={value} [SDK not installed]");
#endif
        }

        public void Flush()
        {
            if (!IsEnabled) return;
#if HAS_FIREBASE_ANALYTICS
            FirebaseAnalytics.LogEvent("app_flush", null);
#endif
        }

#if HAS_FIREBASE_ANALYTICS
        private static string MapEventName(AnalyticsEvent evt) => evt switch
        {
            AnalyticsEvent.SessionStart => "session_start",
            AnalyticsEvent.SessionEnd => "session_end",
            AnalyticsEvent.LevelStarted => "level_start",
            AnalyticsEvent.LevelCompleted => "level_complete",
            AnalyticsEvent.LevelFailed => "level_fail",
            AnalyticsEvent.LevelAbandoned => "level_abandon",
            AnalyticsEvent.HintUsed => "hint_used",
            AnalyticsEvent.UndoUsed => "undo_used",
            AnalyticsEvent.CoinEarned => "coin_earned",
            AnalyticsEvent.CoinSpent => "coin_spent",
            AnalyticsEvent.AdWatched => "ad_watched",
            AnalyticsEvent.DailyChallengeStarted => "daily_start",
            AnalyticsEvent.DailyChallengeCompleted => "daily_complete",
            AnalyticsEvent.SettingsChanged => "settings_change",
            AnalyticsEvent.TutorialStarted => "tutorial_start",
            AnalyticsEvent.TutorialCompleted => "tutorial_complete",
            AnalyticsEvent.Crash => "crash",
            AnalyticsEvent.ErrorShown => "error_shown",
            _ => evt.ToString().ToLowerInvariant()
        };
#endif
    }
}
