using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Domain.Services
{
    /// <summary>
    /// Localization service backed by an injected ITranslationProvider.
    ///
    /// Sprint #12: 53 keys × 5 languages externalized to
    /// Assets/StreamingAssets/Localization/translations.json via JsonTranslationProvider.
    /// Designers/translators can update copy without recompiling. Service is now
    /// pure logic (key lookup, fallback chain) — zero embedded data.
    ///
    /// Fallback chain: current language → English → key-as-is.
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private readonly ITranslationProvider _provider;
        private Dictionary<string, Dictionary<SupportedLanguage, string>> _translations;
        private SupportedLanguage _currentLanguage;

        public SupportedLanguage CurrentLanguage
        {
            get => _currentLanguage;
            set => SetLanguage(value);
        }

        /// <param name="defaultLanguage">Initial active language.</param>
        /// <param name="provider">
        /// Translation source. Inject JsonTranslationProvider for production
        /// (Editor/PC) or StreamingAssetsJsonTranslationProvider (Android).
        /// The actual Load() is deferred to the first GetString call.
        /// </param>
        public LocalizationService(SupportedLanguage defaultLanguage, ITranslationProvider provider)
        {
            _currentLanguage = defaultLanguage;
            _provider = provider;
        }

        public string GetString(string key)
        {
            EnsureLoaded();

            if (_translations.TryGetValue(key, out var languageMap) &&
                languageMap.TryGetValue(_currentLanguage, out var value))
            {
                return value;
            }

            if (languageMap != null && languageMap.TryGetValue(SupportedLanguage.English, out var fallback))
            {
                return fallback;
            }

            return key;
        }

        public string GetStringOrDefault(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key)) return fallback;
            var value = GetString(key);
            // Treat "key-as-is" fallback (i.e. missing translation) as a miss so
            // callers never see a raw translation key in the UI.
            if (string.IsNullOrEmpty(value) || value == key)
                return fallback;
            return value;
        }

        public void SetLanguage(SupportedLanguage language)
        {
            if (_currentLanguage != language)
            {
                _currentLanguage = language;
            }
        }

        public void AddTranslation(string key, SupportedLanguage language, string value)
        {
            EnsureLoaded();
            if (!_translations.ContainsKey(key))
            {
                _translations[key] = new Dictionary<SupportedLanguage, string>();
            }
            _translations[key][language] = value;
        }

        private void EnsureLoaded()
        {
            if (_translations != null) return;

            var data = _provider.Load();
            _translations = new Dictionary<string, Dictionary<SupportedLanguage, string>>(data.Count);
            foreach (var kvp in data)
            {
                _translations[kvp.Key] = new Dictionary<SupportedLanguage, string>(kvp.Value);
            }
        }
    }
}
