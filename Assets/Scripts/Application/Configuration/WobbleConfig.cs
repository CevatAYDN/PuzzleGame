using UnityEngine;

namespace PuzzleGame.Application.Configuration
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
        [Tooltip("Submesh index for the Ore material on the MeshRenderer")]
        public int OreMaterialIndex = 1;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (updateInterval < 0.001f)
            {
                updateInterval = 0.001f;
                UnityEditor.EditorUtility.SetDirty(this);
            }
            if (maxWobble < 0f) maxWobble = 0f;
            if (wobbleSpeed < 0f) wobbleSpeed = 0f;
            if (recoveryRate < 0f) recoveryRate = 0f;
            if (movementMultiplier < 0f) movementMultiplier = 0f;
            if (rotationMultiplier < 0f) rotationMultiplier = 0f;
            if (OreMaterialIndex < 0) OreMaterialIndex = 0;
        }
#endif
    }
}
