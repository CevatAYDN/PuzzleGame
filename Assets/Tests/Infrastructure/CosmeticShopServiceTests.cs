using System;
using System.Collections.Generic;
using NUnit.Framework;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Tests.Fakes;
using UnityEngine;

namespace PuzzleGame.Tests.Infrastructure
{
    [TestFixture]
    public class CosmeticShopServiceTests
    {
        private const string OwnedPrefPrefix = "PuzzleGame.Cosmetic.Owned.";
        private const string EquippedPrefPrefix = "PuzzleGame.Cosmetic.Equipped.";

        private CosmeticConfig _config;
        private CosmeticShopService _sut;
        private FakeCoinWallet _wallet;

        [SetUp]
        public void Setup()
        {
            _config = ScriptableObject.CreateInstance<CosmeticConfig>();
            _config.items = new List<CosmeticItemData>
            {
                new CosmeticItemData { id = "skin_gold", cosmeticType = CosmeticType.MoldSkin, coinCost = 50 },
                new CosmeticItemData { id = "trail_fire", cosmeticType = CosmeticType.TrailEffect, coinCost = 100 },
                new CosmeticItemData { id = "cork_wood", cosmeticType = CosmeticType.CorkDesign, coinCost = 75 }
            };
            _config.defaultUnlockedIds = new[] { "skin_gold" };

            WipePrefs();
            _wallet = new FakeCoinWallet(200);
            _sut = new CosmeticShopService(_config);
        }

        [TearDown]
        public void Teardown()
        {
            WipePrefs();
            if (_config != null)
            {
                UnityEngine.Object.DestroyImmediate(_config);
            }
        }

        private void WipePrefs()
        {
            foreach (var item in _config.items)
            {
                PlayerPrefs.DeleteKey(OwnedPrefPrefix + item.id);
            }
            foreach (CosmeticType type in Enum.GetValues(typeof(CosmeticType)))
            {
                PlayerPrefs.DeleteKey(EquippedPrefPrefix + type);
            }
            PlayerPrefs.Save();
        }

        [Test]
        public void Constructor_LoadsDefaultUnlockedAsOwned()
        {
            Assert.That(_sut.IsOwned("skin_gold"), Is.True);
            Assert.That(_sut.IsOwned("trail_fire"), Is.False);
        }

        [Test]
        public void GetAllItems_ReturnsConfiguredItems()
        {
            var items = _sut.GetAllItems();
            Assert.That(items.Count, Is.EqualTo(3));
            Assert.That(items[0].id, Is.EqualTo("skin_gold"));
        }

        [Test]
        public void TryPurchase_SufficientCoins_SucceedsAndDeductsCoins()
        {
            bool inventoryChangedCalled = false;
            string changedItemId = null;
            _sut.OnInventoryChanged += id =>
            {
                inventoryChangedCalled = true;
                changedItemId = id;
            };

            bool success = _sut.TryPurchase("trail_fire", _wallet);

            Assert.That(success, Is.True);
            Assert.That(_sut.IsOwned("trail_fire"), Is.True);
            Assert.That(_wallet.Balance, Is.EqualTo(100)); // 200 - 100
            Assert.That(inventoryChangedCalled, Is.True);
            Assert.That(changedItemId, Is.EqualTo("trail_fire"));
        }

        [Test]
        public void TryPurchase_InsufficientCoins_Fails()
        {
            _wallet = new FakeCoinWallet(20);
            bool success = _sut.TryPurchase("trail_fire", _wallet);

            Assert.That(success, Is.False);
            Assert.That(_sut.IsOwned("trail_fire"), Is.False);
            Assert.That(_wallet.Balance, Is.EqualTo(20)); // No change
        }

        [Test]
        public void TryPurchase_AlreadyOwned_Fails()
        {
            // skin_gold is default unlocked
            bool success = _sut.TryPurchase("skin_gold", _wallet);
            Assert.That(success, Is.False);
        }

        [Test]
        public void Equip_OwnedItem_Succeeds()
        {
            _sut.Equip("skin_gold");
            Assert.That(_sut.GetEquipped(CosmeticType.MoldSkin), Is.EqualTo("skin_gold"));
        }

        [Test]
        public void Equip_UnownedItem_Fails()
        {
            _sut.Equip("trail_fire"); // Not purchased yet
            Assert.That(_sut.GetEquipped(CosmeticType.TrailEffect), Is.Null);
        }

        [Test]
        public void ResetToDefaults_ClearsPurchasedAndEquipped()
        {
            // Purchase and equip first
            _sut.TryPurchase("trail_fire", _wallet);
            _sut.Equip("trail_fire");
            _sut.Equip("skin_gold");

            Assert.That(_sut.IsOwned("trail_fire"), Is.True);
            Assert.That(_sut.GetEquipped(CosmeticType.TrailEffect), Is.EqualTo("trail_fire"));

            // Reset
            _sut.ResetToDefaults();

            Assert.That(_sut.IsOwned("trail_fire"), Is.False);
            Assert.That(_sut.IsOwned("skin_gold"), Is.True); // Default unlocked
            Assert.That(_sut.GetEquipped(CosmeticType.TrailEffect), Is.Null);
            Assert.That(_sut.GetEquipped(CosmeticType.MoldSkin), Is.Null);
        }
    }
}
