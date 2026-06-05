using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Provides AI-generated art assets keyed by biome.
    /// Implementations read from a ScriptableObject catalog populated with Midjourney/DALL-E sprites.
    /// Returns null/Color.white for biomes without catalog entries (graceful fallback).
    /// </summary>
    public interface IBiomeArtProvider
    {
        Sprite GetCardBackground(Biome biome);
        Sprite GetMoldBackground(Biome biome);
        Sprite GetIcon(Biome biome);
        Color GetAccentColor(Biome biome);
    }
}
