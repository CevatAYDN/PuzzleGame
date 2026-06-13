using System;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// PlayerPrefs-backed accessibility service.
    /// Persists color-blind mode and provides pattern overlay lookup
    /// for each OreColor when color-blind mode is active.
    /// </summary>
    public sealed class AccessibilityService : IAccessibilityService
    {
        private const string LogTag = "[Accessibility]";
        private const string PrefKey = "PuzzleGame.Accessibility.ColorBlindMode";

        private readonly AccessibilityConfig _config;
        private ColorBlindMode _currentMode;

        public bool ColorBlindModeEnabled => _currentMode != ColorBlindMode.None;
        public ColorBlindMode CurrentColorBlindMode => _currentMode;

        public event Action<ColorBlindMode> OnColorBlindModeChanged;

        public AccessibilityService(AccessibilityConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            int savedMode = PlayerPrefs.GetInt(PrefKey, 0);
            _currentMode = (ColorBlindMode)savedMode;
            MoldLogger.LogInfo($"{LogTag} Initialized (mode={_currentMode}).");
        }

        public DomainPattern GetPatternForColor(int oreColorIndex)
        {
            if (!ColorBlindModeEnabled)
                return DomainPattern.None;

            var pattern = _config.GetPattern(oreColorIndex);
            
            // Fail-Fast: Throw exception if a required color (1..10) is missing an accessibility pattern fallback
            if (oreColorIndex >= 1 && oreColorIndex != 11 && pattern == DomainPattern.None)
            {
                throw new PuzzleGame.Domain.Exceptions.MissingAccessibilityFallbackException(
                    $"No accessibility pattern fallback defined for color index {oreColorIndex} when color-blind mode is enabled.");
            }
            
            return pattern;
        }

        public void SetColorBlindMode(ColorBlindMode mode)
        {
            if (_currentMode == mode) return;

            _currentMode = mode;
            PlayerPrefs.SetInt(PrefKey, (int)mode);
            PlayerPrefs.Save();
            MoldLogger.LogInfo($"{LogTag} Color-blind mode set to {mode}.");
            OnColorBlindModeChanged?.Invoke(mode);
        }
    }
}
