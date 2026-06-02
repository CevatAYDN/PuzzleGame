using System;
using PuzzleGame.Domain;
using UnityEngine;

namespace PuzzleGame.Domain.Models
{
    /// <summary>
    /// Pure C# renk temsilidir — UnityEngine bağımlılığı yoktur.
    /// UnityColor dönüşümü için <c>ColorAdapter</c> (Infrastructure katmanında) kullanılır.
    /// Implicit Color ↔ DomainColor dönüşümleri Unity ile sıkı bağı yaratır
    /// (tüm testler UnityEngine'e bağımlı olur); yine de geriye dönük uyumluluk için
    /// korunuyor. Yeni kod ColorAdapter.ToUnity / FromUnity kullanmalı.
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
