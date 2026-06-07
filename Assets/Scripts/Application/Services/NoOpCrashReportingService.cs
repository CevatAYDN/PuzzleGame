using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Default no-op crash reporting sink. Replace via DI with Firebase Crashlytics
    /// or Sentry adapter. All public methods are safe to call in editor or production
    /// and never throw.
    /// </summary>
    public sealed class NoOpCrashReportingService : ICrashReportingService
    {
        private const string LogTag = "[CrashReporting]";

        public bool IsEnabled { get; set; } = true;

        public void LogException(Exception exception)
        {
            if (!IsEnabled || exception == null) return;
            MoldLogger.LogError($"{LogTag} {exception.GetType().Name}: {exception.Message}");
        }

        public void LogMessage(string message, string stackTrace = null)
        {
            if (!IsEnabled || string.IsNullOrEmpty(message)) return;
            MoldLogger.LogError($"{LogTag} {message}");
        }

        public void SetUserId(string userId)
        {
            if (!IsEnabled) return;
            MoldLogger.LogDebug($"{LogTag} UserId={userId}");
        }

        public void SetCustomKey(string key, string value)
        {
            if (!IsEnabled || string.IsNullOrEmpty(key)) return;
            MoldLogger.LogDebug($"{LogTag} Key {key}={value}");
        }

        public void Flush()
        {
        }
    }
}
