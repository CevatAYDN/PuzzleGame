using System;
using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using Random = System.Random;

namespace PuzzleGame.Domain.Services
{
    /// <summary>
    /// Stateless, tamamen test edilebilir level üreteci.
    /// Domain katmanında — UnityEngine bağımlılığı yoktur.
    ///
    /// Çözülebilir puzzle için kural:
    ///   - Her renkten maxLayers adet katman vardır.
    ///   - Aynı renk tüm katmanları aynı şişededir (çözüm için).
    ///   - Katman sırası şişe içinde rastgeledir (zorluk için).
    /// </summary>
    public static class LevelGenerator
    {
        /// <summary>
        /// Rastgele ama çözülebilir bir puzzle oluşturur.
        /// Her renk bir şişeye, katman sırası rastgele.
        /// Kalan şişeler boş (hedef şişeler).
        /// </summary>
        public static List<List<LiquidLayer>> Generate(
            int bottleCount,
            int maxLayers,
            int emptyBottleCount,
            DomainColor[] colorPalette,
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

            // Her rengi rastgele bir dolu şişeye ata.
            // Hiçbir şişeye birden fazla renk atanmaz.
            var bottleIndices = new List<int>(filledCount);
            for (int i = 0; i < filledCount; i++) bottleIndices.Add(i);
            FisherYatesShuffle(bottleIndices, rng);

            // Gather all layers for all active colors
            var allLayers = new List<DomainColor>(numColors * maxLayers);
            for (int c = 0; c < numColors; c++)
            {
                for (int k = 0; k < maxLayers; k++)
                    allLayers.Add(colorPalette[c]);
            }

            // Shuffle all layers to mix colors
            FisherYatesShuffle(allLayers, rng);

            // Distribute mixed layers to the filled bottles
            int layerIndex = 0;
            for (int i = 0; i < filledCount; i++)
            {
                int bottleIndex = bottleIndices[i];
                for (int k = 0; k < maxLayers; k++)
                {
                    if (layerIndex < allLayers.Count)
                    {
                        result[bottleIndex].Add(new LiquidLayer(allLayers[layerIndex], amountPerLayer));
                        layerIndex++;
                    }
                }
            }

            return result;
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
