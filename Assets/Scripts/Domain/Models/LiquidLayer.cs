using System;

namespace BottleShaders.Domain.Models
{
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

        public LiquidLayer(UnityEngine.Color color, float amount)
            : this(DomainColor.FromUnityColor(color), amount) { }

        public LiquidLayer WithColor(DomainColor newColor) => new LiquidLayer(newColor, Amount);
        public LiquidLayer WithColor(UnityEngine.Color newColor) => new LiquidLayer(DomainColor.FromUnityColor(newColor), Amount);

        public LiquidLayer WithAmount(float newAmount) => new LiquidLayer(Color, newAmount);

        public override string ToString() => $"LiquidLayer(color={Color}, amount={Amount:F3})";
    }
}