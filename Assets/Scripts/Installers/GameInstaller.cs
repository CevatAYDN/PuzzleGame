using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Infrastructure.Providers;
using PuzzleGame.Presentation;
using PuzzleGame.Presentation.UI;

namespace PuzzleGame.Installers
{
    /// <summary>
    /// Composition Root: All configurations and catalogs assigned in the Inspector are stored here.
    /// Inherits from Nexus.Core.Root to serve as the context root for the scene.
    /// Also implements IContextLifecycle so the lifecycle is registered even without explicit ContextData,
    /// enabling auto-discovery bypass for instant setup.
    /// </summary>
    public class GameInstaller : Root, IContextLifecycle
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

        private NexusDI _container;

        // ── IContextLifecycle ────────────────────────────────────────────

        public void OnConfigure(IContextBuilder builder)
        {
            // Extract NexusDI from the builder using reflection
            if (builder is ContextBuilder cb)
            {
                var containerField = typeof(ContextBuilder).GetField("_container",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (containerField != null)
                    _container = (NexusDI)containerField.GetValue(cb);
            }

            if (_container == null)
                throw new System.InvalidOperationException("[Nexus] GameInstaller failed to extract NexusDI container from builder.");

            // 1. Load configurations
            ConfigInstallerModule.Configure(builder, _container, this);

            // 2. Core infrastructure services
            CoreServicesInstallerModule.Configure(builder, _container);

            // 3. Localization (platform-aware)
            LocalizationInstallerModule.Configure(builder, _container);

            // 4. Persistence layer
            PersistenceInstallerModule.Configure(builder, _container);

            // 5. Economy, monetization, retention
            EconomyInstallerModule.Configure(builder, _container);

            // 6. Gameplay services
            GameplayInstallerModule.Configure(builder, _container);

            // 7. Presentation layer + UI
            PresentationInstallerModule.Configure(builder, _container);
        }

        public ValueTask OnInitializeAsync(CancellationToken ct)
        {
            // 1. Load and initialize Asset Provider
            var assetProvider = _container.Resolve<IAssetProvider>();
            assetProvider.Initialize();

            // 2. Setup active Crash Reporter
            var crashReporter = _container.Resolve<ICrashReportingService>();
            PuzzleGame.Application.Logging.CrashReporter.Current = crashReporter;

            // 3. Inject scene MonoBehaviours bound in the container
            ResolveAndInject<GameManager>();
            ResolveAndInject<HudPresenter>();
            ResolveAndInject<PowerUpUI>();
            ResolveAndInject<AchievementNotificationUI>();
            ResolveAndInject<AgeGateModal>();
            ResolveAndInject<ConsentModal>();
            ResolveAndInject<SettingsPrivacyController>();
            ResolveAndInject<SettingsSoundController>();
            ResolveAndInject<MainMenuController>();
            ResolveAndInject<WorldMapController>();
            ResolveAndInject<DailyChallengeController>();
            ResolveAndInject<PlayTestBootstrap>();
            ResolveAndInject<LocalizationBootstrap>();

            return default;
        }

        public ValueTask OnStartAsync(CancellationToken ct)
        {
            // Initialize game systems and set target frame rate
            if (_container.IsRegistered(typeof(GameLifecycleBootstrapper)))
            {
                var bootstrapper = _container.Resolve<GameLifecycleBootstrapper>();
                bootstrapper.Initialize();
            }

            return default;
        }

        public void OnDispose()
        {
        }

        private void ResolveAndInject<T>() where T : class
        {
            if (_container.IsRegistered(typeof(T)))
            {
                var instance = _container.Resolve<T>();
                _container.Inject(instance);
            }
        }

        // ── Helpers called by installer modules ──────────────────────────

        internal static T FindOrThrow<T>(IContextBuilder builder) where T : Component
        {
            var component = Object.FindAnyObjectByType<T>();
            if (component == null)
            {
                throw new System.InvalidOperationException(
                    $"[DI Fail-Fast] Required component {typeof(T).Name} not found in scene! " +
                    "Make sure it exists in the active scene.");
            }
            builder.BindInstance(component);
            return component;
        }

        internal static T FindOptional<T>(IContextBuilder builder) where T : Component
        {
            var component = Object.FindAnyObjectByType<T>();
            if (component != null)
                builder.BindInstance(component);
            return component;
        }
    }
}
