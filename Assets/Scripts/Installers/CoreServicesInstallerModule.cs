using UnityEngine;
using Nexus.Core;
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
        public static void Configure(IContextBuilder builder, NexusDI container)
        {
            // Infrastructure — no dependencies
            builder.Bind<IRendererService, RendererService>();
            builder.Bind<IPoolManager, PoolManager>();
            builder.Bind<IColorAdapter, ColorAdapter>();

            // Nexus-powered event aggregator (bridges IEventAggregator to ISignalBus)
            // Uses a factory so the ISignalBus is resolved lazily after context configuration
            IEventAggregator eventAggregator = null;
            container.BindFactory<IEventAggregator>(() =>
            {
                if (eventAggregator == null)
                {
                    var signalBus = container.Resolve<ISignalBus>();
                    eventAggregator = new NexusEventAggregator(signalBus);
                }
                return eventAggregator;
            });
            builder.Bind<IShaderOptimizer, ShaderOptimizer>();

            // UpdateManager MonoBehaviour
            var updateManagerGo = new GameObject("UpdateManager");
            UnityEngine.Object.DontDestroyOnLoad(updateManagerGo);
            var updateManager = updateManagerGo.AddComponent<UpdateManager>();
            builder.BindInstance<IUpdateManager>(updateManager);
            builder.BindInstance(updateManager);

            // Tween service
            builder.Bind<ITweenService, PrimeTweenService>();

            // Input handler
#if UNITY_ANDROID || UNITY_IOS
            builder.Bind<IInputHandler, MobileInputHandler>();
#else
            builder.Bind<IInputHandler, InputHandler>();
#endif

            // Domain services
            IMoldValidator moldValidator = null;
            container.BindFactory<IMoldValidator>(() =>
            {
                if (moldValidator == null)
                {
                    var config = container.Resolve<GameConfig>();
                    moldValidator = new MoldValidationService(config.colorMatchTolerance);
                }
                return moldValidator;
            });

            // Game state machine
            builder.Bind<IGameStateMachine, GameStateMachine>();

            // Game history manager
            builder.Bind<IGameHistoryManager, GameHistoryManager>();

            // Level progress service
            builder.Bind<ILevelProgressService, SecureFileLevelProgressService>();

            // Level repository
            builder.Bind<ILevelRepository, ScriptableObjectLevelRepository>();

            // Level generator
            builder.Bind<ILevelGenerator, ProceduralLevelGenerator>();
        }
    }
}
