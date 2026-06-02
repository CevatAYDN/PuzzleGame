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
    }
}