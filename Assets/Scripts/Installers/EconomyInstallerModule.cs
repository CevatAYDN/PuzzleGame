using VContainer;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Infrastructure.Implementations;
using PuzzleGame.Presentation;

namespace PuzzleGame.Installers
{
    /// <summary>
    /// Registers economy, monetization, and retention services.
    /// Includes: coin wallet, hints, undo, tutorial, haptics, analytics,
    /// crash reporting, ads, GDPR consent, feature flags, retention.
    /// </summary>
    internal static class EconomyInstallerModule
    {
        public static void Configure(IContainerBuilder builder)
        {
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

            // Achievements
            builder.Register<IAchievementService, AchievementService>(Lifetime.Singleton);

            // Accessibility (color-blind mode)
            builder.Register<AccessibilityConfig>(resolver =>
            {
                var config = UnityEngine.Resources.Load<AccessibilityConfig>("Data/AccessibilityConfig");
                if (config == null) throw new System.InvalidOperationException("AccessibilityConfig missing at Resources/Data/AccessibilityConfig");
                return config;
            }, Lifetime.Singleton);
            builder.Register<IAccessibilityService, AccessibilityService>(Lifetime.Singleton);

            // Cosmetic shop
            builder.Register<CosmeticConfig>(resolver =>
            {
                var config = UnityEngine.Resources.Load<CosmeticConfig>("Data/CosmeticConfig");
                if (config == null) throw new System.InvalidOperationException("CosmeticConfig missing at Resources/Data/CosmeticConfig");
                return config;
            }, Lifetime.Singleton);
            builder.Register<ICosmeticShopService, CosmeticShopService>(Lifetime.Singleton);

            // Crash reporting
            builder.Register<ICrashReportingService, NoOpCrashReportingService>(Lifetime.Singleton);
            builder.RegisterBuildCallback(resolver =>
            {
                CrashReporter.Current = resolver.Resolve<ICrashReportingService>();
            });

            // Ads — compile-time selection between AdMobService and the NoOp
            // fallback. The HasGoogleMobileAds symbol is defined by the
            // Infrastructure asmdef when the Google Mobile Ads package is
            // installed; otherwise we never even reference AdMobService, so a
            // missing package can never produce a runtime TypeLoadException.
            builder.Register<IAdService>(resolver =>
            {
#if HAS_GOOGLE_MOBILE_ADS
                var config = resolver.Resolve<GameConfig>();
                return new AdMobService(
                    gameConfig: config,
                    rewardedAdUnitId: null,
                    interstitialAdUnitId: null,
                    analytics: resolver.Resolve<IAnalyticsService>());
#else
                return new NoOpAdService();
#endif
            }, Lifetime.Singleton);
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
        }
    }
}
