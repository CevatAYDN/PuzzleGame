using UnityEngine;

namespace PuzzleGame.Application.Configuration
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "PuzzleGame/LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Level Generation")]
        public bool autoGenerateLevel = true;
        [Range(1, 6)] public int emptyMoldCount = 2;
        public int randomSeed = 0;

        [Header("Color Palette")]
        public Color[] palette = new Color[]
        {
            new Color(0.9f,  0.2f,  0.2f),
            new Color(0.2f,  0.6f,  0.9f),
            new Color(0.2f,  0.8f,  0.2f),
            new Color(0.95f, 0.9f,  0.2f),
            new Color(0.7f,  0.2f,  0.9f),
            new Color(0.9f,  0.5f,  0.2f),
        };

        [Header("Mold Visuals")]
        [Tooltip("Override per-level mold visual style. If null, the global MoldVisualConfig asset is used.")]
        public MoldVisualConfig moldVisualConfig;
    }
}
