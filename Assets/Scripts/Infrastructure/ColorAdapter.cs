using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame.Infrastructure
{
    /// <summary>
    /// DomainColor ↔ UnityEngine.Color dönüşüm adaptörü.
    /// Domain katmanının UnityEngine'e bağımlı olmamasını sağlar.
    /// IColorAdapter interface'ini uygular — Application katmanı bu soyutlama üzerinden kullanır.
    /// </summary>
    public class ColorAdapter : IColorAdapter
    {
        /// <summary>Static helper for contexts where DI is unavailable (e.g. MonoBehaviour serialization).</summary>
        public static DomainColor FromUnityStatic(Color color) =>
            new DomainColor(color.r, color.g, color.b, color.a);

        /// <summary>Static helper for contexts where DI is unavailable (e.g. MonoBehaviour serialization).</summary>
        public static Color ToUnityStatic(DomainColor color) =>
            new Color(color.R, color.G, color.B, color.A);

        // IColorAdapter instance methods — used via DI
        public DomainColor FromUnity(Color color) => FromUnityStatic(color);
        public Color ToUnity(DomainColor color) => ToUnityStatic(color);
    }
}
