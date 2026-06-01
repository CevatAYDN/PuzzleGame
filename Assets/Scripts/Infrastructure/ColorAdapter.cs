using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Infrastructure
{
    /// <summary>
    /// DomainColor ↔ UnityEngine.Color dönüşüm adaptörü.
    /// Domain katmanının UnityEngine'e bağımlı olmamasını sağlar.
    /// </summary>
    public static class ColorAdapter
    {
        public static DomainColor FromUnity(Color color) =>
            new DomainColor(color.r, color.g, color.b, color.a);

        public static Color ToUnity(DomainColor color) =>
            new Color(color.R, color.G, color.B, color.A);
    }
}
