using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Stores power-up charge counts in <c>UnityEngine.PlayerPrefs</c>
    /// with a <c>"PowerUp_"</c> key prefix.
    /// </summary>
    public sealed class PlayerPrefsChargeStorage : IChargeStorageService
    {
        private const string KeyPrefix = "PowerUp_";

        public int GetCharge(PowerUpType type, int defaultValue)
        {
            return PlayerPrefs.GetInt($"{KeyPrefix}{type}", defaultValue);
        }

        public void SetCharge(PowerUpType type, int value)
        {
            PlayerPrefs.SetInt($"{KeyPrefix}{type}", value);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
