using System;
using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Domain.Services
{
    /// <summary>
    /// Basit localization servisi.
    /// Dictionary tabanlı, temiz ve test edilebilir.
    ///
    /// Fix #17: Accepts an optional ITranslationProvider for data-driven translations.
    /// When no provider is supplied, falls back to the built-in hardcoded strings
    /// (backward compatible). To add new languages, inject a custom ITranslationProvider.
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private readonly Dictionary<string, Dictionary<SupportedLanguage, string>> _translations = new();
        private SupportedLanguage _currentLanguage;

        public SupportedLanguage CurrentLanguage
        {
            get => _currentLanguage;
            set => SetLanguage(value);
        }

        /// <param name="defaultLanguage">Initial language.</param>
        /// <param name="provider">
        /// Optional translation provider. If null, built-in hardcoded strings are used.
        /// Injecting a provider (e.g. JSON file loader) follows OCP — no code change needed
        /// to add new languages.
        /// </param>
        public LocalizationService(
            SupportedLanguage defaultLanguage = SupportedLanguage.Turkish,
            ITranslationProvider provider = null)
        {
            _currentLanguage = defaultLanguage;

            if (provider != null)
            {
                var data = provider.Load();
                foreach (var kvp in data)
                {
                    _translations[kvp.Key] = new Dictionary<SupportedLanguage, string>(kvp.Value);
                }
            }
            else
            {
                LoadDefaultTranslations();
            }
        }

        public string GetString(string key)
        {
            if (_translations.TryGetValue(key, out var languageMap) &&
                languageMap.TryGetValue(_currentLanguage, out var value))
            {
                return value;
            }

            // Fallback to English
            if (languageMap != null && languageMap.TryGetValue(SupportedLanguage.English, out var fallback))
            {
                return fallback;
            }

            return key; // Return key as fallback
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
            if (!_translations.ContainsKey(key))
            {
                _translations[key] = new Dictionary<SupportedLanguage, string>();
            }
            _translations[key][language] = value;
        }

        private void LoadDefaultTranslations()
        {
            // Türkçe
            AddTranslation("moves_text", SupportedLanguage.Turkish, "Hamle");
            AddTranslation("level_complete", SupportedLanguage.Turkish, "Seviye Tamamlandı!");
            AddTranslation("undo", SupportedLanguage.Turkish, "Geri Al");
            AddTranslation("restart", SupportedLanguage.Turkish, "Yeniden Başlat");
            AddTranslation("menu", SupportedLanguage.Turkish, "Ana Menü");

            // İngilizce
            AddTranslation("moves_text", SupportedLanguage.English, "Moves");
            AddTranslation("level_complete", SupportedLanguage.English, "Level Complete!");
            AddTranslation("undo", SupportedLanguage.English, "Undo");
            AddTranslation("restart", SupportedLanguage.English, "Restart");
            AddTranslation("menu", SupportedLanguage.English, "Main Menu");

            // Almanca
            AddTranslation("moves_text", SupportedLanguage.German, "Züge");
            AddTranslation("level_complete", SupportedLanguage.German, "Level geschafft!");
            AddTranslation("undo", SupportedLanguage.German, "Rückgängig");
            AddTranslation("restart", SupportedLanguage.German, "Neustart");
            AddTranslation("menu", SupportedLanguage.German, "Hauptmenü");

            // İspanyolca
            AddTranslation("moves_text", SupportedLanguage.Spanish, "Movimientos");
            AddTranslation("level_complete", SupportedLanguage.Spanish, "¡Nivel completado!");
            AddTranslation("undo", SupportedLanguage.Spanish, "Deshacer");
            AddTranslation("restart", SupportedLanguage.Spanish, "Reiniciar");
            AddTranslation("menu", SupportedLanguage.Spanish, "Menú principal");

            // Fransızca
            AddTranslation("moves_text", SupportedLanguage.French, "Coups");
            AddTranslation("level_complete", SupportedLanguage.French, "Niveau terminé !");
            AddTranslation("undo", SupportedLanguage.French, "Annuler");
            AddTranslation("restart", SupportedLanguage.French, "Redémarrer");
            AddTranslation("menu", SupportedLanguage.French, "Menu principal");
        }
    }
}