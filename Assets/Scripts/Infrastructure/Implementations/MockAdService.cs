using System;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Logging;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Mock/Stub Ad Service — production ortamında Unity Ads veya AdMob ile değiştirilebilir.
    /// S7 FIX: Dead interface'i implementasyonla doldurmak.
    /// </summary>
    public class MockAdService : IAdService
    {
        private readonly bool _alwaysSucceed;

        public bool IsAdAvailable { get; private set; }

        public MockAdService(bool isAvailable = false, bool alwaysSucceed = false)
        {
            IsAdAvailable = isAvailable;
            _alwaysSucceed = alwaysSucceed;
        }

        public void ShowRewardedAd(Action onAdComplete = null, Action onAdFailed = null)
        {
            BottleLogger.LogInfo("[MockAdService] ShowRewardedAd called");

            if (!IsAdAvailable)
            {
                BottleLogger.LogWarning("[MockAdService] No ad available");
                onAdFailed?.Invoke();
                return;
            }

            if (_alwaysSucceed)
            {
                BottleLogger.LogInfo("[MockAdService] Rewarded ad completed (mock)");
                onAdComplete?.Invoke();
            }
            else
            {
                // Simülasyon: belirsizlik için rastgele başarı/başarısızlık
                // Burada basitçe başarı döndürüyoruz (test edilebilir mock)
                onAdComplete?.Invoke();
            }
        }

        public void ShowInterstitialAd()
        {
            BottleLogger.LogInfo("[MockAdService] ShowInterstitialAd called");
            if (!IsAdAvailable)
            {
                BottleLogger.LogWarning("[MockAdService] No ad available");
            }
        }
    }
}