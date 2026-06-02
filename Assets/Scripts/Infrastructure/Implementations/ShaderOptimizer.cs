using System.Collections.Generic;
using UnityEngine;
using PuzzleGame.Domain;
using PuzzleGame.Logging;

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

        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private readonly Dictionary<int, Color> _colorCache = new Dictionary<int, Color>();

        private static readonly int FillLevelId = Shader.PropertyToID("_FillLevel");
        private static readonly int LiquidColorId = Shader.PropertyToID("_LiquidColor");
        private static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");
        private static readonly int WaveAmplitudeId = Shader.PropertyToID("_WaveAmplitude");

        public void Initialize(bool applyMobileDefaults)
        {
            if (!applyMobileDefaults)
            {
                BottleLogger.LogInfo("[ShaderOptimizer] Mobile defaults skipped — user Quality Settings preserved.");
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

            BottleLogger.LogInfo("[ShaderOptimizer] Mobile GPU defaults applied.");
        }

        public void SetLiquidFill(Renderer renderer, float fillLevel, Color? color = null)
        {
            if (renderer == null) return;

            _propertyBlock.Clear();
            _propertyBlock.SetFloat(FillLevelId, fillLevel);

            if (color.HasValue)
                _propertyBlock.SetColor(LiquidColorId, color.Value);

            renderer.SetPropertyBlock(_propertyBlock);
        }

        public void SetLiquidFills(Renderer[] renderers, float fillLevel, Color color)
        {
            if (renderers == null) return;

            _propertyBlock.Clear();
            _propertyBlock.SetFloat(FillLevelId, fillLevel);
            _propertyBlock.SetColor(LiquidColorId, color);

            foreach (var r in renderers)
            {
                if (r != null)
                    r.SetPropertyBlock(_propertyBlock);
            }
        }

        public void ApplyLowQualityMode()
        {
            Shader.globalMaximumLOD = LowEndShaderMaximumLOD;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 0;

            BottleLogger.LogInfo("[ShaderOptimizer] Low-quality mode applied (thermal throttling).");
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
        void SetLiquidFill(Renderer renderer, float fillLevel, Color? color = null);
        void SetLiquidFills(Renderer[] renderers, float fillLevel, Color color);
        void ApplyLowQualityMode();
        int GetRecommendedQualityLevel();
    }
}
