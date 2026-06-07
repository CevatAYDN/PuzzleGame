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
using PuzzleGame.Infrastructure.Providers;
using PuzzleGame.Presentation;
using PuzzleGame.Presentation.UI;

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
        [SerializeField] public StreamVFXConfig streamVFXConfig;
        [SerializeField] public EconomyConfig economyConfig;
        [SerializeField] public WobbleConfig wobbleConfig;
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
            builder.RegisterInstance(streamVFXConfig);
            builder.RegisterInstance(economyConfig);
            builder.RegisterInstance(wobbleConfig);
            builder.RegisterInstance(levelCatalog);

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

            builder.Register<IMoldValidator>(resolver =>
                new MoldValidationService(resolver.Resolve<GameConfig>().colorMatchTolerance),
                Lifetime.Singleton);
            builder.Register<IGameStateMachine, GameStateMachine>(Lifetime.Singleton);
            builder.Register<IGameHistoryManager, GameHistoryManager>(Lifetime.Singleton);
            builder.Register<ILevelProgressService, SecureFileLevelProgressService>(Lifetime.Singleton);
            builder.Register<ILevelRepository, ScriptableObjectLevelRepository>(Lifetime.Singleton);
            builder.Register<ILevelGenerator, DifficultyBasedLevelGenerator>(Lifetime.Singleton);
            // Sprint #12 + #15: JsonTranslationProvider is the default sync loader
            // (Editor + Standalone: File.ReadAllText works). On Android the
            // StreamingAssets path lives inside the APK and requires UnityWebRequest,
            // so the platform-specific async provider is registered instead and the
            // LocalizationBootstrap MonoBehaviour is created to preload it.
            builder.Register<ITranslationProvider>(resolver => new JsonTranslationProvider(), Lifetime.Singleton);
            builder.Register<ILocalizationService, LocalizationService>(Lifetime.Singleton)
                   .WithParameter(Domain.Models.SupportedLanguage.Turkish);

#if UNITY_ANDROID && !UNITY_EDITOR
            // Android override: sync File.ReadAllText on StreamingAssets returns 0 bytes.
            // The async provider caches the JSON via UnityWebRequest; LocalizationBootstrap
            // awaits the load during Start() so the first GetString call finds data ready.
            builder.Register<StreamingAssetsJsonTranslationProvider>(Lifetime.Singleton);
            builder.Register<ITranslationProvider>(resolver =>
                resolver.Resolve<StreamingAssetsJsonTranslationProvider>(), Lifetime.Singleton);
            builder.Register<IAsyncTranslationProvider>(resolver =>
                resolver.Resolve<StreamingAssetsJsonTranslationProvider>(), Lifetime.Singleton);
            var bootstrapGo = new GameObject("LocalizationBootstrap");
            builder.RegisterComponent(bootstrapGo.AddComponent<LocalizationBootstrap>());
#endif
            builder.Register<ISaveCrypto, SaveCrypto>(Lifetime.Singleton);
            builder.Register<ISaveStorage, SaveStorage>(Lifetime.Singleton);
            builder.Register<ISaveManager, GameSaveManager>(Lifetime.Singleton);

            // Economy
            builder.Register<ICoinWallet, CoinWallet>(Lifetime.Singleton);
            builder.Register<IHintService, HintService>(Lifetime.Singleton);
            builder.Register<IUndoService, UndoService>(Lifetime.Singleton);

            // Tutorial
            builder.Register<ITutorialService, TutorialService>(Lifetime.Singleton);

            // Haptics + analytics (mobile platform hooks; no-op by default)
            builder.Register<IHapticFeedbackService, HapticFeedbackService>(Lifetime.Singleton);
            builder.Register<HapticObserver>(Lifetime.Singleton);
            builder.Register<IAnalyticsService, NoOpAnalyticsService>(Lifetime.Singleton);

            builder.Register<ICrashReportingService, NoOpCrashReportingService>(Lifetime.Singleton);
            builder.RegisterBuildCallback(resolver =>
            {
                CrashReporter.Current = resolver.Resolve<ICrashReportingService>();
            });

            // Ads (falls back to safe no-op inside AdMobService when SDK is missing)
            builder.Register<IAdService>(resolver => new AdMobService(resolver.Resolve<GameConfig>(), null, null, resolver.Resolve<IAnalyticsService>()), Lifetime.Singleton);
            builder.Register<PurchaseController>(Lifetime.Singleton);

            // GDPR consent + COPPA age gate
            builder.Register<IAgeVerificationService, AgeGateService>(Lifetime.Singleton);
            builder.Register<IConsentManager, ConsentManager>(Lifetime.Singleton);

            // Feature flags
            builder.Register<IFeatureFlagService, FeatureFlagService>(Lifetime.Singleton);

            // Daily challenge + streak (retention)
            builder.Register<IDailyChallengeService, DailyChallengeService>(Lifetime.Singleton);
            builder.Register<IStreakService, StreakService>(Lifetime.Singleton);

            // Audio settings (persistent BGM/SFX enable + volume; survives restarts).
            // IEventAggregator is auto-injected by VContainer; the constructor default
            // (null) keeps persistence working even when aggregator is absent.
            builder.Register<IAudioSettingsService, PlayerPrefsAudioSettingsService>(Lifetime.Singleton);

            // Memory snapshot — Unity Profiler-backed (built-in, no package).
            // Used for leak detection (e.g. capture before/after level load).
            // Swap to Memory Profiler package-backed impl for deep snapshots.
            builder.Register<IMemorySnapshotService, UnityMemorySnapshotService>(Lifetime.Singleton);

            // Application services
            builder.Register<IMoldSelectionService, MoldSelectionService>(Lifetime.Singleton);
            builder.Register<IAudioService, AudioService>(Lifetime.Singleton);
#if ENABLE_ADDRESSABLES
            builder.Register<IAssetProvider, AddressablesAssetProvider>(Lifetime.Singleton);
            builder.RegisterBuildCallback(resolver => resolver.Resolve<IAssetProvider>().Initialize());
#else
            builder.Register<IAssetProvider, ResourcesAssetProvider>(Lifetime.Singleton);
            builder.RegisterBuildCallback(resolver => resolver.Resolve<IAssetProvider>().Initialize());
#endif
            builder.Register<IParticleFactory, ParticleFactory>(Lifetime.Singleton);
            builder.Register<IStreamRenderer, StreamRenderer>(Lifetime.Singleton);
            builder.Register<IStreamTrailController, StreamTrailController>(Lifetime.Singleton);
            builder.Register<IAnimationService, AnimationService>(Lifetime.Singleton);
            builder.Register<ILevelSetupService, LevelSetupService>(Lifetime.Singleton);
            builder.Register<ILevelValidationService, LevelValidationService>(Lifetime.Singleton);
            builder.Register<ICastService, CastService>(Lifetime.Singleton);
            builder.Register<IReactionService, ReactionService>(Lifetime.Singleton);
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
                   .As<IPourSystemController>()
                   .As<IPourSimulator>()
                   .As<IPourHistoryService>()
                   .As<IPourDebugController>()
                   .AsSelf();

            // ErrorIndicator — bootstrap ensures it exists (auto-creates if scene is misconfigured)
            var errorIndicator = PuzzleGame.Presentation.ErrorIndicatorBootstrap.EnsureExists();
            builder.RegisterInstance(errorIndicator)
                .AsSelf()
                .As<IErrorIndicatorService>();

            var cameraEffects = PuzzleGame.Presentation.CameraEffectsBootstrap.EnsureExists();
            builder.RegisterComponent(cameraEffects);

            builder.Register<MoldPoolInitializer>(Lifetime.Singleton)
                   .As<IActiveMoldsProvider>()
                   .AsSelf();

            builder.RegisterComponentInHierarchy<GameManager>();

            // Presentation controllers — POCOs, scoped to scene lifetime via the container
            builder.Register<LevelFlowController>(Lifetime.Singleton);
            builder.Register<WinLoseEvaluator>(Lifetime.Singleton);

            // HUD presenter — must be a MonoBehaviour to serialize inspector references
            RegisterComponentInHierarchyOrFallback<HudPresenter>(builder);

            // Consent flow UI — MonoBehaviours live on the consent scene prefab
            RegisterComponentInHierarchyOrFallback<PuzzleGame.Presentation.UI.AgeGateModal>(builder);
            RegisterComponentInHierarchyOrFallback<PuzzleGame.Presentation.UI.ConsentModal>(builder);
            RegisterComponentInHierarchyOrFallback<PuzzleGame.Presentation.UI.SettingsPrivacyController>(builder);
            RegisterComponentInHierarchyOrFallback<PuzzleGame.Presentation.UI.SettingsSoundController>(builder);

            // Main menu — entry point after onboarding; manages Play/Daily/Settings/Privacy buttons
            RegisterComponentInHierarchyOrFallback<PuzzleGame.Presentation.UI.MainMenuController>(builder);

            // World map — shows 2 biome cards (Crystal Mines + Volcanic Forge) with progress
            RegisterComponentInHierarchyOrFallback<PuzzleGame.Presentation.UI.WorldMapController>(builder);

            // Daily challenge — entry screen with streak/countdown/play
            RegisterComponentInHierarchyOrFallback<PuzzleGame.Presentation.UI.DailyChallengeController>(builder);

            // AI art provider — reads from BiomeArtCatalog ScriptableObject (optional, returns defaults if empty)
            builder.Register<PuzzleGame.Application.Interfaces.IBiomeArtProvider, PuzzleGame.Infrastructure.ScriptableObjectBiomeArtProvider>(Lifetime.Singleton);

            // Onboarding orchestrator — POCO, owned by container; runs Splash → AgeGate → Consent → MainMenu
            builder.Register<OnboardingFlowController>(Lifetime.Singleton);

            MoldLogger.LogInfo("GameInstaller configured — all services registered.");
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

            if (streamVFXConfig == null) streamVFXConfig = Resources.Load<StreamVFXConfig>("Data/StreamVFXConfig");
            if (streamVFXConfig == null)
            {
                MoldLogger.LogWarning("StreamVFXConfig asset missing at Resources/Data/StreamVFXConfig. " +
                    "Using fallback — create it via Tools > PuzzleGame > Open Editor > Data tab.");
                streamVFXConfig = ScriptableObject.CreateInstance<StreamVFXConfig>();
            }

            if (economyConfig == null) economyConfig = Resources.Load<EconomyConfig>("Data/EconomyConfig");
            if (economyConfig == null)
            {
                MoldLogger.LogWarning("EconomyConfig asset missing at Resources/Data/EconomyConfig. " +
                    "Using defaults — create it via Tools > PuzzleGame > Open Editor > Data tab.");
                economyConfig = ScriptableObject.CreateInstance<EconomyConfig>();
            }

            if (wobbleConfig == null) wobbleConfig = Resources.Load<WobbleConfig>("Data/WobbleConfig");
            if (wobbleConfig == null)
            {
                throw new System.InvalidOperationException(
                    "WobbleConfig asset missing at Resources/Data/WobbleConfig.");
            }

            // OnValidate the values the inspector might have corrupted
            gameConfig.colorMatchTolerance = Mathf.Max(
                ForgeConstants.ColorMatchEpsilon, gameConfig.colorMatchTolerance);
            gameConfig.maxLayersPerMold = Mathf.Clamp(
                gameConfig.maxLayersPerMold, 1, ForgeConstants.MaxLayers);
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

        private void RegisterComponentInHierarchyOrFallback<T>(IContainerBuilder builder) where T : Component
        {
            var component = Object.FindAnyObjectByType<T>();
            if (component == null)
            {
                var go = new GameObject($"[Fallback] {typeof(T).Name}");
                component = go.AddComponent<T>();
                DontDestroyOnLoad(go);
                MoldLogger.LogWarning($"[DI Fallback] {typeof(T).Name} not found in scene. Created fallback object: {go.name}");
            }
            builder.RegisterComponent(component);
        }
    }
}
