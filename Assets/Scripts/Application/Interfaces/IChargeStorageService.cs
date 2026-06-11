using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Persistence contract for power-up charge counts.
    /// Separated from <see cref="IPowerUpService"/> to keep the Application
    /// layer free of <c>UnityEngine.PlayerPrefs</c> and to enable unit testing
    /// without filesystem side effects.
    /// </summary>
    public interface IChargeStorageService
    {
        /// <summary>Loads the stored charge count for a power-up type.</summary>
        int GetCharge(PowerUpType type, int defaultValue);

        /// <summary>Persists the charge count for a power-up type.</summary>
        void SetCharge(PowerUpType type, int value);

        /// <summary>Flushes all pending writes to the backing store.</summary>
        void Save();
    }
}
