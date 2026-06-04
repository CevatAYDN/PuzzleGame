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
            new Color(0.95f, 0.20f, 0.25f),
            new Color(0.20f, 0.55f, 0.95f),
            new Color(0.30f, 0.85f, 0.35f),
            new Color(0.98f, 0.80f, 0.15f),
            new Color(0.70f, 0.30f, 0.90f),
            new Color(0.95f, 0.50f, 0.15f),
        };

        [Header("Mold Visuals")]
        [Tooltip("Override per-level mold visual style. If null, the global MoldVisualConfig asset is used.")]
        public MoldVisualConfig moldVisualConfig;
    }
}
