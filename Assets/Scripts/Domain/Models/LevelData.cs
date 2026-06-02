using System;
using System.Collections.Generic;
using UnityEngine;
using PuzzleGame.Domain.Models.FeatureSystem;

namespace PuzzleGame.Domain.Models
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
    /// </summary>
    [CreateAssetMenu(menuName = "PuzzleGame/Level Data", fileName = "Level_NN")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        [Min(1)] public int levelNumber = 1;
        public Difficulty difficulty = Difficulty.Easy;

        [Header("Layout (auto-generate mode)")]
        [Min(2)] public int bottleCount = 5;
        [Min(1)] public int emptyBottleCount = 2;
        [Min(2)] public int colorCount = 4;
        [Min(1)] public int maxLayersPerBottle = 4;
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
        // ═══════════════════════════════════════════════════════════════════
        
        [Header("Features (Experimental)")]
        [Tooltip("Enable MultiLayerPour: pour all matching consecutive layers")]
        public bool enableMultiLayerPour = true;
        
        [Tooltip("Enable Chemical Reaction System")]
        public bool enableReactionSystem = false;
        
        [Tooltip("Enable Key and Lock mechanics")]
        public bool enableKeyAndLock = false;
        
        [Tooltip("Enable Breakable Bottles")]
        public bool enableBreakableBottles = false;
        
        // Feature-specific configurations
        [HideInInspector] public MultiLayerPourData multiLayerPourConfig;
        [HideInInspector] public ReactionSystemData reactionConfig;
        [HideInInspector] public KeyAndLockData keyAndLockConfig;
        [HideInInspector] public BreakableBottleData breakableBottleConfig;
        [HideInInspector] public LimitedMovesData limitedMovesConfig;
        [HideInInspector] public BonusObjectivesData bonusObjectivesConfig;

        /// <summary>3 if moveCount <= par, 2 if moveCount <= good, else 1.</summary>
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
            // goodMoves must be >= parMoves
            if (goodMoves < parMoves) goodMoves = parMoves;
            // emptyBottleCount must be reasonable
            if (emptyBottleCount < 1) emptyBottleCount = 1;
            if (emptyBottleCount > bottleCount - 1) emptyBottleCount = Mathf.Max(1, bottleCount - 1);
            if (colorCount > 12) colorCount = 12;
        }
    }
}
