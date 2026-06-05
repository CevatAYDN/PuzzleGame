using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Test/CI fallback for IAdService. All Show callbacks resolve to true
    /// (reward granted) so game logic and reward flows can be verified
    /// without the AdMob SDK installed.
    /// </summary>
    public class NoOpAdService : IAdService
    {
        private const string LogTag = "[AdMob-NoOp]";

        public bool IsInitialized { get; private set; }
        public bool IsPersonalizedAdsEnabled { get; set; } = true;
        public AdConsentState ConsentState { get; private set; } = AdConsentState.Accepted;

        private readonly HashSet<RewardedAdType> _watchedTypes = new HashSet<RewardedAdType>();

        public void Initialize()
        {
            IsInitialized = true;
            MoldLogger.LogInfo($"{LogTag} Initialize (test mode).");
        }

        public void SetConsentState(AdConsentState state, bool personalizedAds)
        {
            ConsentState = state;
            IsPersonalizedAdsEnabled = personalizedAds;
        }

        public bool IsRewardedAdReady(RewardedAdType type) => IsInitialized;

        public void ShowRewardedAd(RewardedAdType type, Action<bool> onComplete)
        {
            _watchedTypes.Add(type);
            MoldLogger.LogInfo($"{LogTag} ShowRewardedAd type={type} (test mode: reward granted).");
            onComplete?.Invoke(true);
        }

        public bool IsInterstitialReady() => IsInitialized;

        public void ShowInterstitialAd(Action onComplete)
        {
            MoldLogger.LogInfo($"{LogTag} ShowInterstitialAd (test mode: skipped).");
            onComplete?.Invoke();
        }

        public void PreloadAds() { }

        public IReadOnlyCollection<RewardedAdType> WatchedTypes => _watchedTypes;
    }
}
