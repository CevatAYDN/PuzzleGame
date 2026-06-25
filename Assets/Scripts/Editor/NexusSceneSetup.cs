using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PuzzleGame.Installers;

namespace PuzzleGame.EditorTools
{
    /// <summary>
    /// Adds Nexus Root (GameInstaller) to the active scene.
    /// GameInstaller extends Nexus.Core.Root and implements IContextLifecycle,
    /// which is required for DI container initialization and game boot.
    /// Run via: PuzzleGame > Scene > Setup Nexus Root
    /// </summary>
    internal static class NexusSceneSetup
    {
        private const string MenuPath = "PuzzleGame/Scene/Setup Nexus Root %&n";

        [MenuItem(MenuPath, priority = 100)]
        private static void SetupNexusRoot()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
            {
                EditorUtility.DisplayDialog("Nexus Setup", "Save the scene first before setting up Nexus Root.", "OK");
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                scene = SceneManager.GetActiveScene();
                if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
                    return;
            }

            // Check if GameInstaller already exists in the scene
            var existing = Object.FindAnyObjectByType<GameInstaller>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Nexus Setup",
                    $"GameInstaller already exists on '{existing.gameObject.name}'.\n" +
                    "No changes made.", "OK");
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }

            // Create root GameObject with GameInstaller component
            var rootGo = new GameObject("GameRoot");
            var installer = rootGo.AddComponent<GameInstaller>();

            // Mark as not dirty for initial state
            Undo.RegisterCreatedObjectUndo(rootGo, "Setup Nexus Root");
            EditorSceneManager.MarkSceneDirty(scene);

            Selection.activeGameObject = rootGo;
            EditorGUIUtility.PingObject(rootGo);

            Debug.Log($"[Nexus] GameInstaller (Root) added to scene '{scene.name}' on GameObject '{rootGo.name}'.\n" +
                      $"Assign Configs in the Inspector and hit Play.");
        }

        [MenuItem(MenuPath, validate = true)]
        private static bool SetupNexusRootValidate()
        {
            return !EditorApplication.isPlaying && !EditorApplication.isCompiling;
        }
    }
}
