using UnityEngine;

namespace PuzzleGame.Configuration
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "PuzzleGame/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public LayerMask bottleLayerMask = ~0;
        public float colorMatchTolerance = 0.05f;
        public int maxLayersPerBottle = 4;

        [Header("Visuals")]
        public float saturationBoost = 1.35f;
        public float brightnessBoost = 1.2f;
    }
}
