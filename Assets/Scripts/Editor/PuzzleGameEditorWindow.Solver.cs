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
    public partial class PuzzleGameEditorWindow
    {
        // ── Solver & Scene Helpers ──────────────────────────────────────────

        private static List<List<LiquidLayer>> GetLevelAssignments(LevelData level, Application.Configuration.LevelConfig levelConfig)
        {
            if (level == null) return new List<List<LiquidLayer>>();
            if (level.autoGenerate)
            {
                var generator = new DifficultyBasedLevelGenerator();
                Color[] palette = levelConfig != null && levelConfig.palette.Length > 0 ? levelConfig.palette : SceneBuilder.DefaultPalette;
                var domainPalette = new DomainColor[palette.Length];
                for (int i = 0; i < palette.Length; i++)
                    domainPalette[i] = ColorAdapter.FromUnity(palette[i]);

                return generator.Generate(
                    level.bottleCount,
                    level.maxLayersPerBottle,
                    level.emptyBottleCount,
                    domainPalette,
                    level.difficulty,
                    level.randomSeed);
            }
            else
            {
                var assignments = new List<List<LiquidLayer>>();
                if (level.bottles != null)
                {
                    foreach (var bottle in level.bottles)
                    {
                        var layers = new List<LiquidLayer>();
                        if (!bottle.isEmpty && bottle.layers != null)
                        {
                            foreach (var layer in bottle.layers)
                            {
                                layers.Add(new LiquidLayer(ColorAdapter.FromUnity(layer.color), layer.amount));
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
                result[i] = ColorAdapter.FromUnity(colors[i]);
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
            int maxLayers = levelData.autoGenerate ? levelData.maxLayersPerBottle : 4;

            var result = LiquidSortSolver.Solve(assignments, maxLayers);
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
            newLevel.bottleCount = sourceLevel.bottleCount;
            newLevel.emptyBottleCount = sourceLevel.emptyBottleCount;
            newLevel.colorCount = sourceLevel.colorCount;
            newLevel.maxLayersPerBottle = sourceLevel.maxLayersPerBottle;
            newLevel.randomSeed = copyNumber * 1337;
            newLevel.autoGenerate = sourceLevel.autoGenerate;

            // Copy pre-built bottles if not auto-generating
            if (!sourceLevel.autoGenerate && sourceLevel.bottles != null)
            {
                newLevel.bottles = new List<LevelBottleData>();
                foreach (var bottle in sourceLevel.bottles)
                {
                    var copy = new LevelBottleData
                    {
                        isEmpty = bottle.isEmpty,
                        layers = new List<LevelLayerData>()
                    };
                    foreach (var layer in bottle.layers)
                    {
                        copy.layers.Add(new LevelLayerData
                        {
                            color = layer.color,
                            amount = layer.amount
                        });
                    }
                    newLevel.bottles.Add(copy);
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
                    int maxLayers = levelData.autoGenerate ? levelData.maxLayersPerBottle : 4;

                    var result = LiquidSortSolver.Solve(assignments, maxLayers);

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
                    int maxLayers = levelData.autoGenerate ? levelData.maxLayersPerBottle : 4;
                    var result = LiquidSortSolver.Solve(assignments, maxLayers);

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
                            levelData.bottleCount,
                            levelData.maxLayersPerBottle,
                            levelData.emptyBottleCount,
                            ConvertPalette(levelConfig != null && levelConfig.palette.Length > 0 ? levelConfig.palette : SceneBuilder.DefaultPalette),
                            levelData.difficulty,
                            seed);

                        var tempResult = LiquidSortSolver.Solve(tempAssignments, maxLayers);
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
                    int maxLayers = levelData.autoGenerate ? levelData.maxLayersPerBottle : 4;
                    var result = LiquidSortSolver.Solve(assignments, maxLayers);

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

            // 1. Clear existing bottles
            SceneBuilder.RemoveBottles();

            // 2. Load level Config
            var levelConfig = AssetDatabase.LoadAssetAtPath<Application.Configuration.LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            var assignments = GetLevelAssignments(level, levelConfig);

            // 3. Compute positions and instantiate bottles
            int count = assignments.Count;
            var layout = SceneBuilder.BottleLayout.Grid;
            Vector3 center = Vector3.zero;
            var positions = SceneBuilder.ComputePositions(layout, count, center);

            for (int i = 0; i < count; i++)
            {
                var layers = assignments[i];
                var colors = new Color[layers.Count];
                for (int j = 0; j < layers.Count; j++)
                    colors[j] = ColorAdapter.ToUnity(layers[j].Color);

                // Build with layers
                var bottleCfg = SceneBuilder.BottleConfig.WithLayers(
                    positions[i],
                    layers,
                    SceneBuilder.ShaderVariant.Premium,
                    $"Bottle_{i:D2}");

                SceneBuilder.CreateBottle(bottleCfg);
            }

            Undo.CollapseUndoOperations(undoGroup);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            SetStatus($"Loaded Level {level.levelNumber} into active scene ({count} bottles).", MessageType.Info);
        }

        private void ExportSceneToLevel(LevelData level)
        {
            if (level == null)
            {
                SetStatus("No level selected to export to.", MessageType.Warning);
                return;
            }

            var bottles = FindObjectsByType<BottleController>(FindObjectsInactive.Include);
            if (bottles.Length == 0)
            {
                SetStatus("No bottles found in the scene to export.", MessageType.Warning);
                return;
            }

            var sortedBottles = bottles
                .OrderByDescending(b => b.transform.position.z)
                .ThenBy(b => b.transform.position.x)
                .ToArray();

            level.autoGenerate = false;
            level.bottleCount = sortedBottles.Length;
            level.bottles.Clear();

            int emptyCount = 0;

            foreach (var bottle in sortedBottles)
            {
                var bottleData = new LevelBottleData();
                bottleData.isEmpty = bottle.IsEmpty;
                if (bottleData.isEmpty)
                {
                    emptyCount++;
                }

                bottleData.layers = new List<LevelLayerData>();
                if (!bottle.IsEmpty && bottle.State != null && bottle.State.Layers != null)
                {
                    foreach (var layer in bottle.State.Layers)
                    {
                        var layerData = new LevelLayerData();
                        layerData.color = ColorAdapter.ToUnity(layer.Color);
                        layerData.amount = layer.Amount;
                        bottleData.layers.Add(layerData);
                    }
                }
                level.bottles.Add(bottleData);
            }

            level.emptyBottleCount = emptyCount;

            var levelConfig = AssetDatabase.LoadAssetAtPath<Application.Configuration.LevelConfig>($"{DataAssetCreator.DataPath}/LevelConfig.asset");
            var assignments = GetLevelAssignments(level, levelConfig);
            int maxLayers = level.bottles.Count > 0 && level.bottles[0].layers != null && level.bottles[0].layers.Count > 0
                ? level.bottles.Select(b => b.layers?.Count ?? 0).Max()
                : 4;
            if (maxLayers < 4) maxLayers = 4;

            var result = LiquidSortSolver.Solve(assignments, maxLayers);
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
