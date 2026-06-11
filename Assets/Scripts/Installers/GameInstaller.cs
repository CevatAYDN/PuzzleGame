using UnityEngine;
using VContainer;
using VContainer.Unity;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Installers
{
    /// <summary>
    /// Composition Root: All dependencies are wired here.
    /// SOLID & DI principles applied.
    ///
    /// FAIL-LOUDLY: configuration loading and DI registration throw
    /// instead of silently defaulting — the player should never see a
    /// half-configured game due to a missing asset.
    ///
    /// Architecture: This class acts as an orchestrator that delegates
    /// registration to focused installer modules. Each module is a
    /// single-responsibility static class that registers a cohesive
    /// group of services.
    /// </summary>
    public class GameInstaller : LifetimeScope
    {
        [Header("Configurations (auto-loaded from Resources if not assigned)")]
        [SerializeField] public GameConfig gameConfig;
        [SerializeField] public AnimationConfig animationConfig;
        [SerializeField] public LevelConfig levelConfig;
        [SerializeField] public AudioConfig audioConfig;
        [SerializeField] public StreamVFXConfig streamVFXConfig;
        [SerializeField] public EconomyConfig economyConfig;
        [SerializeField] public WobbleConfig wobbleConfig;
        [SerializeField] public LevelData[] levelCatalog;

        protected override void Configure(IContainerBuilder builder)
        {
            // 1. Load configurations (fail-loudly)
            ConfigInstallerModule.Configure(builder, this);

            // 2. Core infrastructure services
            CoreServicesInstallerModule.Configure(builder);

            // 3. Localization (platform-aware)
            LocalizationInstallerModule.Configure(builder);

            // 4. Persistence layer
            PersistenceInstallerModule.Configure(builder);

            // 5. Economy, monetization, retention
            EconomyInstallerModule.Configure(builder);

            // 6. Gameplay services
            GameplayInstallerModule.Configure(builder);

            // 7. Presentation layer + UI
            PresentationInstallerModule.Configure(builder);

            MoldLogger.LogInfo("GameInstaller configured — all services registered.");
        }

        /// <summary>
        /// Finds a MonoBehaviour in the scene hierarchy or throws an exception.
        /// Used by installer modules that need to register services in DI.
        /// </summary>
        internal static T FindOrThrow<T>(IContainerBuilder builder) where T : Component
        {
            var component = Object.FindAnyObjectByType<T>();
            if (component == null)
            {
                throw new System.InvalidOperationException(
                    $"[DI Fail-Fast] Required component {typeof(T).Name} not found in scene! " +
                    $"Make sure it exists in the active scene before initializing the DI container.");
            }
            builder.RegisterComponent(component);
            return component;
        }
    }
}
