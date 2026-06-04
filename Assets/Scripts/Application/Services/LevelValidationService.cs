using System.Collections.Generic;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Logging;

using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Handles validation of level data before loading
    /// </summary>
    public class LevelValidationService : ILevelValidationService
    {
        public bool ValidateLevel(LevelData levelData, int totalMoldsAvailable)
        {
            if (levelData == null)
            {
                MoldLogger.LogError("Level data is null");
                return false;
            }

            // Validate basic properties
            if (levelData.levelNumber <= 0)
            {
                MoldLogger.LogError($"Invalid level number: {levelData.levelNumber}");
                return false;
            }

            // Validate Mold counts
            if (levelData.autoGenerate)
            {
                if (levelData.MoldCount <= 0 || levelData.MoldCount > totalMoldsAvailable)
                {
                    MoldLogger.LogError($"Invalid Mold count: {levelData.MoldCount} for total Molds: {totalMoldsAvailable}");
                    return false;
                }

                if (levelData.emptyMoldCount < 0 || levelData.emptyMoldCount >= totalMoldsAvailable)
                {
                    MoldLogger.LogError($"Invalid empty Mold count: {levelData.emptyMoldCount} for total Molds: {totalMoldsAvailable}");
                    return false;
                }

                if (levelData.maxLayersPerMold <= 0)
                {
                    MoldLogger.LogError($"Invalid max layers per Mold: {levelData.maxLayersPerMold}");
                    return false;
                }
            }
            else
            {
                // Validate predefined level data
                if (levelData.Molds == null)
                {
                    MoldLogger.LogError("Level Molds data is null for predefined level");
                    return false;
                }

                if (levelData.Molds.Count > totalMoldsAvailable)
                {
                    MoldLogger.LogError($"Too many Molds defined ({levelData.Molds.Count}) for available slots ({totalMoldsAvailable})");
                    return false;
                }

                // Validate each Mold in the predefined level
                for (int i = 0; i < levelData.Molds.Count; i++)
                {
                    var MoldData = levelData.Molds[i];
                    if (MoldData != null)
                    {
                        // Validate layers if not empty
                        if (!MoldData.isEmpty && MoldData.layers != null)
                        {
                            float totalAmount = 0f;
                            foreach (var layer in MoldData.layers)
                            {
                                if (layer.amount <= 0)
                                {
                                    MoldLogger.LogError($"Invalid layer amount: {layer.amount} in Mold {i}");
                                    return false;
                                }
                                totalAmount += layer.amount;
                            }

                            // Check if total amount exceeds 1.0 (full capacity)
                            if (totalAmount > 1.0f)
                            {
                                MoldLogger.LogError($"Mold {i} has total amount exceeding 1.0: {totalAmount}");
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