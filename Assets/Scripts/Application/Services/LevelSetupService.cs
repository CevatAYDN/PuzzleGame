using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Services;
using PuzzleGame.Infrastructure;
using PuzzleGame.Configuration;
using PuzzleGame.Logging;
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
        
        private static readonly Color[] DefaultPalette = new Color[]
        {
            new Color(0.95f, 0.20f, 0.55f, 1f),
            new Color(0.20f, 0.55f, 0.95f, 1f),
            new Color(0.30f, 0.85f, 0.35f, 1f),
            new Color(0.98f, 0.80f, 0.15f, 1f),
            new Color(0.70f, 0.30f, 0.90f, 1f),
            new Color(0.95f, 0.50f, 0.15f, 1f),
        };

        public LevelSetupService(GameConfig gameConfig, LevelConfig levelConfig)
        {
            _gameConfig = gameConfig;
            _levelConfig = levelConfig;
        }

        public List<List<LiquidLayer>> GenerateLevelAssignments(IBottleView[] bottles, LevelData currentLevel)
        {
            if (bottles == null || bottles.Length == 0) 
                return new List<List<LiquidLayer>>();

            // Determine generation parameters based on currentLevel first, fallback to levelConfig / defaults
            bool autoGen = true;
            int empties = 2;
            int seed = 0;
            Color[] pal = DefaultPalette;
            List<List<LiquidLayer>> assignments = null;

            if (currentLevel != null)
            {
                autoGen = currentLevel.autoGenerate;
                empties = currentLevel.emptyBottleCount;
                seed = currentLevel.randomSeed;
                pal = _levelConfig != null && _levelConfig.palette.Length > 0 ? _levelConfig.palette : DefaultPalette;

                if (currentLevel.autoGenerate)
                {
                    assignments = LevelGenerator.Generate(
                        bottles.Length,
                        currentLevel.maxLayersPerBottle,
                        empties,
                        ConvertPalette(pal),
                        seed);
                }
                else
                {
                    // Pre-built level: convert List<LevelBottleData> to List<List<LiquidLayer>>
                    assignments = new List<List<LiquidLayer>>();
                    if (currentLevel.bottles != null)
                    {
                        for (int i = 0; i < currentLevel.bottles.Count; i++)
                        {
                            var bottleData = currentLevel.bottles[i];
                            if (bottleData != null)
                            {
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
                    }
                }
            }
            else
            {
                // Fallback to levelConfig
                autoGen = _levelConfig != null ? _levelConfig.autoGenerateLevel : true;
                empties = _levelConfig != null ? _levelConfig.emptyBottleCount : 2;
                seed = _levelConfig != null ? _levelConfig.randomSeed : 0;
                pal = _levelConfig != null && _levelConfig.palette.Length > 0 ? _levelConfig.palette : DefaultPalette;

                assignments = autoGen
                    ? LevelGenerator.Generate(
                        bottles.Length,
                        _gameConfig.maxLayersPerBottle,
                        empties,
                        ConvertPalette(pal),
                        seed)
                    : null;
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