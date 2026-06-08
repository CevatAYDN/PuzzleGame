using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Contract for power-up activation and inventory management.
    /// Gameplay code checks CanActivate before calling Activate.
    /// Charges are consumed on successful activation.
    /// </summary>
    public interface IPowerUpService
    {
        /// <summary>Current charge count for the given power-up type.</summary>
        int GetCharges(PowerUpType type);

        /// <summary>Adds charges (e.g. from purchase, daily reward, level reward).</summary>
        void AddCharges(PowerUpType type, int count);

        /// <summary>Checks if the power-up can be activated (has charges + game state allows).</summary>
        bool CanActivate(PowerUpType type);

        /// <summary>
        /// Activates the power-up. Returns true if successful.
        /// Consumes one charge and publishes a PowerUpActivatedEvent.
        /// </summary>
        bool Activate(PowerUpType type, int moldIndex = -1);

        /// <summary>Resets all charges to defaults (e.g. new game, debug clear).</summary>
        void ResetAll();

        /// <summary>Returns descriptors for all power-up types (for UI rendering).</summary>
        PowerUpDescriptor[] GetAllDescriptors();
    }
}
