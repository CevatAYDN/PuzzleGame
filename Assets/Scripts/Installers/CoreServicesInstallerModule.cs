using VContainer;
using VContainer.Unity;
using UnityEngine;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Services;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Events;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Infrastructure.Pool;
using PuzzleGame.Infrastructure;

namespace PuzzleGame.Installers
{
    /// <summary>
    /// Registers core infrastructure services.
    /// These services have no business dependencies and are used by all other layers.
    /// </summary>
    public static class CoreServicesInstallerModule
    {
        public static void Configure(IContainerBuilder builder)
        {
            // Infrastructure — no dependencies
            builder.Register<IRendererService, RendererService>(Lifetime.Singleton);
            builder.Register<IPoolManager, PoolManager>(Lifetime.Singleton);
            builder.Register<IColorAdapter, ColorAdapter>(Lifetime.Singleton);
            builder.Register<IEventAggregator, EventAggregator>(Lifetime.Singleton);
            builder.Register<IShaderOptimizer, ShaderOptimizer>(Lifetime.Singleton);
            builder.RegisterComponentOnNewGameObject<UpdateManager>(Lifetime.Singleton)
                .UnderTransform((Transform)null)
                .AsImplementedInterfaces()
                .AsSelf(); // DontDestroyOnLoad — root GameObject

            // Tween service — PrimeTween is the chosen impl. Coroutine fallback removed (orphan v2).
            builder.Register<ITweenService, PrimeTweenService>(Lifetime.Singleton);

            // Input handler — MobileInputHandler is the chosen impl for touch devices.
            // Both implementations are kept; selection happens at startup based on platform.
#if UNITY_ANDROID || UNITY_IOS
            builder.Register<IInputHandler, MobileInputHandler>(Lifetime.Singleton);
#else
            builder.Register<IInputHandler, InputHandler>(Lifetime.Singleton);
#endif

            // Domain services
            builder.Register<IMoldValidator>(resolver =>
                new MoldValidationService(resolver.Resolve<GameConfig>().colorMatchTolerance),
                Lifetime.Singleton);

            // Game state machine
            builder.Register<IGameStateMachine, GameStateMachine>(Lifetime.Singleton);

            // Game history manager
            builder.Register<IGameHistoryManager, GameHistoryManager>(Lifetime.Singleton);

            // Level progress service
            builder.Register<ILevelProgressService, SecureFileLevelProgressService>(Lifetime.Singleton);

            // Level repository
            builder.Register<ILevelRepository, ScriptableObjectLevelRepository>(Lifetime.Singleton);

            // Level generator
            builder.Register<ILevelGenerator, ProceduralLevelGenerator>(Lifetime.Singleton);
        }
    }
}
