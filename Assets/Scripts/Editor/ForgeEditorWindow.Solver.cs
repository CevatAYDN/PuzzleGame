using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Configuration.FeatureSystem;
using PuzzleGame.Domain.Services;
using PuzzleGame.Infrastructure;
using PuzzleGame.Application.Services;

namespace PuzzleGame.Editor
{
    public partial class ForgeEditorWindow
    {
        // ── Solver & Scene Helpers ──────────────────────────────────────────

        private static List<List<OreLayer>> GetLevelAssignments(LevelData level, Application.Configuration.LevelConfig levelConfig)
        {
            if (level == null) return new List<List<OreLayer>>();
            if (level.autoGenerate)
            {
                var generator = new DifficultyBasedLevelGenerator();
                Color[] palette = levelConfig != null && levelConfig.palette.Length > 0 ? levelConfig.palette : SceneBuilder.DefaultPalette;
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

        private static DomainColor[] ConvertPalette(Color[] colors)
        {
            var result = new DomainColor[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                result[i] = ColorAdapter.FromUnityStatic(colors[i]);
            return result;
        }

        private void SolveSingleLevel(int index)
        {
            if (index < 0 || index >= _existingLevels.Count) return;
            var lvl = _existingLevels[index];
            if (!lvl.exists) return;

            var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
            if (levelData == null) return;

            var levelConfig = AssetDatabase.LoadAssetAtPath<Application.Configuration.LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            var assignments = GetLevelAssignments(levelData, levelConfig);
            int maxLayers = levelData.autoGenerate ? levelData.maxLayersPerMold : 4;

            var result = OreSortSolver.Solve(assignments, maxLayers);
            lvl.hasSolved = true;
            lvl.isSolvable = result.IsSolvable;
            lvl.optimalMoves = result.IsSolvable ? result.SolutionPath.Count : 0;
            _existingLevels[index] = lvl;

            if (result.IsSolvable)
            {
                SetStatus($"Level {lvl.number:D2}: Solvable in {result.SolutionPath.Count} moves.", MessageType.Info);
            }
            else
            {
                SetStatus($"Level {lvl.number:D2}: UNSOLVABLE!", MessageType.Error);
            }
        }

        private void CopyLevel(int index)
        {
            if (index < 0 || index >= _existingLevels.Count) return;
            var lvl = _existingLevels[index];
            if (!lvl.exists) return;

            var sourceLevel = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
            if (sourceLevel == null) return;

            // Find next available level number
            int copyNumber = 101;
            for (int i = 101; i <= 200; i++)
            {
                string path = $"{LevelDataBatchCreator.LevelPath}/Level_{i:D2}.asset";
                if (AssetDatabase.LoadAssetAtPath<LevelData>(path) == null)
                {
                    copyNumber = i;
                    break;
                }
            }

            if (copyNumber > 200)
            {
                SetStatus("No available slot for copy (101-200). Delete some levels first.", MessageType.Warning);
                return;
            }

            string newPath = $"{LevelDataBatchCreator.LevelPath}/Level_{copyNumber:D2}.asset";

            // Create copy
            var newLevel = ScriptableObject.CreateInstance<LevelData>();
            newLevel.levelNumber = copyNumber;
            newLevel.difficulty = sourceLevel.difficulty;
            newLevel.MoldCount = sourceLevel.MoldCount;
            newLevel.emptyMoldCount = sourceLevel.emptyMoldCount;
            newLevel.colorCount = sourceLevel.colorCount;
            newLevel.maxLayersPerMold = sourceLevel.maxLayersPerMold;
            newLevel.randomSeed = copyNumber * 1337;
            newLevel.autoGenerate = sourceLevel.autoGenerate;

            // Copy pre-built Molds if not auto-generating
            if (!sourceLevel.autoGenerate && sourceLevel.Molds != null)
            {
                newLevel.Molds = new List<LevelMoldData>();
                foreach (var Mold in sourceLevel.Molds)
                {
                    var copy = new LevelMoldData
                    {
                        isEmpty = Mold.isEmpty,
                        layers = new List<LevelLayerData>()
                    };
                    foreach (var layer in Mold.layers)
                    {
                        copy.layers.Add(new LevelLayerData
                        {
                            color = layer.color,
                            amount = layer.amount
                        });
                    }
                    newLevel.Molds.Add(copy);
                }
            }

            // Set par values (slightly higher than source as it's a new level)
            newLevel.parMoves = sourceLevel.parMoves + 2;
            newLevel.goodMoves = sourceLevel.goodMoves + 3;

            // Copy preview image reference
            newLevel.previewImage = sourceLevel.previewImage;

            AssetDatabase.CreateAsset(newLevel, newPath);
            AssetDatabase.SaveAssets();

            SetStatus($"Level {lvl.number:D2} copied to Level {copyNumber:D2}.", MessageType.Info);
            RefreshLevelList();
        }

        private void OptimizeParSingleLevel(int index)
        {
            if (index < 0 || index >= _existingLevels.Count) return;
            var lvl = _existingLevels[index];
            if (!lvl.exists || !lvl.hasSolved || !lvl.isSolvable) return;

            var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
            if (levelData == null) return;

            levelData.parMoves = lvl.optimalMoves;
            levelData.goodMoves = Mathf.RoundToInt(lvl.optimalMoves * 1.4f);
            if (levelData.goodMoves < levelData.parMoves + 2) levelData.goodMoves = levelData.parMoves + 2;

            EditorUtility.SetDirty(levelData);
            AssetDatabase.SaveAssets();

            SetStatus($"Level {lvl.number:D2} par moves optimized to {levelData.parMoves} (good moves: {levelData.goodMoves}).", MessageType.Info);
        }

        private void SolveAndVerifyAll()
        {
            var levelConfig = AssetDatabase.LoadAssetAtPath<Application.Configuration.LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            int total = _existingLevels.Count;
            int unsolvableCount = 0;
            int solvableCount = 0;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    var lvl = _existingLevels[i];
                    if (!lvl.exists) continue;

                    EditorUtility.DisplayProgressBar("Solving Levels", $"Solving Level {lvl.number:D2}...", (float)i / total);

                    var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
                    if (levelData == null) continue;

                    var assignments = GetLevelAssignments(levelData, levelConfig);
                    int maxLayers = levelData.autoGenerate ? levelData.maxLayersPerMold : 4;

                    var result = OreSortSolver.Solve(assignments, maxLayers);

                    lvl.hasSolved = true;
                    lvl.isSolvable = result.IsSolvable;
                    lvl.optimalMoves = result.IsSolvable ? result.SolutionPath.Count : 0;
                    _existingLevels[i] = lvl;

                    if (result.IsSolvable) solvableCount++;
                    else unsolvableCount++;
                }

                SetStatus($"Verification completed. Solvable: {solvableCount}, Unsolvable: {unsolvableCount}",
                    unsolvableCount == 0 ? MessageType.Info : MessageType.Warning);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void AutoReseedUnsolvableLevels()
        {
            var levelConfig = AssetDatabase.LoadAssetAtPath<Application.Configuration.LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            int reseededCount = 0;

            try
            {
                for (int i = 0; i < _existingLevels.Count; i++)
                {
                    var lvl = _existingLevels[i];
                    if (!lvl.exists) continue;

                    var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
                    if (levelData == null) continue;

                    // Solve first to check
                    var assignments = GetLevelAssignments(levelData, levelConfig);
                    int maxLayers = levelData.autoGenerate ? levelData.maxLayersPerMold : 4;
                    var result = OreSortSolver.Solve(assignments, maxLayers);

                    if (result.IsSolvable) continue;

                    // Unsolvable! Let's find a working seed
                    EditorUtility.DisplayProgressBar("Reseeding", $"Finding seed for Level {lvl.number:D2}...", (float)i / _existingLevels.Count);

                    int seed = levelData.randomSeed;
                    bool found = false;
                    for (int attempt = 1; attempt <= 100; attempt++)
                    {
                        seed += 1337; // Change seed
                        // Generate with new seed
                        var tempAssignments = new DifficultyBasedLevelGenerator().Generate(
                            levelData.MoldCount,
                            levelData.maxLayersPerMold,
                            levelData.emptyMoldCount,
                            ConvertPalette(levelConfig != null && levelConfig.palette.Length > 0 ? levelConfig.palette : SceneBuilder.DefaultPalette),
                            levelData.difficulty,
                            seed);

                        var tempResult = OreSortSolver.Solve(tempAssignments, maxLayers);
                        if (tempResult.IsSolvable)
                        {
                            // Save seed
                            levelData.randomSeed = seed;

                            // Auto-optimize par
                            levelData.parMoves = tempResult.SolutionPath.Count;
                            levelData.goodMoves = Mathf.RoundToInt(tempResult.SolutionPath.Count * 1.4f);
                            if (levelData.goodMoves < levelData.parMoves + 2) levelData.goodMoves = levelData.parMoves + 2;

                            EditorUtility.SetDirty(levelData);

                            lvl.hasSolved = true;
                            lvl.isSolvable = true;
                            lvl.optimalMoves = tempResult.SolutionPath.Count;
                            _existingLevels[i] = lvl;
                            reseededCount++;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Debug.LogWarning($"[Auto-Reseed] Could not find solvable seed for Level {lvl.number:D2} in 100 attempts.");
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                SetStatus($"Reseed complete. Successfully reseeded & solved {reseededCount} levels.", MessageType.Info);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void AutoOptimizeAllPars()
        {
            var levelConfig = AssetDatabase.LoadAssetAtPath<Application.Configuration.LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            int optimizedCount = 0;

            try
            {
                for (int i = 0; i < _existingLevels.Count; i++)
                {
                    var lvl = _existingLevels[i];
                    if (!lvl.exists) continue;

                    var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
                    if (levelData == null) continue;

                    var assignments = GetLevelAssignments(levelData, levelConfig);
                    int maxLayers = levelData.autoGenerate ? levelData.maxLayersPerMold : 4;
                    var result = OreSortSolver.Solve(assignments, maxLayers);

                    if (result.IsSolvable)
                    {
                        levelData.parMoves = result.SolutionPath.Count;
                        levelData.goodMoves = Mathf.RoundToInt(result.SolutionPath.Count * 1.4f);
                        if (levelData.goodMoves < levelData.parMoves + 2) levelData.goodMoves = levelData.parMoves + 2;

                        EditorUtility.SetDirty(levelData);
                        optimizedCount++;

                        lvl.hasSolved = true;
                        lvl.isSolvable = true;
                        lvl.optimalMoves = result.SolutionPath.Count;
                        _existingLevels[i] = lvl;
                    }
                }

                AssetDatabase.SaveAssets();
                SetStatus($"Optimized par/good moves for {optimizedCount} solvable levels.", MessageType.Info);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void LoadLevelIntoActiveScene(int index)
        {
            if (index < 0 || index >= _existingLevels.Count) return;
            var lvl = _existingLevels[index];
            if (!lvl.exists) return;

            var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
            LoadLevelIntoScene(levelData);
        }

        private void PlayLevelInActiveScene(int index)
        {
            if (index < 0 || index >= _existingLevels.Count) return;
            var lvl = _existingLevels[index];
            if (!lvl.exists) return;

            var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(lvl.path);
            LoadLevelIntoScene(levelData);
            EditorApplication.isPlaying = true;
        }

        private void LoadLevelIntoScene(LevelData level)
        {
            if (level == null)
            {
                SetStatus("No level selected to load.", MessageType.Warning);
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Load Level {level.levelNumber}");

            // 1. Clear existing Molds
            SceneBuilder.RemoveMolds();

            // 2. Load level Config
            var levelConfig = AssetDatabase.LoadAssetAtPath<Application.Configuration.LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            var assignments = GetLevelAssignments(level, levelConfig);

            // 3. Compute positions and instantiate Molds
            int count = assignments.Count;
            var layout = SceneBuilder.MoldLayout.Grid;
            Vector3 center = Vector3.zero;
            var positions = SceneBuilder.ComputePositions(layout, count, center);

            for (int i = 0; i < count; i++)
            {
                var layers = assignments[i];
                var colors = new Color[layers.Count];
                for (int j = 0; j < layers.Count; j++)
                    colors[j] = ColorAdapter.ToUnityStatic(layers[j].Color);

                // Build with layers
                var MoldCfg = SceneBuilder.MoldConfig.WithLayers(
                    positions[i],
                    layers,
                    SceneBuilder.ShaderVariant.Premium,
                    $"Mold_{i:D2}");

                SceneBuilder.CreateMold(MoldCfg);
            }

            Undo.CollapseUndoOperations(undoGroup);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            SetStatus($"Loaded Level {level.levelNumber} into active scene ({count} Molds).", MessageType.Info);
        }

        private void ExportSceneToLevel(LevelData level)
        {
            if (level == null)
            {
                SetStatus("No level selected to export to.", MessageType.Warning);
                return;
            }

            var Molds = FindObjectsByType<MoldController>(FindObjectsInactive.Include);
            if (Molds.Length == 0)
            {
                SetStatus("No Molds found in the scene to export.", MessageType.Warning);
                return;
            }

            var sortedMolds = Molds
                .OrderByDescending(b => b.transform.position.z)
                .ThenBy(b => b.transform.position.x)
                .ToArray();

            level.autoGenerate = false;
            level.MoldCount = sortedMolds.Length;
            level.Molds.Clear();

            int emptyCount = 0;

            foreach (var Mold in sortedMolds)
            {
                var MoldData = new LevelMoldData();
                MoldData.isEmpty = Mold.IsEmpty;
                if (MoldData.isEmpty)
                {
                    emptyCount++;
                }

                MoldData.layers = new List<LevelLayerData>();
                if (!Mold.IsEmpty && Mold.State != null && Mold.State.Layers != null)
                {
                    foreach (var layer in Mold.State.Layers)
                    {
                        var layerData = new LevelLayerData();
                        layerData.color = ColorAdapter.ToUnityStatic(layer.Color);
                        layerData.amount = layer.Amount;
                        MoldData.layers.Add(layerData);
                    }
                }
                level.Molds.Add(MoldData);
            }

            level.emptyMoldCount = emptyCount;

            var levelConfig = AssetDatabase.LoadAssetAtPath<Application.Configuration.LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            var assignments = GetLevelAssignments(level, levelConfig);
            int maxLayers = level.Molds.Count > 0 && level.Molds[0].layers != null && level.Molds[0].layers.Count > 0
                ? level.Molds.Select(b => b.layers?.Count ?? 0).Max()
                : 4;
            if (maxLayers < 4) maxLayers = 4;

            var result = OreSortSolver.Solve(assignments, maxLayers);
            if (result.IsSolvable)
            {
                level.parMoves = result.SolutionPath.Count;
                level.goodMoves = Mathf.RoundToInt(result.SolutionPath.Count * 1.4f);
                if (level.goodMoves < level.parMoves + 2) level.goodMoves = level.parMoves + 2;
                SetStatus($"Exported scene to Level {level.levelNumber} successfully. Solvable in {result.SolutionPath.Count} moves (Par auto-assigned).", MessageType.Info);
            }
            else
            {
                level.parMoves = 10;
                level.goodMoves = 15;
                SetStatus($"Exported scene to Level {level.levelNumber} successfully, but layout is UNSOLVABLE! Reset par to defaults.", MessageType.Warning);
            }

            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshLevelList();
        }
    }
}
