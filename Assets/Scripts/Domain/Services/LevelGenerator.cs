using System;
using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Infrastructure;
using Random = System.Random;

namespace PuzzleGame.Domain.Services
{
    /// <summary>
    /// Stateless, tamamen test edilebilir level üreteci.
    /// Domain katmanında — UnityEngine bağımlılığı yoktur.
    /// </summary>
    public static class LevelGenerator
    {
        /// <summary>
        /// Rastgele ama çözülebilir bir puzzle oluşturur.
        /// Her renkten maxLayers adet katman, rastgele sırada,
        /// rastgele seçilmiş dolu şişelere dağıtılır.
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

            // Her renkten maxLayers katman
            var pool = new List<DomainColor>(numColors * maxLayers);
            for (int c = 0; c < numColors; c++)
                for (int k = 0; k < maxLayers; k++)
                    pool.Add(colorPalette[c]);

            var rng = seed == 0 ? new Random() : new Random(seed);
            FisherYatesShuffle(pool, rng);

            float amountPerLayer = 1f / maxLayers;

            // Pool'daki katmanları şişelere round-robin dağıt
            for (int p = 0; p < pool.Count; p++)
            {
                int bottleIndex = p % filledCount;
                result[bottleIndex].Add(new LiquidLayer(pool[p], amountPerLayer));
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
