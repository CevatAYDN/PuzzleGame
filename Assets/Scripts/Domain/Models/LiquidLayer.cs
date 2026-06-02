using System;
using UnityEngine;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Sıvı katmanı — DomainColor (render) + LiquidColor (logic) + miktar.
    /// Reaction sistemi için kesin renk tipi gerektiğinden LiquidColor enum saklanır.
    /// </summary>
    public readonly struct LiquidLayer
    {
        public DomainColor Color { get; }           // Unity render için
        public LiquidColor ColorType { get; }       // Reaction/logic için
        public float Amount { get; }

        public bool IsEmpty => Color.IsTransparent || Amount <= 0.001f || ColorType == LiquidColor.None;

        public LiquidLayer(DomainColor color, float amount) : this(color, amount, LiquidColor.None)
        {
            // Auto-detect color type from DomainColor
            ColorType = DetectColorType(color);
        }

        public LiquidLayer(DomainColor color, float amount, LiquidColor colorType)
        {
            Color = color;
            Amount = Math.Max(0f, amount);
            ColorType = colorType;
        }

        private static LiquidColor DetectColorType(DomainColor domainColor)
        {
            return ((Color)domainColor).ToLiquidColor();
        }

        public LiquidLayer WithColor(DomainColor newColor) => new LiquidLayer(newColor, Amount, ColorType);
        public LiquidLayer WithColor(DomainColor newColor, LiquidColor newColorType) => new LiquidLayer(newColor, Amount, newColorType);
        public LiquidLayer WithAmount(float newAmount) => new LiquidLayer(Color, newAmount, ColorType);
        public LiquidLayer WithColorType(LiquidColor colorType) => new LiquidLayer(Color, Amount, colorType);

        public override string ToString() => $"LiquidLayer(color={Color}, type={ColorType}, amount={Amount:F3})";
    }
}
