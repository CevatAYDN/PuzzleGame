using System;
using System.Collections.Generic;

namespace PuzzleGame.Application.Configuration
{
    /// <summary>
    /// Full snapshot of all player progress for cloud save/restore.
    /// Serialized to JSON, signed with HMAC, stored as a single file.
    /// </summary>
    [Serializable]
    public class CloudSaveData
    {
        public int version = 1;
        public long savedAtUnix;

        // Profile
        public int totalXp;
        public int playerLevel;

        // Coins (snapshot — authoritative source remains CoinWallet)
        public int coinBalance;

        // Leaderboard: levelIndex → (score, pours, timestamp)
        public List<CloudLeaderboardEntry> leaderboardEntries = new List<CloudLeaderboardEntry>();

        // Season progress
        public int seasonXp;
        public List<int> claimedTierIds = new List<int>();

        // Cosmetic shop
        public List<string> ownedCosmeticIds = new List<string>();
        public List<CloudEquippedCosmetic> equippedCosmetics = new List<CloudEquippedCosmetic>();

        // Level completion (stars per level)
        public List<CloudLevelCompletion> levelCompletions = new List<CloudLevelCompletion>();
    }

    [Serializable]
    public class CloudLeaderboardEntry
    {
        public int levelIndex;
        public int bestScore;
        public int bestPourCount;
        public long recordedAtUnix;
    }

    [Serializable]
    public class CloudEquippedCosmetic
    {
        public int cosmeticType; // CosmeticType enum value
        public string itemId;
    }

    [Serializable]
    public class CloudLevelCompletion
    {
        public int levelIndex;
        public int stars;
        public bool completed;
    }
}
