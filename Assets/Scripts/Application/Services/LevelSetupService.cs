using System;
using System.Collections.Generic;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Services;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Handles level setup and initialization logic.
    /// Uses IMoldView abstraction instead of MoldController (MonoBehaviour).
    /// Fix #1: DefaultPalette is now DomainColor[] — no UnityEngine.Color dependency.
    /// </summary>
    public class LevelSetupService : ILevelSetupService
    {
        private static readonly DomainColor[] DefaultPalette = new DomainColor[]
        {
            new DomainColor(0.95f, 0.25f, 0.35f),
            new DomainColor(0.30f, 0.75f, 0.40f),
            new DomainColor(0.25f, 0.55f, 0.95f),
            new DomainColor(0.95f, 0.80f, 0.20f),
            new DomainColor(0.65f, 0.30f, 0.85f),
            new DomainColor(0.95f, 0.50f, 0.15f),
        };

        private readonly GameConfig _gameConfig;
        private readonly LevelConfig _levelConfig;
        private readonly ILevelGenerator _levelGenerator;

        public LevelSetupService(GameConfig gameConfig, LevelConfig levelConfig, ILevelGenerator levelGenerator)
        {
            _gameConfig = gameConfig;
            _levelConfig = levelConfig;
            _levelGenerator = levelGenerator;
        }

        public List<List<OreLayer>> GenerateLevelAssignments(IMoldView[] Molds, LevelData currentLevel)
        {
            if (Molds == null || Molds.Length == 0)
                return new List<List<OreLayer>>();

            if (currentLevel == null)
                throw new ArgumentNullException(nameof(currentLevel),
                    "LevelSetupService.GenerateLevelAssignments: currentLevel cannot be null. This indicates a bug in the call chain.");

            bool autoGen = currentLevel.autoGenerate;
            int empties = currentLevel.emptyMoldCount;
            int seed = currentLevel.randomSeed;
            Difficulty diff = currentLevel.difficulty;

            // Convert LevelConfig.palette (UnityEngine.Color[]) only at the boundary.
            DomainColor[] pal = _levelConfig != null && _levelConfig.palette != null && _levelConfig.palette.Length > 0
                ? ConvertPalette(_levelConfig.palette)
                : DefaultPalette;


            if (currentLevel.autoGenerate)
            {
                var (generated, isSolvable) = _levelGenerator.GenerateSolvable(
                    Molds.Length,
                    currentLevel.maxLayersPerMold,
                    empties,
                    pal,
                    diff,
                    seed);

                if (!isSolvable)
                {
                    MoldLogger.LogWarning(
                        $"LevelSetupService: Auto-generated level (seed={seed}) was unsolvable " +
                        "after retry budget. Layout still loaded — player may be stuck.");
                }
                return generated;
            }

            // Pre-built level: convert List<LevelMoldData> to List<List<OreLayer>>
            var assignments = new List<List<OreLayer>>();
            if (currentLevel.Molds != null)
            {
                for (int i = 0; i < currentLevel.Molds.Count; i++)
                {
                    var MoldData = currentLevel.Molds[i];
                    if (MoldData == null) continue;

                    var layers = new List<OreLayer>();
                    if (!MoldData.isEmpty)
                    {
                        foreach (var layerData in MoldData.layers)
                        {
                            layers.Add(new OreLayer(ToDomainColor(layerData.color), layerData.amount));
                        }
                    }
                    assignments.Add(layers);
                }
            }

            return assignments;
        }

        public void SetupMolds(IMoldView[] Molds,
                                 LevelData currentLevel,
                                 IRendererService rendererService,
                                 IMoldValidator validator,
                                 IAnimationService animationService)
        {
            if (Molds.Length == 0) return;

            if (currentLevel == null)
            {
                // Play test mode: initialize Molds using their existing serialized/state layers!
                for (int i = 0; i < Molds.Length; i++)
                {
                    var Mold = Molds[i];
                    var initial = new List<OreLayer>();
                    if (Mold.State != null && Mold.State.Layers != null)
                    {
                        initial.AddRange(Mold.State.Layers);
                    }
                    Mold.Initialize(rendererService, validator, animationService, initial);
                }
                return;
            }

            var assignments = GenerateLevelAssignments(Molds, currentLevel);

            for (int i = 0; i < Molds.Length; i++)
            {
                var Mold = Molds[i];
                var initial = (assignments != null && i < assignments.Count)
                    ? assignments[i]
                    : new List<OreLayer>();

                Mold.Initialize(rendererService, validator, animationService, initial, _levelConfig?.moldVisualConfig);
            }
        }

        private DomainColor[] ConvertPalette(UnityEngine.Color[] colors)
        {
            var result = new DomainColor[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                result[i] = ToDomainColor(colors[i]);
            return result;
        }

        private static DomainColor ToDomainColor(UnityEngine.Color c)
            => new DomainColor(c.r, c.g, c.b, c.a);
    }
}