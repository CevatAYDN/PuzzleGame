using System;
using UnityEngine;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Pure C# renk temsilidir — UnityEngine bağımlılığı yoktur.
    /// UnityColor dönüşümü için <c>ColorAdapter</c> (Infrastructure katmanında) kullanılır.
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

        public bool IsTransparent => A <= 0.01f;

        public override string ToString() => $"DomainColor(r={R:F3}, g={G:F3}, b={B:F3}, a={A:F3})";

        public bool Equals(DomainColor other) =>
            Math.Abs(R - other.R) < 0.001f &&
            Math.Abs(G - other.G) < 0.001f &&
            Math.Abs(B - other.B) < 0.001f &&
            Math.Abs(A - other.A) < 0.001f;

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
        
        /// <summary>
        /// Implicit conversion from Unity Color to DomainColor.
        /// </summary>
        public static implicit operator DomainColor(Color color) => 
            new DomainColor(color.r, color.g, color.b, color.a);
        
        /// <summary>
        /// Implicit conversion from DomainColor to Unity Color.
        /// </summary>
        public static implicit operator Color(DomainColor domainColor) => 
            new Color(domainColor.R, domainColor.G, domainColor.B, domainColor.A);
    }
}
