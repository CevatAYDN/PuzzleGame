using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Application.Configuration
{
    /// <summary>
    /// ScriptableObject catalog of biome art assets (AI-generated sprites, accent colors).
    /// Populated in the Editor by dragging in Midjourney/DALL-E outputs.
    /// Create via: right-click in Project → Create → PuzzleGame/Art/Biome Art Catalog.
    /// Single asset instance; assigned to GameInstaller's SerializeField.
    /// </summary>
    [CreateAssetMenu(fileName = "BiomeArtCatalog", menuName = "PuzzleGame/Art/Biome Art Catalog")]
    public class BiomeArtCatalog : ScriptableObject
    {
        [System.Serializable]
        public class BiomeArtEntry
        {
            public Biome biome;
            public Sprite cardBackground;
            public Sprite moldBackground;
            public Sprite icon;
            public Color accentColor = Color.white;
        }

        [SerializeField] private BiomeArtEntry[] entries = new BiomeArtEntry[0];

        public BiomeArtEntry GetEntry(Biome biome)
        {
            if (entries == null) return null;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].biome == biome)
                    return entries[i];
            }
            return null;
        }
    }
}
