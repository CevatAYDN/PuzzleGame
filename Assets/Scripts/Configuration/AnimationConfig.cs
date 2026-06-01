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
    }
}
