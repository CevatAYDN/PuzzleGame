using UnityEngine;
using Nexus.Core;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Services;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Services;
using PuzzleGame.Infrastructure.Providers;
using PuzzleGame.Infrastructure.Implementations;

namespace PuzzleGame.Installers
{
    /// <summary>
    /// Registers localization services.
    /// Platform-aware: Android uses async StreamingAssets loading,
    /// others use sync File.ReadAllText.
    /// </summary>
    internal static class LocalizationInstallerModule
    {
        public static void Configure(IContextBuilder builder, NexusDI container)
        {
            // JsonTranslationProvider is the default sync loader
            builder.Bind<ITranslationProvider, JsonTranslationProvider>();

            // LocalizationService uses SupportedLanguage enum (value type). 
            // We use BindFactory to support instantiation and caching (Singleton behavior).
            ILocalizationService localizationService = null;
            container.BindFactory<ILocalizationService>(() =>
            {
                if (localizationService == null)
                {
                    var provider = container.Resolve<ITranslationProvider>();
                    localizationService = new LocalizationService(SupportedLanguage.Turkish, provider);
                }
                return localizationService;
            });

#if UNITY_ANDROID && !UNITY_EDITOR
            // Android override: sync File.ReadAllText on StreamingAssets returns 0 bytes.
            // Register the provider and point the interfaces to the same resolved instance.
            builder.Bind<StreamingAssetsJsonTranslationProvider>();
            
            container.BindFactory<ITranslationProvider>(() => container.Resolve<StreamingAssetsJsonTranslationProvider>());
            container.BindFactory<IAsyncTranslationProvider>(() => container.Resolve<StreamingAssetsJsonTranslationProvider>());

            var bootstrapGo = new GameObject("LocalizationBootstrap");
            var bootstrap = bootstrapGo.AddComponent<LocalizationBootstrap>();
            builder.BindInstance(bootstrap);
#endif
        }
    }
}
