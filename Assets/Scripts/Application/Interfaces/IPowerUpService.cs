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

        /// <summary>
        /// Applies ColorBomb power-up: merges adjacent same-color layers in the specified mold.
        /// Molds provider is required for runtime mold state access.
        /// </summary>
        void ApplyColorBomb(IActiveMoldsProvider molds, int moldIndex);

        /// <summary>
        /// Applies Shuffle power-up: collects all layers from all molds, shuffles them
        /// using Fisher-Yates, and redistributes across molds respecting max layer counts.
        /// </summary>
        void ApplyShuffle(IActiveMoldsProvider molds);
    }
}
