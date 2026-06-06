using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Application.Configuration;

#if HAS_GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// AdMob SDK wrapper. Falls back to safe no-op behavior when the
    /// com.google.ads.mobile package is not installed, so the game still
    /// compiles and runs in environments without ads (editor, CI tests).
    /// Install the package and add the proper app/ad-unit IDs in AndroidManifest.xml
    /// to activate real ad delivery.
    /// </summary>
    public class AdMobService : IAdService
    {
        private const string LogTag = "[AdMob]";

        private readonly string _rewardedAdUnitId;
        private readonly string _interstitialAdUnitId;

        public bool IsInitialized { get; private set; }
        public bool IsPersonalizedAdsEnabled { get; set; } = true;
        public AdConsentState ConsentState { get; private set; } = AdConsentState.Unknown;

        private readonly Dictionary<RewardedAdType, DateTime> _lastRewardedShowTime = new Dictionary<RewardedAdType, DateTime>();
        private readonly TimeSpan _rewardedCooldown = TimeSpan.FromSeconds(30);

#if HAS_GOOGLE_MOBILE_ADS
        private RewardedAd _rewardedAd;
        private InterstitialAd _interstitialAd;
#endif

        private readonly GameConfig _gameConfig;

        public AdMobService(GameConfig gameConfig = null, string rewardedAdUnitId = null, string interstitialAdUnitId = null)
        {
            _gameConfig = gameConfig;
            _rewardedAdUnitId = rewardedAdUnitId;
            _interstitialAdUnitId = interstitialAdUnitId;
        }

        public void Initialize()
        {
#if HAS_GOOGLE_MOBILE_ADS
            MobileAds.Initialize(initStatus =>
            {
                IsInitialized = initStatus != null;
                MoldLogger.LogInfo($"{LogTag} Initialize complete. IsInitialized={IsInitialized}");
                ApplyConsentToAdRequest();
                PreloadAds();
            });
#else
            IsInitialized = true;
            MoldLogger.LogInfo($"{LogTag} Initialize (no-op: AdMob SDK not installed).");
#endif
        }

        public void SetConsentState(AdConsentState state, bool personalizedAds)
        {
            ConsentState = state;
            IsPersonalizedAdsEnabled = personalizedAds;
#if HAS_GOOGLE_MOBILE_ADS
            ApplyConsentToAdRequest();
#endif
        }

        public bool IsRewardedAdReady(RewardedAdType type)
        {
            if (!IsInitialized) return false;
            if (!IsWithinCooldown(type)) return false;
#if HAS_GOOGLE_MOBILE_ADS
            return _rewardedAd != null && _rewardedAd.CanShowAd();
#else
            return true;
#endif
        }

        public void ShowRewardedAd(RewardedAdType type, Action<bool> onComplete)
        {
            if (!IsRewardedAdReady(type))
            {
                MoldLogger.LogWarning($"{LogTag} ShowRewardedAd called but ad not ready for {type}.");
                onComplete?.Invoke(false);
                return;
            }

            _lastRewardedShowTime[type] = DateTime.UtcNow;

#if HAS_GOOGLE_MOBILE_ADS
            _rewardedAd.Show(reward =>
            {
                MoldLogger.LogInfo($"{LogTag} Rewarded {type} granted ({reward.Amount} {reward.Type}).");
                onComplete?.Invoke(true);
                LoadRewardedAd();
            });
#else
            MoldLogger.LogInfo($"{LogTag} ShowRewardedAd (no-op: AdMob SDK not installed). type={type}");
            onComplete?.Invoke(true);
#endif
        }

        public bool IsInterstitialReady()
        {
            if (!IsInitialized) return false;
#if HAS_GOOGLE_MOBILE_ADS
            return _interstitialAd != null && _interstitialAd.CanShowAd();
#else
            return true;
#endif
        }

        public void ShowInterstitialAd(Action onComplete)
        {
            if (!IsInterstitialReady())
            {
                MoldLogger.LogWarning($"{LogTag} ShowInterstitialAd called but ad not ready.");
                onComplete?.Invoke();
                return;
            }

#if HAS_GOOGLE_MOBILE_ADS
            _interstitialAd.Show();
            onComplete?.Invoke();
            LoadInterstitialAd();
#else
            MoldLogger.LogInfo($"{LogTag} ShowInterstitialAd (no-op: AdMob SDK not installed).");
            onComplete?.Invoke();
#endif
        }

        public void PreloadAds()
        {
            if (!IsInitialized) return;
#if HAS_GOOGLE_MOBILE_ADS
            LoadRewardedAd();
            LoadInterstitialAd();
#endif
        }

        private bool IsWithinCooldown(RewardedAdType type)
        {
            if (!_lastRewardedShowTime.TryGetValue(type, out var lastTime)) return true;
            return DateTime.UtcNow - lastTime >= _rewardedCooldown;
        }

#if HAS_GOOGLE_MOBILE_ADS
        private void LoadRewardedAd()
        {
            var adUnitId = string.IsNullOrEmpty(_rewardedAdUnitId) ? "ca-app-pub-3940256099942544/5224354917" : _rewardedAdUnitId;
            var adRequest = CreateAdRequest();
            _rewardedAd?.Destroy();
            _rewardedAd = null;

            RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null)
                {
                    MoldLogger.LogError($"{LogTag} Rewarded ad failed to load: {error}");
                    RetryLoadRewarded();
                    return;
                }

                _rewardedAd = ad;
                MoldLogger.LogInfo($"{LogTag} Rewarded ad loaded.");
            });
        }

        private void LoadInterstitialAd()
        {
            var adUnitId = string.IsNullOrEmpty(_interstitialAdUnitId) ? "ca-app-pub-3940256099942544/1033173712" : _interstitialAdUnitId;
            var adRequest = CreateAdRequest();
            _interstitialAd?.Destroy();
            _interstitialAd = null;

            InterstitialAd.Load(adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null)
                {
                    MoldLogger.LogError($"{LogTag} Interstitial failed to load: {error}");
                    RetryLoadInterstitial();
                    return;
                }

                _interstitialAd = ad;
                MoldLogger.LogInfo($"{LogTag} Interstitial loaded.");
            });
        }

        private AdRequest CreateAdRequest()
        {
            var request = new AdRequest();
            if (!IsPersonalizedAdsEnabled)
            {
                request.Extras.Add("npa", "1");
            }
            return request;
        }

        private void ApplyConsentToAdRequest()
        {
            // Real implementation would use PrivacySettings / ConsentDebugSettings here.
            // UMP SDK handles the dialog; we just gate the npa flag on the next ad load.
            MoldLogger.LogInfo($"{LogTag} Consent applied. Personalized={IsPersonalizedAdsEnabled} State={ConsentState}");
        }

        private async void RetryLoadRewarded()
        {
            float delay = _gameConfig != null ? _gameConfig.adRetryDelay : 30f;
            MoldLogger.LogWarning($"{LogTag} Rewarded retry scheduled in {delay}s.");
            await System.Threading.Tasks.Task.Delay((int)(delay * 1000));
            if (IsInitialized)
            {
                MoldLogger.LogInfo($"{LogTag} Retrying rewarded ad load...");
                LoadRewardedAd();
            }
        }

        private async void RetryLoadInterstitial()
        {
            float delay = _gameConfig != null ? _gameConfig.adRetryDelay : 30f;
            MoldLogger.LogWarning($"{LogTag} Interstitial retry scheduled in {delay}s.");
            await System.Threading.Tasks.Task.Delay((int)(delay * 1000));
            if (IsInitialized)
            {
                MoldLogger.LogInfo($"{LogTag} Retrying interstitial ad load...");
                LoadInterstitialAd();
            }
        }
#endif
    }
}
