using System;
using PuzzleGame.Domain;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Sıvı katmanı — DomainColor (render) + OreColor (logic) + miktar.
    /// Reaction sistemi için kesin renk tipi gerektiğinden OreColor enum saklanır.
    /// 
    /// Layer.IsEmpty: considered empty if color is transparent, amount is below epsilon, or color type is None.
    /// </summary>
    public readonly struct OreLayer
    {
        public DomainColor Color { get; }
        public OreColor ColorType { get; }
        public float Amount { get; }
        public bool IsHidden { get; }
        public LayerModifier Modifier { get; }

        public bool IsEmpty =>
            Color.IsTransparent ||
            ColorType == OreColor.None ||
            Amount <= ForgeConstants.LayerAmountEpsilon;

        public bool IsFrozen => Modifier == LayerModifier.Frozen;

        public OreLayer(DomainColor color, float amount) : this(color, amount, OreColor.None, false, LayerModifier.None)
        {
            ColorType = DetectColorType(color);
        }

        public OreLayer(DomainColor color, float amount, OreColor colorType) : this(color, amount, colorType, false, LayerModifier.None)
        {
        }

        public OreLayer(DomainColor color, float amount, OreColor colorType, bool isHidden) : this(color, amount, colorType, isHidden, LayerModifier.None)
        {
        }

        public OreLayer(DomainColor color, float amount, OreColor colorType, bool isHidden, LayerModifier modifier)
        {
            Color = color;
            Amount = Math.Max(0f, amount);
            ColorType = colorType;
            IsHidden = isHidden;
            Modifier = modifier;
        }

        private static OreColor DetectColorType(DomainColor domainColor)
        {
            return OreColorExtensions.FromDomainColor(domainColor);
        }

        public OreLayer WithColor(DomainColor newColor) => new OreLayer(newColor, Amount, ColorType, IsHidden, Modifier);
        public OreLayer WithColor(DomainColor newColor, OreColor newColorType) => new OreLayer(newColor, Amount, newColorType, IsHidden, Modifier);
        public OreLayer WithAmount(float newAmount) => new OreLayer(Color, newAmount, ColorType, IsHidden, Modifier);
        public OreLayer WithHidden(bool hidden) => new OreLayer(Color, Amount, ColorType, hidden, Modifier);
        public OreLayer WithColorType(OreColor colorType) => new OreLayer(Color, Amount, colorType, IsHidden, Modifier);
        public OreLayer WithModifier(LayerModifier modifier) => new OreLayer(Color, Amount, ColorType, IsHidden, modifier);

        public override string ToString() => $"OreLayer(color={Color}, type={ColorType}, amount={Amount:F3}, modifier={Modifier})";
    }
}
