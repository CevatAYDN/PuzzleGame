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
        public DomainColor Color { get; }           // Unity render için
        public OreColor ColorType { get; }       // Reaction/logic için
        public float Amount { get; }

        public bool IsEmpty =>
            Color.IsTransparent ||
            ColorType == OreColor.None ||
            Amount <= ForgeConstants.LayerAmountEpsilon;

        public OreLayer(DomainColor color, float amount) : this(color, amount, OreColor.None)
        {
            // Auto-detect color type from DomainColor
            ColorType = DetectColorType(color);
        }

        public OreLayer(DomainColor color, float amount, OreColor colorType)
        {
            Color = color;
            Amount = Math.Max(0f, amount);
            ColorType = colorType;
        }

        private static OreColor DetectColorType(DomainColor domainColor)
        {
            return OreColorExtensions.FromDomainColor(domainColor);
        }

        public OreLayer WithColor(DomainColor newColor) => new OreLayer(newColor, Amount, ColorType);
        public OreLayer WithColor(DomainColor newColor, OreColor newColorType) => new OreLayer(newColor, Amount, newColorType);
        public OreLayer WithAmount(float newAmount) => new OreLayer(Color, newAmount, ColorType);
        public OreLayer WithColorType(OreColor colorType) => new OreLayer(Color, Amount, colorType);

        public override string ToString() => $"OreLayer(color={Color}, type={ColorType}, amount={Amount:F3})";
    }
}
