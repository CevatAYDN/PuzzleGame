using System.Collections.Generic;
using PuzzleGame.Domain.Models;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// Default ITranslationProvider implementation.
    /// Provides the same hardcoded strings that were previously embedded in LocalizationService.
    ///
    /// Fix #17 / Group 5: ITranslationProvider is no longer orphaned — this implementation
    /// satisfies the interface contract. To add new languages or load from JSON/Assets,
    /// create an alternative ITranslationProvider and inject it via DI.
    /// </summary>
    public class HardcodedTranslationProvider : ITranslationProvider
    {
        public IReadOnlyDictionary<string, Dictionary<SupportedLanguage, string>> Load()
        {
            var result = new Dictionary<string, Dictionary<SupportedLanguage, string>>();

            void Add(string key, SupportedLanguage lang, string value)
            {
                if (!result.ContainsKey(key))
                    result[key] = new Dictionary<SupportedLanguage, string>();
                result[key][lang] = value;
            }

            // Turkish
            Add("moves_text",    SupportedLanguage.Turkish, "Hamle");
            Add("level_complete",SupportedLanguage.Turkish, "Seviye Tamamlandı!");
            Add("undo",          SupportedLanguage.Turkish, "Geri Al");
            Add("restart",       SupportedLanguage.Turkish, "Yeniden Başlat");
            Add("menu",          SupportedLanguage.Turkish, "Ana Menü");

            // English
            Add("moves_text",    SupportedLanguage.English, "Moves");
            Add("level_complete",SupportedLanguage.English, "Level Complete!");
            Add("undo",          SupportedLanguage.English, "Undo");
            Add("restart",       SupportedLanguage.English, "Restart");
            Add("menu",          SupportedLanguage.English, "Main Menu");

            // German
            Add("moves_text",    SupportedLanguage.German, "Züge");
            Add("level_complete",SupportedLanguage.German, "Level geschafft!");
            Add("undo",          SupportedLanguage.German, "Rückgängig");
            Add("restart",       SupportedLanguage.German, "Neustart");
            Add("menu",          SupportedLanguage.German, "Hauptmenü");

            // Spanish
            Add("moves_text",    SupportedLanguage.Spanish, "Movimientos");
            Add("level_complete",SupportedLanguage.Spanish, "¡Nivel completado!");
            Add("undo",          SupportedLanguage.Spanish, "Deshacer");
            Add("restart",       SupportedLanguage.Spanish, "Reiniciar");
            Add("menu",          SupportedLanguage.Spanish, "Menú principal");

            // French
            Add("moves_text",    SupportedLanguage.French, "Coups");
            Add("level_complete",SupportedLanguage.French, "Niveau terminé !");
            Add("undo",          SupportedLanguage.French, "Annuler");
            Add("restart",       SupportedLanguage.French, "Redémarrer");
            Add("menu",          SupportedLanguage.French, "Menu principal");

            return result;
        }
    }
}
