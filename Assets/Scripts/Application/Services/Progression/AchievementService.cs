using System;
using System.Collections.Generic;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    public sealed class AchievementService : IAchievementService, IDisposable
    {
        private const string LogTag = "[Achievement]";
        private const string PrefsPrefix = "PuzzleGame.Achievement.";
        private static readonly int[] RewardCoins =
        {
            25,   // FirstLevel
            50,   // TenLevels
            100,  // TwentyFiveLevels
            200,  // AllLevels
            10,   // FirstPowerUp
            75,   // AllPowerUps
            50,   // DailyStreak7
            200,  // DailyStreak30
            100,  // ThreeStarMaster
            75,   // PerfectForge
            50,   // SpeedDemon
            50,   // ColorMixer
        };

        private readonly IEventAggregator _events;
        private readonly ICoinWallet _wallet;
        private readonly Dictionary<AchievementId, AchievementState> _states = new Dictionary<AchievementId, AchievementState>();

        public event Action<AchievementId> OnUnlocked;

        private static readonly (AchievementId id, int target, string name)[] Definitions =
        {
            (AchievementId.FirstLevel,      1,  "First Steps"),
            (AchievementId.TenLevels,       10, "Getting Started"),
            (AchievementId.TwentyFiveLevels,25, "Halfway There"),
            (AchievementId.AllLevels,       50, "Grand Master"),
            (AchievementId.FirstPowerUp,    1,  "Power Up!"),
            (AchievementId.AllPowerUps,     5,  "Full Arsenal"),
            (AchievementId.DailyStreak7,    7,  "Week Warrior"),
            (AchievementId.DailyStreak30,   30, "Monthly Champion"),
            (AchievementId.ThreeStarMaster, 10, "Perfectionist"),
            (AchievementId.PerfectForge,    5,  "Forge Master"),
            (AchievementId.SpeedDemon,      3,  "Speed Demon"),
            (AchievementId.ColorMixer,      5,  "Color Alchemist"),
        };

        public AchievementService(IEventAggregator events, ICoinWallet wallet)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            LoadStates();
            _events.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            _events.Subscribe<PowerUpActivatedEvent>(OnPowerUpActivated);
        }

        public void Dispose()
        {
            _events.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            _events.Unsubscribe<PowerUpActivatedEvent>(OnPowerUpActivated);
        }

        public IReadOnlyList<AchievementState> GetAll()
        {
            var list = new List<AchievementState>(_states.Count);
            foreach (var kv in _states)
                list.Add(kv.Value);
            return list;
        }

        public bool IsUnlocked(AchievementId id) =>
            _states.TryGetValue(id, out var s) && s.Unlocked;

        private void LoadStates()
        {
            foreach (var (id, target, _) in Definitions)
            {
                var progress = PlayerPrefs.GetInt(PrefsPrefix + id + "_progress", 0);
                var unlocked = PlayerPrefs.GetInt(PrefsPrefix + id + "_unlocked", 0) == 1;
                var ticks = PlayerPrefs.GetString(PrefsPrefix + id + "_at", string.Empty);
                var at = long.TryParse(ticks, out long t) ? new DateTime(t, DateTimeKind.Utc) : DateTime.MinValue;
                _states[id] = new AchievementState(id, unlocked, at, progress, target);
            }
        }

        private void OnLevelCompleted(LevelCompletedEvent e)
        {
            IncrementProgress(AchievementId.FirstLevel);
            IncrementProgress(AchievementId.TenLevels);
            IncrementProgress(AchievementId.TwentyFiveLevels);
            IncrementProgress(AchievementId.AllLevels);

            if (e.Stars == 3)
                IncrementProgress(AchievementId.ThreeStarMaster);

            if (e.CompletionTimeSeconds > 0f && e.CompletionTimeSeconds < 60f)
                IncrementProgress(AchievementId.SpeedDemon);
        }

        private void OnPowerUpActivated(PowerUpActivatedEvent e)
        {
            IncrementProgress(AchievementId.FirstPowerUp);
            IncrementProgress(AchievementId.AllPowerUps);
        }

        public void IncrementProgress(AchievementId id)
        {
            if (!_states.TryGetValue(id, out var state) || state.Unlocked)
                return;

            var next = state.WithProgress(state.Progress + 1);
            _states[id] = next;
            SaveState(next);

            if (next.Unlocked)
            {
                MoldLogger.LogInfo($"{LogTag} Unlocked: {id}");
                GrantCoinReward(id);
                OnUnlocked?.Invoke(id);
            }
        }

        public void TrackStreakClaimed(int currentStreak)
        {
            SetProgress(AchievementId.DailyStreak7, currentStreak);
            SetProgress(AchievementId.DailyStreak30, currentStreak);
        }

        public void TrackColorMix(int moldIndex) => IncrementProgress(AchievementId.ColorMixer);
        public void TrackPerfectForge() => IncrementProgress(AchievementId.PerfectForge);
        public void TrackSpeedRun(float seconds) { if (seconds < 60f) IncrementProgress(AchievementId.SpeedDemon); }

        private void SetProgress(AchievementId id, int value)
        {
            if (!_states.TryGetValue(id, out var state) || state.Unlocked)
                return;

            var next = state.WithProgress(value);
            _states[id] = next;
            SaveState(next);

            if (next.Unlocked)
            {
                MoldLogger.LogInfo($"{LogTag} Unlocked: {id}");
                GrantCoinReward(id);
                OnUnlocked?.Invoke(id);
            }
        }

        private void GrantCoinReward(AchievementId id)
        {
            int index = (int)id;
            if (index < 0 || index >= RewardCoins.Length) return;
            int amount = RewardCoins[index];
            if (amount > 0 && _wallet != null)
            {
                _wallet.Add(amount, $"achievement_{id}");
                MoldLogger.LogInfo($"{LogTag} Reward: +{amount} coins for {id}");
            }
        }

        private void SaveState(AchievementState state)
        {
            PlayerPrefs.SetInt(PrefsPrefix + state.Id + "_progress", state.Progress);
            PlayerPrefs.SetInt(PrefsPrefix + state.Id + "_unlocked", state.Unlocked ? 1 : 0);
            if (state.Unlocked)
                PlayerPrefs.SetString(PrefsPrefix + state.Id + "_at", state.UnlockedAt.Ticks.ToString());
            PlayerPrefs.Save();
        }
    }
}
