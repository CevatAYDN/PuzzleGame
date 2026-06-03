using System;
using PuzzleGame.Domain;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Standardized liquid colors for the game.
    /// Each enum value maps to a specific color in LevelConfig palette.
    /// </summary>
    public enum LiquidColor
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
    /// Pure Domain extension methods for LiquidColor ↔ DomainColor conversion.
    /// UnityEngine.Color conversions live in <c>ColorAdapter</c> (Infrastructure)
    /// — chain <c>LiquidColor.ToDefaultDomainColor()</c> + <c>ColorAdapter.ToUnity()</c>
    /// when a Unity Color is needed from a LiquidColor.
    /// </summary>
    public static class LiquidColorExtensions
    {
        /// <summary>
        /// Get default DomainColor for this LiquidColor enum value.
        /// </summary>
        public static DomainColor ToDefaultDomainColor(this LiquidColor color)
        {
            return color switch
            {
                LiquidColor.Red    => new DomainColor(0.9f,  0.2f,  0.2f,  1f),
                LiquidColor.Blue   => new DomainColor(0.2f,  0.6f,  0.9f,  1f),
                LiquidColor.Green  => new DomainColor(0.2f,  0.8f,  0.2f,  1f),
                LiquidColor.Yellow => new DomainColor(0.95f, 0.9f,  0.2f,  1f),
                LiquidColor.Orange => new DomainColor(0.9f,  0.5f,  0.2f,  1f),
                LiquidColor.Purple => new DomainColor(0.7f,  0.2f,  0.9f,  1f),
                LiquidColor.Cyan   => new DomainColor(0.2f,  0.9f,  0.9f,  1f),
                LiquidColor.Pink   => new DomainColor(0.9f,  0.4f,  0.7f,  1f),
                LiquidColor.Brown  => new DomainColor(0.6f,  0.4f,  0.2f,  1f),
                LiquidColor.White  => new DomainColor(0.95f, 0.95f, 0.95f, 1f),
                LiquidColor.Black  => new DomainColor(0.2f,  0.2f,  0.2f,  1f),
                _ => new DomainColor(1f, 0f, 1f, 1f) // Error/unknown — magenta
            };
        }

        /// <summary>
        /// Convert DomainColor to closest LiquidColor enum.
        /// </summary>
        public static LiquidColor FromDomainColor(DomainColor color, float tolerance = BottleConstants.LiquidColorMatchEpsilon)
        {
            for (int i = 1; i <= (int)LiquidColor.Black; i++)
            {
                var lc = (LiquidColor)i;
                if (DomainColorMatch(color, lc.ToDefaultDomainColor(), tolerance))
                    return lc;
            }
            return LiquidColor.None;
        }

        /// <summary>
        /// Compare two DomainColors with per-channel tolerance.
        /// </summary>
        public static bool DomainColorMatch(DomainColor a, DomainColor b, float tolerance = BottleConstants.LiquidColorMatchEpsilon)
        {
            return Math.Abs(a.R - b.R) < tolerance &&
                   Math.Abs(a.G - b.G) < tolerance &&
                   Math.Abs(a.B - b.B) < tolerance &&
                   Math.Abs(a.A - b.A) < tolerance;
        }
    }
}
