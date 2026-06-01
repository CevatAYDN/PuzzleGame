using UnityEngine;

namespace BottleShaders.Configuration
{
    [CreateAssetMenu(fileName = "AnimationConfig", menuName = "BottleGame/AnimationConfig")]
    public class AnimationConfig : ScriptableObject
    {
        public float liftHeight = 1.0f;
        public float liftDuration = 0.4f;
        public float pourDuration = 0.6f;
        public float returnDuration = 0.4f;

        [Header("Juice & Hover")]
        public float hoverAmplitude = 0.08f;
        public float hoverFrequency = 3.5f;

        [Header("Liquid Stream")]
        public float streamWidth = 0.08f;

        [Header("Error Shake")]
        public float shakeDuration = 0.25f;
        public float shakeAngle = 8f;

        [Header("Pour Phases (portions of pourDuration, must sum to 1.0)")]
        [Range(0f, 1f)] public float tiltPhasePortion = 0.25f;
        [Range(0f, 1f)] public float flowPhasePortion = 0.50f;
        [Range(0f, 1f)] public float returnPhasePortion = 0.25f;
    }
}
