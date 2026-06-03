using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Domain.Services;
using PuzzleGame.Infrastructure;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Infrastructure.Interfaces;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Handles level setup and initialization logic.
    /// BottleController (MonoBehaviour) yerine IBottleView abstraction kullanır.
    /// </summary>
    public class LevelSetupService : ILevelSetupService
    {
        private readonly GameConfig _gameConfig;
        private readonly LevelConfig _levelConfig;
        private readonly ILevelGenerator _levelGenerator;
        
        private static readonly Color[] DefaultPalette = new Color[]
        {
            new Color(0.95f, 0.20f, 0.55f, 1f),
            new Color(0.20f, 0.55f, 0.95f, 1f),
            new Color(0.30f, 0.85f, 0.35f, 1f),
            new Color(0.98f, 0.80f, 0.15f, 1f),
            new Color(0.70f, 0.30f, 0.90f, 1f),
            new Color(0.95f, 0.50f, 0.15f, 1f),
        };

        public LevelSetupService(GameConfig gameConfig, LevelConfig levelConfig, ILevelGenerator levelGenerator)
        {
            _gameConfig = gameConfig;
            _levelConfig = levelConfig;
            _levelGenerator = levelGenerator;
        }

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
            Color[] pal = _levelConfig != null && _levelConfig.palette.Length > 0
                ? _levelConfig.palette
                : DefaultPalette;

            if (currentLevel.autoGenerate)
            {
                return _levelGenerator.Generate(
                    bottles.Length,
                    currentLevel.maxLayersPerBottle,
                    empties,
                    ConvertPalette(pal),
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
                            layers.Add(new LiquidLayer(ColorAdapter.FromUnity(layerData.color), layerData.amount));
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

        private DomainColor[] ConvertPalette(Color[] colors)
        {
            var result = new DomainColor[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                result[i] = ColorAdapter.FromUnity(colors[i]);
            return result;
        }
    }
}