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
    /// Uses IBottleView abstraction instead of BottleController (MonoBehaviour).
    /// Fix #1: DefaultPalette is now DomainColor[] — no UnityEngine.Color dependency.
    /// </summary>
    public class LevelSetupService : ILevelSetupService
    {
        private readonly GameConfig _gameConfig;
        private readonly LevelConfig _levelConfig;
        private readonly ILevelGenerator _levelGenerator;

        // Fix #1: Pure C# DomainColor — no UnityEngine.Color dependency.
        private static readonly DomainColor[] DefaultPalette =
        {
            new DomainColor(0.95f, 0.20f, 0.55f, 1f),
            new DomainColor(0.20f, 0.55f, 0.95f, 1f),
            new DomainColor(0.30f, 0.85f, 0.35f, 1f),
            new DomainColor(0.98f, 0.80f, 0.15f, 1f),
            new DomainColor(0.70f, 0.30f, 0.90f, 1f),
            new DomainColor(0.95f, 0.50f, 0.15f, 1f),
        };

        public LevelSetupService(GameConfig gameConfig, LevelConfig levelConfig, ILevelGenerator levelGenerator, IColorAdapter colorAdapter)
        {
            _gameConfig = gameConfig;
            _levelConfig = levelConfig;
            _levelGenerator = levelGenerator;
            _colorAdapter = colorAdapter;
        }

        private readonly IColorAdapter _colorAdapter;

        public List<List<LiquidLayer>> GenerateLevelAssignments(IBottleView[] bottles, LevelData currentLevel)
        {
            if (bottles == null || bottles.Length == 0)
                return new List<List<LiquidLayer>>();

            if (currentLevel == null)
                throw new ArgumentNullException(nameof(currentLevel),
                    "LevelSetupService.GenerateLevelAssignments: currentLevel cannot be null. This indicates a bug in the call chain.");

            bool autoGen = currentLevel.autoGenerate;
            int empties = currentLevel.emptyBottleCount;
            int seed = currentLevel.randomSeed;
            Difficulty diff = currentLevel.difficulty;

            // Fix #1: Convert LevelConfig.palette (UnityEngine.Color[]) only at the boundary.
            // DefaultPalette is now DomainColor[] so conversion is not needed for the default case.
            DomainColor[] pal = _levelConfig != null && _levelConfig.palette != null && _levelConfig.palette.Length > 0
                ? ConvertPalette(_levelConfig.palette)
                : DefaultPalette;


            if (currentLevel.autoGenerate)
            {
                return _levelGenerator.Generate(
                    bottles.Length,
                    currentLevel.maxLayersPerBottle,
                    empties,
                    pal,
                    diff,
                    seed);
            }

            // Pre-built level: convert List<LevelBottleData> to List<List<LiquidLayer>>
            var assignments = new List<List<LiquidLayer>>();
            if (currentLevel.bottles != null)
            {
                for (int i = 0; i < currentLevel.bottles.Count; i++)
                {
                    var bottleData = currentLevel.bottles[i];
                    if (bottleData == null) continue;

                    var layers = new List<LiquidLayer>();
                    if (!bottleData.isEmpty)
                    {
                        foreach (var layerData in bottleData.layers)
                        {
                            layers.Add(new LiquidLayer(_colorAdapter.FromUnity(layerData.color), layerData.amount));
                        }
                    }
                    assignments.Add(layers);
                }
            }

            return assignments;
        }

        public void SetupBottles(IBottleView[] bottles,
                                 LevelData currentLevel,
                                 IRendererService rendererService,
                                 IBottleValidator validator,
                                 IAnimationService animationService)
        {
            if (bottles.Length == 0) return;

            if (currentLevel == null)
            {
                // Play test mode: initialize bottles using their existing serialized/state layers!
                for (int i = 0; i < bottles.Length; i++)
                {
                    var bottle = bottles[i];
                    var initial = new List<LiquidLayer>();
                    if (bottle.State != null && bottle.State.Layers != null)
                    {
                        initial.AddRange(bottle.State.Layers);
                    }
                    bottle.Initialize(rendererService, validator, animationService, initial);
                }
                return;
            }

            var assignments = GenerateLevelAssignments(bottles, currentLevel);

            for (int i = 0; i < bottles.Length; i++)
            {
                var bottle = bottles[i];
                var initial = (assignments != null && i < assignments.Count)
                    ? assignments[i]
                    : new List<LiquidLayer>();

                bottle.Initialize(rendererService, validator, animationService, initial);
            }
        }

        private DomainColor[] ConvertPalette(UnityEngine.Color[] colors)
        {
            var result = new DomainColor[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                result[i] = _colorAdapter.FromUnity(colors[i]);
            return result;
        }
    }
}