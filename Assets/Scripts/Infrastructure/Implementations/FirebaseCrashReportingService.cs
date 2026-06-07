using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

#if HAS_FIREBASE_CRASHLYTICS
using Firebase;
using Firebase.Crashlytics;
#endif

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Firebase Crashlytics adapter implementing the ICrashReportingService contract.
    /// When com.google.firebase.crashlytics is not installed, methods are no-ops
    /// that still respect IsEnabled and debug logging so other systems can run
    /// in editor/CI without the package. Activate by adding the package to
    /// manifest.json — HAS_FIREBASE_CRASHLYTICS auto-defines via Infrastructure.asmdef.
    /// </summary>
    public class FirebaseCrashReportingService : ICrashReportingService
    {
        private const string LogTag = "[CrashReporting-Firebase]";

        public bool IsEnabled { get; set; } = true;

#if HAS_FIREBASE_CRASHLYTICS
        private bool _initialized;

        public FirebaseCrashReportingService()
        {
            try
            {
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
                {
                    _initialized = task.Result == DependencyStatus.Available;
                    if (_initialized)
                    {
                        Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                    }
                    MoldLogger.LogInfo($"{LogTag} Init. Available={_initialized}");
                });
            }
            catch (Exception e)
            {
                MoldLogger.LogError($"{LogTag} Init failed: {e.Message}");
            }
        }
#endif

        public void LogException(Exception exception)
        {
            if (!IsEnabled || exception == null) return;

#if HAS_FIREBASE_CRASHLYTICS
            if (!_initialized) return;
            try
            {
                Crashlytics.LogException(exception);
            }
            catch (Exception e)
            {
                MoldLogger.LogError($"{LogTag} LogException failed: {e.Message}");
            }
#else
            if (MoldLogger.IsDebugEnabled)
            {
                MoldLogger.LogDebug($"{LogTag} Exception {exception.GetType().Name} [SDK not installed]");
            }
#endif
        }

        public void LogMessage(string message, string stackTrace = null)
        {
            if (!IsEnabled || string.IsNullOrEmpty(message)) return;

#if HAS_FIREBASE_CRASHLYTICS
            if (!_initialized) return;
            try
            {
                Crashlytics.Log(message);
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    Crashlytics.Log(stackTrace);
                }
            }
            catch (Exception e)
            {
                MoldLogger.LogError($"{LogTag} LogMessage failed: {e.Message}");
            }
#else
            if (MoldLogger.IsDebugEnabled)
            {
                MoldLogger.LogDebug($"{LogTag} Message {message} [SDK not installed]");
            }
#endif
        }

        public void SetUserId(string userId)
        {
            if (!IsEnabled) return;

#if HAS_FIREBASE_CRASHLYTICS
            if (!_initialized) return;
            try
            {
                Crashlytics.SetUserId(userId ?? string.Empty);
            }
            catch (Exception e)
            {
                MoldLogger.LogError($"{LogTag} SetUserId failed: {e.Message}");
            }
#else
            if (MoldLogger.IsDebugEnabled)
            {
                MoldLogger.LogDebug($"{LogTag} UserId={userId} [SDK not installed]");
            }
#endif
        }

        public void SetCustomKey(string key, string value)
        {
            if (!IsEnabled || string.IsNullOrEmpty(key)) return;

#if HAS_FIREBASE_CRASHLYTICS
            if (!_initialized) return;
            try
            {
                Crashlytics.SetCustomKey(key, value ?? string.Empty);
            }
            catch (Exception e)
            {
                MoldLogger.LogError($"{LogTag} SetCustomKey failed: {e.Message}");
            }
#else
            if (MoldLogger.IsDebugEnabled)
            {
                MoldLogger.LogDebug($"{LogTag} Key {key}={value} [SDK not installed]");
            }
#endif
        }

        public void Flush()
        {
            if (!IsEnabled) return;

#if HAS_FIREBASE_CRASHLYTICS
            if (_initialized)
            {
                Crashlytics.Log($"{LogTag} Flush");
            }
#endif
        }
    }
}
