using UnityEngine;
using Nexus.Core;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
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
        public static void Configure(IContextBuilder builder, NexusDI container)
        {
            // Economy
            builder.Bind<ICoinWallet, CoinWallet>();
            builder.Bind<IHintService, HintService>();
            builder.Bind<IUndoService, UndoService>();

            // Tutorial
            builder.Bind<ITutorialService, TutorialService>();

            // Haptics + analytics (mobile platform hooks; no-op by default)
            builder.Bind<IHapticFeedbackService, HapticFeedbackService>();
            builder.Bind<HapticObserver>();
            builder.Bind<IAnalyticsService, NoOpAnalyticsService>();

            // Achievements
            builder.Bind<IAchievementService, AchievementService>();

            // Accessibility (color-blind mode) config with singleton closure caching
            AccessibilityConfig accessibilityConfig = null;
            container.BindFactory<AccessibilityConfig>(() =>
            {
                if (accessibilityConfig == null)
                {
                    accessibilityConfig = ConfigLoader.LoadOrThrow<AccessibilityConfig>(null, "Data/AccessibilityConfig", "AccessibilityConfig");
                }
                return accessibilityConfig;
            });
            builder.Bind<IAccessibilityService, AccessibilityService>();

            // Cosmetic shop config with singleton closure caching
            CosmeticConfig cosmeticConfig = null;
            container.BindFactory<CosmeticConfig>(() =>
            {
                if (cosmeticConfig == null)
                {
                    cosmeticConfig = ConfigLoader.LoadOrThrow<CosmeticConfig>(null, "Data/CosmeticConfig", "CosmeticConfig");
                }
                return cosmeticConfig;
            });
            builder.Bind<ICosmeticShopService, CosmeticShopService>();

            // Crash reporting
            builder.Bind<ICrashReportingService, NoOpCrashReportingService>();
            // Note: CrashReporter.Current callback is run during GameplayLifecycle.OnInitializeAsync

            // Ads with singleton closure caching
            IAdService adService = null;
            container.BindFactory<IAdService>(() =>
            {
                if (adService == null)
                {
#if HAS_GOOGLE_MOBILE_ADS
                    var config = container.Resolve<GameConfig>();
                    var analytics = container.Resolve<IAnalyticsService>();
                    adService = new AdMobService(
                        gameConfig: config,
                        rewardedAdUnitId: null,
                        interstitialAdUnitId: null,
                        analytics: analytics);
#else
                    adService = new NoOpAdService();
#endif
                }
                return adService;
            });
            builder.Bind<PurchaseController>();

            // GDPR consent + COPPA age gate
            builder.Bind<IAgeVerificationService, AgeGateService>();
            builder.Bind<IConsentManager, ConsentManager>();

            // Feature flags
            builder.Bind<IFeatureFlagService, FeatureFlagService>();

            // Daily challenge + streak (retention)
            builder.Bind<IDailyChallengeService, DailyChallengeService>();
            builder.Bind<IStreakService, StreakService>();

            // Audio settings
            builder.Bind<IAudioSettingsService, PlayerPrefsAudioSettingsService>();

            // Memory snapshot
            builder.Bind<IMemorySnapshotService, UnityMemorySnapshotService>();
        }
    }
}
