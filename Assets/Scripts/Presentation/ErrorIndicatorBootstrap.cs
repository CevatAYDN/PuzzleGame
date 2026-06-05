using UnityEngine;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Ensures a single <see cref="ErrorIndicatorController"/> exists in the active scene.
    /// VContainer's RegisterComponentInHierarchy throws if the component is missing, breaking
    /// boot on misconfigured scenes. This bootstrap auto-creates a fallback instance (with a
    /// warning) so the rest of the DI graph can resolve.
    /// The auto-created GameObject is DontDestroyOnLoad so it survives scene transitions
    /// (e.g., boot → gameplay) just like the GameInstaller LifetimeScope.
    /// </summary>
    public static class ErrorIndicatorBootstrap
    {
        public const string AutoCreatedName = "[ErrorIndicator] (auto-created)";

        public static ErrorIndicatorController EnsureExists()
        {
            var existing = Object.FindAnyObjectByType<ErrorIndicatorController>();
            if (existing != null) return existing;

            var go = new GameObject(AutoCreatedName);
            var controller = go.AddComponent<ErrorIndicatorController>();
            Object.DontDestroyOnLoad(go);
            MoldLogger.LogWarning(
                $"ErrorIndicatorController not found in scene. Auto-created '{AutoCreatedName}'. " +
                "Add the component to the scene in the Unity Editor to silence this warning.");
            return controller;
        }
    }
}
