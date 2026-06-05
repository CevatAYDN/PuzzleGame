using UnityEngine;

namespace PuzzleGame.Application.Configuration
{
    /// <summary>
    /// Centralized UI style tokens. Load via Resources.Load&lt;UIStyleConfig&gt;("Data/UIStyleConfig")
    /// or assign in the inspector of any UI screen.
    /// Replaces ad-hoc colors / sizes sprinkled across prefabs.
    /// </summary>
    [CreateAssetMenu(fileName = "UIStyleConfig", menuName = "PuzzleGame/UIStyleConfig")]
    public class UIStyleConfig : ScriptableObject
    {
        [Header("Palette")]
        public Color background = new Color(0.08f, 0.05f, 0.16f, 1f);
        public Color panel = new Color(0.12f, 0.10f, 0.22f, 0.95f);
        public Color primaryAccent = new Color(0.95f, 0.65f, 0.20f);
        public Color dangerAccent = new Color(0.85f, 0.20f, 0.20f);
        public Color successAccent = new Color(0.25f, 0.85f, 0.45f);
        public Color textPrimary = Color.white;
        public Color textSecondary = new Color(0.7f, 0.7f, 0.75f);

        [Header("Sizes")]
        [Range(8, 96)] public int titleFontSize = 48;
        [Range(8, 96)] public int headingFontSize = 32;
        [Range(8, 96)] public int bodyFontSize = 22;
        [Range(8, 96)] public int captionFontSize = 16;

        [Header("Buttons")]
        [Range(40, 200)] public int buttonHeight = 64;
        [Range(80, 600)] public int buttonWidth = 240;
        [Range(0f, 0.5f)] public float buttonCornerRadius = 0.1f;

        [Header("Spacing")]
        [Range(0f, 64f)] public float defaultPadding = 16f;
        [Range(0f, 64f)] public float defaultSpacing = 12f;
        [Range(0f, 200f)] public float titleToBodySpacing = 32f;
    }
}
