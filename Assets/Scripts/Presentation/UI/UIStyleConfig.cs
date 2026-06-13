using UnityEngine;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Holds the visual styling configuration (colors, fonts, materials) 
    /// for the UI generation and runtime, based on the OreSorter Stitch design.
    /// </summary>
    [CreateAssetMenu(fileName = "UIStyleConfig", menuName = "PuzzleGame/UI Style Config", order = 0)]
    public class UIStyleConfig : ScriptableObject
    {
        [Header("Colors (OreSorter Theme)")]
        [Tooltip("Obsidian Void: #0D1117")]
        public Color colorBackground = new Color(0.05f, 0.06f, 0.09f, 1f); 
        
        [Tooltip("Glass Panel: #1F2937 with 60% opacity")]
        public Color colorPanel = new Color(0.12f, 0.16f, 0.22f, 0.6f);
        
        [Tooltip("Emerald: #34D399")]
        public Color colorPrimary = new Color(0.20f, 0.82f, 0.60f, 1f);
        
        [Tooltip("Amethyst: #A855F7")]
        public Color colorSecondary = new Color(0.66f, 0.33f, 0.96f, 1f);
        
        [Tooltip("Ruby: #EF4444")]
        public Color colorError = new Color(0.93f, 0.26f, 0.26f, 1f);
        
        [Tooltip("Gold: #FBBF24")]
        public Color colorGold = new Color(0.98f, 0.75f, 0.14f, 1f);

        [Header("Text Colors")]
        public Color colorTextPrimary = Color.white;
        public Color colorTextSecondary = new Color(0.85f, 0.89f, 0.96f, 1f);

        [Header("Typography")]
        [Tooltip("Sora (TMP_FontAsset)")]
        public UnityEngine.Object headlineFont; 
        
        [Tooltip("Rubik (TMP_FontAsset)")]
        public UnityEngine.Object bodyFont;

        [Header("Materials & Sprites")]
        public Material glassmorphismPanelMaterial;
        [Tooltip("The auto-generated 9-slice Pill Sprite for organic rounded buttons.")]
        public Sprite buttonSprite;

        [Header("Metrics (1080x1920 scale)")]
        public float buttonWidth = 800f;
        public float buttonHeight = 140f;
        public int titleFontSize = 80;
        public int bodyFontSize = 50;
        
        [Header("Stitch Spacing")]
        public float safeAreaBottom = 34f;
        public float elementGap = 16f;
        public float containerPadding = 24f;
    }
}
