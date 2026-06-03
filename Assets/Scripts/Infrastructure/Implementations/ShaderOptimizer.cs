using System.Collections.Generic;
using UnityEngine;
using PuzzleGame.Domain;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Runtime shader optimization service.
    /// Pure Infrastructure — Unity-specific shader management.
    /// Quality override is OPT-IN: caller passes a flag; no silent mutation of
    /// <see cref="QualitySettings"/>.
    /// </summary>
    public class ShaderOptimizer : IShaderOptimizer
    {
        private const int MobileQualityLevelIndex = 1;
        private const int MobileShaderMaximumLOD = 200;
        private const int LowEndShaderMaximumLOD = 100;

        public void Initialize(bool applyMobileDefaults)
        {
            if (!applyMobileDefaults)
            {
                Debug.Log("[ShaderOptimizer] Mobile defaults skipped — user Quality Settings preserved.");
                return;
            }

            QualitySettings.SetQualityLevel(
                QualitySettings.names.Length > MobileQualityLevelIndex
                    ? MobileQualityLevelIndex
                    : 0,
                true);
            Shader.globalMaximumLOD = MobileShaderMaximumLOD;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;

            Debug.Log("[ShaderOptimizer] Mobile GPU defaults applied.");
        }

        public void ApplyLowQualityMode()
        {
            Shader.globalMaximumLOD = LowEndShaderMaximumLOD;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 0;

            Debug.Log("[ShaderOptimizer] Low-quality mode applied (thermal throttling).");
        }

        public int GetRecommendedQualityLevel()
        {
            int cpuCores = SystemInfo.processorCount;
            int memMb = SystemInfo.systemMemorySize;

            if (cpuCores >= 6 && memMb >= 6144) return 2;
            if (cpuCores >= 4 && memMb >= 3072) return 1;
            return 0;
        }
    }

    public interface IShaderOptimizer
    {
        void Initialize(bool applyMobileDefaults);
        void ApplyLowQualityMode();
        int GetRecommendedQualityLevel();
    }
}
