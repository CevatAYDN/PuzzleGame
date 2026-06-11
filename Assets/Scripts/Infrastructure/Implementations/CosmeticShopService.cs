using System;
using System.Collections.Generic;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    public sealed class CosmeticShopService : ICosmeticShopService
    {
        private const string LogTag = "[CosmeticShop]";
        private const string OwnedPrefPrefix = "PuzzleGame.Cosmetic.Owned.";
        private const string EquippedPrefPrefix = "PuzzleGame.Cosmetic.Equipped.";

        private readonly CosmeticConfig _config;
        private readonly HashSet<string> _owned = new HashSet<string>();
        private readonly Dictionary<CosmeticType, string> _equipped = new Dictionary<CosmeticType, string>();

        public event Action<string> OnInventoryChanged;

        public CosmeticShopService(CosmeticConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            LoadState();
        }

        public IReadOnlyList<CosmeticItemData> GetAllItems()
        {
            return _config.items.AsReadOnly();
        }

        public IReadOnlyList<string> GetOwnedItemIds()
        {
            var result = new List<string>(_owned);
            return result.AsReadOnly();
        }

        public bool IsOwned(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && _owned.Contains(itemId);
        }

        public bool TryPurchase(string itemId, ICoinWallet wallet)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            if (_owned.Contains(itemId)) return false;

            var item = _config.GetItem(itemId);
            if (item == null) return false;

            if (wallet == null || !wallet.CanAfford(item.coinCost)) return false;

            if (!wallet.TrySpend(item.coinCost, $"cosmetic_{itemId}")) return false;

            _owned.Add(itemId);
            PlayerPrefs.SetInt(OwnedPrefPrefix + itemId, 1);
            PlayerPrefs.Save();
            MoldLogger.LogInfo($"{LogTag} Purchased: {itemId} for {item.coinCost} coins.");
            OnInventoryChanged?.Invoke(itemId);
            return true;
        }

        public string GetEquipped(CosmeticType type)
        {
            return _equipped.TryGetValue(type, out var id) ? id : null;
        }

        public void Equip(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            if (!_owned.Contains(itemId)) return;

            var item = _config.GetItem(itemId);
            if (item == null) return;

            _equipped[item.cosmeticType] = itemId;
            PlayerPrefs.SetString(EquippedPrefPrefix + item.cosmeticType, itemId);
            PlayerPrefs.Save();
            MoldLogger.LogInfo($"{LogTag} Equipped: {itemId} for type {item.cosmeticType}.");
            OnInventoryChanged?.Invoke(itemId);
        }

        public void ResetToDefaults()
        {
            _owned.Clear();
            _equipped.Clear();

            foreach (var id in _config.defaultUnlockedIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    _owned.Add(id);
                    PlayerPrefs.SetInt(OwnedPrefPrefix + id, 1);
                }
            }

            foreach (CosmeticType type in Enum.GetValues(typeof(CosmeticType)))
            {
                if (type == CosmeticType.None) continue;
                PlayerPrefs.DeleteKey(EquippedPrefPrefix + type);
            }

            PlayerPrefs.Save();
            MoldLogger.LogInfo($"{LogTag} Reset to defaults.");
            OnInventoryChanged?.Invoke(null);
        }

        private void LoadState()
        {
            foreach (var item in _config.items)
            {
                if (PlayerPrefs.GetInt(OwnedPrefPrefix + item.id, 0) == 1)
                {
                    _owned.Add(item.id);
                }
            }

            foreach (CosmeticType type in Enum.GetValues(typeof(CosmeticType)))
            {
                if (type == CosmeticType.None) continue;
                var equipped = PlayerPrefs.GetString(EquippedPrefPrefix + type, string.Empty);
                if (!string.IsNullOrEmpty(equipped))
                {
                    _equipped[type] = equipped;
                }
            }

            MoldLogger.LogInfo($"{LogTag} Loaded state: {_owned.Count} owned, {_equipped.Count} equipped.");
        }
    }
}
