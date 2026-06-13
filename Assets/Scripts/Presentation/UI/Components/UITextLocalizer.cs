using UnityEngine;
using TMPro;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using VContainer;

namespace PuzzleGame.Presentation.UI.Components
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UITextLocalizer : MonoBehaviour
    {
        [SerializeField] private string _localizationKey;
        private TextMeshProUGUI _textComponent;
        private ILocalizationService _localization;

        public string LocalizationKey
        {
            get => _localizationKey;
            set
            {
                _localizationKey = value;
                RefreshText();
            }
        }

        [Inject]
        public void Construct(ILocalizationService localization)
        {
            _localization = localization;
        }

        private void Awake()
        {
            _textComponent = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            RefreshText();
        }

        private void OnDisable()
        {
        }

        public void RefreshText()
        {
            if (_localization == null || _textComponent == null || string.IsNullOrEmpty(_localizationKey)) return;
            
            string localized = _localization.GetStringOrDefault(_localizationKey, $"[{_localizationKey}]");
            if (!string.IsNullOrEmpty(localized))
            {
                _textComponent.text = localized;
            }
        }
    }
}
