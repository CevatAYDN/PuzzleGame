using System;
using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame.Application.Logging
{
    /// <summary>
    /// Installs global Unity + AppDomain exception hooks and forwards them to the
    /// registered <see cref="ICrashReportingService"/>. The DI container assigns
    /// <see cref="Current"/> during composition; the hooks auto-install on first
    /// scene load and become active as soon as a service is registered. Safe to
    /// use without a service: hooks remain installed but no-op.
    /// </summary>
    public static class CrashReporter
    {
        public static ICrashReportingService Current { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallHooks()
        {
            UnityEngine.Application.logMessageReceivedThreaded += OnLogMessageReceived;
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            MoldLogger.LogInfo("[CrashReporter] Global hooks installed. Forwarding activates once ICrashReportingService is registered.");
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            var service = Current;
            if (service == null || !service.IsEnabled) return;
            try
            {
                service.LogMessage(condition, stackTrace);
            }
            catch (Exception ex)
            {
                MoldLogger.LogError($"[CrashReporter] Forwarding Unity log failed: {ex.Message}");
            }
        }

        private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var service = Current;
            if (service == null || !service.IsEnabled) return;
            if (e.ExceptionObject is Exception ex)
            {
                try
                {
                    service.LogException(ex);
                }
                catch (Exception inner)
                {
                    MoldLogger.LogError($"[CrashReporter] Forwarding unhandled exception failed: {inner.Message}");
                }
            }
        }
    }
}
