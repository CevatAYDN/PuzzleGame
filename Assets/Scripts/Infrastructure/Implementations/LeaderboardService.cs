using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    public sealed class LeaderboardService : ILeaderboardService
    {
        private const string LogTag = "[Leaderboard]";
        private const string ScorePrefPrefix = "PuzzleGame.Leaderboard.Score.";
        private const string PourPrefPrefix = "PuzzleGame.Leaderboard.Pour.";
        private const string TimePrefPrefix = "PuzzleGame.Leaderboard.Time.";

        private readonly Dictionary<int, LeaderboardEntry> _entries = new Dictionary<int, LeaderboardEntry>();
        private IReadOnlyList<LeaderboardEntry> _cachedEntries;
        private bool _cacheDirty = true;
        private int _totalScore;
        private int _levelsCompleted;

        public int TotalScore => _totalScore;
        public int LevelsCompleted => _levelsCompleted;

        public LeaderboardService()
        {
            LoadAll();
        }

        public IReadOnlyList<LeaderboardEntry> GetAllEntries()
        {
            if (!_cacheDirty && _cachedEntries != null)
                return _cachedEntries;

            var list = new List<LeaderboardEntry>(_entries.Values);
            list.Sort((a, b) => a.LevelIndex.CompareTo(b.LevelIndex));
            _cachedEntries = list.AsReadOnly();
            _cacheDirty = false;
            return _cachedEntries;
        }

        public LeaderboardEntry GetEntry(int levelIndex)
        {
            _entries.TryGetValue(levelIndex, out var entry);
            return entry;
        }

        public bool TrySubmitScore(int levelIndex, int score, int pourCount)
        {
            if (score <= 0) return false;

            if (_entries.TryGetValue(levelIndex, out var existing) && existing.BestScore >= score)
                return false;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var entry = new LeaderboardEntry(levelIndex, score, pourCount, now);

            _entries[levelIndex] = entry;
            _cacheDirty = true;
            PlayerPrefs.SetInt(ScorePrefPrefix + levelIndex, score);
            PlayerPrefs.SetInt(PourPrefPrefix + levelIndex, pourCount);
            PlayerPrefs.SetString(TimePrefPrefix + levelIndex, now.ToString());
            PlayerPrefs.Save();

            RecalculateTotals();
            MoldLogger.LogInfo($"{LogTag} New best for level {levelIndex}: {score} pts, {pourCount} pours.");
            return true;
        }

        public void ResetAll()
        {
            foreach (var key in _entries.Keys)
            {
                PlayerPrefs.DeleteKey(ScorePrefPrefix + key);
                PlayerPrefs.DeleteKey(PourPrefPrefix + key);
                PlayerPrefs.DeleteKey(TimePrefPrefix + key);
            }
            _entries.Clear();
            _cacheDirty = true;
            _totalScore = 0;
            _levelsCompleted = 0;
            PlayerPrefs.Save();
            MoldLogger.LogInfo($"{LogTag} All data reset.");
        }

        private void LoadAll()
        {
            _entries.Clear();

            for (int i = 0; i < 1000; i++)
            {
                if (!PlayerPrefs.HasKey(ScorePrefPrefix + i)) continue;

                var score = PlayerPrefs.GetInt(ScorePrefPrefix + i, 0);
                if (score <= 0) continue;

                var pour = PlayerPrefs.GetInt(PourPrefPrefix + i, 0);
                var timeStr = PlayerPrefs.GetString(TimePrefPrefix + i, "0");
                long.TryParse(timeStr, out var time);

                _entries[i] = new LeaderboardEntry(i, score, pour, time);
            }

            RecalculateTotals();
            MoldLogger.LogInfo($"{LogTag} Loaded {_entries.Count} entries.");
        }

        private void RecalculateTotals()
        {
            _totalScore = 0;
            _levelsCompleted = _entries.Count;
            foreach (var entry in _entries.Values)
            {
                _totalScore += entry.BestScore;
            }
        }
    }
}
