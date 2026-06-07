using System;
using System.Security.Cryptography;
using System.Text;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Persistent coin wallet backed by PlayerPrefs.
    /// Secured with HMAC-SHA256 signature verification to prevent local file tampering.
    /// Fires <see cref="OnBalanceChanged"/> on every mutation.
    /// </summary>
    public sealed class CoinWallet : ICoinWallet
    {
        private const string PrefsKey = "PuzzleGame.CoinBalance";
        private const string HashKey = "PuzzleGame.CoinBalance.Hash";
        private const string LogTag = "[CoinWallet]";

        private readonly EconomyConfig _config;
        private int _balance;

        public int Balance => _balance;
        public event Action<int> OnBalanceChanged;

        public CoinWallet(EconomyConfig config)
        {
            _config = config;
            int defaultCoins = config != null ? config.startingCoins : 0;

            if (PlayerPrefs.HasKey(PrefsKey))
            {
                int savedBalance = PlayerPrefs.GetInt(PrefsKey, defaultCoins);

                if (PlayerPrefs.HasKey(HashKey))
                {
                    string savedHash = PlayerPrefs.GetString(HashKey);
                    string computedHash = ComputeHash(savedBalance);

                    if (CryptographicEquals(savedHash, computedHash))
                    {
                        _balance = savedBalance;
                    }
                    else
                    {
                        MoldLogger.LogWarning($"{LogTag} Security check failed: Coin balance signature mismatch! Local save file has been tampered with. Resetting to default.");
                        _balance = defaultCoins;
                        Persist();
                    }
                }
                else
                {
                    // Migration path: balance exists but hash is not set yet.
                    // Compute and save it now to prevent resetting legacy players.
                    _balance = savedBalance;
                    Persist();
                }
            }
            else
            {
                _balance = defaultCoins;
                Persist();
            }
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
            PlayerPrefs.SetString(HashKey, ComputeHash(_balance));
            PlayerPrefs.Save();
        }

        private string ComputeHash(int balance)
        {
            string deviceId = GetOrCreateDeviceId();
            string deviceModel = SystemInfo.deviceModel ?? "unknown";
            const string pepper = "PG-Coin-v1-S3cr3tKey";
            string secretKey = $"{deviceId}:{deviceModel}:{pepper}";

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] data = Encoding.UTF8.GetBytes(balance.ToString());
                byte[] hash = hmac.ComputeHash(data);
                
                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private string GetOrCreateDeviceId()
        {
            const string DeviceIdKey = "PuzzleGame.DeviceId";
            string deviceId = PlayerPrefs.GetString(DeviceIdKey, string.Empty);
            
            if (string.IsNullOrEmpty(deviceId))
            {
                string unityDeviceId = SystemInfo.deviceUniqueIdentifier;
                if (!string.IsNullOrEmpty(unityDeviceId) && unityDeviceId != "UNKNOWN")
                {
                    deviceId = unityDeviceId;
                }
                else
                {
                    var bytes = new byte[16];
                    using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                        rng.GetBytes(bytes);
                    deviceId = Convert.ToBase64String(bytes);
                }
                PlayerPrefs.SetString(DeviceIdKey, deviceId);
                PlayerPrefs.Save();
            }
            return deviceId;
        }

        private static bool CryptographicEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
