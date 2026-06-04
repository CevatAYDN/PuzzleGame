using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Runs validation checks on all LevelData assets before building.
    /// Aborts compilation and reporting if unsolvable levels are detected.
    /// </summary>
    public class LevelBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            if (levelConfig == null)
            {
                Debug.LogWarning("[LevelBuildProcessor] LevelConfig.asset not found at default path. Proceeding without custom config.");
            }

            var guids = AssetDatabase.FindAssets("t:LevelData");
            if (guids == null || guids.Length == 0)
            {
                Debug.LogWarning("[LevelBuildProcessor] No LevelData assets found to validate.");
                return;
            }

            var unsolvableLevels = new List<string>();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level == null) continue;

                // Validate level playability using the headless utility solver
                var result = LevelSolverUtility.SolveLevel(level, levelConfig);
                if (!result.IsSolvable)
                {
                    unsolvableLevels.Add($"Level {level.levelNumber} (Difficulty: {level.difficulty}, Path: {path})");
                }
            }

            if (unsolvableLevels.Count > 0)
            {
                string errorMessage = $"[LevelBuildProcessor] Build Aborted! The following levels are UNSOLVABLE:\n" +
                                     string.Join("\n", unsolvableLevels);
                Debug.LogError(errorMessage);
                throw new BuildFailedException(errorMessage);
            }

            Debug.Log($"[LevelBuildProcessor] Level validation successful. All {guids.Length} levels are solvable!");
        }
    }
}
