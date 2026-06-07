using VContainer;
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
    internal static class GameplayInstallerModule
    {
        public static void Configure(IContainerBuilder builder)
        {
            // Application services
            builder.Register<IMoldSelectionService, MoldSelectionService>(Lifetime.Singleton);
            builder.Register<IAudioService, AudioService>(Lifetime.Singleton);

            // Asset provider (Addressables or Resources)
#if ENABLE_ADDRESSABLES
            builder.Register<IAssetProvider, AddressablesAssetProvider>(Lifetime.Singleton);
            builder.RegisterBuildCallback(resolver => resolver.Resolve<IAssetProvider>().Initialize());
#else
            builder.Register<IAssetProvider, ResourcesAssetProvider>(Lifetime.Singleton);
            builder.RegisterBuildCallback(resolver => resolver.Resolve<IAssetProvider>().Initialize());
#endif

            // Particle and streaming
            builder.Register<IParticleFactory, ParticleFactory>(Lifetime.Singleton);
            builder.Register<IStreamRenderer, StreamRenderer>(Lifetime.Singleton);
            builder.Register<IStreamTrailController, StreamTrailController>(Lifetime.Singleton);

            // Animation
            builder.Register<IAnimationService, AnimationService>(Lifetime.Singleton);

            // Level setup and validation
            builder.Register<ILevelSetupService, LevelSetupService>(Lifetime.Singleton);
            builder.Register<ILevelValidationService, LevelValidationService>(Lifetime.Singleton);

            // Cast and reaction
            builder.Register<ICastService, CastService>(Lifetime.Singleton);
            builder.Register<IReactionService, ReactionService>(Lifetime.Singleton);

            // Input handler service
            builder.Register<IInputHandlerService, InputHandlerService>(Lifetime.Singleton);

            // Input handler subsystem — composed from 3 focused services.
            // Register the focused interfaces so consumers (MoldPoolInitializer,
            // GameManager) can depend on the smallest contract they need.
            builder.Register<IMoldLookupCache, MoldLookupCache>(Lifetime.Singleton);
            builder.Register<IInputHandlerDefaults, InputHandlerDefaults>(Lifetime.Singleton);
            builder.Register<IMoldInputRouter, MoldInputRouter>(Lifetime.Singleton);

            // Developer tools — PourSystemController implements 3 focused
            // interfaces (IPourSimulator / IPourHistoryService / IPourDebugController)
            // plus the IPourSystemController facade. Register all 4 so consumers
            // can depend on the smallest contract that fits their need.
            builder.Register<PourSystemController>(Lifetime.Singleton)
                   .As<IPourSimulator>()
                   .As<IPourHistoryService>()
                   .As<IPourDebugController>()
                   .AsSelf();
        }
    }
}
