using System;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Sıvı katmanı — DomainColor + miktar (normalize 0..1).
    /// UnityEngine.Color dönüşümü için ColorAdapter (Infrastructure) kullanılır.
    /// </summary>
    public readonly struct LiquidLayer
    {
        public DomainColor Color { get; }
        public float Amount { get; }

        public bool IsEmpty => Color.IsTransparent || Amount <= 0.001f;

        public LiquidLayer(DomainColor color, float amount)
        {
            Color = color;
            Amount = Math.Max(0f, amount);
        }

        public LiquidLayer WithColor(DomainColor newColor) => new LiquidLayer(newColor, Amount);
        public LiquidLayer WithAmount(float newAmount) => new LiquidLayer(Color, newAmount);

        public override string ToString() => $"LiquidLayer(color={Color}, amount={Amount:F3})";
    }
}
