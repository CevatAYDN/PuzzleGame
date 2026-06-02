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
        [Header("Configurations (auto-loaded from Resources if not assigned)")]
        [SerializeField] public GameConfig gameConfig;
        [SerializeField] public AnimationConfig animationConfig;
        [SerializeField] public LevelConfig levelConfig;
        [SerializeField] public AudioConfig audioConfig;
        [SerializeField] public LevelData[] levelCatalog;

        protected override void Configure(IContainerBuilder builder)
        {
            // Configs — load from Resources if not assigned
            if (gameConfig == null) gameConfig = Resources.Load<GameConfig>("Data/GameConfig");
            if (animationConfig == null) animationConfig = Resources.Load<AnimationConfig>("Data/AnimationConfig");
            if (levelConfig == null) levelConfig = Resources.Load<LevelConfig>("Data/LevelConfig");
            if (audioConfig == null) audioConfig = Resources.Load<AudioConfig>("Data/AudioConfig");

            if (gameConfig == null)
            {
                gameConfig = ScriptableObject.CreateInstance<GameConfig>();
                gameConfig.name = "GameConfig (Default)";
                BottleLogger.LogWarning("GameConfig not found — created default instance.");
            }

            builder.RegisterInstance(gameConfig);
            builder.RegisterInstance(animationConfig);
            builder.RegisterInstance(levelConfig);
            builder.RegisterInstance(audioConfig);

            // Level catalog
            if (levelCatalog == null || levelCatalog.Length == 0)
            {
                levelCatalog = Resources.LoadAll<LevelData>("Levels");
            }
            if (levelCatalog == null || levelCatalog.Length == 0)
            {
                BottleLogger.LogWarning("No LevelData found — level selection will be empty.");
                levelCatalog = System.Array.Empty<LevelData>();
            }
            builder.RegisterInstance(levelCatalog);
            builder.RegisterInstance<Camera>(Camera.main);

            // Infrastructure — no dependencies
            builder.Register<IRendererService, RendererService>(Lifetime.Singleton);
            builder.Register<PoolManager>(Lifetime.Singleton);
            builder.RegisterInstance<IUpdateManager>(UpdateManager.Instance);

            // Tween service
#if PRIME_TWEEN_INSTALLED
            builder.Register<ITweenService, PrimeTweenService>(Lifetime.Singleton);
#else
            builder.Register<ITweenService, CoroutineTweenService>(Lifetime.Singleton);
#endif

            // Input handler
#if UNITY_ANDROID || UNITY_IOS
            builder.Register<IInputHandler, MobileInputHandler>(Lifetime.Singleton);
#else
            builder.Register<IInputHandler, InputHandler>(Lifetime.Singleton);
#endif

            // Domain services
            var colorTolerance = gameConfig.colorMatchTolerance;
            builder.Register<IBottleValidator, BottleValidationService>(Lifetime.Singleton)
                   .WithParameter(colorTolerance);
            builder.Register<IGameStateMachine, GameStateMachine>(Lifetime.Singleton);
            builder.Register<IGameHistoryManager, GameHistoryManager>(Lifetime.Singleton);
            builder.Register<ILevelProgressService, SecureFileLevelProgressService>(Lifetime.Singleton);
            builder.Register<ILevelRepository, ScriptableObjectLevelRepository>(Lifetime.Singleton);
            builder.Register<ILevelGenerator, DifficultyBasedLevelGenerator>(Lifetime.Singleton);
            builder.Register<ILocalizationService, LocalizationService>(Lifetime.Singleton)
                   .WithParameter(Domain.Models.SupportedLanguage.Turkish);

            // Application services — depend on config/tween
            builder.Register<IBottleSelectionService, BottleSelectionService>(Lifetime.Singleton);
            builder.Register<IAudioService, AudioService>(Lifetime.Singleton);
            builder.Register<IAnimationService, AnimationService>(Lifetime.Singleton);
            builder.Register<ILevelSetupService, LevelSetupService>(Lifetime.Singleton);
            builder.Register<ILevelValidationService, LevelValidationService>(Lifetime.Singleton);
            builder.Register<IInputHandlerService, InputHandlerService>(Lifetime.Singleton);

            // GameManager — inject via VContainer
            builder.RegisterComponentInHierarchy<GameManager>();

            BottleLogger.LogInfo("GameInstaller configured — all services registered.");
        }
    }
}
