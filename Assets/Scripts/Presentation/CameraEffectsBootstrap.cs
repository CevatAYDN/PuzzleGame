using UnityEngine;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Ensures a single <see cref="CameraEffectsController"/> exists in the active scene.
    /// VContainer's RegisterComponentInHierarchy throws if the component is missing, breaking
    /// boot on misconfigured scenes. This bootstrap auto-adds it to the Main Camera (or creates
    /// a fallback GameObject) so the rest of the DI graph can resolve.
    /// </summary>
    public static class CameraEffectsBootstrap
    {
        public const string AutoCreatedName = "[CameraEffects] (auto-created)";

        public static CameraEffectsController EnsureExists()
        {
            var existing = Object.FindAnyObjectByType<CameraEffectsController>();
            if (existing != null) return existing;

            var cam = Camera.main;
            if (cam != null)
            {
                var controller = cam.gameObject.AddComponent<CameraEffectsController>();
                MoldLogger.LogWarning(
                    $"CameraEffectsController not found in scene. Auto-added to Main Camera '{cam.name}'. " +
                    "Add the component to the camera in the Unity Editor to silence this warning.");
                return controller;
            }
            else
            {
                var go = new GameObject(AutoCreatedName);
                var controller = go.AddComponent<CameraEffectsController>();
                Object.DontDestroyOnLoad(go);
                MoldLogger.LogWarning(
                    $"CameraEffectsController and Main Camera not found in scene. Auto-created '{AutoCreatedName}'. " +
                    "Add the component or a Main Camera to the scene in the Unity Editor to silence this warning.");
                return controller;
            }
        }
    }
}
