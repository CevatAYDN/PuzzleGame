using System.Collections.Generic;
using UnityEngine;
using PuzzleGame.Domain;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

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
                MoldLogger.LogInfo("[ShaderOptimizer] Mobile defaults skipped — user Quality Settings preserved.");
                return;
            }

            QualitySettings.SetQualityLevel(
                QualitySettings.names.Length > MobileQualityLevelIndex
                    ? MobileQualityLevelIndex
                    : 0,
                true);
            
            if (!UnityEngine.Application.isEditor)
            {
                Shader.globalMaximumLOD = MobileShaderMaximumLOD;
            }
            
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0f;

            MoldLogger.LogInfo("[ShaderOptimizer] Mobile GPU defaults applied.");
        }

        public void ApplyLowQualityMode()
        {
            if (!UnityEngine.Application.isEditor)
            {
                Shader.globalMaximumLOD = LowEndShaderMaximumLOD;
            }
            
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 0;

            MoldLogger.LogInfo("[ShaderOptimizer] Low-quality mode applied (thermal throttling).");
        }

        public int GetRecommendedQualityLevel()
        {
            int cpuCores = SystemInfo.processorCount;
            int memMb = SystemInfo.systemMemorySize;

            if (cpuCores >= 6 && memMb >= 6144) return 2;
            if (cpuCores >= 4 && memMb >= 3072) return 1;
            return 0;
        }
        public void OptimizeMaterial(Material mat)
        {
            if (mat == null) return;
            mat.shader.maximumLOD = Shader.globalMaximumLOD;
        }
    }
}
