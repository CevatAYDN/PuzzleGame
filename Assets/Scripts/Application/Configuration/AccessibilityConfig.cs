using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame.Application.Configuration
{
    /// <summary>
    /// Configuration asset for accessibility features.
    /// Defines the pattern-to-color mapping used in color-blind mode.
    /// Create via Assets/Create/PuzzleGame/AccessibilityConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "AccessibilityConfig", menuName = "PuzzleGame/AccessibilityConfig")]
    public class AccessibilityConfig : ScriptableObject
    {
        [Header("Color-Blind Mode")]
        [Tooltip("Default state of color-blind mode on first launch.")]
        public bool colorBlindModeDefault = false;

        [Header("Pattern Mapping")]
        [Tooltip("Pattern assigned to each OreColor index (1=Red, 2=Blue, 3=Green, 4=Yellow, " +
                 "5=Orange, 6=Purple, 7=Cyan, 8=Pink, 9=Brown, 10=White, 11=Black). " +
                 "Index 0 (None) is unused. Values outside 0..11 are ignored.")]
        public PatternId[] patternByColorIndex = new PatternId[]
        {
            PatternId.None,     // 0 – unused
            PatternId.Dots,     // 1 – Red
            PatternId.Stripes,  // 2 – Blue
            PatternId.Crosshatch, // 3 – Green
            PatternId.Wavy,     // 4 – Yellow
            PatternId.Zigzag,   // 5 – Orange
            PatternId.Plaid,    // 6 – Purple
            PatternId.Rings,    // 7 – Cyan
            PatternId.Diagonal, // 8 – Pink
            PatternId.Grid,     // 9 – Brown
            PatternId.Chevron,  // 10 – White
            PatternId.None,     // 11 – Black (solid, no pattern needed)
        };

        /// <summary>
        /// Returns the pattern for a 1-based OreColor index.
        /// Falls back to <see cref="PatternId.None"/> for out-of-range.
        /// </summary>
        public PatternId GetPattern(int oreColorIndex)
        {
            if (oreColorIndex >= 0 && oreColorIndex < patternByColorIndex.Length)
                return patternByColorIndex[oreColorIndex];
            return PatternId.None;
        }
    }
}
