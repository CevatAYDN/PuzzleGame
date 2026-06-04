using UnityEngine;
using PuzzleGame.Domain;

namespace PuzzleGame.Application.Configuration
{
    [CreateAssetMenu(fileName = "MoldVisualConfig", menuName = "PuzzleGame/MysticCrafter/MoldVisualConfig")]
    public class MoldVisualConfig : ScriptableObject
    {
        [Header("Mold Geometry")]
        public float moldHeight = 2.4f;
        
        [Header("Capacity")]
        [Range(1, 8)]
        [Tooltip("Maximum ore layers per mold. Must not exceed ForgeConstants.MaxLayers.")]
        public int maxLayers = 4;

        [Header("Material Indices")]
        [Tooltip("Submesh index for the stone/glass material on the MeshRenderer")]
        [Min(0)] public int moldMaterialIndex = 0;
        [Tooltip("Submesh index for the molten ore material on the MeshRenderer")]
        [Min(0)] public int oreMaterialIndex = 1;

        [Header("Ore Appearance")]
        public float saturationBoost = 1.25f;
        public float brightnessBoost = 1.15f;

        [Header("Mold Appearance (Empty)")]
        public Color moldEmptyColor = new Color(1.0f, 1.0f, 1.0f, 0.18f);

        [Header("Mold Tint (With Ore)")]
        [Tooltip("Base luminance added to mold when tinted with ore color.")]
        public float moldTintBase = 0.85f;
        [Tooltip("How much of the ore color tints the mold (0=pure white, 1=pure ore).")]
        public float moldTintMultiplier = 0.15f;
        [Tooltip("Alpha applied to the mold tint.")]
        public float moldTintAlpha = 0.25f;

        [Header("Sparkle & Surface")]
        [Tooltip("Global sparkle intensity for PremiumLayeredOre / MobileMold shaders.")]
        [Range(0f, 2f)] public float sparkleIntensity = 0.1f;
        [Tooltip("Sparkle grain size.")]
        [Range(1f, 32f)] public float sparkleSize = 12f;
        [Tooltip("Layer boundary darkening width in normalized height.")]
        [Range(0f, 0.05f)] public float layerBoundaryWidth = 0.012f;

        [Header("Animation Defaults")]
        public float wobbleBaseImpulse = 2.0f;
        public float wobbleTargetMultiplier = 0.8f;
        public float highlightActiveFresnel = 4.0f;
        public float highlightInactiveFresnel = 1.5f;
        public float completionFlashIntensity = 4.0f;
        public float completionFlashDuration = 0.6f;
        public float settleBounceDuration = 0.6f;
        
        [Header("Cast (Cast) Effect")]
        [Min(0f)]
        public float castImpulseStrength = 2.0f;

        [Header("Camera & Environment")]
        public Color cameraBackgroundColor = new Color(0.08f, 0.05f, 0.16f, 1.0f);
        public float winCheckDelaySeconds = 0.5f;

        private void OnValidate()
        {
            if (maxLayers < 1) maxLayers = 1;
            // Since ForgeConstants is not fully compiled if we have errors, we use literal 8 or ForgeConstants.MaxLayers
            if (maxLayers > ForgeConstants.MaxLayers) maxLayers = ForgeConstants.MaxLayers;
            if (moldMaterialIndex < 0) moldMaterialIndex = 0;
            if (oreMaterialIndex < 0) oreMaterialIndex = 0;
        }
    }
}
