using System;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Pattern overlay identifiers for color-blind mode.
    /// Each pattern is a visual texture drawn on top of the ore layer
    /// so that colors can be distinguished even without perceiving hue.
    /// </summary>
    public enum PatternId
    {
        None = 0,
        Dots = 1,
        Stripes = 2,
        Crosshatch = 3,
        Wavy = 4,
        Zigzag = 5,
        Plaid = 6,
        Rings = 7,
        Diagonal = 8,
        Grid = 9,
        Chevron = 10,
    }

    /// <summary>
    /// Accessibility service that manages color-blind mode.
    /// When enabled, each OreColor is rendered with a distinct pattern overlay
    /// so players with color vision deficiencies can still distinguish ores.
    /// Setting persists across app restarts via PlayerPrefs.
    /// </summary>
    public interface IAccessibilityService
    {
        /// <summary>Whether color-blind mode is currently active.</summary>
        bool ColorBlindModeEnabled { get; }

        /// <summary>Fired when color-blind mode is toggled.</summary>
        event Action<bool> OnColorBlindModeChanged;

        /// <summary>
        /// Get the pattern ID for a given OreColor index (1-based).
        /// When <see cref="ColorBlindModeEnabled"/> is false, always returns <see cref="PatternId.None"/>.
        /// When enabled, returns a deterministic pattern based on the color index.
        /// </summary>
        PatternId GetPatternForColor(int oreColorIndex);

        /// <summary>Enable or disable color-blind mode. Persists immediately.</summary>
        void SetColorBlindMode(bool enabled);

        /// <summary>Toggle color-blind mode.</summary>
        void ToggleColorBlindMode();
    }
}
