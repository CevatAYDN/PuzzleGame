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
    }
}
