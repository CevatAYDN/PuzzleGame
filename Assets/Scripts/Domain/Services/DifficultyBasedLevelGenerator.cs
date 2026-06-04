using System;
using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;
using Random = System.Random;

namespace PuzzleGame.Domain.Services
{
    /// <summary>
    /// Advanced level generator with difficulty curves and smart color mixing.
    /// Implements ILevelGenerator.
    /// </summary>
    public class DifficultyBasedLevelGenerator : ILevelGenerator
    {
        public List<List<OreLayer>> Generate(
            int MoldCount,
            int maxLayers,
            int emptyMoldCount,
            DomainColor[] colorPalette,
            Difficulty difficulty,
            int seed = 0)
        {
            var result = new List<List<OreLayer>>(MoldCount);
            for (int i = 0; i < MoldCount; i++)
                result.Add(new List<OreLayer>());

            int empties = Math.Clamp(emptyMoldCount, 1, MoldCount - 1);
            int filledCount = MoldCount - empties;
            int numColors = Math.Min(filledCount, colorPalette.Length);

            if (numColors < 1)
                return result;

            var rng = seed == 0 ? new Random() : new Random(seed);
            float amountPerLayer = 1f / maxLayers;

            // Map Difficulty enum to a float difficulty index (0.0 to 1.0)
            float mixFactor = difficulty switch
            {
                Difficulty.Trivial => 0.1f,
                Difficulty.Easy => 0.3f,
                Difficulty.Medium => 0.5f,
                Difficulty.Hard => 0.75f,
                Difficulty.Expert => 1.0f,
                _ => 0.3f
            };

            int shufflePasses = Math.Max(1, (int)(mixFactor * 5));

            // Gather all layers for all active colors
            var allLayers = new List<DomainColor>(numColors * maxLayers);
            for (int c = 0; c < numColors; c++)
            {
                for (int k = 0; k < maxLayers; k++)
                    allLayers.Add(colorPalette[c]);
            }

            // Shuffle based on difficulty
            for (int s = 0; s < shufflePasses; s++)
            {
                FisherYatesShuffle(allLayers, rng);
            }

            // Distribute mixed layers to the filled Molds
            int layerIndex = 0;
            for (int i = 0; i < filledCount; i++)
            {
                for (int k = 0; k < maxLayers; k++)
                {
                    if (layerIndex < allLayers.Count)
                    {
                        result[i].Add(new OreLayer(allLayers[layerIndex], amountPerLayer));
                        layerIndex++;
                    }
                }
            }

            // For higher difficulties, swap some upper layers to make puzzle sorting trickier
            if (mixFactor > 0.7f)
            {
                ApplyAdvancedDifficulty(result, mixFactor, rng);
            }

            return result;
        }

        private static void ApplyAdvancedDifficulty(
            List<List<OreLayer>> assignments,
            float difficulty,
            Random rng)
        {
            int affectedMolds = (int)(assignments.Count * difficulty);
            
            for (int i = 0; i < affectedMolds && i < assignments.Count; i++)
            {
                var Mold = assignments[i];
                if (Mold.Count >= 2)
                {
                    var top = Mold[Mold.Count - 1];
                    var second = Mold[Mold.Count - 2];
                    
                    if (rng.NextDouble() > 0.5)
                    {
                        Mold[Mold.Count - 1] = second;
                        Mold[Mold.Count - 2] = top;
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
