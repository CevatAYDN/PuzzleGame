using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame.Application.Configuration
{
    /// <summary>
    /// Types of rewards that can be earned via season/progression.
    /// </summary>
    [Serializable]
    public enum RewardType
    {
        Coins = 0,
        CosmeticItem = 1,
        PowerUp = 2,
        Hint = 3,
    }

    /// <summary>
    /// A single reward tier within a season.
    /// </summary>
    [Serializable]
    public class SeasonTierReward
    {
        [Tooltip("XP required to unlock this tier.")]
        [Min(1)] public int xpRequired = 100;

        [Tooltip("What type of reward is given.")]
        public RewardType rewardType = RewardType.Coins;

        [Tooltip("Amount (coins, hints) or item id (cosmetic).")]
        public string rewardValue = "50";

        [Tooltip("Optional cosmetic item id (only used when rewardType = CosmeticItem).")]
        public string cosmeticItemId = string.Empty;
    }

    /// <summary>
    /// Defines a single season with its XP curve and reward tiers.
    /// </summary>
    [Serializable]
    public class SeasonDef
    {
        public string seasonId = string.Empty;
        public string displayNameKey = string.Empty;

        [Tooltip("XP required to reach the first tier.")]
        [Min(1)] public int baseXp = 100;

        [Tooltip("XP increase per tier (tier n requires baseXp + tierIndex * xpPerTier).")]
        [Min(0)] public int xpPerTier = 50;

        [Tooltip("Number of reward tiers in this season.")]
        [Min(1)] public int tierCount = 10;

        [Tooltip("Per-tier rewards. Will be auto-generated from defaults if empty.")]
        public List<SeasonTierReward> rewards = new List<SeasonTierReward>();

        [Tooltip("Season start date (ISO 8601). Empty means always active.")]
        public string startDateIso = string.Empty;

        [Tooltip("Season end date (ISO 8601). Empty means never ends.")]
        public string endDateIso = string.Empty;
    }

    /// <summary>
    /// Configuration asset for seasons and progression.
    /// Define XP curves, tier rewards, and season schedules.
    /// Create via Assets/Create/PuzzleGame/SeasonConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "SeasonConfig", menuName = "PuzzleGame/SeasonConfig")]
    public class SeasonConfig : ScriptableObject
    {
        [Header("XP Configuration")]
        [Tooltip("Base XP earned for completing a level.")]
        [Min(1)] public int xpPerLevelComplete = 50;

        [Tooltip("Bonus XP per star earned (0-3 stars).")]
        [Min(0)] public int xpPerStar = 10;

        [Tooltip("Bonus XP for efficient pour (under par).")]
        [Min(0)] public int xpEfficiencyBonus = 25;

        [Header("Level Progression")]
        [Tooltip("XP required per player level. Player level = total XP / xpPerPlayerLevel.")]
        [Min(1)] public int xpPerPlayerLevel = 500;

        [Header("Seasons")]
        public List<SeasonDef> seasons = new List<SeasonDef>();
    }
}
