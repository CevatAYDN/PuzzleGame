using System;
using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using Random = System.Random;

namespace PuzzleGame.Domain.Services
{
    /// <summary>
    /// Zorluk eğrili, seed-based ve AI destekli puzzle üretici.
    /// Clean Architecture: Domain katmanı — UnityEngine bağımlılığı yok.
    /// </summary>
    public static class LevelGeneratorV2
    {
        /// <summary>
        /// Zorluk seviyesine göre puzzle üretir.
        /// difficulty: 0.0 (kolay) → 1.0 (zor)
        /// seed: Aynı zorluk ve seed ile aynı puzzle üretilir.
        /// </summary>
        public static List<List<LiquidLayer>> Generate(
            int bottleCount,
            int maxLayers,
            int emptyBottleCount,
            DomainColor[] colorPalette,
            float difficulty,
            int seed = 0)
        {
            var result = new List<List<LiquidLayer>>(bottleCount);
            for (int i = 0; i < bottleCount; i++)
                result.Add(new List<LiquidLayer>());

            int empties = Math.Clamp(emptyBottleCount, 1, bottleCount - 1);
            int filledCount = bottleCount - empties;
            int numColors = Math.Min(filledCount, colorPalette.Length);

            if (numColors < 1)
                return result;

            var rng = seed == 0 ? new Random() : new Random(seed);
            float amountPerLayer = 1f / maxLayers;

            // Zorluk faktörüne göre renk karışım oranını ayarla
            // Kolay: Renkler daha az karışık, Zor: Renkler tamamen karışık
            float mixFactor = Math.Clamp(difficulty, 0f, 1f);
            int shufflePasses = Math.Max(1, (int)(mixFactor * 5));

            // Gather all layers for all active colors
            var allLayers = new List<DomainColor>(numColors * maxLayers);
            for (int c = 0; c < numColors; c++)
            {
                for (int k = 0; k < maxLayers; k++)
                    allLayers.Add(colorPalette[c]);
            }

            // Zorluğa göre karıştırma
            for (int s = 0; s < shufflePasses; s++)
            {
                FisherYatesShuffle(allLayers, rng);
            }

            // Distribute mixed layers to the filled bottles
            int layerIndex = 0;
            for (int i = 0; i < filledCount; i++)
            {
                for (int k = 0; k < maxLayers; k++)
                {
                    if (layerIndex < allLayers.Count)
                    {
                        result[i].Add(new LiquidLayer(allLayers[layerIndex], amountPerLayer));
                        layerIndex++;
                    }
                }
            }

            // Zorluk ayarı: Zor seviyelerde, bazı şişelerin üst katmanlarını değiştir
            // Bu, çözümü daha zor hale getirir
            if (mixFactor > 0.7f)
            {
                ApplyAdvancedDifficulty(result, maxLayers, numColors, rng, mixFactor);
            }

            return result;
        }

        /// <summary>
        /// Yüksek zorluk seviyeleri için gelişmiş karışım algoritması.
        /// </summary>
        private static void ApplyAdvancedDifficulty(
            List<List<LiquidLayer>> assignments,
            int maxLayers,
            int numColors,
            Random rng,
            float difficulty)
        {
            // Zorluk faktörüne göre kaç şişenin üst katmanlarının değiştirileceğini belirle
            int affectedBottles = (int)(assignments.Count * difficulty);
            
            for (int i = 0; i < affectedBottles && i < assignments.Count; i++)
            {
                var bottle = assignments[i];
                if (bottle.Count >= 2)
                {
                    // Üst iki katmanı karıştır
                    var top = bottle[bottle.Count - 1];
                    var second = bottle[bottle.Count - 2];
                    
                    // %50 ihtimalle yer değiştir
                    if (rng.NextDouble() > 0.5)
                    {
                        bottle[bottle.Count - 1] = second;
                        bottle[bottle.Count - 2] = top;
                    }
                }
            }
        }

        private static void FisherYatesShuffle<T>(List<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}