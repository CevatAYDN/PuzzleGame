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
        [Tooltip("Surface Lowest: #091421")]
        public Color colorBackground = new Color(0.035f, 0.078f, 0.129f, 1f); 
        
        [Tooltip("Surface Container High: #212b39 with 80% opacity for Glassmorphism")]
        public Color colorPanel = new Color(0.129f, 0.168f, 0.223f, 0.8f);
        
        [Tooltip("Primary Container: #34D399")]
        public Color colorPrimary = new Color(0.203f, 0.827f, 0.600f, 1f);
        
        [Tooltip("Secondary Container: #6F00BE")]
        public Color colorSecondary = new Color(0.435f, 0f, 0.745f, 1f);
        
        [Tooltip("Error Container: #93000A")]
        public Color colorError = new Color(0.576f, 0f, 0.039f, 1f);
        
        [Tooltip("Tertiary Container (Gold): #FFA668")]
        public Color colorGold = new Color(1f, 0.650f, 0.407f, 1f);

        [Header("Text Colors")]
        [Tooltip("On-Surface: #D9E3F6")]
        public Color colorTextPrimary = new Color(0.850f, 0.890f, 0.964f, 1f);
        [Tooltip("On-Surface-Variant: #BBCAC0")]
        public Color colorTextSecondary = new Color(0.733f, 0.792f, 0.752f, 1f);

        [Header("Accessibility (WCAG 2.1 AA)")]
        [Tooltip("Fallback colors for high contrast or colorblind modes")]
        public Color colorPrimaryHighContrast = new Color(0f, 1f, 0f, 1f);
        public Color colorErrorHighContrast = new Color(1f, 0f, 0f, 1f);
        public Color colorSecondaryHighContrast = new Color(0f, 0f, 1f, 1f);
        
        [Tooltip("If true, UI animations (PrimeTween) should use zero duration or fade instead of scaling/bouncing")]
        public bool reduceMotion = false;

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
