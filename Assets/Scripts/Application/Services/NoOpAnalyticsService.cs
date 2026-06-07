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
            if (properties == null || properties.Count == 0)
            {
                MoldLogger.LogDebug($"{LogTag} {evt}");
                return;
            }
            var sb = new System.Text.StringBuilder(128);
            sb.Append(LogTag).Append(' ').Append(evt).Append(" {");
            bool first = true;
            foreach (var kvp in properties)
            {
                if (!first) sb.Append(", ");
                sb.Append(kvp.Key).Append('=').Append(kvp.Value);
                first = false;
            }
            sb.Append('}');
            MoldLogger.LogDebug(sb.ToString());
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
