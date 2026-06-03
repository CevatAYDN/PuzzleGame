using UnityEngine;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Runtime shader optimization service contract.
    /// Infrastructure implementasyonu Unity'ye özel shader yönetimi yapar.
    /// </summary>
    public interface IShaderOptimizer
    {
        /// <summary>
        /// Initialize shader quality with optional mobile defaults.
        /// Does NOT mutate QualitySettings unless explicitly requested.
        /// </summary>
        void Initialize(bool applyMobileDefaults);

        /// <summary>
        /// Apply aggressive low-quality mode (e.g., thermal throttling).
        /// </summary>
        void ApplyLowQualityMode();

        /// <summary>
        /// Get the recommended quality level for the current device.
        /// </summary>
        int GetRecommendedQualityLevel();

        /// <summary>
        /// Optimize a specific material for the current platform.
        /// </summary>
        void OptimizeMaterial(Material mat);
    }
}
