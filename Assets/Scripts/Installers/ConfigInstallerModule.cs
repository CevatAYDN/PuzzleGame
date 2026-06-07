using UnityEngine;
using VContainer;
using PuzzleGame.Domain;
using PuzzleGame.Domain.Models;
using PuzzleGame.Application.Configuration;

namespace PuzzleGame.Installers
{
    /// <summary>
    /// Loads and registers all game configurations.
    /// Responsibilities:
    ///   - Load configs from Resources (fail-loudly for required, default for optional)
    ///   - Validate config values (clamp, min/max)
    ///   - Register configs as DI instances
    ///   - Load level catalog
    ///   - Register Camera singleton
    /// </summary>
    internal static class ConfigInstallerModule
    {
        public static void Configure(IContainerBuilder builder, GameInstaller installer)
        {
            LoadOrThrowConfigs(installer);
            LoadOrThrowLevelCatalog(installer);

            // Configs as instances
            builder.RegisterInstance(installer.gameConfig);
            builder.RegisterInstance(installer.animationConfig);
            builder.RegisterInstance(installer.levelConfig);
            builder.RegisterInstance(installer.audioConfig);
            builder.RegisterInstance(installer.streamVFXConfig);
            builder.RegisterInstance(installer.economyConfig);
            builder.RegisterInstance(installer.wobbleConfig);
            builder.RegisterInstance(installer.levelCatalog);

            // Camera singleton
            builder.Register<Camera>(resolver =>
            {
                var cam = Camera.main;
                if (cam == null)
                    throw new System.InvalidOperationException(
                        "Camera.main is null when resolving Camera dependency. " +
                        "Ensure a Camera tagged 'MainCamera' exists in the scene before the LifetimeScope activates.");
                return cam;
            }, Lifetime.Singleton);
        }

        private static void LoadOrThrowConfigs(GameInstaller installer)
        {
            // Fix #16: Replaced 8 near-identical if/Resources.Load/throw blocks with
            // a single helper call per config. The helper centralises the fail-
            // loudly policy and the Inspector-override-vs-Resources fallback, so
            // adding a new config no longer means copy-pasting 5 lines and a unique
            // log message.
            installer.gameConfig       = ConfigLoader.LoadOrThrow(installer.gameConfig,       "Data/GameConfig",       nameof(installer.gameConfig));
            installer.animationConfig  = ConfigLoader.LoadOrThrow(installer.animationConfig,  "Data/AnimationConfig",  nameof(installer.animationConfig));
            installer.levelConfig      = ConfigLoader.LoadOrThrow(installer.levelConfig,      "Data/LevelConfig",      nameof(installer.levelConfig));
            installer.audioConfig      = ConfigLoader.LoadOrThrow(installer.audioConfig,      "Data/AudioConfig",      nameof(installer.audioConfig));

            // These two are tolerated missing (developer tools) but still
            // get a default-instance fallback so downstream code never sees null.
            installer.streamVFXConfig  = ConfigLoader.LoadOrDefault(installer.streamVFXConfig, "Data/StreamVFXConfig", nameof(installer.streamVFXConfig));
            installer.economyConfig    = ConfigLoader.LoadOrDefault(installer.economyConfig,   "Data/EconomyConfig",   nameof(installer.economyConfig));

            installer.wobbleConfig     = ConfigLoader.LoadOrThrow(installer.wobbleConfig,     "Data/WobbleConfig",     nameof(installer.wobbleConfig));

            // OnValidate the values the inspector might have corrupted
            installer.gameConfig.colorMatchTolerance = Mathf.Max(
                ForgeConstants.ColorMatchEpsilon, installer.gameConfig.colorMatchTolerance);
            installer.gameConfig.maxLayersPerMold = Mathf.Clamp(
                installer.gameConfig.maxLayersPerMold, 1, ForgeConstants.MaxLayers);
        }

        private static void LoadOrThrowLevelCatalog(GameInstaller installer)
        {
            if (installer.levelCatalog != null && installer.levelCatalog.Length > 0) return;

            installer.levelCatalog = Resources.LoadAll<LevelData>("Levels");
            if (installer.levelCatalog == null || installer.levelCatalog.Length == 0)
            {
                throw new System.InvalidOperationException(
                    "No LevelData assets found in Resources/Levels. Build a level catalog or " +
                    "assign one in the GameInstaller inspector.");
            }
        }
    }
}
