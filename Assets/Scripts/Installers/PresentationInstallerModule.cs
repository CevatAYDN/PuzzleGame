using UnityEngine;
using Nexus.Core;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Infrastructure;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Presentation;
using PuzzleGame.Presentation.UI;

namespace PuzzleGame.Installers
{
    /// <summary>
    /// Registers presentation layer and UI services.
    /// Includes: error indicator, camera effects, mold pool initializer,
    /// game manager, level flow, win/lose evaluator, HUD, consent UI,
    /// main menu, world map, daily challenge, biome art, onboarding.
    /// </summary>
    internal static class PresentationInstallerModule
    {
        public static void Configure(IContextBuilder builder, NexusDI container)
        {
            // ErrorIndicator — bootstrap ensures it exists (auto-creates if scene is misconfigured)
            var errorIndicator = ErrorIndicatorBootstrap.EnsureExists();
            builder.BindInstance(errorIndicator);
            container.BindFactory<IErrorIndicatorService>(() => container.Resolve<ErrorIndicatorController>());

            // Camera effects
            var cameraEffects = CameraEffectsBootstrap.EnsureExists();
            builder.BindInstance(cameraEffects);

            // ActiveMoldsProvider — separate singleton to break circular dependency
            builder.Bind<IActiveMoldsProvider, ActiveMoldsProvider>();

            // Mold pool initializer
            builder.Bind<MoldPoolInitializer>();

            // Game manager (MonoBehaviour in hierarchy)
            GameplayLifecycle.FindOrThrow<GameManager>(builder);

            // Game lifecycle bootstrapper
            builder.Bind<GameLifecycleBootstrapper>();

            // Play-test bootstrapper (optional)
            GameplayLifecycle.FindOptional<PlayTestBootstrap>(builder);

            // Presentation controllers — POCOs, scoped to scene lifetime via the container
            builder.Bind<LevelFlowController>();
            builder.Bind<WinLoseEvaluator>();

            // UI components (MonoBehaviours that may or may not be in scene)
            GameplayLifecycle.FindOrThrow<HudPresenter>(builder);
            GameplayLifecycle.FindOrThrow<PowerUpUI>(builder);
            GameplayLifecycle.FindOrThrow<AchievementNotificationUI>(builder);

            // Consent flow UI — MonoBehaviours live on the consent scene prefab
            GameplayLifecycle.FindOrThrow<AgeGateModal>(builder);
            GameplayLifecycle.FindOrThrow<ConsentModal>(builder);
            GameplayLifecycle.FindOrThrow<SettingsPrivacyController>(builder);
            GameplayLifecycle.FindOrThrow<SettingsSoundController>(builder);

            // Main menu — entry point after onboarding; manages Play/Daily/Settings/Privacy buttons
            GameplayLifecycle.FindOrThrow<MainMenuController>(builder);

            // World map — shows 2 biome cards (Crystal Mines + Volcanic Forge) with progress
            GameplayLifecycle.FindOrThrow<WorldMapController>(builder);

            // Daily challenge — entry screen with streak/countdown/play
            GameplayLifecycle.FindOrThrow<DailyChallengeController>(builder);

            // AI art provider — reads from BiomeArtCatalog ScriptableObject (optional, returns defaults if empty)
            IBiomeArtProvider biomeArtProvider = null;
            container.BindFactory<IBiomeArtProvider>(() =>
            {
                if (biomeArtProvider == null)
                {
                    var catalog = ConfigLoader.LoadOrThrow<BiomeArtCatalog>(null, "Data/BiomeArtCatalog", "BiomeArtCatalog");
                    biomeArtProvider = new ScriptableObjectBiomeArtProvider(catalog);
                }
                return biomeArtProvider;
            });

            // Onboarding orchestrator — POCO, owned by container; runs Splash → AgeGate → Consent → MainMenu
            builder.Bind<OnboardingFlowController>();
        }
    }
}
