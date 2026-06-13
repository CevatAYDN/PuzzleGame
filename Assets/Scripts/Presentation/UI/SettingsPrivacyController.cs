using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Settings > Privacy screen. Lets the player toggle analytics,
    /// personalized ads, reset consent, and delete all stored data.
    /// </summary>
    public class SettingsPrivacyController : MonoBehaviour
    {
        private const string LogTag = "[SettingsPrivacy]";

        [Header("Toggles")]
        [SerializeField] private Toggle _analyticsToggle;
        [SerializeField] private Toggle _personalizedAdsToggle;

        [Header("Buttons")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _resetConsentButton;
        [SerializeField] private Button _deleteDataButton;
        [SerializeField] private Button _privacyPolicyButton;
        [SerializeField] private Button _termsButton;
        [SerializeField] private Button _ageVerifyButton;

        [Header("Confirmation Modal")]
        [SerializeField] private GameObject _confirmPanel;
        [SerializeField] private Text _confirmMessage;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;

        private IEventAggregator _events;
        private IConsentManager _consentManager;
        private IAdService _adService;
        private IAnalyticsService _analyticsService;
        private IAgeVerificationService _ageService;

        [Inject]
        public void Initialize(
            IEventAggregator events,
            IConsentManager consentManager,
            IAdService adService,
            IAnalyticsService analyticsService,
            IAgeVerificationService ageService)
        {
            _events = events;
            _consentManager = consentManager;
            _adService = adService;
            _analyticsService = analyticsService;
            _ageService = ageService;

            if (_analyticsToggle != null) _analyticsToggle.onValueChanged.AddListener(OnAnalyticsToggled);
            if (_personalizedAdsToggle != null) _personalizedAdsToggle.onValueChanged.AddListener(OnPersonalizedToggled);
            if (_resetConsentButton != null) _resetConsentButton.onClick.AddListener(OnResetConsent);
            if (_deleteDataButton != null) _deleteDataButton.onClick.AddListener(OnDeleteData);
            if (_ageVerifyButton != null) _ageVerifyButton.onClick.AddListener(OnReVerifyAge);
            if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);

            if (_privacyPolicyButton != null)
                _privacyPolicyButton.onClick.AddListener(() => UnityEngine.Application.OpenURL("https://oresorter.app/privacy"));
            if (_termsButton != null)
                _termsButton.onClick.AddListener(() => UnityEngine.Application.OpenURL("https://oresorter.app/terms"));

            if (_confirmNoButton != null) _confirmNoButton.onClick.AddListener(HideConfirm);

            RefreshToggles();
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        private void OnEnable()
        {
            if (_events != null)
            {
                _events.Subscribe<ShowPrivacyRequestEvent>(OnShowPrivacy);
                _events.Subscribe<HidePrivacyRequestEvent>(OnHidePrivacy);
            }
        }

        private void OnDisable()
        {
            if (_events != null)
            {
                _events.Unsubscribe<ShowPrivacyRequestEvent>(OnShowPrivacy);
                _events.Unsubscribe<HidePrivacyRequestEvent>(OnHidePrivacy);
            }
        }

        private void OnShowPrivacy(ShowPrivacyRequestEvent e) => Show();
        private void OnHidePrivacy(HidePrivacyRequestEvent e) => Hide();

        private void OnBackClicked()
        {
            _events?.Publish(new HidePrivacyRequestEvent());
        }

        private void RefreshToggles()
        {
            if (_analyticsToggle != null) _analyticsToggle.SetIsOnWithoutNotify(_analyticsService.IsEnabled);
            if (_personalizedAdsToggle != null) _personalizedAdsToggle.SetIsOnWithoutNotify(_adService.IsPersonalizedAdsEnabled);
        }

        private void OnAnalyticsToggled(bool enabled)
        {
            _analyticsService.IsEnabled = enabled;
            MoldLogger.LogInfo($"{LogTag} Analytics toggled: {enabled}");
        }

        private void OnPersonalizedToggled(bool enabled)
        {
            _adService.IsPersonalizedAdsEnabled = enabled;
            _adService.SetConsentState(_consentManager.ConsentState, enabled);
            MoldLogger.LogInfo($"{LogTag} Personalized ads: {enabled}");
        }

        private void OnResetConsent()
        {
            ShowConfirm("Reset your privacy choices? You'll see the consent dialog again on next launch.",
                () =>
                {
                    _consentManager.ResetConsent();
                    _adService.SetConsentState(AdConsentState.Unknown, true);
                    MoldLogger.LogInfo($"{LogTag} Consent reset.");
                });
        }

        private void OnDeleteData()
        {
            ShowConfirm("Delete ALL your data? This will reset progress, coins, and settings. This cannot be undone.",
                () =>
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    _analyticsService.Flush();
                    MoldLogger.LogInfo($"{LogTag} All data deleted.");
                });
        }

        private void OnReVerifyAge()
        {
            ShowConfirm("Re-verify your age? This is required if you want to re-enable personalized ads.",
                () => _ageService.Clear());
        }

        private void ShowConfirm(string message, Action onYes)
        {
            _confirmPanel.SetActive(true);
            _confirmMessage.text = message;
            _confirmYesButton.onClick.RemoveAllListeners();
            _confirmYesButton.onClick.AddListener(() =>
            {
                HideConfirm();
                onYes?.Invoke();
            });
        }

        private void HideConfirm() => _confirmPanel.SetActive(false);

        private void OnDestroy()
        {
            if (_analyticsToggle != null) _analyticsToggle.onValueChanged.RemoveListener(OnAnalyticsToggled);
            if (_personalizedAdsToggle != null) _personalizedAdsToggle.onValueChanged.RemoveListener(OnPersonalizedToggled);
            if (_resetConsentButton != null) _resetConsentButton.onClick.RemoveListener(OnResetConsent);
            if (_deleteDataButton != null) _deleteDataButton.onClick.RemoveListener(OnDeleteData);
            if (_ageVerifyButton != null) _ageVerifyButton.onClick.RemoveListener(OnReVerifyAge);
            if (_backButton != null) _backButton.onClick.RemoveListener(OnBackClicked);
        }
    }
}
