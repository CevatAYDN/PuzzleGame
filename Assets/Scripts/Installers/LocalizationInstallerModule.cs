using VContainer;
using UnityEngine;
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
        public static void Configure(IContainerBuilder builder)
        {
            // Sprint #12 + #15: JsonTranslationProvider is the default sync loader
            // (Editor + Standalone: File.ReadAllText works). On Android the
            // StreamingAssets path lives inside the APK and requires UnityWebRequest,
            // so the platform-specific async provider is registered instead and the
            // LocalizationBootstrap MonoBehaviour is created to preload it.
            builder.Register<ITranslationProvider>(resolver => new JsonTranslationProvider(), Lifetime.Singleton);
            builder.Register<ILocalizationService, LocalizationService>(Lifetime.Singleton)
                   .WithParameter(SupportedLanguage.Turkish);

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
        }
    }
}
