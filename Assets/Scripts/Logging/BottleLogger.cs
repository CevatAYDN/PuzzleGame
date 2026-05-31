using UnityEngine;

namespace BottleShaders.Logging
{
    public enum LogType
    {
        Info,
        Warning,
        Error,
        Debug
    }

    public static class BottleLogger
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static bool enableDebugLogs = true;
        private static bool enableInfoLogs = true;
#else
        private static bool enableDebugLogs = false;
        private static bool enableInfoLogs = false;
#endif
        private static bool enableWarningLogs = true;
        private static bool enableErrorLogs = true;

        public static void Log(string message, LogType type = LogType.Info, Object context = null)
        {
            switch (type)
            {
                case LogType.Info:
                    if (enableInfoLogs)
                        Debug.Log($"[BottleGame - INFO] {message}", context);
                    break;
                case LogType.Warning:
                    if (enableWarningLogs)
                        Debug.LogWarning($"[BottleGame - WARNING] {message}", context);
                    break;
                case LogType.Error:
                    if (enableErrorLogs)
                        Debug.LogError($"[BottleGame - ERROR] {message}", context);
                    break;
                case LogType.Debug:
                    if (enableDebugLogs)
                        Debug.Log($"[BottleGame - DEBUG] {message}", context);
                    break;
            }
        }

        public static void LogInfo(string message, Object context = null)
        {
            if (enableInfoLogs)
                Debug.Log($"[BottleGame - INFO] {message}", context);
        }

        public static void LogWarning(string message, Object context = null)
        {
            if (enableWarningLogs)
                Debug.LogWarning($"[BottleGame - WARNING] {message}", context);
        }

        public static void LogError(string message, Object context = null)
        {
            if (enableErrorLogs)
                Debug.LogError($"[BottleGame - ERROR] {message}", context);
        }

        public static void LogDebug(string message, Object context = null)
        {
            if (enableDebugLogs)
                Debug.Log($"[BottleGame - DEBUG] {message}", context);
        }

        public static void SetLogLevel(LogType type, bool enabled)
        {
            switch (type)
            {
                case LogType.Info:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    enableInfoLogs = enabled;
#endif
                    break;
                case LogType.Warning:
                    enableWarningLogs = enabled;
                    break;
                case LogType.Error:
                    enableErrorLogs = enabled;
                    break;
                case LogType.Debug:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    enableDebugLogs = enabled;
#endif
                    break;
            }
        }
    }
}