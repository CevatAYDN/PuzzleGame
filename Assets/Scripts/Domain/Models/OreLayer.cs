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
        public DomainPattern Pattern { get; }
        public float Amount { get; }
        public bool IsHidden { get; }
        public LayerModifier Modifier { get; }

        public bool IsEmpty =>
            Color.IsTransparent ||
            ColorType == OreColor.None ||
            Amount <= ForgeConstants.LayerAmountEpsilon;

        public bool IsFrozen => Modifier == LayerModifier.Frozen;

        public OreLayer(DomainColor color, float amount) : this(color, amount, OreColor.None, DomainPattern.None, false, LayerModifier.None)
        {
            ColorType = DetectColorType(color);
            Pattern = DetectPatternType(ColorType);
        }

        public OreLayer(DomainColor color, float amount, OreColor colorType) : this(color, amount, colorType, DomainPattern.None, false, LayerModifier.None)
        {
            Pattern = DetectPatternType(colorType);
        }

        public OreLayer(DomainColor color, float amount, OreColor colorType, DomainPattern pattern) : this(color, amount, colorType, pattern, false, LayerModifier.None)
        {
        }

        public OreLayer(DomainColor color, float amount, OreColor colorType, DomainPattern pattern, bool isHidden) : this(color, amount, colorType, pattern, isHidden, LayerModifier.None)
        {
        }

        public OreLayer(DomainColor color, float amount, OreColor colorType, DomainPattern pattern, bool isHidden, LayerModifier modifier)
        {
            Color = color;
            Amount = Math.Max(0f, amount);
            ColorType = colorType;
            Pattern = pattern;
            IsHidden = isHidden;
            Modifier = modifier;
        }

        private static OreColor DetectColorType(DomainColor domainColor)
        {
            return OreColorExtensions.FromDomainColor(domainColor);
        }

        private static DomainPattern DetectPatternType(OreColor colorType)
        {
            return OreColorExtensions.GetDefaultPattern(colorType);
        }

        public OreLayer WithColor(DomainColor newColor) => new OreLayer(newColor, Amount, ColorType, Pattern, IsHidden, Modifier);
        public OreLayer WithColor(DomainColor newColor, OreColor newColorType) => new OreLayer(newColor, Amount, newColorType, DetectPatternType(newColorType), IsHidden, Modifier);
        public OreLayer WithPattern(DomainPattern newPattern) => new OreLayer(Color, Amount, ColorType, newPattern, IsHidden, Modifier);
        public OreLayer WithAmount(float newAmount) => new OreLayer(Color, newAmount, ColorType, Pattern, IsHidden, Modifier);
        public OreLayer WithHidden(bool hidden) => new OreLayer(Color, Amount, ColorType, Pattern, hidden, Modifier);
        public OreLayer WithColorType(OreColor colorType) => new OreLayer(Color, Amount, colorType, DetectPatternType(colorType), IsHidden, Modifier);
        public OreLayer WithModifier(LayerModifier modifier) => new OreLayer(Color, Amount, ColorType, Pattern, IsHidden, modifier);

        public override string ToString() => $"OreLayer(color={Color}, type={ColorType}, pattern={Pattern}, amount={Amount:F3}, modifier={Modifier})";
    }
}
