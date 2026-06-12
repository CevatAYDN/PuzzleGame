using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Presentation.UI;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Batch-creates LevelData assets for the GDD-aligned 50-level campaign.
    /// GDD: L01-L25 = Crystal Mines, L26-L50 = Volcanic Forge, escalating
    /// difficulty (Trivial → Expert, 5 tiers × 10 levels, with biome seam at L25/26).
    /// All assets land in Assets/Resources/Levels/.
    /// Pre-existing assets (L01-L10 hand-tuned) are preserved — the tool only
    /// creates missing levels.
    /// </summary>
    public static class LevelDataBatchCreator
    {
        public const string LevelPath = "Assets/Resources/Levels";
        public const int TotalLevels = 50;

        // GDD-aligned difficulty escalation. Returns level parameters for
        // a given level number. Pure POCO so tests can verify progression
        // without touching the AssetDatabase.
        public static LevelParameters GetParametersForLevel(int levelNumber)
        {
            if (levelNumber < 1 || levelNumber > TotalLevels)
                return default;

            int t = (levelNumber - 1) / 10; // 0..4 (Trivial..Expert)
            int tier = levelNumber - t * 10; // 1..10 within each tier

            // Smooth escalation: difficulty tier + intra-tier ramp on colors.
            // Within a tier, color count grows by 1 from level 1 to 5, then plateaus.
            // Mold count grows by 1 at the tier boundary.
            Difficulty diff = t switch
            {
                0 => Difficulty.Trivial,
                1 => Difficulty.Easy,
                2 => Difficulty.Medium,
                3 => Difficulty.Hard,
                _ => Difficulty.Expert,
            };

            int moldCount = t switch
            {
                0 => 3,  // Trivial
                1 => 4,  // Easy
                2 => 5,  // Medium
                3 => 7,  // Hard
                _ => 9,  // Expert
            };

            int baseColors = t switch
            {
                0 => 2,
                1 => 3,
                2 => 4,
                3 => 5,
                _ => 6,
            };
            // Intra-tier color ramp: +1 every 2 levels, capped.
            int colorRamp = (tier - 1) / 2;
            int colorCount = Mathf.Min(baseColors + colorRamp, ForgeConstants.MaxColorsPerLevel);

            // maxLayersPerMold: 3..4 (modest growth — solver depth matters more than layer count)
            int maxLayers = t switch
            {
                0 => 3,
                1 => 3,
                2 => 4,
                3 => 4,
                _ => 4,
            };

            int emptyMolds = 1; // minimum for solvability

            // Move thresholds: par/good = base * (moldCount / 3) * (colorCount / 3)
            // Approximation: ~1.5 moves per mold-color pair, with breathing room.
            int par = Mathf.Max(3, Mathf.RoundToInt(moldCount * colorCount * 0.9f));
            int good = par + Mathf.Max(2, par / 2);

            return new LevelParameters(
                levelNumber: levelNumber,
                difficulty: diff,
                moldCount: moldCount,
                emptyMoldCount: emptyMolds,
                colorCount: colorCount,
                maxLayersPerMold: maxLayers,
                randomSeed: levelNumber * 1337,
                parMoves: par,
                goodMoves: good,
                biome: LevelBiomeClassifier.GetBiome(levelNumber));
        }

        /// <summary>
        /// Creates any missing level assets in LevelPath. Existing assets
        /// (e.g. L01-L10 hand-tuned) are left untouched.
        /// </summary>
        public static void CreateAllLevels()
        {
            try
            {
                if (!System.IO.Directory.Exists(LevelPath))
                    System.IO.Directory.CreateDirectory(LevelPath);

                var created = new List<LevelData>();
                int skipped = 0;
                for (int i = 1; i <= TotalLevels; i++)
                {
                    EditorUtility.DisplayProgressBar("PuzzleGame Batch Creator",
                        $"Creating Level {i:D2} of {TotalLevels}...", i / (float)TotalLevels);

                    string fileName = $"Level_{i:D2}";
                    string fullPath = $"{LevelPath}/{fileName}.asset";

                    var existing = AssetDatabase.LoadAssetAtPath<LevelData>(fullPath);
                    if (existing != null)
                    {
                        skipped++;
                        continue;
                    }

                    var p = GetParametersForLevel(i);
                    var level = ScriptableObject.CreateInstance<LevelData>();
                    if (level == null)
                    {
                        Debug.LogError($"[LevelDataBatchCreator] Failed to create LevelData instance for level {i}.");
                        continue;
                    }

                    level.levelNumber = p.LevelNumber;
                    level.difficulty = p.Difficulty;
                    level.MoldCount = p.MoldCount;
                    level.colorCount = p.ColorCount;
                    level.emptyMoldCount = p.EmptyMoldCount;
                    level.maxLayersPerMold = p.MaxLayersPerMold;
                    level.randomSeed = p.RandomSeed;
                    level.parMoves = p.ParMoves;
                    level.goodMoves = p.GoodMoves;
                    level.autoGenerate = true; // runtime ProceduralLevelGenerator populates Molds

                    AssetDatabase.CreateAsset(level, fullPath);
                    created.Add(level);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[LevelDataBatchCreator] Created {created.Count} new level asset(s), skipped {skipped} existing in {LevelPath}.");
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

    /// <summary>
    /// GDD-aligned level parameters for a given level number. Pure POCO
    /// (no Unity types) so it can be unit-tested without Unity.
    /// </summary>
    public readonly struct LevelParameters
    {
        public int LevelNumber { get; }
        public Difficulty Difficulty { get; }
        public int MoldCount { get; }
        public int EmptyMoldCount { get; }
        public int ColorCount { get; }
        public int MaxLayersPerMold { get; }
        public int RandomSeed { get; }
        public int ParMoves { get; }
        public int GoodMoves { get; }
        public Biome Biome { get; }

        public LevelParameters(
            int levelNumber, Difficulty difficulty, int moldCount, int emptyMoldCount,
            int colorCount, int maxLayersPerMold, int randomSeed, int parMoves, int goodMoves,
            Biome biome)
        {
            LevelNumber = levelNumber;
            Difficulty = difficulty;
            MoldCount = moldCount;
            EmptyMoldCount = emptyMoldCount;
            ColorCount = colorCount;
            MaxLayersPerMold = maxLayersPerMold;
            RandomSeed = randomSeed;
            ParMoves = parMoves;
            GoodMoves = goodMoves;
            Biome = biome;
        }
    }
}
