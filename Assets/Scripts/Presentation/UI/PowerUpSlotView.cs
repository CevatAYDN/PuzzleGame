using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Presentation.UI
{
    public sealed class PowerUpSlotView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI chargesText;
        [SerializeField] private Button activateButton;
        [SerializeField] private Image iconImage;

        public PowerUpType? PowerUpType { get; private set; }
        private Action<PowerUpType> _onActivate;

        private void Awake()
        {
            if (activateButton != null)
                activateButton.onClick.AddListener(OnActivateClicked);
        }

        public void Setup(PowerUpDescriptor descriptor, Action<PowerUpType> onActivate)
        {
            PowerUpType = descriptor.Type;
            _onActivate = onActivate;

            if (nameText != null)
                nameText.text = descriptor.Type.ToString();

            if (chargesText != null)
                chargesText.text = "0";

            activateButton.interactable = false;
        }

        public void Refresh(int charges, bool canAfford)
        {
            if (chargesText != null)
                chargesText.text = charges.ToString();

            if (activateButton != null)
                activateButton.interactable = charges > 0;
        }

        private void OnActivateClicked()
        {
            if (PowerUpType == null) return;
            _onActivate?.Invoke(PowerUpType.Value);
        }
    }
}
