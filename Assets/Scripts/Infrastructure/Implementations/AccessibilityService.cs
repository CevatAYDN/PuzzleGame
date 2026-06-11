using System;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// PlayerPrefs-backed accessibility service.
    /// Persists color-blind mode toggle and provides pattern overlay lookup
    /// for each OreColor when color-blind mode is active.
    /// </summary>
    public sealed class AccessibilityService : IAccessibilityService
    {
        private const string LogTag = "[Accessibility]";
        private const string PrefKey = "PuzzleGame.Accessibility.ColorBlindMode";

        private readonly AccessibilityConfig _config;
        private bool _colorBlindMode;

        public bool ColorBlindModeEnabled => _colorBlindMode;

        public event Action<bool> OnColorBlindModeChanged;

        public AccessibilityService(AccessibilityConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _colorBlindMode = PlayerPrefs.GetInt(PrefKey, _config.colorBlindModeDefault ? 1 : 0) == 1;
            MoldLogger.LogInfo($"{LogTag} Initialized (colorBlindMode={_colorBlindMode}).");
        }

        public PatternId GetPatternForColor(int oreColorIndex)
        {
            if (!_colorBlindMode)
                return PatternId.None;

            return _config.GetPattern(oreColorIndex);
        }

        public void SetColorBlindMode(bool enabled)
        {
            if (_colorBlindMode == enabled) return;

            _colorBlindMode = enabled;
            PlayerPrefs.SetInt(PrefKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            MoldLogger.LogInfo($"{LogTag} Color-blind mode set to {enabled}.");
            OnColorBlindModeChanged?.Invoke(enabled);
        }

        public void ToggleColorBlindMode()
        {
            SetColorBlindMode(!_colorBlindMode);
        }
    }
}
