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
    /// Uses prefab Instantiate instead of AddComponent to ensure serialized fields
    /// (buttons, labels, panels) are wired up from the prefab hierarchy. Fixes
    /// council blocker B2.
    ///
    /// Prefabs live under Assets/Resources/Prefabs/ and are loaded via Resources.Load.
    /// Run Tools > PuzzleGame > Generate UI Prefabs to (re)create them.
    /// </summary>
    internal static class SceneBuilderUI
    {
        // UI root name used to avoid duplicate Canvas creation
        private const string UIRootName = "PuzzleGame_Canvas";
        // Prefab resource path (without extension) — matches Assets/Resources/Prefabs/
        private const string PrefabPrefix = "Prefabs/";

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
            CreateFromPrefab<Presentation.UI.AgeGateModal>("AgeGateModal");
            CreateFromPrefab<Presentation.UI.ConsentModal>("ConsentModal");
            CreateFromPrefab<Presentation.UI.MainMenuController>("MainMenuController");
            CreateFromPrefab<Presentation.UI.SettingsPrivacyController>("SettingsPrivacyController");
            CreateFromPrefab<Presentation.UI.SettingsSoundController>("SettingsSoundController");
            CreateFromPrefab<Presentation.UI.WorldMapController>("WorldMapController");
            CreateFromPrefab<Presentation.UI.DailyChallengeController>("DailyChallengeController");
            CreateFromPrefab<Presentation.UI.PowerUpUI>("PowerUpUI");
            CreateFromPrefab<Presentation.UI.AchievementNotificationUI>("AchievementNotificationUI");

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

            Undo.AddComponent<Presentation.CameraEffectsController>(cam.gameObject);
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
            InstantiatePrefab<Presentation.UI.HudPresenter>("HudPresenter", asFirstSibling: true);
        }

        // ── Generic modal scaffolding (prefab-based) ──────────────────────────

        private static void CreateFromPrefab<T>(string prefabName) where T : MonoBehaviour
        {
            InstantiatePrefab<T>(prefabName, asFirstSibling: false);
        }

        private static void InstantiatePrefab<T>(string prefabName, bool asFirstSibling) where T : MonoBehaviour
        {
            if (Object.FindAnyObjectByType<T>(FindObjectsInactive.Include) != null) return;

            var canvas = GameObject.Find(UIRootName);
            if (canvas == null) return;

            // Try prefab first; fall back to empty GameObject + AddComponent
            string resPath = PrefabPrefix + prefabName;
            GameObject prefab = Resources.Load<GameObject>(resPath);
            GameObject go;

            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
                go.name = prefabName;

                // Ensure the target component exists
                if (go.GetComponent<T>() == null)
                    go.AddComponent<T>();
            }
            else
            {
                throw new System.IO.FileNotFoundException($"[Fail-Fast] UI Prefab not found at Resources/{resPath}. Please run 'Tools > PuzzleGame > Generate UI Prefabs' first.");
            }

            SetFullScreen(go);
            if (asFirstSibling)
                go.transform.SetAsFirstSibling();

            Undo.RegisterCreatedObjectUndo(go, $"Create {prefabName}");
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
