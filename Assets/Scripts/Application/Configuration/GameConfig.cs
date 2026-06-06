using UnityEngine;
using PuzzleGame.Domain;

namespace PuzzleGame.Application.Configuration
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "PuzzleGame/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Layer Mask")]
        public LayerMask MoldLayerMask = ~0;

        [Header("Validation")]
        [Tooltip("Per-channel color match tolerance for the validator. Must be > 0. " +
                 "Wired into MoldValidationService via VContainer.")]
        [Min(0.0001f)]
        public float colorMatchTolerance = ForgeConstants.ColorMatchEpsilon;

        [Header("Mold Capacity")]
        [Tooltip("Default maximum Ore layers per Mold. Must be in [1, ForgeConstants.MaxLayers].")]
        [Range(1, ForgeConstants.MaxLayers)]
        public int maxLayersPerMold = ForgeConstants.DefaultLayerCapacity;

        [Header("Visuals (defaults)")]
        public float saturationBoost = 1.25f;
        public float brightnessBoost = 1.15f;

        [Header("Mobile Shader Optimizer")]
        [Tooltip("When enabled, ShaderOptimizer.Initialize() forces mobile quality defaults " +
                 "(shadows disabled, low LOD). Disable to respect user Quality Settings.")]
        public bool applyMobileShaderDefaults = true;

        [Header("Level Solver")]
        [Tooltip("BFS visit budget for the OreSortSolver. Beyond this it gives up gracefully.")]
        [Min(100)]
        public int solverMaxVisitedStates = ForgeConstants.SolverMaxVisitedStates;

        [Header("Ads Settings")]
        [Tooltip("Enable or disable ads completely in the game.")]
        public bool enableAds = true;

        [Tooltip("Show an interstitial ad every N completed levels.")]
        [Range(1, 10)]
        public int interstitialInterval = 3;

        [Tooltip("Delay in seconds before retrying to load an ad after a failure.")]
        [Min(5f)]
        public float adRetryDelay = 30f;

        private void OnValidate()
        {
            if (colorMatchTolerance <= 0f)
                colorMatchTolerance = ForgeConstants.ColorMatchEpsilon;
            if (maxLayersPerMold < 1)
                maxLayersPerMold = 1;
            if (maxLayersPerMold > ForgeConstants.MaxLayers)
                maxLayersPerMold = ForgeConstants.MaxLayers;
            if (adRetryDelay < 5f)
                adRetryDelay = 5f;
        }
    }
}
