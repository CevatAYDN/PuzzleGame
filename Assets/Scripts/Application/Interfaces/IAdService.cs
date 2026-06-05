using System;

namespace PuzzleGame.Application.Interfaces
{
    public enum RewardedAdType
    {
        CoinDouble,
        HintBonus,
        UndoBonus,
        DailyBonusDouble
    }

    public enum AdConsentState
    {
        Unknown,
        Accepted,
        Rejected,
        Partial
    }

    public interface IAdService
    {
        bool IsInitialized { get; }
        bool IsPersonalizedAdsEnabled { get; set; }
        AdConsentState ConsentState { get; }

        void Initialize();
        void SetConsentState(AdConsentState state, bool personalizedAds);

        bool IsRewardedAdReady(RewardedAdType type);
        void ShowRewardedAd(RewardedAdType type, Action<bool> onComplete);

        bool IsInterstitialReady();
        void ShowInterstitialAd(Action onComplete);

        void PreloadAds();
    }
}
