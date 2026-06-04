using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PuzzleGame
{
    /// <summary>
    /// Helper to verify URP settings for mobile glass/Ore shaders.
    /// Read-only inspector — shows current pipeline config at a glance.
    /// </summary>
    [ExecuteInEditMode]
    public class URPConfigHelper : MonoBehaviour
    {
        [Header("Current URP Pipeline Settings")]
        [Tooltip("Auto-read on enable")]
        public bool autoReadOnEnable = true;

        private UniversalRenderPipelineAsset urpAsset;

        private void OnEnable()
        {
            if (autoReadOnEnable)
                ReadSettings();
        }

        [ContextMenu("Read URP Settings")]
        public void ReadSettings()
        {
            urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;

            if (urpAsset == null)
            {
                Debug.LogWarning("[URPConfigHelper] No URP asset found in Quality Settings.");
                return;
            }

            Debug.Log("=== URP Mobile Pipeline Settings ===");
            Debug.Log($"Render Scale: {urpAsset.renderScale}");
            Debug.Log($"MSAA: {urpAsset.msaaSampleCount}");
            Debug.Log($"Depth Texture: {urpAsset.supportsCameraDepthTexture}");
            Debug.Log($"Opaque Texture: {urpAsset.supportsCameraOpaqueTexture}");
            Debug.Log($"Additional Lights: {urpAsset.additionalLightsRenderingMode} (max {urpAsset.maxAdditionalLightsCount})");
            Debug.Log($"SRP Batcher: {urpAsset.useSRPBatcher}");
            Debug.Log($"HDR: {urpAsset.supportsHDR}");
            Debug.Log($"Shadow Distance: {urpAsset.shadowDistance}");
            Debug.Log($"Main Light Shadows: {urpAsset.mainLightRenderingMode}");
        }
    }
}
