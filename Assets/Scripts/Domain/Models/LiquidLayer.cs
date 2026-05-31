using UnityEngine;

namespace BottleShaders.Domain.Models
{
    /// <summary>
    /// Immutable value object representing a single colored liquid layer inside a bottle.
    /// </summary>
    public readonly struct LiquidLayer
    {
        public Color Color  { get; }
        public float Amount { get; }

        public bool IsEmpty => Color.a <= 0.01f || Amount <= 0.001f;

        public LiquidLayer(Color color, float amount)
        {
            Color  = color;
            Amount = Mathf.Max(0f, amount);
        }

        /// <summary>Returns a new LiquidLayer with a different color, keeping the same amount.</summary>
        public LiquidLayer WithColor(Color newColor) => new LiquidLayer(newColor, Amount);

        /// <summary>Returns a new LiquidLayer with a different amount, keeping the same color.</summary>
        public LiquidLayer WithAmount(float newAmount) => new LiquidLayer(Color, newAmount);

        public override string ToString() => $"LiquidLayer(color={Color}, amount={Amount:F3})";
    }
}
