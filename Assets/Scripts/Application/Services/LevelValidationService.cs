using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Logging;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Handles validation of level data before loading
    /// </summary>
    public class LevelValidationService
    {
        public bool ValidateLevel(LevelData levelData, int totalBottlesAvailable)
        {
            if (levelData == null)
            {
                BottleLogger.LogError("Level data is null");
                return false;
            }

            // Validate basic properties
            if (levelData.levelNumber <= 0)
            {
                BottleLogger.LogError($"Invalid level number: {levelData.levelNumber}");
                return false;
            }

            // Validate bottle counts
            if (levelData.autoGenerate)
            {
                if (levelData.emptyBottleCount < 0 || levelData.emptyBottleCount >= totalBottlesAvailable)
                {
                    BottleLogger.LogError($"Invalid empty bottle count: {levelData.emptyBottleCount} for total bottles: {totalBottlesAvailable}");
                    return false;
                }

                if (levelData.maxLayersPerBottle <= 0)
                {
                    BottleLogger.LogError($"Invalid max layers per bottle: {levelData.maxLayersPerBottle}");
                    return false;
                }
            }
            else
            {
                // Validate predefined level data
                if (levelData.bottles == null)
                {
                    BottleLogger.LogError("Level bottles data is null for predefined level");
                    return false;
                }

                if (levelData.bottles.Count > totalBottlesAvailable)
                {
                    BottleLogger.LogError($"Too many bottles defined ({levelData.bottles.Count}) for available slots ({totalBottlesAvailable})");
                    return false;
                }

                // Validate each bottle in the predefined level
                for (int i = 0; i < levelData.bottles.Count; i++)
                {
                    var bottleData = levelData.bottles[i];
                    if (bottleData != null)
                    {
                        // Validate layers if not empty
                        if (!bottleData.isEmpty && bottleData.layers != null)
                        {
                            float totalAmount = 0f;
                            foreach (var layer in bottleData.layers)
                            {
                                if (layer.amount <= 0)
                                {
                                    BottleLogger.LogError($"Invalid layer amount: {layer.amount} in bottle {i}");
                                    return false;
                                }
                                totalAmount += layer.amount;
                            }

                            // Check if total amount exceeds 1.0 (full capacity)
                            if (totalAmount > 1.0f)
                            {
                                BottleLogger.LogError($"Bottle {i} has total amount exceeding 1.0: {totalAmount}");
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}