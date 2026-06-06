using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// GDPR / COPPA consent modal. Three-button flow: Accept All, Reject All,
    /// Manage Choices (granular toggles). Calls IConsentManager and propagates
    /// the result to IAdService + IAnalyticsService. Raises OnCompleted for
    /// the OnboardingFlowController to proceed to Main Menu.
    /// </summary>
    public class ConsentModal : MonoBehaviour
    {
        private const string LogTag = "[ConsentModal]";

        [Header("Buttons")]
        [SerializeField] private Button _acceptAllButton;
        [SerializeField] private Button _rejectAllButton;
        [SerializeField] private Button _manageChoicesButton;
        [SerializeField] private Button _saveChoicesButton;

        [Header("Granular Panel (Manage Choices)")]
        [SerializeField] private GameObject _granularPanel;
        [SerializeField] private Toggle _analyticsToggle;
        [SerializeField] private Toggle _personalizedAdsToggle;
        [SerializeField] private Toggle _crashReportsToggle;

        [Header("Main Panel")]
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private Text _under13Notice;

        [Header("Policy Links")]
        [SerializeField] private Button _privacyPolicyButton;
        [SerializeField] private Button _termsOfServiceButton;

        public event Action<AdConsentState, bool> OnCompleted;

        private IConsentManager _consentManager;
        private IAdService _adService;
        private IAnalyticsService _analyticsService;
        private IAgeVerificationService _ageService;

        public void Initialize(
            IConsentManager consentManager,
            IAdService adService,
            IAnalyticsService analyticsService,
            IAgeVerificationService ageService)
        {
            _consentManager = consentManager ?? throw new ArgumentNullException(nameof(consentManager));
            _adService = adService ?? throw new ArgumentNullException(nameof(adService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _ageService = ageService ?? throw new ArgumentNullException(nameof(ageService));

            if (_acceptAllButton != null) _acceptAllButton.onClick.AddListener(OnAcceptAll);
            if (_rejectAllButton != null) _rejectAllButton.onClick.AddListener(OnRejectAll);
            if (_manageChoicesButton != null) _manageChoicesButton.onClick.AddListener(OnManageChoices);
            if (_saveChoicesButton != null) _saveChoicesButton.onClick.AddListener(OnSaveChoices);

            if (_privacyPolicyButton != null)
                _privacyPolicyButton.onClick.AddListener(() => UnityEngine.Application.OpenURL("https://oresorter.app/privacy"));
            if (_termsOfServiceButton != null)
                _termsOfServiceButton.onClick.AddListener(() => UnityEngine.Application.OpenURL("https://oresorter.app/terms"));

            Hide();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (_under13Notice != null) _under13Notice.gameObject.SetActive(_ageService.IsUnder13);
            if (_mainPanel != null) _mainPanel.SetActive(true);
            if (_granularPanel != null) _granularPanel.SetActive(false);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnAcceptAll()
        {
            Apply(AdConsentState.Accepted, true);
        }

        private void OnRejectAll()
        {
            Apply(AdConsentState.Rejected, false);
        }

        private void OnManageChoices()
        {
            _mainPanel.SetActive(false);
            _granularPanel.SetActive(true);
        }

        private void OnSaveChoices()
        {
            bool analytics = _analyticsToggle != null && _analyticsToggle.isOn;
            bool personalized = _personalizedAdsToggle != null && _personalizedAdsToggle.isOn;
            bool crash = _crashReportsToggle != null && _crashReportsToggle.isOn;

            _analyticsService.IsEnabled = analytics;
            _adService.IsPersonalizedAdsEnabled = personalized;

            AdConsentState state = (analytics || personalized) ? AdConsentState.Partial : AdConsentState.Rejected;
            Apply(state, personalized);
        }

        private void Apply(AdConsentState state, bool personalized)
        {
            _consentManager.SetConsent(state, personalized);
            _adService.SetConsentState(state, personalized);
            if (state == AdConsentState.Rejected)
            {
                _analyticsService.IsEnabled = false;
                _adService.IsPersonalizedAdsEnabled = false;
            }
            MoldLogger.LogInfo($"{LogTag} Applied state={state} personalized={personalized}");
            Hide();
            OnCompleted?.Invoke(state, personalized);
        }

        private void OnDestroy()
        {
            if (_acceptAllButton != null) _acceptAllButton.onClick.RemoveListener(OnAcceptAll);
            if (_rejectAllButton != null) _rejectAllButton.onClick.RemoveListener(OnRejectAll);
            if (_manageChoicesButton != null) _manageChoicesButton.onClick.RemoveListener(OnManageChoices);
            if (_saveChoicesButton != null) _saveChoicesButton.onClick.RemoveListener(OnSaveChoices);
        }
    }
}
