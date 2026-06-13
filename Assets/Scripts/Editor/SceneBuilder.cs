using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Presentation;
// Type aliases — disambiguate from UnityEditor.BuildOptions, etc. and keep call sites concise.
using BuildOptions = PuzzleGame.Editor.SceneBuilderModel.BuildOptions;
using MoldConfig = PuzzleGame.Editor.SceneBuilderModel.MoldConfig;
using MoldLayout = PuzzleGame.Editor.SceneBuilderModel.MoldLayout;
using ShaderVariant = PuzzleGame.Editor.SceneBuilderModel.ShaderVariant;

namespace PuzzleGame.Editor
{
    /// <summary>
    /// Scene build orchestrator. Public API surface — delegates scene primitive creation
    /// to <see cref="SceneBuilderPrimitives"/> and mold operations to
    /// <see cref="SceneBuilderMoldFactory"/>. Data types live in <see cref="SceneBuilderModel"/>.
    ///
    /// Sprint #14: 710 LOC god-class split into 4 focused files (Model + Primitives +
    /// MoldFactory + orchestrator). Public API unchanged — all entry points (Build,
    /// SetupCurrentScene, FixURPPipeline, CreateMold, RemoveMolds, ComputePositions,
    /// GenerateMixedContents, CreateDefaultMoldSet, CountMolds) preserved via delegation.
    /// </summary>
    public static class SceneBuilder
    {
        // ── Full preset build ────────────────────────────────────────────────

        public static void Build(BuildOptions opts)
        {
            if (opts.newScene)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("PuzzleGame Scene Build");

            if (opts.lighting)   SceneBuilderPrimitives.SetupLighting();
            if (opts.ground)     SceneBuilderPrimitives.SetupGround();
            if (opts.camera)     SceneBuilderPrimitives.SetupCamera();
            if (opts.postProcessing) SceneBuilderPrimitives.SetupPostProcessing();
            if (opts.cauldron)   SceneBuilderPrimitives.CreateCauldron();
            if (opts.gameManager)
            {
                CreateGameManager();
                CreateGameInstaller();
            }
            if (opts.Molds)    SceneBuilderMoldFactory.CreateDefaultMoldSet();

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[SceneBuilder] Scene build complete. Ctrl+Z to undo.");
        }

        // ── Mold set helpers (public API delegation) ─────────────────────

        public static void CreateDefaultMoldSet() => SceneBuilderMoldFactory.CreateDefaultMoldSet();
        public static GameObject CreateMold(MoldConfig cfg) => SceneBuilderMoldFactory.CreateMold(cfg);
        public static void RemoveMolds() => SceneBuilderMoldFactory.RemoveMolds();
        public static int CountMolds() => SceneBuilderMoldFactory.CountMolds();

        public static Vector3[] ComputePositions(MoldLayout layout, int count, Vector3 center) =>
            SceneBuilderMoldFactory.ComputePositions(layout, count, center);

        public static Color[][] GenerateMixedContents(int moldCount) =>
            SceneBuilderMoldFactory.GenerateMixedContents(moldCount);

        // ── GameManager & DI ─────────────────────────────────────────────────

        public static void CreateGameManager()
        {
            if (Object.FindAnyObjectByType<GameManager>() != null) return;
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
            var ptb = go.AddComponent<PlayTestBootstrap>();
            ptb.ForcePlayTestMode = true;
        }

        public static void SetupCurrentScene()
        {
            // Create environment + GameManager + DI in current scene
            var opts = new BuildOptions
            {
                lighting = true,
                ground = true,
                camera = true,
                postProcessing = true,
                cauldron = false,
                Molds = false,
                gameManager = true,
                newScene = false
            };

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Current Scene");

            if (opts.lighting) SceneBuilderPrimitives.SetupLighting();
            if (opts.ground) SceneBuilderPrimitives.SetupGround();
            if (opts.camera) SceneBuilderPrimitives.SetupCamera();
            if (opts.postProcessing) SceneBuilderPrimitives.SetupPostProcessing();
            if (opts.gameManager)
            {
                CreateGameManager();
                CreateGameInstaller();
            }

            // Always create UI controllers so DI does not need fallback GameObjects at runtime.
            // Idempotent — re-running will skip nodes that already exist.
            SceneBuilderUI.SetupUIControllers();

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[SceneBuilder] Current scene set up with GameManager + DI. Ctrl+Z to undo.");
        }

        [MenuItem("Tools/PuzzleGame/Fix URP Pipeline Settings")]
        public static void FixURPPipeline()
        {
            var guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset PC_RPAsset");
            if (guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
            }
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
                GraphicsSettings.defaultRenderPipeline = asset;

                int originalQuality = QualitySettings.GetQualityLevel();
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = asset;
                }
                QualitySettings.SetQualityLevel(originalQuality, false);

                Debug.Log($"[SceneBuilder] Successfully assigned URP Asset to Graphics & Quality Settings: {path}");
            }
            else
            {
                Debug.LogError("[SceneBuilder] No UniversalRenderPipelineAsset found in project!");
            }
        }

        private static void CreateGameInstaller()
        {
            if (Object.FindAnyObjectByType<Installers.GameInstaller>() != null) return;

            var go = new GameObject("GameInstaller");
            var installer = go.AddComponent<Installers.GameInstaller>();

            // Auto-assign configs from Resources, create defaults if not found
            if (!System.IO.Directory.Exists("Assets/Resources/Data"))
            {
                System.IO.Directory.CreateDirectory("Assets/Resources/Data");
            }

            installer.gameConfig = Resources.Load<GameConfig>("Data/GameConfig");
            if (installer.gameConfig == null)
            {
                installer.gameConfig = ScriptableObject.CreateInstance<GameConfig>();
                UnityEditor.AssetDatabase.CreateAsset(installer.gameConfig, "Assets/Resources/Data/GameConfig.asset");
                Debug.LogWarning("[SceneBuilder] GameConfig not found in Resources — created and saved default asset.");
            }

            installer.animationConfig = Resources.Load<AnimationConfig>("Data/AnimationConfig");
            if (installer.animationConfig == null)
            {
                installer.animationConfig = ScriptableObject.CreateInstance<AnimationConfig>();
                UnityEditor.AssetDatabase.CreateAsset(installer.animationConfig, "Assets/Resources/Data/AnimationConfig.asset");
                Debug.LogWarning("[SceneBuilder] AnimationConfig not found in Resources — created and saved default asset.");
            }

            installer.levelConfig = Resources.Load<LevelConfig>("Data/LevelConfig");
            if (installer.levelConfig == null)
            {
                installer.levelConfig = ScriptableObject.CreateInstance<LevelConfig>();
                UnityEditor.AssetDatabase.CreateAsset(installer.levelConfig, "Assets/Resources/Data/LevelConfig.asset");
                Debug.LogWarning("[SceneBuilder] LevelConfig not found in Resources — created and saved default asset.");
            }

            installer.audioConfig = Resources.Load<AudioConfig>("Data/AudioConfig");
            if (installer.audioConfig == null)
            {
                installer.audioConfig = ScriptableObject.CreateInstance<AudioConfig>();
                UnityEditor.AssetDatabase.CreateAsset(installer.audioConfig, "Assets/Resources/Data/AudioConfig.asset");
                Debug.LogWarning("[SceneBuilder] AudioConfig not found in Resources — created and saved default asset.");
            }

            Debug.Log("[SceneBuilder] GameInstaller created and configs assigned.");
        }
    }
}
