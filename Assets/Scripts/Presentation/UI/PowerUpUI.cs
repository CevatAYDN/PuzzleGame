using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Presentation.UI
{
    public sealed class PowerUpUI : MonoBehaviour
    {
        private const string LogTag = "[PowerUpUI]";

        [Header("Power-Up Slots")]
        [SerializeField] private PowerUpSlotView[] powerUpSlots;

        [Header("Global State")]
        [SerializeField] private CanvasGroup blocker;

        private IPowerUpService _powerUpService;
        private ICoinWallet _wallet;
        private IAnimationService _animationService;

        [VContainer.Inject]
        public void Construct(
            IPowerUpService powerUpService,
            ICoinWallet wallet,
            IAnimationService animationService)
        {
            _powerUpService = powerUpService;
            _wallet = wallet;
            _animationService = animationService;
        }

        private void Start()
        {
            if (_powerUpService == null) return;

            var descriptors = _powerUpService.GetAllDescriptors();
            for (int i = 0; i < powerUpSlots.Length && i < descriptors.Length; i++)
            {
                var slot = powerUpSlots[i];
                if (slot == null) continue;

                var desc = descriptors[i];
                slot.Setup(desc, OnPowerUpClicked);
                RefreshSlot(slot, desc.Type);
            }

            if (_wallet != null)
                _wallet.OnBalanceChanged += OnCoinBalanceChanged;
        }

        private void OnDestroy()
        {
            if (_wallet != null)
                _wallet.OnBalanceChanged -= OnCoinBalanceChanged;
        }

        private void OnCoinBalanceChanged(int _)
        {
            RefreshAllSlots();
        }

        private void OnPowerUpClicked(PowerUpType type)
        {
            if (_animationService != null && _animationService.IsAnimating)
            {
                MoldLogger.LogWarning($"{LogTag} Activation blocked: animation in progress.");
                return;
            }

            if (_powerUpService == null) return;

            if (!_powerUpService.CanActivate(type))
            {
                MoldLogger.LogInfo($"{LogTag} No charges for {type}.");
                return;
            }

            _powerUpService.Activate(type, moldIndex: -1);
            RefreshSlotByType(type);
        }

        private void RefreshAllSlots()
        {
            foreach (var slot in powerUpSlots)
            {
                if (slot == null || slot.PowerUpType == null) continue;
                RefreshSlot(slot, slot.PowerUpType.Value);
            }
        }

        private void RefreshSlotByType(PowerUpType type)
        {
            foreach (var slot in powerUpSlots)
            {
                if (slot == null) continue;
                if (slot.PowerUpType == type)
                {
                    RefreshSlot(slot, type);
                    return;
                }
            }
        }

        private void RefreshSlot(PowerUpSlotView slot, PowerUpType type)
        {
            if (slot == null || _powerUpService == null) return;

            int charges = _powerUpService.GetCharges(type);
            bool canAfford = _wallet != null;
            slot.Refresh(charges, canAfford);
        }
    }

}
