using UnityEngine;
using PuzzleGame.Domain;

namespace PuzzleGame.Configuration
{
    [CreateAssetMenu(fileName = "BottleVisualConfig", menuName = "PuzzleGame/BottleVisualConfig")]
    public class BottleVisualConfig : ScriptableObject
    {
        [Header("Color")]
        public float saturationBoost = BottleConstants.DefaultSaturationBoost;
        public float brightnessBoost = BottleConstants.DefaultBrightnessBoost;

        [Header("Capacity")]
        [Range(1, BottleConstants.MaxLayers)]
        [Tooltip("Maximum liquid layers per bottle. Must be in [1, BottleConstants.MaxLayers].")]
        public int maxLayers = BottleConstants.DefaultLayerCapacity;

        [Header("Pour Effect")]
        [Min(0f)]
        public float pourImpulseStrength = BottleConstants.DefaultPourImpulseStrength;

        [Header("Material Indices")]
        [Tooltip("Submesh index for the glass material on the MeshRenderer")]
        [Min(0)] public int glassMaterialIndex = BottleConstants.DefaultGlassMaterialIndex;
        [Tooltip("Submesh index for the liquid material on the MeshRenderer")]
        [Min(0)] public int liquidMaterialIndex = BottleConstants.DefaultLiquidMaterialIndex;

        private void OnValidate()
        {
            if (maxLayers < 1) maxLayers = 1;
            if (maxLayers > BottleConstants.MaxLayers) maxLayers = BottleConstants.MaxLayers;
            if (glassMaterialIndex < 0) glassMaterialIndex = 0;
            if (liquidMaterialIndex < 0) liquidMaterialIndex = 0;
        }
    }
}
