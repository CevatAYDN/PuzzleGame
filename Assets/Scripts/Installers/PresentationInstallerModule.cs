using VContainer;
using VContainer.Unity;
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
        public static void Configure(IContainerBuilder builder)
        {
            // ErrorIndicator — bootstrap ensures it exists (auto-creates if scene is misconfigured)
            var errorIndicator = ErrorIndicatorBootstrap.EnsureExists();
            builder.RegisterInstance(errorIndicator)
                .AsSelf()
                .As<IErrorIndicatorService>();

            // Camera effects
            var cameraEffects = CameraEffectsBootstrap.EnsureExists();
            builder.RegisterComponent(cameraEffects);

            // ActiveMoldsProvider — separate singleton to break circular dependency
            // (MoldPoolInitializer no longer implements IActiveMoldsProvider)
            builder.Register<ActiveMoldsProvider>(Lifetime.Singleton)
                   .As<IActiveMoldsProvider>();

            // Mold pool initializer (no longer implements IActiveMoldsProvider)
            builder.Register<MoldPoolInitializer>(Lifetime.Singleton)
                   .AsSelf();

            // Game manager (MonoBehaviour in hierarchy)
            builder.RegisterComponentInHierarchy<GameManager>();

            // Game lifecycle bootstrapper
            builder.RegisterEntryPoint<GameLifecycleBootstrapper>();

            // Play-test bootstrapper
            GameInstaller.FindOrFallback<PlayTestBootstrap>(builder);

            // Presentation controllers — POCOs, scoped to scene lifetime via the container
            builder.Register<LevelFlowController>(Lifetime.Singleton);
            builder.Register<WinLoseEvaluator>(Lifetime.Singleton);

            // UI components (MonoBehaviours that may or may not be in scene)
            GameInstaller.FindOrFallback<HudPresenter>(builder);

            GameInstaller.FindOrFallback<PowerUpUI>(builder);
            GameInstaller.FindOrFallback<AchievementNotificationUI>(builder);

            // Consent flow UI — MonoBehaviours live on the consent scene prefab
            GameInstaller.FindOrFallback<AgeGateModal>(builder);
            GameInstaller.FindOrFallback<ConsentModal>(builder);
            GameInstaller.FindOrFallback<SettingsPrivacyController>(builder);
            GameInstaller.FindOrFallback<SettingsSoundController>(builder);

            // Main menu — entry point after onboarding; manages Play/Daily/Settings/Privacy buttons
            GameInstaller.FindOrFallback<MainMenuController>(builder);

            // World map — shows 2 biome cards (Crystal Mines + Volcanic Forge) with progress
            GameInstaller.FindOrFallback<WorldMapController>(builder);

            // Daily challenge — entry screen with streak/countdown/play
            GameInstaller.FindOrFallback<DailyChallengeController>(builder);

            // AI art provider — reads from BiomeArtCatalog ScriptableObject (optional, returns defaults if empty)
            // Fix #A3: Load catalog from Resources and register, otherwise always returns null/default
            var catalog = UnityEngine.Resources.Load<BiomeArtCatalog>("Data/BiomeArtCatalog");
            builder.Register<IBiomeArtProvider>(resolver =>
                new ScriptableObjectBiomeArtProvider(catalog), Lifetime.Singleton);

            // Onboarding orchestrator — POCO, owned by container; runs Splash → AgeGate → Consent → MainMenu
            builder.Register<OnboardingFlowController>(Lifetime.Singleton);
        }
    }
}
