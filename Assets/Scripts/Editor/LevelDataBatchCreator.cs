using UnityEditor;
using UnityEngine;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Batch-creates LevelData assets (1-N) with progressive difficulty.
    /// Each level gets a unique seed, increasing color/bottle count, and tighter move thresholds.
    /// All assets land in Assets/Resources/Levels/.
    /// </summary>
    public static class LevelDataBatchCreator
    {
        public const string LevelPath = "Assets/Resources/Levels";

        public static void Create100Levels()
        {
            try
            {
                if (!System.IO.Directory.Exists(LevelPath))
                    System.IO.Directory.CreateDirectory(LevelPath);

                var levels = new System.Collections.Generic.List<LevelData>();
                for (int i = 1; i <= 100; i++)
                {
                    EditorUtility.DisplayProgressBar("PuzzleGame Batch Creator", $"Creating Level {i:D2} of 100...", i / 100f);

                    string fileName = $"Level_{i:D2}";
                    string fullPath = $"{LevelPath}/{fileName}.asset";

                    var existing = AssetDatabase.LoadAssetAtPath<LevelData>(fullPath);
                    if (existing != null)
                    {
                        levels.Add(existing);
                        continue;
                    }

                    var level = ScriptableObject.CreateInstance<LevelData>();
                    if (level == null)
                    {
                        Debug.LogError($"[LevelDataBatchCreator] Failed to create LevelData ScriptableObject instance for level {i}.");
                        continue;
                    }

                    level.levelNumber = i;
                    level.randomSeed = i * 1337;

                    // Progressive difficulty
                    if (i <= 10)
                    {
                        level.difficulty = Difficulty.Trivial;
                        level.bottleCount = 3;
                        level.colorCount = 2;
                        level.emptyBottleCount = 1;
                        level.maxLayersPerBottle = 3;
                        level.parMoves = 5;
                        level.goodMoves = 8;
                    }
                    else if (i <= 30)
                    {
                        level.difficulty = Difficulty.Easy;
                        level.bottleCount = 5;
                        level.colorCount = 3;
                        level.emptyBottleCount = 2;
                        level.maxLayersPerBottle = 4;
                        level.parMoves = 10;
                        level.goodMoves = 15;
                    }
                    else if (i <= 60)
                    {
                        level.difficulty = Difficulty.Medium;
                        level.bottleCount = 7;
                        level.colorCount = 5;
                        level.emptyBottleCount = 2;
                        level.maxLayersPerBottle = 5;
                        level.parMoves = 18;
                        level.goodMoves = 25;
                    }
                    else if (i <= 85)
                    {
                        level.difficulty = Difficulty.Hard;
                        level.bottleCount = 9;
                        level.colorCount = 7;
                        level.emptyBottleCount = 2;
                        level.maxLayersPerBottle = 6;
                        level.parMoves = 28;
                        level.goodMoves = 38;
                    }
                    else
                    {
                        level.difficulty = Difficulty.Expert;
                        level.bottleCount = 12;
                        level.colorCount = 10;
                        level.emptyBottleCount = 2;
                        level.maxLayersPerBottle = 6;
                        level.parMoves = 40;
                        level.goodMoves = 55;
                    }

                    AssetDatabase.CreateAsset(level, fullPath);
                    levels.Add(level);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Created/verified {levels.Count} level assets in {LevelPath}.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LevelDataBatchCreator] Error batch creating levels: {ex}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

    }
}
