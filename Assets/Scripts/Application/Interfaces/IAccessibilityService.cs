using System;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Supported color-blind modes.
    /// </summary>
    public enum ColorBlindMode
    {
        None = 0,
        Protanopia = 1,
        Deuteranopia = 2,
        Tritanopia = 3
    }

    /// <summary>
    /// Accessibility service that manages color-blind mode.
    /// When enabled, each OreColor is rendered with a distinct pattern overlay
    /// so players with color vision deficiencies can still distinguish ores.
    /// Setting persists across app restarts via PlayerPrefs.
    /// </summary>
    public interface IAccessibilityService
    {
        /// <summary>Whether any color-blind mode is currently active.</summary>
        bool ColorBlindModeEnabled { get; }

        /// <summary>The currently active color-blind mode.</summary>
        ColorBlindMode CurrentColorBlindMode { get; }

        /// <summary>Fired when color-blind mode is changed.</summary>
        event Action<ColorBlindMode> OnColorBlindModeChanged;

        /// <summary>
        /// Get the pattern for a given OreColor index (1-based).
        /// When <see cref="ColorBlindModeEnabled"/> is false, always returns <see cref="DomainPattern.None"/>.
        /// When enabled, returns a deterministic pattern based on the color index and the current mode.
        /// </summary>
        DomainPattern GetPatternForColor(int oreColorIndex);

        /// <summary>Set the active color-blind mode. Persists immediately.</summary>
        void SetColorBlindMode(ColorBlindMode mode);
    }
}
