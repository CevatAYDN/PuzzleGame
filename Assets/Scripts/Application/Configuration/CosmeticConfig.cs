using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame.Application.Configuration
{
    /// <summary>
    /// Types of cosmetic items that can be purchased in the shop.
    /// </summary>
    public enum CosmeticType
    {
        None = 0,
        MoldSkin = 1,
        TrailEffect = 2,
        CorkDesign = 3,
        ParticleEffect = 4,
    }

    /// <summary>
    /// A single cosmetic item available for purchase.
    /// </summary>
    [Serializable]
    public class CosmeticItemData
    {
        public string id = string.Empty;
        public CosmeticType cosmeticType = CosmeticType.None;
        public string displayNameKey = string.Empty;
        public string descriptionKey = string.Empty;
        [Min(0)] public int coinCost = 100;
        public Sprite previewIcon;
    }

    /// <summary>
    /// Configuration asset for the cosmetic shop.
    /// Defines all purchasable items and their prices.
    /// Create via Assets/Create/PuzzleGame/CosmeticConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "CosmeticConfig", menuName = "PuzzleGame/CosmeticConfig")]
    public class CosmeticConfig : ScriptableObject
    {
        [Header("Shop Items")]
        public List<CosmeticItemData> items = new List<CosmeticItemData>();

        [Header("Economy")]
        [Tooltip("Default items given to new players for free.")]
        public string[] defaultUnlockedIds = Array.Empty<string>();

        /// <summary>
        /// Find a cosmetic item by its unique id. Returns null if not found.
        /// </summary>
        public CosmeticItemData GetItem(string id)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i].id, id, StringComparison.Ordinal))
                    return items[i];
            }
            return null;
        }
    }
}
