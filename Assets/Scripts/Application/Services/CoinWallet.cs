using System;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Persistent coin wallet backed by PlayerPrefs.
    /// Fires <see cref="OnBalanceChanged"/> on every mutation.
    /// </summary>
    public sealed class CoinWallet : ICoinWallet
    {
        private const string PrefsKey = "PuzzleGame.CoinBalance";
        private const string LogTag = "[CoinWallet]";

        private readonly EconomyConfig _config;
        private int _balance;

        public int Balance => _balance;
        public event Action<int> OnBalanceChanged;

        public CoinWallet(EconomyConfig config)
        {
            _config = config;
            _balance = PlayerPrefs.GetInt(PrefsKey, config != null ? config.startingCoins : 0);
        }

        public bool CanAfford(int amount) => amount >= 0 && _balance >= amount;

        public void Add(int amount, string reason)
        {
            if (amount <= 0) return;
            _balance += amount;
            Persist();
            MoldLogger.LogInfo($"{LogTag} +{amount} ({reason}). New balance: {_balance}.");
            OnBalanceChanged?.Invoke(_balance);
        }

        public bool TrySpend(int amount, string reason)
        {
            if (amount < 0) return false;
            if (!CanAfford(amount)) return false;
            _balance -= amount;
            Persist();
            MoldLogger.LogInfo($"{LogTag} -{amount} ({reason}). New balance: {_balance}.");
            OnBalanceChanged?.Invoke(_balance);
            return true;
        }

        private void Persist()
        {
            PlayerPrefs.SetInt(PrefsKey, _balance);
            PlayerPrefs.Save();
        }
    }
}
