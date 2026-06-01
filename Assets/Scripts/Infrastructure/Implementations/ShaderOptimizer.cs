using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Runtime shader optimization service.
    /// - Sets global GPU quality level
    /// - Manages MaterialPropertyBlocks for efficient per-instance updates
    /// - Applies keyword overrides for mobile
    ///
    /// Clean architecture: Infrastructure — Unity-specific shader management.
    /// </summary>
    public class ShaderOptimizer : IShaderOptimizer
    {
        // Shared MaterialPropertyBlock — reused to avoid allocations
        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private readonly Dictionary<int, Color> _colorCache = new Dictionary<int, Color>();

        private static readonly int FillLevelId = Shader.PropertyToID("_FillLevel");
        private static readonly int LiquidColorId = Shader.PropertyToID("_LiquidColor");
        private static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");
        private static readonly int WaveAmplitudeId = Shader.PropertyToID("_WaveAmplitude");

        public void Initialize()
        {
            // Set mobile-friendly quality defaults
            QualitySettings.SetQualityLevel(QualitySettings.names.Length > 1 ? 1 : 0, true);
            Shader.globalMaximumLOD = 200; // Cap shader complexity
            QualitySettings.shadows = ShadowQuality.Disable; // No shadows for mobile puzzle
            QualitySettings.shadowDistance = 0f;

            BottleLogger.LogInfo("[ShaderOptimizer] Mobile GPU defaults applied.");
        }

        /// <summary>
        /// Set liquid fill level on a renderer without material instantiation.
        /// Uses MaterialPropertyBlock — zero GC alloc, GPU-friendly.
        /// </summary>
        public void SetLiquidFill(Renderer renderer, float fillLevel, Color? color = null)
        {
            if (renderer == null) return;

            _propertyBlock.Clear();
            _propertyBlock.SetFloat(FillLevelId, fillLevel);

            if (color.HasValue)
                _propertyBlock.SetColor(LiquidColorId, color.Value);

            renderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// Bulk-update multiple liquid fill levels. Batches property blocks.
        /// </summary>
        public void SetLiquidFills(Renderer[] renderers, float fillLevel, Color color)
        {
            _propertyBlock.Clear();
            _propertyBlock.SetFloat(FillLevelId, fillLevel);
            _propertyBlock.SetColor(LiquidColorId, color);

            foreach (var r in renderers)
            {
                if (r != null)
                    r.SetPropertyBlock(_propertyBlock);
            }
        }

        /// <summary>
        /// Apply low-end mobile quality settings at runtime.
        /// Call when device thermal throttling is detected.
        /// </summary>
        public void ApplyLowQualityMode()
        {
            Shader.globalMaximumLOD = 100;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 0;

            BottleLogger.LogInfo("[ShaderOptimizer] Low-quality mode applied (thermal throttling).");
        }

        /// <summary>
        /// Get the appropriate quality level based on device capabilities.
        /// </summary>
        public int GetRecommendedQualityLevel()
        {
            int cpuCores = SystemInfo.processorCount;
            int memMb = SystemInfo.systemMemorySize;

            // High-end: 6+ cores, 6GB+ RAM
            if (cpuCores >= 6 && memMb >= 6144) return 2;
            // Mid-range: 4+ cores, 3GB+ RAM
            if (cpuCores >= 4 && memMb >= 3072) return 1;
            // Low-end
            return 0;
        }
    }

    /// <summary>
    /// Contract for runtime shader optimization.
    /// </summary>
    public interface IShaderOptimizer
    {
        void Initialize();
        void SetLiquidFill(Renderer renderer, float fillLevel, Color? color = null);
        void SetLiquidFills(Renderer[] renderers, float fillLevel, Color color);
        void ApplyLowQualityMode();
        int GetRecommendedQualityLevel();
    }
}
