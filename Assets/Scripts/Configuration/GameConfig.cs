using UnityEngine;
using PuzzleGame.Domain;

namespace PuzzleGame.Configuration
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "PuzzleGame/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Layer Mask")]
        public LayerMask bottleLayerMask = ~0;

        [Header("Validation")]
        [Tooltip("Per-channel color match tolerance for the validator. Must be > 0. " +
                 "Wired into BottleValidationService via VContainer.")]
        [Min(0.0001f)]
        public float colorMatchTolerance = BottleConstants.ColorMatchEpsilon;

        [Header("Bottle Capacity")]
        [Tooltip("Default maximum liquid layers per bottle. Must be in [1, BottleConstants.MaxLayers].")]
        [Range(1, BottleConstants.MaxLayers)]
        public int maxLayersPerBottle = BottleConstants.DefaultLayerCapacity;

        [Header("Visuals (compile-time defaults live in BottleConstants)")]
        public float saturationBoost = BottleConstants.DefaultSaturationBoost;
        public float brightnessBoost = BottleConstants.DefaultBrightnessBoost;

        [Header("Mobile Shader Optimizer")]
        [Tooltip("When enabled, ShaderOptimizer.Initialize() forces mobile quality defaults " +
                 "(shadows disabled, low LOD). Disable to respect user Quality Settings.")]
        public bool applyMobileShaderDefaults = true;

        [Header("Level Solver")]
        [Tooltip("BFS visit budget for the LiquidSortSolver. Beyond this it gives up gracefully.")]
        [Min(100)]
        public int solverMaxVisitedStates = BottleConstants.SolverMaxVisitedStates;

        private void OnValidate()
        {
            if (colorMatchTolerance <= 0f)
                colorMatchTolerance = BottleConstants.ColorMatchEpsilon;
            if (maxLayersPerBottle < 1)
                maxLayersPerBottle = 1;
            if (maxLayersPerBottle > BottleConstants.MaxLayers)
                maxLayersPerBottle = BottleConstants.MaxLayers;
        }
    }
}
