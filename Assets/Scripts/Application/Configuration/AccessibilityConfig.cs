using PuzzleGame.Domain.Models;
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
        [Header("Pattern Mapping")]
        [Tooltip("Pattern assigned to each OreColor index (1=Red, 2=Blue, 3=Green, 4=Yellow, " +
                 "5=Orange, 6=Purple, 7=Cyan, 8=Pink, 9=Brown, 10=White, 11=Black). " +
                 "Index 0 (None) is unused. Values outside 0..11 are ignored.")]
        public DomainPattern[] patternByColorIndex = new DomainPattern[]
        {
            DomainPattern.None,       // 0 – unused
            DomainPattern.Dots,       // 1 – Red
            DomainPattern.Stripes,    // 2 – Blue
            DomainPattern.Crosshatch, // 3 – Green
            DomainPattern.Waves,      // 4 – Yellow
            DomainPattern.Zigzag,     // 5 – Orange
            DomainPattern.Checkered,  // 6 – Purple
            DomainPattern.Rings,      // 7 – Cyan
            DomainPattern.Diamonds,   // 8 – Pink
            DomainPattern.Checkered,  // 9 – Brown
            DomainPattern.Stars,      // 10 – White
            DomainPattern.None,       // 11 – Black (solid, no pattern needed)
        };

        /// <summary>
        /// Returns the pattern for a 1-based OreColor index.
        /// Falls back to <see cref="DomainPattern.None"/> for out-of-range.
        /// </summary>
        public DomainPattern GetPattern(int oreColorIndex)
        {
            if (oreColorIndex >= 0 && oreColorIndex < patternByColorIndex.Length)
                return patternByColorIndex[oreColorIndex];
            return DomainPattern.None;
        }
    }
}
