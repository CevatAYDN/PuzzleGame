using UnityEngine;

namespace PuzzleGame.Configuration
{
    [CreateAssetMenu(fileName = "BottleVisualConfig", menuName = "PuzzleGame/BottleVisualConfig")]
    public class BottleVisualConfig : ScriptableObject
    {
        [Header("Color")]
        public float saturationBoost = 1.35f;
        public float brightnessBoost = 1.2f;

        [Header("Capacity")]
        [Range(1, 8)] public int maxLayers = 4;

        [Header("Pour Effect")]
        public float pourImpulseStrength = 2.0f;
    }
}
