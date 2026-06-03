using System;
using System.Collections.Generic;
using PuzzleGame.Domain;
using UnityEngine;
using PuzzleGame.Application.Configuration.FeatureSystem;

namespace PuzzleGame.Application.Configuration
{
    public enum Difficulty
    {
        Trivial,
        Easy,
        Medium,
        Hard,
        Expert
    }

    /// <summary>
    /// Pre-built bottle state for non-auto-generated levels.
    /// </summary>
    [Serializable]
    public class LevelBottleData
    {
        public List<LevelLayerData> layers = new List<LevelLayerData>();
        public bool isEmpty;
    }

    [Serializable]
    public class LevelLayerData
    {
        public Color color = Color.white;
        [Range(0f, 1f)] public float amount = 0.25f;
    }

    /// <summary>
    /// Static level descriptor. Inspector-driven, supports both auto-generated
    /// and pre-built bottle layouts. Star thresholds (par/good) drive reward UI.
    /// 
    /// NOTE: extends <see cref="ScriptableObject"/> because Unity inspector
    /// authoring of static level data is the design workflow. Domain purity is
    /// partially preserved — all *runtime* logic (validation, generation,
    /// simulation) operates on POCO representations produced by
    /// <see cref="LevelRepository"/> implementations.
    /// </summary>
    [CreateAssetMenu(menuName = "PuzzleGame/Level Data", fileName = "Level_NN")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        [Min(1)] public int levelNumber = 1;
        public Difficulty difficulty = Difficulty.Easy;

        [Header("Layout (auto-generate mode)")]
        [Min(BottleConstants.MinBottlesPerLevel)] public int bottleCount = 5;
        [Min(BottleConstants.MinEmptyBottles)] public int emptyBottleCount = 2;
        [Min(BottleConstants.MinColorsPerLevel)] public int colorCount = 4;
        [Range(1, BottleConstants.MaxLayers)] public int maxLayersPerBottle = BottleConstants.DefaultLayerCapacity;
        [Min(0)] public int randomSeed = 0;
        public bool autoGenerate = true;

        [Header("Pre-built (if autoGenerate = false)")]
        public List<LevelBottleData> bottles = new List<LevelBottleData>();

        [Header("Stars")]
        [Tooltip("3 stars if moves <= parMoves")]
        [Min(1)] public int parMoves = 10;
        [Tooltip("2 stars if moves <= goodMoves")]
        [Min(1)] public int goodMoves = 15;

        [Header("Display")]
        public Sprite previewImage;

        // ═══════════════════════════════════════════════════════════════════
        // MODULAR FEATURES (Data-Driven)
        // Feature data is loaded at runtime via LevelFeatureLoader from
        // Resources/Levels/<level>/features.asset. Storing inline here was
        // abandoned to keep LevelData lean and feature-modular.
        // ═══════════════════════════════════════════════════════════════════

        [Header("Features (Experimental)")]
        [Tooltip("Enable MultiLayerPour: pour all matching consecutive layers")]
        public bool enableMultiLayerPour = true;

        [Tooltip("Enable Chemical Reaction System")]
        public bool enableReactionSystem = false;

        [HideInInspector] public MultiLayerPourData multiLayerPourConfig;
        [HideInInspector] public ReactionSystemData reactionConfig;

        /// <summary>3 if moveCount &lt;= par, 2 if moveCount &lt;= good, else 1.</summary>
        public int CalculateStars(int moveCount)
        {
            if (moveCount <= parMoves) return 3;
            if (moveCount <= goodMoves) return 2;
            return 1;
        }

        /// <summary>3 star / 2 star / 1 star threshold values for UI.</summary>
        public (int par, int good) GetStarThresholds() => (parMoves, goodMoves);

        private void OnValidate()
        {
            if (goodMoves < parMoves) goodMoves = parMoves;
            if (emptyBottleCount < BottleConstants.MinEmptyBottles) emptyBottleCount = BottleConstants.MinEmptyBottles;
            if (emptyBottleCount > bottleCount - 1) emptyBottleCount = Mathf.Max(1, bottleCount - 1);
            if (colorCount > BottleConstants.MaxColorsPerLevel) colorCount = BottleConstants.MaxColorsPerLevel;
            if (colorCount < BottleConstants.MinColorsPerLevel) colorCount = BottleConstants.MinColorsPerLevel;
            if (maxLayersPerBottle < 1) maxLayersPerBottle = 1;
            if (maxLayersPerBottle > BottleConstants.MaxLayers) maxLayersPerBottle = BottleConstants.MaxLayers;
        }
    }
}
