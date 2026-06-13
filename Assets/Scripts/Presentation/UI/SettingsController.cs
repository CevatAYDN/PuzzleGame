using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Events;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using VContainer;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Unified Settings panel controller. Manages:
    /// - Language selection (tr/en/de)
    /// - Haptic feedback toggle
    /// - Navigation to Sound sub-panel (SettingsSoundController)
    /// - Navigation to Privacy sub-panel (SettingsPrivacyController)
    /// - Back to main menu
    ///
    /// SRP: only owns settings-panel UI state and navigation.
    /// Persistence is delegated to ILocalizationService, IHapticFeedbackService, etc.
    /// </summary>
    public class SettingsController : MonoBehaviour
    {
        private const string LogTag = "[Settings]";

        [Header("Language")]
        [SerializeField] private TMP_Dropdown _languageDropdown;
        [SerializeField] private TextMeshProUGUI _languageLabel;

        [Header("Haptic")]
        [SerializeField] private Toggle _hapticToggle;
        [SerializeField] private TextMeshProUGUI _hapticLabel;

        [Header("Sub-Panel Buttons")]
        [SerializeField] private Button _soundButton;
        [SerializeField] private Button _privacyButton;

        [Header("Navigation")]
        [SerializeField] private Button _backButton;

        [Header("Sub-Panels (managed visibility)")]
        [SerializeField] private GameObject _soundPanel;
        [SerializeField] private GameObject _privacyPanel;

        private IEventAggregator _events;
        private ILocalizationService _localization;
        private IHapticFeedbackService _haptic;

        [Inject]
        public void Construct(
            IEventAggregator events,
            ILocalizationService localization,
            IHapticFeedbackService haptic)
        {
            _events = events;
            _localization = localization;
            _haptic = haptic;
        }

        private void Start()
        {
            SetupLanguageDropdown();
            SetupHapticToggle();
            SetupButtons();
        }

        private void OnEnable()
        {
            if (_events != null)
            {
                _events.Subscribe<ShowSettingsRequestEvent>(OnShowSettings);
            }
            RefreshUI();
        }

        private void OnDisable()
        {
            if (_events != null)
            {
                _events.Unsubscribe<ShowSettingsRequestEvent>(OnShowSettings);
            }
        }

        private void OnShowSettings(ShowSettingsRequestEvent e)
        {
            gameObject.SetActive(true);
        }

        // ─── Language ────────────────────────────────────────────────────────

        private void SetupLanguageDropdown()
        {
            if (_languageDropdown == null) return;

            _languageDropdown.ClearOptions();
            _languageDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Türkçe",
                "English",
                "Deutsch"
            });

            // Set current language
            var current = _localization?.CurrentLanguage ?? SupportedLanguage.Turkish;
            _languageDropdown.value = current switch
            {
                SupportedLanguage.Turkish => 0,
                SupportedLanguage.English => 1,
                SupportedLanguage.German => 2,
                _ => 0
            };

            _languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        private void OnLanguageChanged(int index)
        {
            if (_localization == null) return;

            var lang = index switch
            {
                0 => SupportedLanguage.Turkish,
                1 => SupportedLanguage.English,
                2 => SupportedLanguage.German,
                _ => SupportedLanguage.Turkish
            };

            _localization.SetLanguage(lang);
            RefreshLanguageLabel();
        }

        private void RefreshLanguageLabel()
        {
            if (_languageLabel == null || _localization == null) return;

            _languageLabel.text = _localization.CurrentLanguage switch
            {
                SupportedLanguage.Turkish => "Dil / Language",
                SupportedLanguage.English => "Language",
                SupportedLanguage.German => "Sprache",
                _ => "Language"
            };
        }

        // ─── Haptic ──────────────────────────────────────────────────────────

        private void SetupHapticToggle()
        {
            if (_hapticToggle == null) return;

            _hapticToggle.isOn = _haptic?.IsEnabled ?? true;
            _hapticToggle.onValueChanged.AddListener(OnHapticToggled);
        }

        private void OnHapticToggled(bool enabled)
        {
            if (_haptic != null)
                _haptic.IsEnabled = enabled;
        }

        // ─── Buttons ─────────────────────────────────────────────────────────

        private void SetupButtons()
        {
            if (_soundButton != null)
                _soundButton.onClick.AddListener(OnSoundClicked);

            if (_privacyButton != null)
                _privacyButton.onClick.AddListener(OnPrivacyClicked);

            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnSoundClicked()
        {
            HideAllSubPanels();
            if (_soundPanel != null)
                _soundPanel.SetActive(true);
        }

        private void OnPrivacyClicked()
        {
            HideAllSubPanels();
            if (_privacyPanel != null)
                _privacyPanel.SetActive(true);
        }

        private void OnBackClicked()
        {
            // If a sub-panel is open, close it first
            if (_soundPanel != null && _soundPanel.activeSelf)
            {
                _soundPanel.SetActive(false);
                return;
            }
            if (_privacyPanel != null && _privacyPanel.activeSelf)
            {
                _privacyPanel.SetActive(false);
                return;
            }

            // Otherwise, close settings and return to main menu
            _events?.Publish(new HideSettingsRequestEvent());
        }

        // ─── Refresh ─────────────────────────────────────────────────────────

        private void RefreshUI()
        {
            RefreshLanguageLabel();

            if (_hapticToggle != null && _haptic != null)
                _hapticToggle.SetIsOnWithoutNotify(_haptic.IsEnabled);
        }

        private void HideAllSubPanels()
        {
            if (_soundPanel != null) _soundPanel.SetActive(false);
            if (_privacyPanel != null) _privacyPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_languageDropdown != null)
                _languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
            if (_hapticToggle != null)
                _hapticToggle.onValueChanged.RemoveListener(OnHapticToggled);
            if (_soundButton != null)
                _soundButton.onClick.RemoveListener(OnSoundClicked);
            if (_privacyButton != null)
                _privacyButton.onClick.RemoveListener(OnPrivacyClicked);
            if (_backButton != null)
                _backButton.onClick.RemoveListener(OnBackClicked);
        }
    }

    /// <summary>
    /// Published by SettingsController when "Back" is clicked (no sub-panel active).
    /// MainMenuController subscribes to hide the settings panel and show main menu.
    /// </summary>
    public class HideSettingsRequestEvent { }
}
