using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

#if HAS_GOOGLE_MOBILE_ADS
using GoogleMobileAds.Ump;
using GoogleMobileAds.Ump.Api;
#endif

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Google UMP (User Messaging Platform) consent wrapper.
    /// Falls back to immediate consent (Accepted) when AdMob SDK is not installed.
    /// </summary>
    public class ConsentManager : IConsentManager
    {
        private const string LogTag = "[Consent]";

        public AdConsentState ConsentState { get; private set; } = AdConsentState.Unknown;
        public bool IsReady { get; private set; }
        public bool IsUnder13 { get; private set; }

        private Action<AdConsentState> _pendingCallback;

        public void Initialize(bool isUnder13)
        {
            IsUnder13 = isUnder13;
#if HAS_GOOGLE_MOBILE_ADS
            var request = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = IsUnder13
            };

            ConsentInformation.Update(request, (FormError updateError) =>
            {
                if (updateError != null)
                {
                    MoldLogger.LogError($"{LogTag} Update failed: {updateError.Message}");
                    FallbackToAccepted();
                    return;
                }
                IsReady = true;
                MoldLogger.LogInfo($"{LogTag} Initialized. Available={ConsentInformation.IsConsentFormAvailable()}");
            });
#else
            IsReady = true;
            MoldLogger.LogInfo($"{LogTag} Initialize (no-op: AdMob SDK not installed). isUnder13={isUnder13}");
#endif
        }

        public void RequestConsentIfNeeded(Action<AdConsentState> onComplete)
        {
            if (!IsReady)
            {
                Initialize(IsUnder13);
            }

#if HAS_GOOGLE_MOBILE_ADS
            if (ConsentInformation.ConsentStatus == ConsentStatus.NotRequired ||
                ConsentInformation.ConsentStatus == ConsentStatus.Obtained)
            {
                AdConsentState state = ConsentInformation.ConsentStatus == ConsentStatus.Obtained
                    ? AdConsentState.Accepted
                    : AdConsentState.Unknown;
                ConsentState = state;
                onComplete?.Invoke(state);
                return;
            }

            _pendingCallback = onComplete;
            ConsentForm.Load((ConsentForm form, FormError loadError) =>
            {
                if (loadError != null || form == null)
                {
                    MoldLogger.LogError($"{LogTag} Form load failed.");
                    FallbackToAccepted();
                    return;
                }
                form.Show((FormError showError) =>
                {
                    if (showError != null)
                    {
                        MoldLogger.LogError($"{LogTag} Form show failed.");
                        FallbackToAccepted();
                        return;
                    }
                    AdConsentState result = ConsentInformation.ConsentStatus == ConsentStatus.Obtained
                        ? AdConsentState.Accepted
                        : AdConsentState.Rejected;
                    ConsentState = result;
                    _pendingCallback?.Invoke(result);
                    _pendingCallback = null;
                });
            });
#else
            FallbackToAccepted();
            onComplete?.Invoke(ConsentState);
#endif
        }

        public void SetConsent(AdConsentState state, bool personalizedAds)
        {
            ConsentState = state;
            MoldLogger.LogInfo($"{LogTag} SetConsent state={state} personalizedAds={personalizedAds}");
        }

        public void ResetConsent()
        {
            ConsentState = AdConsentState.Unknown;
#if HAS_GOOGLE_MOBILE_ADS
            ConsentInformation.Reset();
#endif
            MoldLogger.LogInfo($"{LogTag} Consent reset.");
        }

        private void FallbackToAccepted()
        {
            ConsentState = AdConsentState.Accepted;
            _pendingCallback?.Invoke(ConsentState);
            _pendingCallback = null;
        }
    }
}
