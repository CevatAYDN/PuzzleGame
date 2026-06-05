using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// First-launch age gate modal. Collects birth year + month, calls
    /// IAgeVerificationService.Verify, then raises OnCompleted for the
    /// OnboardingFlowController to chain into the consent flow.
    /// Attach to a modal canvas with year/month sliders and a Continue button.
    /// </summary>
    public class AgeGateModal : MonoBehaviour
    {
        private const string LogTag = "[AgeGateModal]";

        [Header("UI References (assign in Inspector)")]
        [SerializeField] private GameObject _modalPanel;
        [SerializeField] private Slider _yearSlider;
        [SerializeField] private Slider _monthSlider;
        [SerializeField] private Text _yearLabel;
        [SerializeField] private Text _monthLabel;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Text _errorLabel;

        [Header("Year Range")]
        [SerializeField] private int _minYear = 1940;

        public event Action<DateTime> OnCompleted;

        private IAgeVerificationService _ageService;

        public void Initialize(IAgeVerificationService ageService)
        {
            _ageService = ageService ?? throw new ArgumentNullException(nameof(ageService));
            ConfigureSliders();
            _continueButton.onClick.AddListener(OnContinueClicked);
            Hide();
        }

        public void Show()
        {
            if (_ageService != null && _ageService.IsVerified)
            {
                OnCompleted?.Invoke(_ageService.BirthDate ?? DateTime.UtcNow.AddYears(-20));
                return;
            }
            gameObject.SetActive(true);
            _modalPanel.SetActive(true);
            if (_errorLabel != null) _errorLabel.gameObject.SetActive(false);
        }

        public void Hide()
        {
            if (_modalPanel != null) _modalPanel.SetActive(false);
        }

        private void ConfigureSliders()
        {
            int maxYear = DateTime.UtcNow.Year;
            if (_yearSlider != null)
            {
                _yearSlider.minValue = _minYear;
                _yearSlider.maxValue = maxYear;
                _yearSlider.value = maxYear - 20;
                _yearSlider.onValueChanged.AddListener(_ => RefreshLabels());
            }
            if (_monthSlider != null)
            {
                _monthSlider.minValue = 1;
                _monthSlider.maxValue = 12;
                _monthSlider.value = 1;
                _monthSlider.onValueChanged.AddListener(_ => RefreshLabels());
            }
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (_yearLabel != null) _yearLabel.text = ((int)_yearSlider.value).ToString();
            if (_monthLabel != null) _monthLabel.text = ((int)_monthSlider.value).ToString("00");
        }

        private void OnContinueClicked()
        {
            int year = (int)_yearSlider.value;
            int month = (int)_monthSlider.value;
            var birth = new DateTime(year, month, 1);
            _ageService.Verify(birth);
            MoldLogger.LogInfo($"{LogTag} Verified year={year} month={month}");
            Hide();
            OnCompleted?.Invoke(birth);
        }

        private void OnDestroy()
        {
            if (_continueButton != null) _continueButton.onClick.RemoveListener(OnContinueClicked);
        }
    }
}
