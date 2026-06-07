using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// In-memory feature flag service with fallback values.
    /// Implements IFeatureFlagService with offline-safe defaults.
    /// </summary>
    public class FeatureFlagService : IFeatureFlagService
    {
        private readonly Dictionary<string, object> _flags = new Dictionary<string, object>
        {
            // Default fallback values
            { "enable_punch_animation", false },
            { "onboarding_flow_type", "classic" }
        };
        
        private bool _isOnline = false;
        private bool _hasFetchedRemote = false;
        
        public void SetOnlineStatus(bool isOnline)
        {
            _isOnline = isOnline;
            if (_isOnline && !_hasFetchedRemote)
            {
                // Simulate fetching from remote config
                // In production, this would be replaced with actual remote config fetch
                _flags.Clear();
                _flags.Add("enable_punch_animation", false);
                _flags.Add("onboarding_flow_type", "classic");
                _hasFetchedRemote = true;
            }
        }

        public bool GetBool(string key, bool defaultValue)
        {
            if (_flags.TryGetValue(key, out var value) && value is bool boolValue)
            {
                return boolValue;
            }
            MoldLogger.LogDebug($"FeatureFlag: {key} not found, using default {defaultValue}");
            return defaultValue;
        }

        public string GetString(string key, string defaultValue)
        {
            if (_flags.TryGetValue(key, out var value) && value is string stringValue)
            {
                return stringValue;
            }
            MoldLogger.LogDebug($"FeatureFlag: {key} not found, using default {defaultValue}");
            return defaultValue;
        }

        public int GetInt(string key, int defaultValue)
        {
            if (_flags.TryGetValue(key, out var value) && value is int intValue)
            {
                return intValue;
            }
            MoldLogger.LogDebug($"FeatureFlag: {key} not found, using default {defaultValue}");
            return defaultValue;
        }
    }
}