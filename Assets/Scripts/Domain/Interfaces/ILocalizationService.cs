using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Interfaces
{
    /// <summary>
    /// Çoklu dil desteği arayüzü.
    /// </summary>
    public interface ILocalizationService
    {
        SupportedLanguage CurrentLanguage { get; set; }
        string GetString(string key);
        void SetLanguage(SupportedLanguage language);
        void AddTranslation(string key, SupportedLanguage language, string value);

        /// <summary>
        /// Returns the localized string for <paramref name="key"/>, or
        /// <paramref name="fallback"/> if the key is missing or empty.
        /// Prefer this overload in UI code so missing translations never
        /// surface as blank labels.
        /// </summary>
        string GetStringOrDefault(string key, string fallback);
    }
}