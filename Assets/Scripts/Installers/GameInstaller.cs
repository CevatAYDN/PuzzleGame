using UnityEngine;
using VContainer;
using VContainer.Unity;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Services;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Animation;
using PuzzleGame.Infrastructure.Interfaces;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Infrastructure.Pool;
using PuzzleGame.Configuration;
using PuzzleGame.Logging;

namespace PuzzleGame.Installers
{
    /// <summary>
    /// Composition Root: All dependencies are wired here.
    /// SOLID & DI principles applied.
    /// </summary>
    public class GameInstaller : LifetimeScope
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private AnimationConfig animationConfig;
        [SerializeField] private LevelConfig levelConfig;
        [SerializeField] private AudioConfig audioConfig;
        [SerializeField] private LevelData[] levelCatalog;

        protected override void Configure(IContainerBuilder builder)
        {
            // Domain services (stateless — Singleton)
            builder.Register<IBottleValidator, BottleValidationService>(Lifetime.Singleton);
            builder.Register<IGameHistoryService, GameHistoryService>(Lifetime.Scoped);
            builder.Register<IGameStateMachine, GameStateMachine>(Lifetime.Singleton);
            builder.Register<ILevelRepository, ScriptableObjectLevelRepository>(Lifetime.Singleton);
            builder.Register<ILevelProgressService, PlayerPrefsLevelProgressService>(Lifetime.Singleton);

            // Application services
            builder.Register<IAnimationService, AnimationService>(Lifetime.Singleton);
            builder.Register<IBottleSelectionService, BottleSelectionService>(Lifetime.Singleton);
            builder.Register<IAudioService, AudioService>(Lifetime.Singleton);

            // Infrastructure
            builder.Register<IRendererService, RendererService>(Lifetime.Singleton);
            builder.Register<IUpdateManager, UpdateManager>(Lifetime.Singleton);

            // Tween service — use PrimeTween if installed, else coroutine fallback
            builder.Register<ITweenService, CoroutineTweenService>(Lifetime.Singleton);

            // Input handler — platform-specific
#if UNITY_ANDROID || UNITY_IOS
            builder.Register<IInputHandler, MobileInputHandler>(Lifetime.Singleton);
#else
            builder.Register<IInputHandler, InputHandler>(Lifetime.Singleton);
#endif

            // Configuration
            builder.RegisterInstance(gameConfig);
            builder.RegisterInstance(animationConfig);
            builder.RegisterInstance(levelConfig);
            builder.RegisterInstance(audioConfig);
            builder.RegisterInstance(levelCatalog);

            // Pools
            builder.Register<PoolManager>(Lifetime.Singleton);
        }
    }
}
