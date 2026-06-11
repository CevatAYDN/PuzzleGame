using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// UI controller creation for the puzzle game scene. Builds a complete Canvas
    /// hierarchy with all required controllers (HudPresenter, MainMenu, Settings,
    /// Consent, WorldMap, DailyChallenge) so DI does not need to create fallback
    /// GameObjects at runtime. Reduces per-frame Update overhead and silences WARN logs.
    ///
    /// Uses only UnityEngine.UI (built-in) — no TextMeshPro dependency to keep
    /// the Editor assembly's reference footprint minimal.
    /// </summary>
    internal static class SceneBuilderUI
    {
        // UI root name used to avoid duplicate Canvas creation
        private const string UIRootName = "PuzzleGame_Canvas";

        /// <summary>
        /// Creates the full UI hierarchy in the current scene: EventSystem, Canvas,
        /// and all required controllers. Idempotent — re-running skips existing nodes.
        /// </summary>
        public static void SetupUIControllers()
        {
            EnsureEventSystem();
            EnsureCanvas();
            EnsureErrorIndicator();
            EnsureCameraEffectsController();
            CreateHudPresenter();
            CreateModal<Presentation.UI.AgeGateModal>("AgeGateModal");
            CreateModal<Presentation.UI.ConsentModal>("ConsentModal");
            CreateModal<Presentation.UI.MainMenuController>("MainMenuController");
            CreateModal<Presentation.UI.SettingsPrivacyController>("SettingsPrivacyController");
            CreateModal<Presentation.UI.SettingsSoundController>("SettingsSoundController");
            CreateModal<Presentation.UI.WorldMapController>("WorldMapController");
            CreateModal<Presentation.UI.DailyChallengeController>("DailyChallengeController");
            CreateModal<Presentation.UI.PowerUpUI>("PowerUpUI");
            CreateModal<Presentation.UI.AchievementNotificationUI>("AchievementNotificationUI");

            Debug.Log("[SceneBuilderUI] UI controllers created successfully.");
        }

        // ── Standalone scene components (outside Canvas) ─────────────────────

        private static void EnsureErrorIndicator()
        {
            if (Object.FindAnyObjectByType<Presentation.ErrorIndicatorController>(FindObjectsInactive.Include) != null)
                return;

            var go = new GameObject("ErrorIndicatorController");
            go.AddComponent<Presentation.ErrorIndicatorController>();
            Undo.RegisterCreatedObjectUndo(go, "Create ErrorIndicatorController");
        }

        private static void EnsureCameraEffectsController()
        {
            if (Object.FindAnyObjectByType<Presentation.CameraEffectsController>(FindObjectsInactive.Include) != null)
                return;

            var cam = Camera.main;
            if (cam == null) return; // Skip silently — camera will be created later

            var controller = cam.GetComponent<Presentation.CameraEffectsController>();
            if (controller != null) return;

            cam.gameObject.AddComponent<Presentation.CameraEffectsController>();
        }

        // ── Core infrastructure ──────────────────────────────────────────────

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            // The project ships with the new Input System package active; legacy
            // StandaloneInputModule would throw InvalidOperationException at runtime
            // (UnityEngine.Input + Input System package are mutually exclusive in Player
            // Settings). Using InputSystemUIInputModule avoids the conflict when present.
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        }

        private static GameObject EnsureCanvas()
        {
            var existing = GameObject.Find(UIRootName);
            if (existing != null) return existing;

            var canvasGo = new GameObject(UIRootName,
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            Undo.RegisterCreatedObjectUndo(canvasGo, "Create PuzzleGame Canvas");
            return canvasGo;
        }

        // ── HUD ──────────────────────────────────────────────────────────────

        private static void CreateHudPresenter()
        {
            var canvas = GameObject.Find(UIRootName);
            if (canvas == null) return;

            // Skip if already present (idempotent)
            if (Object.FindAnyObjectByType<Presentation.UI.HudPresenter>(FindObjectsInactive.Include) != null)
                return;

            var hudGo = new GameObject("HudPresenter",
                typeof(RectTransform), typeof(CanvasGroup));
            hudGo.transform.SetParent(canvas.transform, false);
            SetFullScreen(hudGo);
            hudGo.transform.SetAsFirstSibling();

            var group = hudGo.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            // Placeholder Text so any null-guard against label binding does not NRE.
            var label = new GameObject("HudLabel",
                typeof(RectTransform), typeof(Text));
            label.transform.SetParent(hudGo.transform, false);
            SetFullScreen(label);
            var text = label.GetComponent<Text>();
            text.text = string.Empty;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            hudGo.AddComponent<Presentation.UI.HudPresenter>();
            Undo.RegisterCreatedObjectUndo(hudGo, "Create HudPresenter");
        }

        // ── Generic modal scaffolding ────────────────────────────────────────

        private static void CreateModal<T>(string name) where T : MonoBehaviour
        {
            if (Object.FindAnyObjectByType<T>(FindObjectsInactive.Include) != null) return;

            var canvas = GameObject.Find(UIRootName);
            if (canvas == null) return;

            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(canvas.transform, false);
            SetFullScreen(go);

            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            go.AddComponent<T>();
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        }

        private static void SetFullScreen(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
