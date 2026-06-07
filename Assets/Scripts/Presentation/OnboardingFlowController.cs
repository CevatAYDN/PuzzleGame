using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Orchestrates the first-launch flow:
    ///   1. Age gate (if not verified)
    ///   2. GDPR consent (if under 13, ads disabled; else request consent)
    ///   3. Proceed to Main Menu
    /// Implements IDisposable — must be Disposed when the app shuts down
    /// to unsubscribe from modal events.
    /// </summary>
    public class OnboardingFlowController : IDisposable
    {
        private const string LogTag = "[OnboardingFlow]";

        private readonly IAgeVerificationService _ageService;
        private readonly IConsentManager _consentManager;
        private readonly IAdService _adService;
        private readonly IAnalyticsService _analyticsService;
        private readonly IFeatureFlagService _featureFlagService;

        private readonly UI.AgeGateModal _ageGateModal;
        private readonly UI.ConsentModal _consentModal;

        private bool _disposed;

        public event Action OnCompletedFlow;

        public OnboardingFlowController(
            IAgeVerificationService ageService,
            IConsentManager consentManager,
            IAdService adService,
            IAnalyticsService analyticsService,
            IFeatureFlagService featureFlagService,
            UI.AgeGateModal ageGateModal,
            UI.ConsentModal consentModal)
        {
            _ageService = ageService ?? throw new ArgumentNullException(nameof(ageService));
            _consentManager = consentManager ?? throw new ArgumentNullException(nameof(consentManager));
            _adService = adService ?? throw new ArgumentNullException(nameof(adService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _featureFlagService = featureFlagService ?? throw new ArgumentNullException(nameof(featureFlagService));
            _ageGateModal = ageGateModal ?? throw new ArgumentNullException(nameof(ageGateModal));
            _consentModal = consentModal ?? throw new ArgumentNullException(nameof(consentModal));

            _ageGateModal.Initialize(_ageService);
            _consentModal.Initialize(_consentManager, _adService, _analyticsService, _ageService);

            _ageGateModal.OnCompleted += OnAgeGateCompleted;
            _consentModal.OnCompleted += OnConsentCompleted;
        }

        public void Run()
        {
            MoldLogger.LogInfo($"{LogTag} Starting onboarding flow.");

            MoldLogger.LogInfo($"{LogTag} Checking if age is verified... IsVerified={_ageService.IsVerified}");
            if (_ageService.IsVerified)
            {
                MoldLogger.LogInfo($"{LogTag} Age is already verified. Invoking OnAgeGateCompleted.");
                OnAgeGateCompleted(_ageService.BirthDate ?? DateTime.UtcNow.AddYears(-20));
            }
            else
            {
                MoldLogger.LogInfo($"{LogTag} Age is NOT verified. Invoking _ageGateModal.Show().");
                _ageGateModal.Show();
            }
        }

        private void OnAgeGateCompleted(DateTime birthDate)
        {
            MoldLogger.LogInfo($"{LogTag} Age gate completed. Under13={_ageService.IsUnder13}");

            _consentManager.Initialize(_ageService.IsUnder13);
            _adService.Initialize();

            if (_ageService.IsUnder13)
            {
                _analyticsService.IsEnabled = false;
                _adService.IsPersonalizedAdsEnabled = false;
                MoldLogger.LogInfo($"{LogTag} User is under 13 — analytics disabled, personalized ads disabled.");
                ProceedToMainMenu();
                return;
            }

            _consentModal.Show();
        }

        private void OnConsentCompleted(AdConsentState state, bool personalizedAds)
        {
            MoldLogger.LogInfo($"{LogTag} Consent completed. state={state} personalized={personalizedAds}");
            
            // Track session start after consent is obtained (GDPR compliant)
            if (_analyticsService != null && _analyticsService.IsEnabled)
            {
                _analyticsService.Track(AnalyticsEvent.SessionStart);
            }
            
            ProceedToMainMenu();
        }

        private void ProceedToMainMenu()
        {
            _adService.PreloadAds();
            
            string onboardingType = _featureFlagService.GetString("onboarding_flow_type", "classic");
            if (onboardingType == "sandbox")
            {
                MoldLogger.LogInfo($"{LogTag} Sandbox onboarding mode enabled — starting free-play period.");
            }
            
            MoldLogger.LogInfo($"{LogTag} Onboarding complete — proceeding to Main Menu.");
            OnCompletedFlow?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_ageGateModal != null) _ageGateModal.OnCompleted -= OnAgeGateCompleted;
            if (_consentModal != null) _consentModal.OnCompleted -= OnConsentCompleted;
        }
    }
}
