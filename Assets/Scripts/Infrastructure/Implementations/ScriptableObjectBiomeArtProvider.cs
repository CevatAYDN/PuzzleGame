using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Infrastructure
{
    /// <summary>
    /// Reads biome art from a BiomeArtCatalog ScriptableObject.
    /// Catalog is optional — when null or empty, returns defaults (null sprite / white color).
    /// Allows soft-launch without AI art: ship with empty catalog, populate later via hot-swap.
    /// </summary>
    public class ScriptableObjectBiomeArtProvider : IBiomeArtProvider
    {
        private readonly BiomeArtCatalog _catalog;

        public ScriptableObjectBiomeArtProvider(BiomeArtCatalog catalog = null)
        {
            _catalog = catalog;
        }

        public Sprite GetCardBackground(Biome biome) =>
            _catalog?.GetEntry(biome)?.cardBackground;

        public Sprite GetMoldBackground(Biome biome) =>
            _catalog?.GetEntry(biome)?.moldBackground;

        public Sprite GetIcon(Biome biome) =>
            _catalog?.GetEntry(biome)?.icon;

        public Color GetAccentColor(Biome biome)
        {
            var entry = _catalog?.GetEntry(biome);
            return entry != null ? entry.accentColor : Color.white;
        }
    }
}
