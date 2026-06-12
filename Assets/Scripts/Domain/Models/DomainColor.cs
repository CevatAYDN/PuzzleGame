using System;
using PuzzleGame.Domain;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Pure C# renk temsilidir — Unity oyun motoruna bağımlılığı yoktur.
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

        public bool IsTransparent => A <= ForgeConstants.TransparencyAlphaEpsilon;

        public bool Matches(DomainColor other, float tolerance) =>
            Math.Abs(R - other.R) < tolerance &&
            Math.Abs(G - other.G) < tolerance &&
            Math.Abs(B - other.B) < tolerance &&
            Math.Abs(A - other.A) < tolerance;

        public override string ToString() => $"DomainColor(r={R:F3}, g={G:F3}, b={B:F3}, a={A:F3})";

        public bool Equals(DomainColor other) =>
            RoundedHash(R) == RoundedHash(other.R) &&
            RoundedHash(G) == RoundedHash(other.G) &&
            RoundedHash(B) == RoundedHash(other.B) &&
            RoundedHash(A) == RoundedHash(other.A);

        public override bool Equals(object obj) => obj is DomainColor other && Equals(other);

        public override int GetHashCode()
        {
            // Fix #16: Float GetHashCode is unstable when used with tolerance-based Equals().
            // Two DomainColors that compare equal via Equals() (within epsilon) could produce
            // different hash codes if their raw float bits differ, breaking Dictionary/HashSet invariants.
            //
            // Solution: round each channel to an epsilon-sized bucket before hashing.
            // This guarantees: if Equals(a, b) == true -> a.GetHashCode() == b.GetHashCode().
            return HashCode.Combine(
                RoundedHash(R),
                RoundedHash(G),
                RoundedHash(B),
                RoundedHash(A));
        }

        /// <summary>
        /// Rounds a float to the nearest epsilon bucket for hash-stable comparison.
        /// </summary>
        private static int RoundedHash(float v)
        {
            // Divide by epsilon, round to nearest int - values within epsilon map to the same bucket.
            int bucket = (int)Math.Round(v / ForgeConstants.DomainColorHashEpsilon);
            return bucket;
        }

        public static bool operator ==(DomainColor left, DomainColor right) => left.Equals(right);
        public static bool operator !=(DomainColor left, DomainColor right) => !left.Equals(right);
    }
}
