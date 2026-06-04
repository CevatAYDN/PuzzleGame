using UnityEngine;

namespace PuzzleGame.Application.Configuration
{
    /// <summary>
    /// Configuration for the MagmaFlow VFX Graph stream renderer.
    /// Controls visual quality, particle behavior, bounds scaling, and trail effects.
    /// </summary>
    [CreateAssetMenu(fileName = "StreamVFXConfig", menuName = "PuzzleGame/MysticCrafter/StreamVFXConfig")]
    public class StreamVFXConfig : ScriptableObject
    {
        [Header("Flow Intensity")]
        [Tooltip("Base flow intensity multiplier. Higher = denser, brighter stream.")]
        [Range(0f, 5f)]
        public float flowIntensity = 1.2f;

        [Tooltip("HDR color boost applied to OreColor before sending to VFX.")]
        [Range(0.5f, 5f)]
        public float colorIntensityBoost = 1.5f;

        [Tooltip("Multiplier applied to the bell-curve peak during flow phase.")]
        [Range(0.5f, 3f)]
        public float streamWidthMultiplier = 1f;

        [Header("Particle Capacity")]
        [Tooltip("Maximum particle count in the VFX Graph.")]
        [Min(1)]
        public int particleCapacity = 128;

        [Header("Bounds")]
        [Tooltip("Base bounds radius for the VFX. Scaled by distance when scaleBoundsWithDistance is enabled.")]
        [Min(0.1f)]
        public float boundsRadius = 3f;

        [Tooltip("When true, VFX bounds grow proportionally with source-target distance.")]
        public bool scaleBoundsWithDistance = true;

        [Tooltip("Bounds radius multiplier per unit of distance between molds.")]
        [Min(0.5f)]
        public float boundsScalePerUnit = 1.5f;

        [Header("Color Hint")]
        [Tooltip("Optional editor preview color. Runtime color comes from ore layer data.")]
        public Color streamColorHint = new Color(0.98f, 0.72f, 0.05f, 1f);

        [Header("Distance-Scaled Particles")]
        [Tooltip("When true, particle count scales with distance between molds.")]
        public bool useDistanceScaledParticles = true;

        [Tooltip("Base particle count per unit of distance between molds.")]
        [Min(1f)]
        public float particlesPerUnitDistance = 40f;

        [Header("Trail Effect")]
        [Tooltip("When true, a fading trail follows the stream during pour.")]
        public bool enableTrail = true;

        [Tooltip("Trail fade-out duration after pour completes.")]
        [Range(0.1f, 2f)]
        public float trailFadeDuration = 0.5f;

        [Tooltip("Trail alpha at peak intensity.")]
        [Range(0.05f, 0.5f)]
        public float trailAlpha = 0.15f;

        [Tooltip("Number of trail position samples in the ring buffer.")]
        [Range(4, 32)]
        public int trailSampleCount = 8;

        private void OnValidate()
        {
            if (flowIntensity < 0f) flowIntensity = 0f;
            if (colorIntensityBoost < 0.5f) colorIntensityBoost = 0.5f;
            if (particleCapacity < 1) particleCapacity = 1;
            if (boundsRadius < 0.1f) boundsRadius = 0.1f;
            if (trailFadeDuration < 0.1f) trailFadeDuration = 0.1f;
            if (trailSampleCount < 4) trailSampleCount = 4;
        }
    }
}
