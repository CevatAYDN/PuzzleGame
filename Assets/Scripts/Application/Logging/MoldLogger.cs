using UnityEngine;

namespace PuzzleGame.Application.Logging
{
    /// <summary>
    /// Thin wrapper around Unity's Debug logger.
    /// Uses a project-specific enum to avoid the name clash with UnityEngine.LogType.
    /// Verbose logs are stripped in release builds automatically.
    /// </summary>
    public static class MoldLogger
    {
        public enum Level { Info, Warning, Error, Debug }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static bool _debugEnabled = true;
        private static bool _infoEnabled  = true;
#else
        private static bool _debugEnabled = false;
        private static bool _infoEnabled  = false;
#endif
        private static bool _warningEnabled = true;
        private static bool _errorEnabled   = true;

        public static bool IsInfoEnabled => _infoEnabled;
        public static bool IsWarningEnabled => _warningEnabled;
        public static bool IsErrorEnabled => _errorEnabled;
        public static bool IsDebugEnabled => _debugEnabled;

        // ── Public API ───────────────────────────────────────────────────────

        // ── String overloads (for plain string messages, no interpolation) ─────

        public static void LogInfo(string message, Object context = null)
        {
            if (_infoEnabled)
                Debug.Log($"[MoldGame | INFO] {message}", context);
        }

        public static void LogWarning(string message, Object context = null)
        {
            if (_warningEnabled)
                Debug.LogWarning($"[MoldGame | WARN] {message}", context);
        }

        public static void LogError(string message, Object context = null)
        {
            if (_errorEnabled)
                Debug.LogError($"[MoldGame | ERROR] {message}", context);
        }

        public static void LogDebug(string message, Object context = null)
        {
            if (_debugEnabled)
                Debug.Log($"[MoldGame | DEBUG] {message}", context);
        }

        // ── FormattableString overloads (C# compiler prefers these for $"" strings) ──
        // String interpolation is deferred until after the enabled check, preventing GC alloc
        // when the log level is disabled in release builds.

        public static void LogInfo(System.FormattableString message, Object context = null)
        {
            if (_infoEnabled)
                Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "[MoldGame | INFO] " + message.Format, message.GetArguments()), context);
        }

        public static void LogWarning(System.FormattableString message, Object context = null)
        {
            if (_warningEnabled)
                Debug.LogWarning(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "[MoldGame | WARN] " + message.Format, message.GetArguments()), context);
        }

        public static void LogError(System.FormattableString message, Object context = null)
        {
            if (_errorEnabled)
                Debug.LogError(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "[MoldGame | ERROR] " + message.Format, message.GetArguments()), context);
        }

        public static void LogDebug(System.FormattableString message, Object context = null)
        {
            if (_debugEnabled)
                Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "[MoldGame | DEBUG] " + message.Format, message.GetArguments()), context);
        }

        /// <summary>Runtime toggle — useful for in-game debug menus.</summary>
        public static void SetLevel(Level level, bool enabled)
        {
            switch (level)
            {
                case Level.Info:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    _infoEnabled = enabled;
#endif
                    break;
                case Level.Warning: _warningEnabled = enabled; break;
                case Level.Error:   _errorEnabled   = enabled; break;
                case Level.Debug:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    _debugEnabled = enabled;
#endif
                    break;
            }
        }
    }
}
