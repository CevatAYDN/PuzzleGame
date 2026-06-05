using NUnit.Framework;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Services;
using UnityEngine;

namespace PuzzleGame.Tests.Application.Services
{
    public class CoinWalletTests
    {
        private CoinWallet _sut;
        private EconomyConfig _config;

        [SetUp]
        public void Setup()
        {
            _config = ScriptableObject.CreateInstance<EconomyConfig>();
            _config.startingCoins = 100;
            _config.hintCost = 15;
            _config.undoCost = 10;
            PlayerPrefs.DeleteKey("PuzzleGame.CoinBalance");
        }

        [TearDown]
        public void Teardown()
        {
            PlayerPrefs.DeleteKey("PuzzleGame.CoinBalance");
            if (_config != null) Object.DestroyImmediate(_config);
        }

        [Test]
        public void Constructor_LoadsStartingBalanceFromConfig()
        {
            _sut = new CoinWallet(_config);
            Assert.That(_sut.Balance, Is.EqualTo(100));
        }

        [Test]
        public void Add_IncreasesBalance()
        {
            _sut = new CoinWallet(_config);
            _sut.Add(50, "test_reward");
            Assert.That(_sut.Balance, Is.EqualTo(150));
        }

        [Test]
        public void Add_ZeroOrNegative_NoChange()
        {
            _sut = new CoinWallet(_config);
            int before = _sut.Balance;
            _sut.Add(0, "zero");
            _sut.Add(-10, "negative");
            Assert.That(_sut.Balance, Is.EqualTo(before));
        }

        [Test]
        public void TrySpend_Affordable_DeductsAndReturnsTrue()
        {
            _sut = new CoinWallet(_config);
            bool result = _sut.TrySpend(30, "test_purchase");
            Assert.That(result, Is.True);
            Assert.That(_sut.Balance, Is.EqualTo(70));
        }

        [Test]
        public void TrySpend_NotAffordable_NoDeductionAndReturnsFalse()
        {
            _sut = new CoinWallet(_config);
            _config.startingCoins = 10;
            _sut = new CoinWallet(_config);
            bool result = _sut.TrySpend(50, "expensive_purchase");
            Assert.That(result, Is.False);
            Assert.That(_sut.Balance, Is.EqualTo(10));
        }

        [Test]
        public void TrySpend_NegativeAmount_Rejected()
        {
            _sut = new CoinWallet(_config);
            bool result = _sut.TrySpend(-1, "negative");
            Assert.That(result, Is.False);
            Assert.That(_sut.Balance, Is.EqualTo(100));
        }

        [Test]
        public void CanAfford_ReflectsBalance()
        {
            _sut = new CoinWallet(_config);
            Assert.That(_sut.CanAfford(50), Is.True);
            Assert.That(_sut.CanAfford(100), Is.True);
            Assert.That(_sut.CanAfford(101), Is.False);
        }

        [Test]
        public void OnBalanceChanged_FiresOnAdd()
        {
            _sut = new CoinWallet(_config);
            int? observed = null;
            _sut.OnBalanceChanged += newBalance => observed = newBalance;
            _sut.Add(25, "test");
            Assert.That(observed, Is.EqualTo(125));
        }

        [Test]
        public void OnBalanceChanged_FiresOnSpend()
        {
            _sut = new CoinWallet(_config);
            int? observed = null;
            _sut.OnBalanceChanged += newBalance => observed = newBalance;
            _sut.TrySpend(40, "test");
            Assert.That(observed, Is.EqualTo(60));
        }

        [Test]
        public void OnBalanceChanged_DoesNotFire_WhenSpendRejected()
        {
            _sut = new CoinWallet(_config);
            int fireCount = 0;
            _sut.OnBalanceChanged += _ => fireCount++;
            bool result = _sut.TrySpend(999, "too_much");
            Assert.That(result, Is.False);
            Assert.That(fireCount, Is.EqualTo(0));
        }

        [Test]
        public void Persistence_SurvivesReconstruction()
        {
            _sut = new CoinWallet(_config);
            _sut.Add(50, "test");
            _sut.TrySpend(20, "test");

            var reloaded = new CoinWallet(_config);
            Assert.That(reloaded.Balance, Is.EqualTo(130));
        }
    }
}
