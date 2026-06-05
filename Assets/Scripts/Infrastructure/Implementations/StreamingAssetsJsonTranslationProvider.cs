using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;
using UnityEngine.Networking;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Android-safe loader for translations.json. Uses UnityWebRequest because
    /// StreamingAssets on Android is compressed inside the APK and File.ReadAllText
    /// returns 0 bytes. Editor and Standalone use the sync JsonTranslationProvider
    /// instead (no need for the overhead).
    ///
    /// Lifecycle: call LoadAsync() at app startup (LocalizationBootstrap). Load()
    /// returns the cached dictionary once preload completes; throws if called too early.
    /// </summary>
    public class StreamingAssetsJsonTranslationProvider : IAsyncTranslationProvider
    {
        private readonly string _url;
        private IReadOnlyDictionary<string, Dictionary<SupportedLanguage, string>> _cache;

        public StreamingAssetsJsonTranslationProvider(string relativePath = "Localization/translations.json")
        {
            _url = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, relativePath);
        }

        public async Task LoadAsync(CancellationToken ct = default)
        {
            using var req = UnityWebRequest.Get(_url);
            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                if (ct.IsCancellationRequested) return;
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException($"[StreamingAssetsJson] Failed to load {_url}: {req.error}");

            _cache = JsonTranslationProvider.Parse(req.downloadHandler.text);
        }

        public IReadOnlyDictionary<string, Dictionary<SupportedLanguage, string>> Load()
        {
            if (_cache == null)
                throw new InvalidOperationException("Call LoadAsync() first and await completion.");
            return _cache;
        }
    }
}
