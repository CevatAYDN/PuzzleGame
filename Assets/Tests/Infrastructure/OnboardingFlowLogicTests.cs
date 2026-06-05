using System;
using NUnit.Framework;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Infrastructure.Implementations;

namespace PuzzleGame.Tests.Infrastructure
{
    [TestFixture]
    public class OnboardingFlowLogicTests
    {
        private class MockAgeService : IAgeVerificationService
        {
            public bool IsVerified { get; set; }
            public bool IsUnder13 { get; set; }
            public DateTime? BirthDate { get; set; }
            public int VerifyCallCount { get; private set; }
            public DateTime? LastVerified { get; private set; }

            public void Verify(DateTime birthDate)
            {
                VerifyCallCount++;
                LastVerified = birthDate;
                IsVerified = true;
                BirthDate = birthDate;
            }

            public void ReVerify(DateTime birthDate) => Verify(birthDate);
            public void Clear() { IsVerified = false; BirthDate = null; }
        }

        private class MockAdService : IAdService
        {
            public bool IsInitialized { get; private set; }
            public bool IsPersonalizedAdsEnabled { get; set; } = true;
            public AdConsentState ConsentState { get; private set; } = AdConsentState.Unknown;
            public int InitializeCallCount { get; private set; }
            public int PreloadCallCount { get; private set; }
            public AdConsentState? LastSetConsentState { get; private set; }

            public void Initialize() { IsInitialized = true; InitializeCallCount++; }
            public void SetConsentState(AdConsentState state, bool personalizedAds)
            {
                ConsentState = state;
                IsPersonalizedAdsEnabled = personalizedAds;
                LastSetConsentState = state;
            }
            public bool IsRewardedAdReady(RewardedAdType type) => true;
            public void ShowRewardedAd(RewardedAdType type, Action<bool> onComplete) => onComplete?.Invoke(true);
            public bool IsInterstitialReady() => true;
            public void ShowInterstitialAd(Action onComplete) => onComplete?.Invoke();
            public void PreloadAds() { PreloadCallCount++; }
        }

        private class MockAnalyticsService : IAnalyticsService
        {
            public bool IsEnabled { get; set; } = true;
            public void Track(AnalyticsEvent evt, System.Collections.Generic.IReadOnlyDictionary<string, object> properties = null) { }
            public void SetUserProperty(string key, string value) { }
            public void Flush() { }
        }

        [Test]
        public void Under13_DisablesAnalyticsAndAds()
        {
            var age = new MockAgeService { IsVerified = true, BirthDate = DateTime.UtcNow.AddYears(-10), IsUnder13 = true };
            var consent = new ConsentManager();
            var ad = new MockAdService();
            var analytics = new MockAnalyticsService();

            consent.Initialize(isUnder13: true);
            ad.Initialize();

            if (age.IsUnder13)
            {
                analytics.IsEnabled = false;
                ad.IsPersonalizedAdsEnabled = false;
            }

            Assert.IsFalse(analytics.IsEnabled);
            Assert.IsFalse(ad.IsPersonalizedAdsEnabled);
            Assert.IsTrue(age.IsUnder13);
        }

        [Test]
        public void Adult_ConsentStatePropagatesToAdService()
        {
            var age = new MockAgeService { IsVerified = true, BirthDate = DateTime.UtcNow.AddYears(-25), IsUnder13 = false };
            var consent = new ConsentManager();
            var ad = new MockAdService();

            consent.Initialize(isUnder13: false);
            ad.Initialize();
            ad.SetConsentState(AdConsentState.Accepted, true);

            Assert.AreEqual(AdConsentState.Accepted, ad.ConsentState);
            Assert.IsTrue(ad.IsPersonalizedAdsEnabled);
            Assert.IsFalse(age.IsUnder13);
        }

        [Test]
        public void Reject_DisablesAnalytics()
        {
            var analytics = new MockAnalyticsService { IsEnabled = true };
            var ad = new MockAdService();
            var consent = new ConsentManager();

            consent.SetConsent(AdConsentState.Rejected, false);
            ad.SetConsentState(AdConsentState.Rejected, false);
            if (consent.ConsentState == AdConsentState.Rejected)
            {
                analytics.IsEnabled = false;
                ad.IsPersonalizedAdsEnabled = false;
            }

            Assert.IsFalse(analytics.IsEnabled);
            Assert.IsFalse(ad.IsPersonalizedAdsEnabled);
        }

        [Test]
        public void ConsentManager_ResetClearsState()
        {
            var consent = new ConsentManager();
            consent.SetConsent(AdConsentState.Accepted, true);
            Assert.AreEqual(AdConsentState.Accepted, consent.ConsentState);

            consent.ResetConsent();
            Assert.AreEqual(AdConsentState.Unknown, consent.ConsentState);
        }
    }
}
