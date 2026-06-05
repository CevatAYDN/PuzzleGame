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
        public static List<List<OreLayer>> GetLevelAssignments(LevelData level, LevelConfig levelConfig)
        {
            if (level == null) return new List<List<OreLayer>>();
            if (level.autoGenerate)
            {
                var generator = new DifficultyBasedLevelGenerator();
                Color[] palette = levelConfig != null && levelConfig.palette.Length > 0 ? levelConfig.palette : SceneBuilderModel.DefaultPalette;
                var domainPalette = new DomainColor[palette.Length];
                for (int i = 0; i < palette.Length; i++)
                    domainPalette[i] = ColorAdapter.FromUnityStatic(palette[i]);

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
            int maxLayers = level.autoGenerate ? level.maxLayersPerMold : 4;
            if (maxLayers < 4) maxLayers = 4;

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
