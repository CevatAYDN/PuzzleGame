using System;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Crash reporting contract. Implementations forward unhandled exceptions
    /// and Unity log errors to an external sink (Firebase Crashlytics, Sentry, etc.).
    /// All methods are best-effort and must never throw.
    /// </summary>
    public interface ICrashReportingService
    {
        bool IsEnabled { get; set; }

        /// <summary>Records a caught exception. Stack trace is read from <see cref="Exception"/>.</summary>
        void LogException(Exception exception);

        /// <summary>Records a Unity log error (no managed exception object available).</summary>
        void LogMessage(string message, string stackTrace = null);

        /// <summary>Sets the user identifier attached to subsequent reports (e.g. install ID).</summary>
        void SetUserId(string userId);

        /// <summary>Sets a custom key/value pair visible in the crash dashboard.</summary>
        void SetCustomKey(string key, string value);

        /// <summary>Forces any buffered reports to be uploaded.</summary>
        void Flush();
    }
}
