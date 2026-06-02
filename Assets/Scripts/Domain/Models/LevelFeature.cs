using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame.Domain.Models.FeatureSystem
{
    /// <summary>
    /// Modular feature system for level extensions.
    /// Each feature can be enabled/disabled per level with custom parameters.
    /// </summary>
    [Flags]
    public enum LevelFeatureType
    {
        None = 0,
        
        // Pour mechanics
        MultiLayerPour = 1 << 0,      // Pour all matching consecutive layers at once
        ReactionSystem = 1 << 1,      // Chemical reactions between colors
        
        // Interactive elements
        KeyAndLock = 1 << 2,          // Keys hidden in bottles, locks in scene
        BreakableBottles = 1 << 3,    // Bottles that explode when certain conditions met
        
        // Advanced mechanics
        LimitedMoves = 1 << 4,        // Hard move limit (different from par)
        BonusObjectives = 1 << 5,      // Collect items, time challenges
        
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

    // ═══════════════════════════════════════════════════════════════════════
    // KEY AND LOCK FEATURE
    // ═══════════════════════════════════════════════════════════════════════

    [Serializable]
    public class KeyItemData
    {
        public string keyId = "key_1";
        public Color keyColor = Color.yellow;
        public int layerIndex = 0;           // Which layer contains the key
        public int bottleIndex = 0;          // Which bottle contains the key
        public bool isCollected = false;
    }

    [Serializable]
    public class LockData
    {
        public string lockId = "lock_1";
        public Color lockColor = Color.gray;
        public string requiredKeyId = "key_1";  // Which key opens this
        public Vector3 position;
        public bool isUnlocked = false;
        
        // Lock type: 0=Door, 1=Secret Area, 2=Extra Bottle
        public LockType lockType = LockType.Door;
        
        public enum LockType
        {
            Door,
            SecretArea,
            ExtraBottle,
            EndLevel
        }
    }

    [Serializable]
    public class KeyAndLockData : LevelFeatureData
    {
        public bool enableKeysAndLocks = true;
        public List<KeyItemData> keys = new List<KeyItemData>();
        public List<LockData> locks = new List<LockData>();
        
        // Reward for unlocking: extra moves, bonus points, etc.
        public int bonusMovesOnUnlock = 2;
        
        public override LevelFeatureType GetFeatureType() => LevelFeatureType.KeyAndLock;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BREAKABLE BOTTLE FEATURE
    // ═══════════════════════════════════════════════════════════════════════

    [Serializable]
    public class BreakableBottleData : LevelFeatureData
    {
        public bool enableBreakableBottles = true;
        
        // Conditions that cause bottle to break
        public BreakCondition breakCondition = BreakCondition.ExplodeOnShake;
        
        public int shakeThreshold = 3;      // Shakes needed to break
        public int maxLayersToBreak = 4;     // Layers needed to trigger break
        
        // Reward/penalty
        public int penaltyMoves = 5;         // Extra moves penalty
        public bool levelFailedOnBreak = true; // Does breaking fail the level?
        
        public enum BreakCondition
        {
            ExplodeOnShake,     // Shake bottle (click rapidly) to explode
            Overfill,           // Fill beyond max to explode
            ColorCount          // Too many colors triggers explosion
        }
        
        public override LevelFeatureType GetFeatureType() => LevelFeatureType.BreakableBottles;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LIMITED MOVES FEATURE
    // ═══════════════════════════════════════════════════════════════════════

    [Serializable]
    public class LimitedMovesData : LevelFeatureData
    {
        public bool enableHardLimit = false;
        public int hardMoveLimit = 15;
        
        // Progressive difficulty: limit decreases after each pour
        public bool progressiveLimit = false;
        public int movesDecreasePerPour = 1;
        
        public override LevelFeatureType GetFeatureType() => LevelFeatureType.LimitedMoves;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BONUS OBJECTIVES FEATURE
    // ═══════════════════════════════════════════════════════════════════════

    [Serializable]
    public class BonusObjectiveData
    {
        public string objectiveId = "bonus_1";
        public ObjectiveType type = ObjectiveType.CollectDrops;
        public int targetCount = 5;
        public int currentCount = 0;
        
        public enum ObjectiveType
        {
            CollectDrops,       // Collect dropped liquid
            PourFromHeight,    // Pour from elevated bottles
            MinimizeSpills,     // Don't spill more than X
            SpeedRun,           // Complete in time
            ComboPour           // Chain X pours without selecting new bottle
        }
    }

    [Serializable]
    public class BonusObjectivesData : LevelFeatureData
    {
        public bool enableBonusObjectives = false;
        public List<BonusObjectiveData> objectives = new List<BonusObjectiveData>();
        public int bonusStarsOnComplete = 1;  // Extra star for bonus completion
        
        public override LevelFeatureType GetFeatureType() => LevelFeatureType.BonusObjectives;
    }
}
