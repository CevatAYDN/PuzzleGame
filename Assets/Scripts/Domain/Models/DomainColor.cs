using System;
using PuzzleGame.Domain;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Pure C# renk temsilidir — UnityEngine bağımlılığı yoktur.
    /// <c>Color</c> ↔ <c>DomainColor</c> dönüşümü için
    /// <c>ColorAdapter</c> (Infrastructure katmanında) kullanılmalıdır.
    /// Bu tür Domain katmanında kalır; Infrastructure katmanı Domain'e bağımlıdır
    /// (Dependency Inversion), tersi değil.
    /// </summary>
    public readonly struct DomainColor : IEquatable<DomainColor>
    {
        public float R { get; }
        public float G { get; }
        public float B { get; }
        public float A { get; }

        public DomainColor(float r, float g, float b, float a = 1f)
        {
            R = Math.Max(0f, r);
            G = Math.Max(0f, g);
            B = Math.Max(0f, b);
            A = Math.Max(0f, a);
        }

        public bool IsTransparent => A <= BottleConstants.TransparencyAlphaEpsilon;

        public override string ToString() => $"DomainColor(r={R:F3}, g={G:F3}, b={B:F3}, a={A:F3})";

        public bool Equals(DomainColor other) =>
            Math.Abs(R - other.R) < BottleConstants.DomainColorHashEpsilon &&
            Math.Abs(G - other.G) < BottleConstants.DomainColorHashEpsilon &&
            Math.Abs(B - other.B) < BottleConstants.DomainColorHashEpsilon &&
            Math.Abs(A - other.A) < BottleConstants.DomainColorHashEpsilon;

        public override bool Equals(object obj) => obj is DomainColor other && Equals(other);

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + R.GetHashCode();
            hash = hash * 23 + G.GetHashCode();
            hash = hash * 23 + B.GetHashCode();
            hash = hash * 23 + A.GetHashCode();
            return hash;
        }

        public static bool operator ==(DomainColor left, DomainColor right) => left.Equals(right);
        public static bool operator !=(DomainColor left, DomainColor right) => !left.Equals(right);
    }
}
