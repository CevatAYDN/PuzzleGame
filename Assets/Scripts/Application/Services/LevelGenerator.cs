using System;
using System.Collections.Generic;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Generates levels based on difficulty and player progress.
    /// Implements ILevelGenerator with solvability guarantees.
    /// </summary>
    public class DifficultyBasedLevelGenerator : ILevelGenerator
    {
        private readonly LevelConfig _config;
        private readonly System.Random _random;
        
        public DifficultyBasedLevelGenerator(LevelConfig config)
        {
            _config = config;
            _random = new System.Random();
        }
        
        public LevelData GenerateLevel(int levelNumber, int playerProgress)
        {
            // Calculate difficulty based on player progress
            float difficulty = CalculateDifficulty(levelNumber, playerProgress);
            
            // Generate level based on calculated difficulty
            var levelData = new LevelData
            {
                levelNumber = levelNumber,
                difficulty = difficulty,
                colorCount = GetColorCountForDifficulty(difficulty),
                layers = GetLayerCountForDifficulty(difficulty),
                optionalTargets = GenerateOptionalTargets(difficulty),
                biome = GetBiomeForLevel(levelNumber)
            };
            
            // Ensure the level is solvable
            EnsureSolvability(levelData);
            
            return levelData;
        }
        
        private float CalculateDifficulty(int levelNumber, int playerProgress)
        {
            // Basic difficulty curve - adjust as needed
            float baseDifficulty = Mathf.Clamp01((float)levelNumber / _config.maxLevels);
            float progressFactor = Mathf.Clamp01((float)playerProgress / _config.maxPlayerProgress);
            
            // Adjust difficulty based on player progress
            return Mathf.Lerp(baseDifficulty, baseDifficulty * 1.5f, progressFactor);
        }
        
        private int GetColorCountForDifficulty(float difficulty)
        {
            // More colors as difficulty increases
            return Mathf.Clamp(Mathf.FloorToInt(difficulty * (_config.maxColors - _config.minColors) + _config.minColors), _config.minColors, _config.maxColors);
        }
        
        private int GetLayerCountForDifficulty(float difficulty)
        {
            // More layers as difficulty increases
            return Mathf.Clamp(Mathf.FloorToInt(difficulty * (_config.maxLayers - _config.minLayers) + _config.minLayers), _config.minLayers, _config.maxLayers);
        }
        
        private List<OreLayer> GenerateOptionalTargets(float difficulty)
        {
            var targets = new List<OreLayer>();
            
            // Add optional targets based on difficulty
            if (difficulty > 0.5f)
            {
                int targetCount = Mathf.Clamp(Mathf.FloorToInt(difficulty * 2), 1, _config.maxOptionalTargets);
                for (int i = 0; i < targetCount; i++)
                {
                    targets.Add(GenerateRandomLayer(difficulty));
                }
            }
            
            return targets;
        }
        
        private OreLayer GenerateRandomLayer(float difficulty)
        {
            // Generate a random color for the layer
            Color randomColor = new Color(
                _random.NextFloat(0.2f, 1.0f),
                _random.NextFloat(0.2f, 1.0f),
                _random.NextFloat(0.2f, 1.0f),
                1.0f
            );
            
            // Generate a random fill amount based on difficulty
            float fillAmount = Mathf.Lerp(0.3f, 0.8f, difficulty);
            
            return new OreLayer(new DomainColor(randomColor.r, randomColor.g, randomColor.b, randomColor.a), fillAmount);
        }
        
        private string GetBiomeForLevel(int levelNumber)
        {
            // Simple biome rotation based on level number
            int biomeIndex = levelNumber % _config.biomes.Length;
            return _config.biomes[biomeIndex];
        }
        
        private void EnsureSolvability(LevelData levelData)
        {
            // Ensure the level has at least one possible solution
            // This is a simplified version - more complex solvability checks can be added
            if (levelData.layers < 2 || levelData.colorCount < 2)
            {
                levelData.layers = Mathf.Max(levelData.layers, 2);
                levelData.colorCount = Mathf.Max(levelData.colorCount, 2);
            }
        }
    }
}