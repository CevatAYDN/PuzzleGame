using UnityEngine;

namespace PuzzleGame.Application.Configuration
{
    /// <summary>
    /// Developer configuration for pouring simulation parameters.
    /// Used by PourSystemController and DebugOverlayUI at runtime.
    /// Create via Tools > PuzzleGame > Open Editor > Data tab.
    /// </summary>
    [CreateAssetMenu(fileName = "PourConfig", menuName = "PuzzleGame/MysticCrafter/PourConfig")]
    public class PourConfig : ScriptableObject
    {
        [Header("Pour Physics")]
        [Tooltip("Base duration for a full pour animation.")]
        [Range(0.2f, 5f)] public float pourDuration = 1.2f;

        [Tooltip("Multiplier applied to duration when mold is far from source.")]
        [Range(1f, 3f)] public float distanceSpeedMultiplier = 1.5f;

        [Tooltip("Minimum flow intensity during pour.")]
        [Range(0f, 2f)] public float minFlowIntensity = 0.3f;

        [Tooltip("Maximum flow intensity during pour.")]
        [Range(0f, 2f)] public float maxFlowIntensity = 1.5f;

        [Header("VFX Overrides")]
        [Tooltip("If true, PourConfig values override StreamVFXConfig at runtime.")]
        public bool overrideVFX = false;
        public float flowIntensity = 0.8f;
        public float streamWidth = 0.08f;
        public float streamArcHeight = 0.1f;

        [Header("Preview")]
        [Tooltip("Show pour preview information before executing.")]
        public bool showPourPreview = true;

        [Tooltip("Highlight mold boundaries that will be affected.")]
        public bool showAffectedBoundaries = false;
    }
}
