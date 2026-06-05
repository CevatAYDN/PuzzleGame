using System;

namespace PuzzleGame.Application.Interfaces
{
    public interface IConsentManager
    {
        AdConsentState ConsentState { get; }
        bool IsReady { get; }
        bool IsUnder13 { get; }

        void Initialize(bool isUnder13);
        void RequestConsentIfNeeded(Action<AdConsentState> onComplete);
        void SetConsent(AdConsentState state, bool personalizedAds);
        void ResetConsent();
    }
}
