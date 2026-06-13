using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using PuzzleGame.Presentation.UI;
using PuzzleGame.Presentation.UI.Components;

namespace PuzzleGame.Editor.UI
{
    public class UIGeneratorContext
    {
        public UIStyleConfig Config { get; set; }
        public Canvas TestCanvas { get; set; }
        public string PrefabRootPath { get; set; }
    }

    public interface IUIBuilder
    {
        void Build(UIGeneratorContext context);
    }

    public abstract class UIBuilderBase : IUIBuilder
    {
        protected UIGeneratorContext Context { get; private set; }
        private static Type _textMeshProType;
        private static Type _tmpDropdownType;
        private static bool _tmprChecked;

        public void Build(UIGeneratorContext context)
        {
            Context = context;
            BuildCore();
        }

        protected abstract void BuildCore();

        protected void EnsureTMProTypes()
        {
            if (_tmprChecked) return;
            _tmprChecked = true;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (_textMeshProType == null)
                    _textMeshProType = asm.GetType("TMPro.TextMeshProUGUI");
                if (_tmpDropdownType == null)
                    _tmpDropdownType = asm.GetType("TMPro.TMP_Dropdown");
                if (_textMeshProType != null && _tmpDropdownType != null) break;
            }
        }

        protected GameObject CreateRoot(string name, out GameObject contentPanel)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            if (Context.TestCanvas != null)
                go.transform.SetParent(Context.TestCanvas.transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = Context.Config.colorBackground;

            var safeArea = new GameObject("SafeArea", typeof(RectTransform));
            safeArea.transform.SetParent(go.transform, false);
            var safeRect = safeArea.GetComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = new Vector2(Context.Config.containerPadding, Context.Config.safeAreaBottom);
            safeRect.offsetMax = new Vector2(-Context.Config.containerPadding, -Context.Config.containerPadding);

            contentPanel = new GameObject("ContentPanel", typeof(RectTransform));
            contentPanel.transform.SetParent(safeArea.transform, false);
            var contentRect = contentPanel.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            var panelBg = contentPanel.AddComponent<Image>();
            panelBg.color = Context.Config.colorPanel;
            if (Context.Config.buttonSprite != null)
            {
                panelBg.sprite = Context.Config.buttonSprite;
                panelBg.type = Image.Type.Sliced;
            }

            var layout = contentPanel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.padding = new RectOffset((int)Context.Config.containerPadding, (int)Context.Config.containerPadding, (int)Context.Config.containerPadding, (int)Context.Config.containerPadding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = Context.Config.elementGap;

            return go;
        }

        protected GameObject CreateRoot(string name)
        {
            return CreateRoot(name, out _);
        }

        protected Button CreateButton(GameObject parent, string name, string locKey, Color? overrideColor = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(Context.Config.buttonWidth, Context.Config.buttonHeight);

            var image = go.AddComponent<Image>();
            image.color = Context.Config.colorPrimary;
            image.type = Image.Type.Sliced;
            image.sprite = Context.Config.buttonSprite;

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(Context.Config.colorPrimary.r, Context.Config.colorPrimary.g, Context.Config.colorPrimary.b, 0.5f);
            shadow.effectDistance = new Vector2(0, -15f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minHeight = Context.Config.buttonHeight;
            layoutElement.minWidth = Context.Config.buttonWidth;

            var colors = button.colors;
            colors.normalColor = overrideColor ?? Context.Config.colorPrimary;
            colors.highlightedColor = Color.Lerp(overrideColor ?? Context.Config.colorPrimary, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(overrideColor ?? Context.Config.colorPrimary, Color.black, 0.2f);
            colors.selectedColor = Color.Lerp(Context.Config.colorPrimary, Color.white, 0.2f);
            button.colors = colors;

            // Haptic and Juice components
            go.AddComponent<UIButtonHaptic>();
            
            // Add UIButtonJuice if it exists
            var juiceType = Type.GetType("PuzzleGame.Presentation.UI.UIButtonJuice, Assembly-CSharp");
            if (juiceType != null) go.AddComponent(juiceType);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelCmp = AddTextComponent(labelGo, locKey, Context.Config.bodyFontSize);

            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }

        protected Component AddTextComponent(GameObject go, string locKey, int fontSize = 18)
        {
            EnsureTMProTypes();
            if (_textMeshProType != null)
            {
                var tmp = go.AddComponent(_textMeshProType);
                _textMeshProType.GetProperty("fontSize")?.SetValue(tmp, (float)fontSize);
                _textMeshProType.GetProperty("color")?.SetValue(tmp, Color.white);
                
                var settingsType = _textMeshProType.Assembly.GetType("TMPro.TMP_Settings");
                if (settingsType != null)
                {
                    var defaultFontProp = settingsType.GetProperty("defaultFontAsset", BindingFlags.Public | BindingFlags.Static);
                    if (defaultFontProp != null)
                    {
                        var defaultFont = defaultFontProp.GetValue(null);
                        if (defaultFont != null) _textMeshProType.GetProperty("font")?.SetValue(tmp, defaultFont);
                    }
                }

                var alignType = _textMeshProType.Assembly.GetType("TMPro.TextAlignmentOptions");
                if (alignType != null)
                {
                    var center = Enum.Parse(alignType, "Center");
                    _textMeshProType.GetProperty("alignment")?.SetValue(tmp, center);
                }

                // Attach Localizer
                var localizer = go.AddComponent<UITextLocalizer>();
                localizer.LocalizationKey = locKey;
                // For editor preview, set text to key initially
                _textMeshProType.GetProperty("text")?.SetValue(tmp, $"[{locKey}]");

                return (Component)tmp;
            }
            return null;
        }

        protected void CreateLabelAndAssign(GameObject parent, string name, string locKey, object controller, string fieldName)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var cmp = AddTextComponent(go, locKey);
            SetField(controller, fieldName, cmp);
        }

        protected void CreateLegacyTextAndAssign(GameObject parent, string name, string locKey, object controller, string fieldName)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var t = go.AddComponent<Text>();
            t.text = $"[{locKey}]";
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.color = Color.white;
            SetField(controller, fieldName, t);
        }

        protected Component CreateDropdown(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            EnsureTMProTypes();
            Component cmp;
            if (_tmpDropdownType != null)
                cmp = (Component)go.AddComponent(_tmpDropdownType);
            else
                cmp = go.AddComponent<Dropdown>();

            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(go.transform, false);
            AddTextComponent(label, "ui_option");

            return cmp;
        }

        protected Toggle CreateToggle(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bg;

            var check = new GameObject("Checkmark", typeof(RectTransform));
            check.transform.SetParent(go.transform, false);
            var checkImage = check.AddComponent<Image>();
            checkImage.color = new Color(0.2f, 0.8f, 0.4f, 1f);
            toggle.graphic = checkImage;
            toggle.isOn = true;

            return toggle;
        }

        protected Slider CreateSlider(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var slider = go.AddComponent<Slider>();
            slider.targetGraphic = bg;

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(go.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.8f, 0.4f, 1f);
            slider.fillRect = fill.GetComponent<RectTransform>();

            return slider;
        }

        protected GameObject CreateEmptyChild(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        protected void CreateSpacer(GameObject parent, float flexibleHeight)
        {
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(parent.transform, false);
            var le = spacer.AddComponent<LayoutElement>();
            le.flexibleHeight = flexibleHeight;
        }

        protected void SetField(object controller, string fieldName, object value)
        {
            var field = controller.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(controller, value);
        }

        protected void Save(GameObject root, string fileName)
        {
            SetLayerRecursively(root, 5); // UI Layer
            string path = $"{Context.PrefabRootPath}/{fileName}";

            var dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
            if (prefabAsset == null)
            {
                Debug.LogWarning($"[UIBuilder] Failed to save prefab: {path}");
            }
        }

        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }
}
