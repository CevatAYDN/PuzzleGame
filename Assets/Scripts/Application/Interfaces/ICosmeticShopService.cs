using System;
using System.Collections.Generic;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Manages the cosmetic shop: purchasing, equipping, and querying owned items.
    /// Persists owned state across app restarts.
    /// </summary>
    public interface ICosmeticShopService
    {
        /// <summary>Fired when a cosmetic item is purchased or equipped.</summary>
        event Action<string> OnInventoryChanged;

        /// <summary>All items available in the shop (from CosmeticConfig).</summary>
        IReadOnlyList<CosmeticItemData> GetAllItems();

        /// <summary>Items the player has purchased (or received by default).</summary>
        IReadOnlyList<string> GetOwnedItemIds();

        /// <summary>Whether the player owns this item.</summary>
        bool IsOwned(string itemId);

        /// <summary>Try to purchase an item. Returns false if already owned or cannot afford.</summary>
        bool TryPurchase(string itemId, ICoinWallet wallet);

        /// <summary>Currently equipped item for a given cosmetic type (null if using default).</summary>
        string GetEquipped(CosmeticType type);

        /// <summary>Equip an owned cosmetic item for its type.</summary>
        void Equip(string itemId);

        /// <summary>Reset to defaults (all items unequipped, only defaultUnlockedIds owned).</summary>
        void ResetToDefaults();
    }
}
