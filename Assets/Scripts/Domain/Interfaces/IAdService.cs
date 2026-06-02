namespace PuzzleGame.Domain.Interfaces
{
    /// <summary>
    /// Reklam servisi arayüzü.
    /// Unity Ads veya başka bir provider ile implement edilebilir.
    /// </summary>
    public interface IAdService
    {
        bool IsAdAvailable { get; }
        void ShowRewardedAd(System.Action onAdComplete = null, System.Action onAdFailed = null);
        void ShowInterstitialAd();
    }
}