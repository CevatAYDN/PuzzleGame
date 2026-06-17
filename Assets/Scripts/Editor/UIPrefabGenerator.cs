using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PuzzleGame.Presentation.UI;
using PuzzleGame.Editor.UI;
using PuzzleGame.Application.Events;


namespace PuzzleGame.Editor
{
    internal static class UIPrefabGenerator
    {
        private static string _currentPrefabRoot = "Assets/Prefabs/UI";
        private static UIStyleConfig _config;

        // Sadece ForgeEditorWindow (Open Editor) > ToolsTab içinden erişilir.
        public static void GenerateAll()
        {
            EnsureConfig();

            var context = new UIGeneratorContext
            {
                Config = _config,
                PrefabRootPath = _currentPrefabRoot
            };

            SetupUITestScene(context);

            var builders = new IUIBuilder[]
            {
                new MainMenuBuilder(),
                new WorldMapBuilder(),
                new SettingsBuilder(),
                new SettingsPrivacyBuilder(),
                new SettingsSoundBuilder(),
                new DailyChallengeBuilder(),
                new LevelSelectUIBuilder(),
                new ShopBuilder()
            };

            foreach (var builder in builders)
            {
                builder.Build(context);
            }

            LinkUITestScene(context);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UIPrefabGenerator] Base prefabs generated under {_currentPrefabRoot}/");
        }

        private static void SetupUITestScene(UIGeneratorContext context)
        {
            string scenePath = "Assets/Scenes/UITest.unity";
            
            if (!System.IO.Directory.Exists("Assets/Scenes"))
                System.IO.Directory.CreateDirectory("Assets/Scenes");

            Scene scene;
            if (!System.IO.File.Exists(scenePath))
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene.name = "UITest";
                EditorSceneManager.SaveScene(scene, scenePath);
            }
            else
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // Root GameObject for scene hierarchy (no GameInstaller — EditorUIRoot handles DI)
            var sceneRoot = new GameObject("UITestRoot");

            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {

                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.transform.SetParent(sceneRoot.transform);
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                
                var inputModuleType = ResolveType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
                if (inputModuleType != null)
                {
                    eventSystemGo.AddComponent(inputModuleType);
                }
                else
                {
                    eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("MasterCanvas");
                canvasGo.transform.SetParent(sceneRoot.transform);
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            context.TestCanvas = canvas;
        }

        private static void LinkUITestScene(UIGeneratorContext context)
        {
            if (context.TestCanvas == null) return;

            // Clear old children
            for (int i = context.TestCanvas.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(context.TestCanvas.transform.GetChild(i).gameObject);
            }

            // Instantiate MainMenu
            var mainMenuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{_currentPrefabRoot}/MainMenuController.prefab");
            if (mainMenuPrefab == null) return;

            var mainMenuGo = PrefabUtility.InstantiatePrefab(mainMenuPrefab, context.TestCanvas.transform) as GameObject;
            var mainMenuCtrl = mainMenuGo.GetComponent<PuzzleGame.Presentation.UI.MainMenuController>();

            // Instantiate sub-panels and link them
            LinkSubPanel(mainMenuCtrl, "worldMapPanel", "WorldMapController.prefab", context);
            LinkSubPanel(mainMenuCtrl, "levelSelectPanel", "LevelSelectController.prefab", context);
            LinkSubPanel(mainMenuCtrl, "dailyChallengePanel", "DailyChallengeController.prefab", context);

            // Settings panel — also wire its sub-panels (Sound + Privacy)
            var settingsInstance = LinkSubPanel(mainMenuCtrl, "settingsPanel", "SettingsController.prefab", context);
            if (settingsInstance != null)
            {
                var settingsCtrl = settingsInstance.GetComponent<PuzzleGame.Presentation.UI.SettingsController>();
                if (settingsCtrl != null)
                {
                    LinkSubPanelToSettings(settingsCtrl, "_privacyPanel", "SettingsPrivacyController.prefab", context);
                    LinkSubPanelToSettings(settingsCtrl, "_soundPanel", "SettingsSoundController.prefab", context);
                }
            }

            // MainMenu's direct sound entry (separate from Settings' sound sub-panel)
            LinkSubPanel(mainMenuCtrl, "soundPanel", "SettingsSoundController.prefab", context);

            // ── Attach EditorUIRoot for editor-time DI ──────────────────────
            var canvasGo = context.TestCanvas.gameObject;
            if (canvasGo.GetComponent<EditorUIRoot>() == null)
            {
                canvasGo.AddComponent<EditorUIRoot>();
                Debug.Log("[UIPrefabGenerator] EditorUIRoot attached for editor-time DI.");
            }

            EditorSceneManager.MarkSceneDirty(context.TestCanvas.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
        }

        private static GameObject LinkSubPanel(PuzzleGame.Presentation.UI.MainMenuController ctrl, string fieldName, string prefabName, UIGeneratorContext context)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{_currentPrefabRoot}/{prefabName}");
            if (prefab == null) return null;

            var instance = PrefabUtility.InstantiatePrefab(prefab, ctrl.transform) as GameObject;
            if (instance == null) return null;

            var field = ctrl.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(ctrl, instance);
                instance.SetActive(false);
            }
            return instance;
        }

        private static void LinkSubPanelToSettings(PuzzleGame.Presentation.UI.SettingsController ctrl, string fieldName, string prefabName, UIGeneratorContext context)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{_currentPrefabRoot}/{prefabName}");
            if (prefab == null) return;

            var instance = PrefabUtility.InstantiatePrefab(prefab, ctrl.transform) as GameObject;
            if (instance == null) return;

            var field = ctrl.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(ctrl, instance);
                instance.SetActive(false);
            }
        }

        /// <summary>
        /// Resolves a type by full name across all loaded assemblies (avoids hardcoding assembly names).
        /// </summary>
        private static Type ResolveType(string fullTypeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(fullTypeName);
                if (type != null) return type;
            }
            return null;
        }

        private static void EnsureConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:UIStyleConfig");
            if (guids.Length > 0)
            {
                _config = AssetDatabase.LoadAssetAtPath<UIStyleConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            else
            {
                _config = ScriptableObject.CreateInstance<UIStyleConfig>();
                _config.buttonWidth = 800f;
                _config.buttonHeight = 140f;
                _config.titleFontSize = 80;
                _config.bodyFontSize = 50;
                _config.safeAreaBottom = 34f;
                _config.elementGap = 40f;
                _config.containerPadding = 24f;
                
                ColorUtility.TryParseHtmlString("#091421", out _config.colorBackground);
                ColorUtility.TryParseHtmlString("#34d399", out _config.colorPrimary);
                ColorUtility.TryParseHtmlString("#a855f7", out _config.colorSecondary);
                ColorUtility.TryParseHtmlString("#ef4444", out _config.colorError);
                ColorUtility.TryParseHtmlString("#fbbf24", out _config.colorGold);
                ColorUtility.TryParseHtmlString("#1f293799", out _config.colorPanel);
                
                if (!System.IO.Directory.Exists("Assets/Resources")) System.IO.Directory.CreateDirectory("Assets/Resources");
                AssetDatabase.CreateAsset(_config, "Assets/Resources/UIStyleConfig.asset");
            }
        }
    }
}
