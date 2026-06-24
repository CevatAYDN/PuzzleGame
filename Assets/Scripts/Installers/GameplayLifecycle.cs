using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Presentation;
using PuzzleGame.Presentation.UI;

namespace PuzzleGame.Installers
{
    /// <summary>
    /// Lifecycle orchestrator for the Gameplay context in Nexus.
    /// Ends with "Lifecycle" to be auto-discovered when Context scope tag is "Gameplay".
    /// </summary>
    public class GameplayLifecycle : IContextLifecycle
    {
        private NexusDI _container;

        public void OnConfigure(IContextBuilder builder)
        {
            if (builder is ContextBuilder cb)
            {
                // Extract NexusDI container using reflection
                var containerField = typeof(ContextBuilder).GetField("_container", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (containerField != null)
                {
                    _container = (NexusDI)containerField.GetValue(cb);
                }
            }

            if (_container == null)
            {
                throw new InvalidOperationException("[Nexus] GameplayLifecycle failed to extract NexusDI container from builder.");
            }

            // Find the GameInstaller root in the scene to access Inspector-serialized configs
            var installer = UnityEngine.Object.FindAnyObjectByType<GameInstaller>();
            if (installer == null)
            {
                throw new InvalidOperationException("[Nexus] GameInstaller not found in the active scene.");
            }

            // 1. Load configurations
            ConfigInstallerModule.Configure(builder, _container, installer);

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

        public async ValueTask OnInitializeAsync(CancellationToken ct)
        {
            // 1. Load and initialize Asset Provider
            var assetProvider = _container.Resolve<IAssetProvider>();
            assetProvider.Initialize();

            // 2. Setup active Crash Reporter
            var crashReporter = _container.Resolve<ICrashReportingService>();
            PuzzleGame.Application.Logging.CrashReporter.Current = crashReporter;

            // 3. Inject scene MonoBehaviours that were bound in the container
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

            await ValueTask.CompletedTask;
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

        // Helpers to be called by installer modules during OnConfigure
        internal static T FindOrThrow<T>(IContextBuilder builder) where T : Component
        {
            var component = UnityEngine.Object.FindAnyObjectByType<T>();
            if (component == null)
            {
                throw new System.InvalidOperationException(
                    $"[DI Fail-Fast] Required component {typeof(T).Name} not found in scene! " +
                    $"Make sure it exists in the active scene.");
            }
            builder.BindInstance(component);
            return component;
        }

        internal static T FindOptional<T>(IContextBuilder builder) where T : Component
        {
            var component = UnityEngine.Object.FindAnyObjectByType<T>();
            if (component != null)
            {
                builder.BindInstance(component);
            }
            return component;
        }
    }
}
