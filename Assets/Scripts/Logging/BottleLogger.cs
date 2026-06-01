using UnityEngine;

namespace PuzzleGame.Logging
{
    /// <summary>
    /// Thin wrapper around Unity's Debug logger.
    /// Uses a project-specific enum to avoid the name clash with UnityEngine.LogType.
    /// Verbose logs are stripped in release builds automatically.
    /// </summary>
    public static class BottleLogger
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

        // ── Public API ───────────────────────────────────────────────────────

        public static void LogInfo(string message, Object context = null)
        {
            if (_infoEnabled)
                Debug.Log($"[BottleGame | INFO] {message}", context);
        }

        public static void LogWarning(string message, Object context = null)
        {
            if (_warningEnabled)
                Debug.LogWarning($"[BottleGame | WARN] {message}", context);
        }

        public static void LogError(string message, Object context = null)
        {
            if (_errorEnabled)
                Debug.LogError($"[BottleGame | ERROR] {message}", context);
        }

        public static void LogDebug(string message, Object context = null)
        {
            if (_debugEnabled)
                Debug.Log($"[BottleGame | DEBUG] {message}", context);
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
