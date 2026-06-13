using UnityEngine;
using UnityEngine.UI;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Overrides Graphic elements (Images, Texts) with high contrast colors 
    /// from UIStyleConfig when Accessibility modes are toggled.
    /// </summary>
    public class AccessibilityColorOverride : MonoBehaviour
    {
        [SerializeField] private UIStyleConfig _styleConfig;
        [SerializeField] private Graphic _targetGraphic;
        
        public enum ElementType { Background, Primary, Secondary, Error, Gold, TextPrimary, TextSecondary }
        [SerializeField] private ElementType _elementType;

        private void OnEnable()
        {
            // Note: In a full implementation, this would subscribe to an AccessibilitySettingsChanged event
            // from the Application layer. For now, we poll or apply on enable.
            ApplyAccessibilityColors();
        }

        public void ApplyAccessibilityColors()
        {
            if (_targetGraphic == null || _styleConfig == null) return;

            // In a real scenario, we'd check if High Contrast mode is enabled via a settings service.
            // Assuming High Contrast is required for demonstration:
            bool isHighContrast = false; // Mock flag

            Color targetColor = GetColorForElement(isHighContrast);
            _targetGraphic.color = targetColor;
        }

        private Color GetColorForElement(bool highContrast)
        {
            if (highContrast)
            {
                switch (_elementType)
                {
                    case ElementType.Primary: return _styleConfig.colorPrimaryHighContrast;
                    case ElementType.Secondary: return _styleConfig.colorSecondaryHighContrast;
                    case ElementType.Error: return _styleConfig.colorErrorHighContrast;
                    // Fallback to defaults for others or define more high contrast colors
                }
            }

            switch (_elementType)
            {
                case ElementType.Background: return _styleConfig.colorBackground;
                case ElementType.Primary: return _styleConfig.colorPrimary;
                case ElementType.Secondary: return _styleConfig.colorSecondary;
                case ElementType.Error: return _styleConfig.colorError;
                case ElementType.Gold: return _styleConfig.colorGold;
                case ElementType.TextPrimary: return _styleConfig.colorTextPrimary;
                case ElementType.TextSecondary: return _styleConfig.colorTextSecondary;
                default: return Color.white;
            }
        }
    }
}
