using System;
using PuzzleGame.Domain;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Standardized Ore colors for the game.
    /// Each enum value maps to a specific color in LevelConfig palette.
    /// </summary>
    public enum OreColor
    {
        None = 0,
        Red = 1,
        Blue = 2,
        Green = 3,
        Yellow = 4,
        Orange = 5,
        Purple = 6,
        Cyan = 7,
        Pink = 8,
        Brown = 9,
        White = 10,
        Black = 11,

        // Special colors for reactions
        Custom1 = 101,
        Custom2 = 102,
        Custom3 = 103,
        Custom4 = 104
    }

    /// <summary>
    /// Pure Domain extension methods for OreColor ↔ DomainColor conversion.
    /// Unity Color conversions live in <c>ColorAdapter</c> (Infrastructure)
    /// — chain <c>OreColor.ToDefaultDomainColor()</c> + <c>ColorAdapter.ToUnityStatic()</c>
    /// when a Unity Color is needed from a OreColor.
    /// </summary>
    public static class OreColorExtensions
    {
        /// <summary>
        /// Get default DomainColor for this OreColor enum value.
        /// </summary>
        public static DomainColor ToDefaultDomainColor(this OreColor color)
        {
            return color switch
            {
                OreColor.Red    => new DomainColor(0.9f,  0.2f,  0.2f,  1f),
                OreColor.Blue   => new DomainColor(0.2f,  0.6f,  0.9f,  1f),
                OreColor.Green  => new DomainColor(0.2f,  0.8f,  0.2f,  1f),
                OreColor.Yellow => new DomainColor(0.95f, 0.9f,  0.2f,  1f),
                OreColor.Orange => new DomainColor(0.9f,  0.5f,  0.2f,  1f),
                OreColor.Purple => new DomainColor(0.7f,  0.2f,  0.9f,  1f),
                OreColor.Cyan   => new DomainColor(0.2f,  0.9f,  0.9f,  1f),
                OreColor.Pink   => new DomainColor(0.9f,  0.4f,  0.7f,  1f),
                OreColor.Brown  => new DomainColor(0.6f,  0.4f,  0.2f,  1f),
                OreColor.White  => new DomainColor(0.95f, 0.95f, 0.95f, 1f),
                OreColor.Black  => new DomainColor(0.2f,  0.2f,  0.2f,  1f),
                _ => new DomainColor(1f, 0f, 1f, 1f) // Error/unknown — magenta
            };
        }

        /// <summary>
        /// Convert DomainColor to closest OreColor enum.
        /// </summary>
        public static OreColor FromDomainColor(DomainColor color, float tolerance = ForgeConstants.OreColorMatchEpsilon)
        {
            for (int i = 1; i <= (int)OreColor.Black; i++)
            {
                var lc = (OreColor)i;
                if (DomainColorMatch(color, lc.ToDefaultDomainColor(), tolerance))
                    return lc;
            }
            return OreColor.None;
        }

        /// <summary>
        /// Compare two DomainColors with per-channel tolerance.
        /// </summary>
        public static bool DomainColorMatch(DomainColor a, DomainColor b, float tolerance = ForgeConstants.OreColorMatchEpsilon)
        {
            return Math.Abs(a.R - b.R) < tolerance &&
                   Math.Abs(a.G - b.G) < tolerance &&
                   Math.Abs(a.B - b.B) < tolerance &&
                   Math.Abs(a.A - b.A) < tolerance;
        }

        /// <summary>
        /// Get default DomainPattern for this OreColor enum value.
        /// </summary>
        public static DomainPattern GetDefaultPattern(this OreColor color)
        {
            return color switch
            {
                OreColor.Red    => DomainPattern.Solid,
                OreColor.Blue   => DomainPattern.Waves,
                OreColor.Green  => DomainPattern.Dots,
                OreColor.Yellow => DomainPattern.Stripes,
                OreColor.Orange => DomainPattern.Checkered,
                OreColor.Purple => DomainPattern.Crosshatch,
                OreColor.Cyan   => DomainPattern.Diamonds,
                OreColor.Pink   => DomainPattern.Stars,
                OreColor.Brown  => DomainPattern.Zigzag,
                OreColor.White  => DomainPattern.Rings,
                OreColor.Black  => DomainPattern.Triangles,
                _ => DomainPattern.None
            };
        }
    }
}
