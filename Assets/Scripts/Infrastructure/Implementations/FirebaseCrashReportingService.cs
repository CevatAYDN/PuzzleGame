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

        // Keys that are never forwarded to Crashlytics as raw values. Callers
        // can still log these for debugging, but the SDK stores a redacted token
        // so a leaked dashboard does not expose player PII.
        private static readonly System.Collections.Generic.HashSet<string> PiiKeyBlocklist =
            new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "email", "mail", "phone", "phoneNumber", "msisdn",
                "address", "playerName", "userName", "fullName",
                "deviceUniqueId", "deviceId", "ipAddress",
            };

        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Returns <paramref name="value"/> if <paramref name="key"/> is not on
        /// the PII blocklist, otherwise a redacted token. Lets callers stay
        /// careless about what they pass in <see cref="SetCustomKey"/> without
        /// leaking player PII to the crash backend.
        /// </summary>
        private static string SanitizeValue(string key, string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (key != null && PiiKeyBlocklist.Contains(key))
                return "[redacted]";
            // Scrub obvious email / phone patterns from arbitrary string values.
            if (System.Text.RegularExpressions.Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return "[redacted-email]";
            if (System.Text.RegularExpressions.Regex.IsMatch(value, @"^\+?\d{7,}$"))
                return "[redacted-phone]";
            return value;
        }

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
                // UserId is PII — never forward the raw value. Replace with a
                // short opaque token derived from the id so crash reports can
                // still be grouped per user without leaking the identifier.
                string safeId = HashToShortToken(userId);
                Crashlytics.SetUserId(safeId);
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

            string safe = SanitizeValue(key, value);

#if HAS_FIREBASE_CRASHLYTICS
            if (!_initialized) return;
            try
            {
                Crashlytics.SetCustomKey(key, safe ?? string.Empty);
            }
            catch (Exception e)
            {
                MoldLogger.LogError($"{LogTag} SetCustomKey failed: {e.Message}");
            }
#else
            if (MoldLogger.IsDebugEnabled)
            {
                MoldLogger.LogDebug($"{LogTag} Key {key}={safe} [SDK not installed]");
            }
#endif
        }

        /// <summary>
        /// Returns a 12-char hex token derived from <paramref name="input"/>. Used
        /// to scrub user identifiers before they reach the crash backend. Not a
        /// security boundary — it just keeps PII off the dashboard.
        /// </summary>
        private static string HashToShortToken(string input)
        {
            if (string.IsNullOrEmpty(input)) return "anon";
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                var sb = new System.Text.StringBuilder(12);
                for (int i = 0; i < 6; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
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
