using UnityEngine;

namespace PuzzleGame.Configuration
{
    [CreateAssetMenu(fileName = "WobbleConfig", menuName = "PuzzleGame/WobbleConfig")]
    public class WobbleConfig : ScriptableObject
    {
        [Header("Wobble Strength")]
        public float maxWobble = 0.05f;

        [Header("Animation")]
        public float wobbleSpeed = 6.0f;
        public float recoveryRate = 1.5f;

        [Header("Sensitivity")]
        public float movementMultiplier = 1.0f;
        public float rotationMultiplier = 0.15f;

        [Header("Performance")]
        [Tooltip("Wobble update interval (s). 0.033 = ~30fps, 0.05 = 20fps.")]
        public float updateInterval = 0.05f;

        [Header("Material Indices")]
        [Tooltip("Submesh index for the liquid material on the MeshRenderer")]
        public int liquidMaterialIndex = 1;
    }
}
