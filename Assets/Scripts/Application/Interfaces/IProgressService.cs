using System.Collections.Generic;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Tracks player XP, player level, season progress, and reward claiming.
    /// PlayerPrefs-backed persistence.
    /// </summary>
    public interface IProgressService
    {
        // --- XP & Level ---

        /// <summary>Total XP earned across all time.</summary>
        int TotalXp { get; }

        /// <summary>Current player level (computed from total XP).</summary>
        int PlayerLevel { get; }

        /// <summary>XP earned in the current season.</summary>
        int SeasonXp { get; }

        /// <summary>XP remaining to reach the next season tier.</summary>
        int SeasonXpToNextTier { get; }

        /// <summary>Current season tier index (0-based, -1 if no season active).</summary>
        int CurrentTierIndex { get; }

        /// <summary>Add XP from a level completion.</summary>
        void AddLevelXp(int levelIndex, int stars, bool wasEfficient);

        /// <summary>Direct XP addition (e.g. daily bonus).</summary>
        void AddXp(int amount);

        // --- Seasons ---

        /// <summary>Active season definition, or null if none active.</summary>
        SeasonDef ActiveSeason { get; }

        /// <summary>Whether a season is currently active (date check).</summary>
        bool IsSeasonActive { get; }

        /// <summary>All defined seasons from config.</summary>
        IReadOnlyList<SeasonDef> GetAllSeasons();

        // --- Rewards ---

        /// <summary>IDs of tiers whose rewards have been claimed.</summary>
        IReadOnlyCollection<int> GetClaimedTierIds();

        /// <summary>Claim a season tier reward. Returns false if already claimed or tier locked.</summary>
        bool ClaimTierReward(int tierIndex, ICoinWallet wallet);

        /// <summary>Check if a tier is claimable (XP met + not claimed).</summary>
        bool CanClaimTier(int tierIndex);

        /// <summary>Reset all progress (debug/admin).</summary>
        void ResetAll();
    }
}
