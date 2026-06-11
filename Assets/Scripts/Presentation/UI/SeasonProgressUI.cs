using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Presentation.UI
{
    public sealed class SeasonProgressUI : MonoBehaviour
    {
        [Header("Season Info")]
        [SerializeField] private TextMeshProUGUI seasonNameText;
        [SerializeField] private TextMeshProUGUI tierNameText;

        [Header("Progress Bar")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("Reward Preview")]
        [SerializeField] private TextMeshProUGUI nextRewardText;
        [SerializeField] private GameObject rewardIconContainer;

        [Header("Player Level")]
        [SerializeField] private TextMeshProUGUI playerLevelText;
        [SerializeField] private TextMeshProUGUI totalXpText;

        private IProgressService _progress;
        private ILocalizationService _localization;

        [VContainer.Inject]
        public void Construct(IProgressService progress, ILocalizationService localization)
        {
            _progress = progress;
            _localization = localization;
        }

        private void Start()
        {
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_progress == null) return;

            UpdateSeasonInfo();
            UpdateProgressBar();
            UpdateNextReward();
            UpdatePlayerLevel();
        }

        private void UpdateSeasonInfo()
        {
            if (seasonNameText != null)
            {
                if (_progress.IsSeasonActive && _progress.ActiveSeason != null)
                {
                    string key = _progress.ActiveSeason.displayNameKey;
                    seasonNameText.text = string.IsNullOrEmpty(key)
                        ? _progress.ActiveSeason.seasonId
                        : (_localization?.GetString(key) ?? key);
                }
                else
                {
                    seasonNameText.text = _localization?.GetString("no_active_season") ?? "No Active Season";
                }
            }

            if (tierNameText != null)
            {
                int tier = _progress.CurrentTierIndex;
                tierNameText.text = tier >= 0
                    ? $"Tier {tier + 1}"
                    : "--";
            }
        }

        private void UpdateProgressBar()
        {
            if (progressSlider == null) return;

            if (!_progress.IsSeasonActive)
            {
                progressSlider.value = 0f;
                if (progressText != null)
                    progressText.text = "-- / --";
                return;
            }

            int currentXp = _progress.SeasonXp;
            int nextTierXp = _progress.SeasonXpToNextTier;
            int tierIndex = _progress.CurrentTierIndex;
            int neededForCurrent = ComputeTierXpNeeded(tierIndex + 1);
            int neededForCurrentTier = ComputeTierXpNeeded(tierIndex);

            if (neededForCurrent > neededForCurrentTier)
            {
                float progress = (float)(currentXp - neededForCurrentTier)
                    / (neededForCurrent - neededForCurrentTier);
                progressSlider.value = Mathf.Clamp01(progress);
            }
            else
            {
                progressSlider.value = 1f;
            }

            if (progressText != null)
            {
                progressText.text = $"{currentXp} / {neededForCurrent} XP";
            }
        }

        private int ComputeTierXpNeeded(int tierIndex)
        {
            if (_progress.ActiveSeason == null) return 0;
            return _progress.ActiveSeason.baseXp + tierIndex * _progress.ActiveSeason.xpPerTier;
        }

        private void UpdateNextReward()
        {
            if (nextRewardText == null) return;
            if (!_progress.IsSeasonActive || _progress.ActiveSeason == null)
            {
                nextRewardText.text = "";
                return;
            }

            int nextTier = _progress.CurrentTierIndex + 1;
            var season = _progress.ActiveSeason;
            if (nextTier < 0 || nextTier >= season.rewards.Count)
            {
                nextRewardText.text = _localization?.GetString("all_rewards_claimed") ?? "All rewards claimed!";
                return;
            }

            var reward = season.rewards[nextTier];
            string rewardLabel = reward.rewardType switch
            {
                RewardType.Coins => $"{reward.rewardValue} Coins",
                RewardType.CosmeticItem => !string.IsNullOrEmpty(reward.cosmeticItemId)
                    ? reward.cosmeticItemId : "Cosmetic",
                RewardType.PowerUp => "Power-Up",
                RewardType.Hint => "Hint",
                _ => "Reward"
            };
            nextRewardText.text = $"{_localization?.GetString("next_reward") ?? "Next"}: {rewardLabel}";
        }

        private void UpdatePlayerLevel()
        {
            if (playerLevelText != null)
            {
                playerLevelText.text = $"{_localization?.GetString("level_short") ?? "Lv"}. {_progress.PlayerLevel}";
            }
            if (totalXpText != null)
            {
                totalXpText.text = $"{_localization?.GetString("total_xp") ?? "Total XP"}: {_progress.TotalXp}";
            }
        }
    }
}
