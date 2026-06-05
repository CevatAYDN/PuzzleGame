using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;
using UnityEngine.UI;

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
            _consentManager = consentManager;
            _adService = adService;
            _analyticsService = analyticsService;
            _ageService = ageService;

            _analyticsToggle.onValueChanged.AddListener(OnAnalyticsToggled);
            _personalizedAdsToggle.onValueChanged.AddListener(OnPersonalizedToggled);
            _resetConsentButton.onClick.AddListener(OnResetConsent);
            _deleteDataButton.onClick.AddListener(OnDeleteData);
            _ageVerifyButton.onClick.AddListener(OnReVerifyAge);

            if (_privacyPolicyButton != null)
                _privacyPolicyButton.onClick.AddListener(() => UnityEngine.Application.OpenURL("https://oresorter.app/privacy"));
            if (_termsButton != null)
                _termsButton.onClick.AddListener(() => UnityEngine.Application.OpenURL("https://oresorter.app/terms"));

            _confirmNoButton.onClick.AddListener(HideConfirm);

            RefreshToggles();
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        private void RefreshToggles()
        {
            _analyticsToggle.SetIsOnWithoutNotify(_analyticsService.IsEnabled);
            _personalizedAdsToggle.SetIsOnWithoutNotify(_adService.IsPersonalizedAdsEnabled);
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
            _analyticsToggle.onValueChanged.RemoveListener(OnAnalyticsToggled);
            _personalizedAdsToggle.onValueChanged.RemoveListener(OnPersonalizedToggled);
            _resetConsentButton.onClick.RemoveListener(OnResetConsent);
            _deleteDataButton.onClick.RemoveListener(OnDeleteData);
            _ageVerifyButton.onClick.RemoveListener(OnReVerifyAge);
        }
    }
}
