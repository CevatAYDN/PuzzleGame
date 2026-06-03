using UnityEngine;
using VContainer;
using VContainer.Unity;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Services;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Events;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Infrastructure.Pool;
using PuzzleGame.Infrastructure;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Installers
{
    /// <summary>
    /// Composition Root: All dependencies are wired here.
    /// SOLID & DI principles applied.
    ///
    /// FAIL-LOUDLY: configuration loading and DI registration throw
    /// instead of silently defaulting — the player should never see a
    /// half-configured game due to a missing asset.
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
            LoadOrThrowConfigs();
            LoadOrThrowLevelCatalog();

            // Configs as instances
            builder.RegisterInstance(gameConfig);
            builder.RegisterInstance(animationConfig);
            builder.RegisterInstance(levelConfig);
            builder.RegisterInstance(audioConfig);
            builder.RegisterInstance(levelCatalog);

            // Fix #7: Lazy Camera.main — Configure() may run before the scene is fully ready.
            // Using a factory ensures Camera.main is resolved at the time the first consumer
            // requests it (after Start()), not during LifetimeScope.Configure().
            builder.Register<Camera>(resolver =>
            {
                var cam = Camera.main;
                if (cam == null)
                    throw new System.InvalidOperationException(
                        "Camera.main is null when resolving Camera dependency. " +
                        "Ensure a Camera tagged 'MainCamera' exists in the scene before the LifetimeScope activates.");
                return cam;
            }, Lifetime.Singleton);

            // Infrastructure — no dependencies
            builder.Register<IRendererService, RendererService>(Lifetime.Singleton);
            builder.Register<IPoolManager, PoolManager>(Lifetime.Singleton);
            builder.Register<IColorAdapter, ColorAdapter>(Lifetime.Singleton);
            builder.Register<IEventAggregator, EventAggregator>(Lifetime.Singleton);
            builder.Register<IShaderOptimizer, ShaderOptimizer>(Lifetime.Singleton);
            builder.RegisterComponentOnNewGameObject<UpdateManager>(Lifetime.Singleton)
                .UnderTransform((Transform)null); // DontDestroyOnLoad — root GameObject

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
            var colorTolerance = gameConfig.colorMatchTolerance;
            builder.Register<IBottleValidator, BottleValidationService>(Lifetime.Singleton)
                   .WithParameter(colorTolerance);
            builder.Register<IGameStateMachine, GameStateMachine>(Lifetime.Singleton);
            builder.Register<IGameHistoryManager, GameHistoryManager>(Lifetime.Singleton);
            builder.Register<ILevelProgressService, SecureFileLevelProgressService>(Lifetime.Singleton);
            builder.Register<ILevelRepository, ScriptableObjectLevelRepository>(Lifetime.Singleton);
            builder.Register<ILevelGenerator, DifficultyBasedLevelGenerator>(Lifetime.Singleton);
            builder.Register<ITranslationProvider, HardcodedTranslationProvider>(Lifetime.Singleton);
            builder.Register<ILocalizationService, LocalizationService>(Lifetime.Singleton)
                   .WithParameter(Domain.Models.SupportedLanguage.Turkish);
            builder.Register<ISaveManager, GameSaveManager>(Lifetime.Singleton);

            // Application services
            builder.Register<IBottleSelectionService, BottleSelectionService>(Lifetime.Singleton);
            builder.Register<IAudioService, AudioService>(Lifetime.Singleton);
            builder.Register<IParticleFactory, ParticleFactory>(Lifetime.Singleton);
            builder.Register<IStreamRenderer, StreamRenderer>(Lifetime.Singleton);
            builder.Register<IAnimationService, AnimationService>(Lifetime.Singleton);
            builder.Register<ILevelSetupService, LevelSetupService>(Lifetime.Singleton);
            builder.Register<ILevelValidationService, LevelValidationService>(Lifetime.Singleton);
            builder.Register<IPourService, PourService>(Lifetime.Singleton);
            builder.Register<IReactionService, ReactionService>(Lifetime.Singleton);
            builder.Register<IInputHandlerService, InputHandlerService>(Lifetime.Singleton);

            // GameManager — inject via VContainer
            builder.RegisterComponentInHierarchy<GameManager>();

            BottleLogger.LogInfo("GameInstaller configured — all services registered.");
        }

        private void LoadOrThrowConfigs()
        {
            if (gameConfig == null) gameConfig = Resources.Load<GameConfig>("Data/GameConfig");
            if (gameConfig == null)
            {
                throw new System.InvalidOperationException(
                    "GameConfig asset missing at Resources/Data/GameConfig. Cannot start without it.");
            }

            if (animationConfig == null) animationConfig = Resources.Load<AnimationConfig>("Data/AnimationConfig");
            if (animationConfig == null)
            {
                throw new System.InvalidOperationException(
                    "AnimationConfig asset missing at Resources/Data/AnimationConfig.");
            }

            if (levelConfig == null) levelConfig = Resources.Load<LevelConfig>("Data/LevelConfig");
            if (levelConfig == null)
            {
                throw new System.InvalidOperationException(
                    "LevelConfig asset missing at Resources/Data/LevelConfig.");
            }

            if (audioConfig == null) audioConfig = Resources.Load<AudioConfig>("Data/AudioConfig");
            if (audioConfig == null)
            {
                throw new System.InvalidOperationException(
                    "AudioConfig asset missing at Resources/Data/AudioConfig.");
            }

            // OnValidate the values the inspector might have corrupted
            gameConfig.colorMatchTolerance = Mathf.Max(
                BottleConstants.ColorMatchEpsilon, gameConfig.colorMatchTolerance);
            gameConfig.maxLayersPerBottle = Mathf.Clamp(
                gameConfig.maxLayersPerBottle, 1, BottleConstants.MaxLayers);
        }

        private void LoadOrThrowLevelCatalog()
        {
            if (levelCatalog != null && levelCatalog.Length > 0) return;

            levelCatalog = Resources.LoadAll<LevelData>("Levels");
            if (levelCatalog == null || levelCatalog.Length == 0)
            {
                throw new System.InvalidOperationException(
                    "No LevelData assets found in Resources/Levels. Build a level catalog or " +
                    "assign one in the GameInstaller inspector.");
            }
        }
    }
}
