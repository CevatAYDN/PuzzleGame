using TMPro;
using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Presentation.UI
{
    public sealed class AchievementNotificationUI : MonoBehaviour
    {
        [Header("Notification Panel")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private float displayDuration = 3f;

        private IAchievementService _achievementService;
        private ICoinWallet _wallet;
        private float _hideTimer;

        private static readonly string[] AchievementNames =
        {
            "First Steps", "Getting Started", "Halfway There", "Grand Master",
            "Power Up!", "Full Arsenal", "Week Warrior", "Monthly Champion",
            "Perfectionist", "Forge Master", "Speed Demon", "Color Alchemist"
        };

        private static readonly int[] RewardCoins =
        {
            25, 50, 100, 200, 10, 75, 50, 200, 100, 75, 50, 50
        };

        [VContainer.Inject]
        public void Construct(IAchievementService achievementService, ICoinWallet wallet)
        {
            _achievementService = achievementService;
            _wallet = wallet;
        }

        private void Start()
        {
            if (_achievementService != null)
                _achievementService.OnUnlocked += OnAchievementUnlocked;

            if (notificationPanel != null)
                notificationPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_achievementService != null)
                _achievementService.OnUnlocked -= OnAchievementUnlocked;
        }

        private void Update()
        {
            if (_hideTimer > 0f)
            {
                _hideTimer -= Time.deltaTime;
                if (_hideTimer <= 0f && notificationPanel != null)
                    notificationPanel.SetActive(false);
            }
        }

        private void OnAchievementUnlocked(AchievementId id)
        {
            int index = (int)id;
            string name = index >= 0 && index < AchievementNames.Length
                ? AchievementNames[index]
                : id.ToString();

            int coins = index >= 0 && index < RewardCoins.Length
                ? RewardCoins[index]
                : 0;

            if (titleText != null)
                titleText.text = $"Achievement Unlocked: {name}";

            if (rewardText != null && coins > 0)
                rewardText.text = $"+{coins} Coins";
            else if (rewardText != null)
                rewardText.text = string.Empty;

            if (notificationPanel != null)
            {
                notificationPanel.SetActive(true);
                _hideTimer = displayDuration;
            }
        }
    }
}
