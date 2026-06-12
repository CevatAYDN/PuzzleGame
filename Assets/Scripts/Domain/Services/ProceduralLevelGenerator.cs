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
    public class ProceduralLevelGenerator : ILevelGenerator
    {
        public List<List<OreLayer>> Generate(
            int MoldCount,
            int maxLayers,
            int emptyMoldCount,
            DomainColor[] colorPalette,
            Difficulty difficulty,
            int seed = 0)
        {
            return Generate(MoldCount, maxLayers, emptyMoldCount, colorPalette, difficulty, seed, false);
        }

        public List<List<OreLayer>> Generate(
            int MoldCount,
            int maxLayers,
            int emptyMoldCount,
            DomainColor[] colorPalette,
            Difficulty difficulty,
            int seed = 0,
            bool enableFrozenLayers = false)
        {
            var result = new List<List<OreLayer>>(MoldCount);
            for (int i = 0; i < MoldCount; i++)
                result.Add(new List<OreLayer>());

            if (MoldCount < 2)
                return result;

            int empties = Math.Clamp(emptyMoldCount, 1, MoldCount - 1);
            int filledCount = MoldCount - empties;
            int numColors = Math.Min(filledCount, colorPalette.Length);

            if (numColors < 1)
                return result;

            var rng = new Random(seed);
            float amountPerLayer = 1f / maxLayers;

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

            var allLayers = new List<DomainColor>(numColors * maxLayers);
            for (int c = 0; c < numColors; c++)
            {
                for (int k = 0; k < maxLayers; k++)
                    allLayers.Add(colorPalette[c]);
            }

            for (int s = 0; s < shufflePasses; s++)
            {
                FisherYatesShuffle(allLayers, rng);
            }

            int layerIndex = 0;
            for (int i = 0; i < filledCount; i++)
            {
                for (int k = 0; k < maxLayers; k++)
                {
                    if (layerIndex < allLayers.Count)
                    {
                        var domainColor = allLayers[layerIndex];
                        var oreColor = OreColorExtensions.FromDomainColor(domainColor);
                        result[i].Add(new OreLayer(domainColor, amountPerLayer, oreColor));
                        layerIndex++;
                    }
                }
            }

            if (mixFactor > 0.7f)
            {
                ApplyAdvancedDifficulty(result, mixFactor, rng);
            }

            if (enableFrozenLayers && mixFactor >= 0.5f)
            {
                ApplyFrozenLayers(result, filledCount, mixFactor, rng);
            }

            return result;
        }

        private static void ApplyFrozenLayers(
            List<List<OreLayer>> assignments,
            int filledCount,
            float difficulty,
            Random rng)
        {
            int frozenCount = Math.Max(1, (int)(filledCount * difficulty * 0.3f));
            var candidates = new List<int>();
            for (int i = 0; i < filledCount; i++)
            {
                if (assignments[i].Count >= 2)
                    candidates.Add(i);
            }

            for (int f = 0; f < frozenCount && candidates.Count > 0; f++)
            {
                int idx = rng.Next(candidates.Count);
                int moldIdx = candidates[idx];
                candidates.RemoveAt(idx);

                var layers = assignments[moldIdx];
                int frozenPos = rng.Next(0, layers.Count - 1);
                var layer = layers[frozenPos];
                layers[frozenPos] = layer.WithModifier(LayerModifier.Frozen);
            }
        }

        public (List<List<OreLayer>> Molds, bool IsSolvable) GenerateSolvable(
            int MoldCount,
            int maxLayers,
            int emptyMoldCount,
            DomainColor[] colorPalette,
            Difficulty difficulty,
            int seed = 0,
            int maxAttempts = 8,
            bool enableFrozenLayers = false,
            bool enableMultiPour = false)
        {
            if (maxAttempts < 1) maxAttempts = 1;

            int baseSeed = seed;
            List<List<OreLayer>> lastResult = null;

            // First pass: try with full features (frozen + multi-pour if enabled)
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int currentSeed = baseSeed == 0
                    ? unchecked((int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF)) + attempt
                    : baseSeed + attempt;

                lastResult = Generate(MoldCount, maxLayers, emptyMoldCount, colorPalette, difficulty, currentSeed, enableFrozenLayers);
                var solverResult = OreSortSolver.Solve(lastResult, maxLayers);
                if (solverResult.IsSolvable)
                {
                    return (lastResult, true);
                }
            }

            // Fallback pass: if frozen layers were enabled but no solution found,
            // retry without frozen layers to guarantee a solvable level
            if (enableFrozenLayers)
            {
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    int currentSeed = baseSeed == 0
                        ? unchecked((int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF)) + attempt + maxAttempts
                        : baseSeed + attempt + maxAttempts;

                    lastResult = Generate(MoldCount, maxLayers, emptyMoldCount, colorPalette, difficulty, currentSeed, false);
                    var solverResult = OreSortSolver.Solve(lastResult, maxLayers);
                    if (solverResult.IsSolvable)
                    {
                        return (lastResult, true);
                    }
                }
            }

            return (lastResult ?? Generate(MoldCount, maxLayers, emptyMoldCount, colorPalette, difficulty, baseSeed), false);
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
