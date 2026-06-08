using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Services;
using PuzzleGame.Infrastructure;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Headless level verification and solving utility.
    /// Used by LevelBuildProcessor, LevelsTab, and Solver functions.
    /// </summary>
    public static class LevelSolverUtility
    {
        // Palette cache: the source <see cref="Color"/> array is used as the
        // identity key. The converted <see cref="DomainColor"/> array is cached
        // so that batch processing (e.g. LevelBuildProcessor iterating 1000+
        // levels with the same LevelConfig) does not re-convert per call.
        // Cleared by <see cref="ClearPaletteCache"/> for editor test isolation.
        private static Color[] s_paletteCacheKey;
        private static DomainColor[] s_paletteCacheValue;

        /// <summary>
        /// Drops the palette conversion cache. Editor tests should call this
        /// from <c>[TearDown]</c> to keep fixtures isolated.
        /// </summary>
        public static void ClearPaletteCache()
        {
            s_paletteCacheKey = null;
            s_paletteCacheValue = null;
        }

        private static DomainColor[] GetOrConvertPalette(Color[] palette)
        {
            if (palette == null) return System.Array.Empty<DomainColor>();
            if (ReferenceEquals(palette, s_paletteCacheKey) && s_paletteCacheValue != null)
                return s_paletteCacheValue;

            var converted = new DomainColor[palette.Length];
            for (int i = 0; i < palette.Length; i++)
                converted[i] = ColorAdapter.FromUnityStatic(palette[i]);

            s_paletteCacheKey = palette;
            s_paletteCacheValue = converted;
            return converted;
        }

        public static List<List<OreLayer>> GetLevelAssignments(LevelData level, LevelConfig levelConfig)
        {
            if (level == null) return new List<List<OreLayer>>();
            if (level.autoGenerate)
            {
                var generator = new DifficultyBasedLevelGenerator();
                Color[] palette = levelConfig != null && levelConfig.palette.Length > 0 ? levelConfig.palette : SceneBuilderModel.DefaultPalette;
                var domainPalette = GetOrConvertPalette(palette);

                return generator.Generate(
                    level.MoldCount,
                    level.maxLayersPerMold,
                    level.emptyMoldCount,
                    domainPalette,
                    level.difficulty,
                    level.randomSeed);
            }
            else
            {
                var assignments = new List<List<OreLayer>>();
                if (level.Molds != null)
                {
                    foreach (var Mold in level.Molds)
                    {
                        var layers = new List<OreLayer>();
                        if (!Mold.isEmpty && Mold.layers != null)
                        {
                            foreach (var layer in Mold.layers)
                            {
                                layers.Add(new OreLayer(ColorAdapter.FromUnityStatic(layer.color), layer.amount));
                            }
                        }
                        assignments.Add(layers);
                    }
                }
                return assignments;
            }
        }

        public static DomainColor[] ConvertPalette(Color[] colors)
        {
            var result = new DomainColor[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                result[i] = ColorAdapter.FromUnityStatic(colors[i]);
            return result;
        }

        public static OreSortSolver.SolverResult SolveLevel(LevelData level, LevelConfig levelConfig)
        {
            var assignments = GetLevelAssignments(level, levelConfig);
            int maxLayers = level.autoGenerate ? level.maxLayersPerMold : ForgeConstants.MaxLayers;
            if (maxLayers < ForgeConstants.MaxLayers) maxLayers = ForgeConstants.MaxLayers;

            var config = level.multiLayerCastConfig;
            var options = new OreSortSolver.OreSortSolverOptions
            {
                EnableMultiLayerCast = level.enableMultiLayerCast,
                CastConsecutiveOnly = config?.CastConsecutiveOnly ?? true,
                MinConsecutiveForCast = config?.minConsecutiveForCast ?? ForgeConstants.MinEmptyMolds,
                ColorTolerance = ForgeConstants.ColorMatchEpsilon
            };
            return OreSortSolver.Solve(assignments, maxLayers, options);
        }
    }
}
