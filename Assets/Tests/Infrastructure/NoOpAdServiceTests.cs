using System;
using System.Collections.Generic;
using NUnit.Framework;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Infrastructure.Implementations;

namespace PuzzleGame.Tests.Infrastructure
{
    [TestFixture]
    public class NoOpAdServiceTests
    {
        private NoOpAdService _adService;

        [SetUp]
        public void SetUp()
        {
            _adService = new NoOpAdService();
        }

        [Test]
        public void Initialize_MarksAsInitialized()
        {
            Assert.IsFalse(_adService.IsInitialized);
            _adService.Initialize();
            Assert.IsTrue(_adService.IsInitialized);
        }

        [Test]
        public void ShowRewardedAd_InvokesComplete_True()
        {
            _adService.Initialize();
            bool? result = null;
            _adService.ShowRewardedAd(RewardedAdType.CoinDouble, success => result = success);

            Assert.IsTrue(result.HasValue);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public void ShowRewardedAd_RecordsWatchedType()
        {
            _adService.Initialize();
            _adService.ShowRewardedAd(RewardedAdType.HintBonus, _ => { });
            _adService.ShowRewardedAd(RewardedAdType.HintBonus, _ => { });
            _adService.ShowRewardedAd(RewardedAdType.UndoBonus, _ => { });

            Assert.Contains(RewardedAdType.HintBonus, new List<RewardedAdType>(_adService.WatchedTypes));
            Assert.Contains(RewardedAdType.UndoBonus, new List<RewardedAdType>(_adService.WatchedTypes));
            Assert.AreEqual(2, _adService.WatchedTypes.Count);
        }

        [Test]
        public void IsRewardedAdReady_TrueWhenInitialized()
        {
            Assert.IsFalse(_adService.IsRewardedAdReady(RewardedAdType.CoinDouble));
            _adService.Initialize();
            Assert.IsTrue(_adService.IsRewardedAdReady(RewardedAdType.CoinDouble));
        }

        [Test]
        public void ShowInterstitialAd_InvokesCallback()
        {
            _adService.Initialize();
            bool called = false;
            _adService.ShowInterstitialAd(() => called = true);
            Assert.IsTrue(called);
        }

        [Test]
        public void SetConsentState_UpdatesProperties()
        {
            _adService.SetConsentState(AdConsentState.Rejected, false);
            Assert.AreEqual(AdConsentState.Rejected, _adService.ConsentState);
            Assert.IsFalse(_adService.IsPersonalizedAdsEnabled);
        }

        [Test]
        public void PreloadAds_DoesNotThrow()
        {
            _adService.Initialize();
            Assert.DoesNotThrow(() => _adService.PreloadAds());
        }
    }
}
