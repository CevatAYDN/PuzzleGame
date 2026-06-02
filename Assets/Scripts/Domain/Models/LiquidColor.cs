using System.Collections.Generic;
using UnityEngine;

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
    /// Extension methods for LiquidColor enum.
    /// </summary>
    public static class LiquidColorExtensions
    {
        /// <summary>
        /// Get Unity Color from LiquidColor using LevelConfig palette.
        /// </summary>
        public static Color ToUnityColor(this LiquidColor color, Configuration.LevelConfig levelConfig)
        {
            if (color == LiquidColor.None)
                return new Color(0, 0, 0, 0);
            
            int index = (int)color - 1;
            if (levelConfig?.palette != null && index >= 0 && index < levelConfig.palette.Length)
                return levelConfig.palette[index];
            
            // Fallback to default colors
            return color.ToDefaultColor();
        }
        
        /// <summary>
        /// Get default Unity Color without LevelConfig.
        /// </summary>
        public static Color ToDefaultColor(this LiquidColor color)
        {
            return color switch
            {
                LiquidColor.Red => new Color(0.9f, 0.2f, 0.2f, 1f),
                LiquidColor.Blue => new Color(0.2f, 0.6f, 0.9f, 1f),
                LiquidColor.Green => new Color(0.2f, 0.8f, 0.2f, 1f),
                LiquidColor.Yellow => new Color(0.95f, 0.9f, 0.2f, 1f),
                LiquidColor.Orange => new Color(0.9f, 0.5f, 0.2f, 1f),
                LiquidColor.Purple => new Color(0.7f, 0.2f, 0.9f, 1f),
                LiquidColor.Cyan => new Color(0.2f, 0.9f, 0.9f, 1f),
                LiquidColor.Pink => new Color(0.9f, 0.4f, 0.7f, 1f),
                LiquidColor.Brown => new Color(0.6f, 0.4f, 0.2f, 1f),
                LiquidColor.White => new Color(0.95f, 0.95f, 0.95f, 1f),
                LiquidColor.Black => new Color(0.2f, 0.2f, 0.2f, 1f),
                _ => Color.magenta // Error/unknown
            };
        }
        
        /// <summary>
        /// Convert Unity Color to closest LiquidColor enum.
        /// </summary>
        public static LiquidColor FromUnityColor(Color color, float tolerance = 0.1f)
        {
            // Check all standard colors
            for (int i = 1; i <= 11; i++)
            {
                var lc = (LiquidColor)i;
                var defaultColor = lc.ToDefaultColor();
                
                if (ColorMatch(color, defaultColor, tolerance))
                    return lc;
            }
            
            return LiquidColor.None;
        }
        
        /// <summary>
        /// Compare two colors with tolerance.
        /// </summary>
        public static bool ColorMatch(Color a, Color b, float tolerance = 0.1f)
        {
            return Mathf.Abs(a.r - b.r) < tolerance &&
                   Mathf.Abs(a.g - b.g) < tolerance &&
                   Mathf.Abs(a.b - b.b) < tolerance &&
                   Mathf.Abs(a.a - b.a) < tolerance;
        }
    }

    /// <summary>
    /// Extension methods for Unity Color to convert to LiquidColor.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Convert Unity Color to LiquidColor enum.
        /// </summary>
        public static LiquidColor ToLiquidColor(this Color color, float tolerance = 0.1f)
        {
            return LiquidColorExtensions.FromUnityColor(color, tolerance);
        }
    }
}
