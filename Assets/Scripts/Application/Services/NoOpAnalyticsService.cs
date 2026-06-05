using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Default no-op analytics sink. Replace via DI with a Firebase / Amplitude / Unity Analytics adapter
    /// to wire telemetry. All public methods are safe to call in editor or production.
    /// </summary>
    public sealed class NoOpAnalyticsService : IAnalyticsService
    {
        private const string LogTag = "[Analytics]";

        public bool IsEnabled { get; set; } = true;

        public void Track(AnalyticsEvent evt, IReadOnlyDictionary<string, object> properties = null)
        {
            if (!IsEnabled) return;
            if (!MoldLogger.IsDebugEnabled) return;
            MoldLogger.LogDebug($"{LogTag} {evt} (properties: {properties?.Count ?? 0})");
        }

        public void SetUserProperty(string key, string value)
        {
            if (!IsEnabled) return;
            MoldLogger.LogDebug($"{LogTag} UserProp {key}={value}");
        }

        public void Flush()
        {
            if (!IsEnabled) return;
        }
    }
}
