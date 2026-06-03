using System;
using System.Collections.Generic;
using UnityEngine;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Configuration.FeatureSystem
{
    /// <summary>
    /// Modular feature system for level extensions.
    /// Each feature can be enabled/disabled per level with custom parameters.
    ///
    /// CURRENT FEATURES (wired with runtime service + event publishing):
    ///   - MultiLayerPour  : PourService (Application/Services/PourService.cs)
    ///   - ReactionSystem  : ReactionService (Application/Services/ReactionService.cs)
    /// </summary>
    [Flags]
    public enum LevelFeatureType
    {
        None = 0,
        MultiLayerPour = 1 << 0,      // Pour all matching consecutive layers at once
        ReactionSystem = 1 << 1,      // Chemical reactions between colors
        All = ~0
    }

    /// <summary>
    /// Base class for feature-specific data.
    /// Each feature type has its own config class extending this.
    /// </summary>
    [Serializable]
    public abstract class LevelFeatureData
    {
        public LevelFeatureType featureType;
        public bool isEnabled;

        public abstract LevelFeatureType GetFeatureType();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MULTI-LAYER POUR FEATURE
    // ═══════════════════════════════════════════════════════════════════════

    [Serializable]
    public class MultiLayerPourData : LevelFeatureData
    {
        public bool pourAllMatching = true;        // Pour all consecutive same colors
        public bool pourConsecutiveOnly = true;    // Only pour if 2+ consecutive
        public int minConsecutiveForPour = 2;      // Minimum layers to pour together

        public override LevelFeatureType GetFeatureType() => LevelFeatureType.MultiLayerPour;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // REACTION SYSTEM FEATURE
    // ═══════════════════════════════════════════════════════════════════════

    [Serializable]
    public class ReactionRule
    {
        [Tooltip("First color that triggers reaction")]
        public LiquidColor colorA = LiquidColor.Red;

        [Tooltip("Second color that triggers reaction")]
        public LiquidColor colorB = LiquidColor.Blue;

        [Tooltip("Result color after reaction (for Transform type)")]
        public LiquidColor resultColor = LiquidColor.Green;

        [Tooltip("Reaction type: 0=None, 1=Bubble, 2=Explode, 3=Transform")]
        public ReactionType reactionType = ReactionType.None;

        [Tooltip("Custom reaction effect prefab (optional)")]
        public GameObject effectPrefab;

        public enum ReactionType
        {
            None,
            Bubble,      // Creates bubbles, harmless
            Explode,     // Bottle explodes, level fails
            Transform    // Colors transform to resultColor
        }
    }

    [Serializable]
    public class ReactionSystemData : LevelFeatureData
    {
        public bool enableReactions = true;
        public List<ReactionRule> reactionRules = new List<ReactionRule>();

        public override LevelFeatureType GetFeatureType() => LevelFeatureType.ReactionSystem;
    }
}
