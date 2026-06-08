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

            // Y21: progress bar + try/catch so a single malformed LevelData
            // does not abort the entire build pipeline with an unhandled
            // exception (the previous behaviour made the editor hang at
            // "Resolving packages..." for 5-10 minutes on big catalogs).
            int total = guids.Length;
            try
            {
                for (int i = 0; i < total; i++)
                {
                    var guid = guids[i];
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    // ShowCancelableProgress so the user can abort the build
                    // from the bar itself if the validation takes too long.
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Validating Levels",
                            $"({i + 1}/{total}) {path}",
                            total == 0 ? 0f : (float)i / total))
                    {
                        Debug.LogWarning("[LevelBuildProcessor] Validation cancelled by user. Aborting build.");
                        throw new BuildFailedException("Level validation cancelled by user.");
                    }

                    LevelData level = null;
                    try
                    {
                        level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                    }
                    catch (System.Exception loadEx)
                    {
                        Debug.LogError($"[LevelBuildProcessor] Failed to load level at {path}: {loadEx.Message}");
                        continue;
                    }
                    if (level == null) continue;

                    PuzzleGame.Domain.Services.OreSortSolver.SolverResult result;
                    try
                    {
                        result = LevelSolverUtility.SolveLevel(level, levelConfig);
                    }
                    catch (System.Exception solveEx)
                    {
                        // A throw from SolveLevel usually means the level has
                        // an impossible parameter combination (negative counts,
                        // 0 palette, etc.). Mark it unsolvable and keep going.
                        Debug.LogError($"[LevelBuildProcessor] Solver threw on {path}: {solveEx.Message}");
                        unsolvableLevels.Add($"Level {level.levelNumber} (THREW at {path}: {solveEx.Message})");
                        continue;
                    }

                    if (!result.IsSolvable)
                    {
                        unsolvableLevels.Add($"Level {level.levelNumber} (Difficulty: {level.difficulty}, Path: {path})");
                    }
                }
            }
            finally
            {
                // Always clear the progress bar — otherwise the editor will
                // keep showing the last "X / Y" message after the build ends.
                EditorUtility.ClearProgressBar();
            }

            if (unsolvableLevels.Count > 0)
            {
                string errorMessage = $"[LevelBuildProcessor] Build Aborted! The following levels are UNSOLVABLE:\n" +
                                     string.Join("\n", unsolvableLevels);
                Debug.LogError(errorMessage);
                throw new BuildFailedException(errorMessage);
            }

            Debug.Log($"[LevelBuildProcessor] Level validation successful. All {total} levels are solvable!");
        }
    }
}
