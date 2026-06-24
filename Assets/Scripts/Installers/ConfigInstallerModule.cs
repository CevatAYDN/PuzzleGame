using UnityEngine;
using Nexus.Core;
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
        public static void Configure(IContextBuilder builder, NexusDI container, GameInstaller installer)
        {
            LoadOrThrowConfigs(installer);
            LoadOrThrowLevelCatalog(installer);

            // Configs as instances
            builder.BindInstance(installer.gameConfig);
            builder.BindInstance(installer.animationConfig);
            builder.BindInstance(installer.levelConfig);
            builder.BindInstance(installer.audioConfig);
            builder.BindInstance(installer.streamVFXConfig);
            builder.BindInstance(installer.economyConfig);
            builder.BindInstance(installer.wobbleConfig);
            builder.BindInstance(installer.levelCatalog);

            // Camera singleton via closure factory
            Camera camera = null;
            container.BindFactory<Camera>(() =>
            {
                if (camera == null)
                {
                    camera = Camera.main;
                    if (camera == null)
                        throw new System.InvalidOperationException(
                            "Camera.main is null when resolving Camera dependency. " +
                            "Ensure a Camera tagged 'MainCamera' exists in the scene.");
                }
                return camera;
            });
        }

        private static void LoadOrThrowConfigs(GameInstaller installer)
        {
            installer.gameConfig       = ConfigLoader.LoadOrThrow(installer.gameConfig,       "Data/GameConfig",       nameof(installer.gameConfig));
            installer.animationConfig  = ConfigLoader.LoadOrThrow(installer.animationConfig,  "Data/AnimationConfig",  nameof(installer.animationConfig));
            installer.levelConfig      = ConfigLoader.LoadOrThrow(installer.levelConfig,      "Data/LevelConfig",      nameof(installer.levelConfig));
            installer.audioConfig      = ConfigLoader.LoadOrThrow(installer.audioConfig,      "Data/AudioConfig",      nameof(installer.audioConfig));

            // These two are tolerated missing (developer tools) but still
            // get a default-instance fallback so downstream code never sees null.
            installer.streamVFXConfig  = ConfigLoader.LoadOrThrow(installer.streamVFXConfig, "Data/StreamVFXConfig", nameof(installer.streamVFXConfig));
            installer.economyConfig    = ConfigLoader.LoadOrThrow(installer.economyConfig,   "Data/EconomyConfig",   nameof(installer.economyConfig));

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
