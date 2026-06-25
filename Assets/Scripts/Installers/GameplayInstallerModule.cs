using UnityEngine;
using Nexus.Core;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Infrastructure.Providers;
using PuzzleGame.Presentation;

namespace PuzzleGame.Installers
{
    /// <summary>
    /// Registers gameplay services.
    /// Includes: mold selection, audio, asset provider, particles, streaming,
    /// animation, level setup/validation, cast, reaction, input handling,
    /// pour system (dev tools).
    /// </summary>
    public static class GameplayInstallerModule
    {
        public static void Configure(IContextBuilder builder, NexusDI container)
        {
            // Application services
            builder.Bind<IMoldSelectionService, MoldSelectionService>();
            builder.Bind<IAudioService, AudioService>();

            // Asset provider (Addressables or Resources)
#if ENABLE_ADDRESSABLES
            builder.Bind<IAssetProvider, AddressablesAssetProvider>();
#else
            builder.Bind<IAssetProvider, ResourcesAssetProvider>();
#endif
            // Note: IAssetProvider.Initialize() is run in GameInstaller.OnInitializeAsync

            // Particle and streaming
            builder.Bind<IParticleFactory, ParticleFactory>();
            builder.Bind<IStreamRenderer, StreamRenderer>();
            builder.Bind<IStreamTrailController, StreamTrailController>();

            // Animation
            builder.Bind<IAnimationService, AnimationService>();

            // Level setup and validation
            builder.Bind<ILevelSetupService, LevelSetupService>();
            builder.Bind<ILevelValidationService, LevelValidationService>();

            // Cast and reaction
            builder.Bind<ICastService, CastService>();
            builder.Bind<IReactionService, ReactionService>();

            // Leaderboard
            builder.Bind<ILevelEditorService, LevelEditorService>();
            builder.Bind<ILeaderboardService, LeaderboardService>();

            // Season / Progress
            SeasonConfig seasonConfig = null;
            container.BindFactory<SeasonConfig>(() =>
            {
                if (seasonConfig == null)
                {
                    seasonConfig = ConfigLoader.LoadOrThrow<SeasonConfig>(null, "Data/SeasonConfig", "SeasonConfig");
                }
                return seasonConfig;
            });
            builder.Bind<IProgressService, ProgressService>();

            // XP & Season progress persistence for IProgressService
            builder.Bind<IProgressRepository, PlayerPrefsProgressRepository>();

            // Power-ups
            IPowerUpService powerUpService = null;
            container.BindFactory<IPowerUpService>(() =>
            {
                if (powerUpService == null)
                {
                    var events = container.Resolve<IEventAggregator>();
                    var animation = container.Resolve<IAnimationService>();
                    var storage = container.Resolve<IChargeStorageService>();
                    var random = container.Resolve<IRandomProvider>();
                    powerUpService = new PowerUpService(events, animation, storage, random);
                }
                return powerUpService;
            });

            builder.Bind<IChargeStorageService, PlayerPrefsChargeStorage>();

            IRandomProvider randomProvider = null;
            container.BindFactory<IRandomProvider>(() =>
            {
                if (randomProvider == null)
                {
                    randomProvider = new SystemRandomProvider();
                }
                return randomProvider;
            });

            // Multi-pour orchestration
            builder.Bind<IMultiPourService, MultiPourService>();

            // Input handler service
            builder.Bind<IInputHandlerService, InputHandlerService>();

            // Input handler subsystem
            builder.Bind<IMoldLookupCache, MoldLookupCache>();
            builder.Bind<IInputHandlerDefaults, InputHandlerDefaults>();
            builder.Bind<IMoldInputRouter, MoldInputRouter>();

            // Developer tools — PourSystemController implements 3 focused
            // interfaces (IPourSimulator / IPourHistoryService / IPourDebugController)
            // plus the IPourSystemController facade.
            builder.Bind<PourSystemController>();

            container.BindFactory<IPourSimulator>(() => container.Resolve<PourSystemController>());
            container.BindFactory<IPourHistoryService>(() => container.Resolve<PourSystemController>());
            container.BindFactory<IPourDebugController>(() => container.Resolve<PourSystemController>());
        }
    }
}
