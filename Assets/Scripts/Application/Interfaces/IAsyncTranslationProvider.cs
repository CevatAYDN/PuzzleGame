using System.Threading;
using System.Threading.Tasks;
using PuzzleGame.Domain.Interfaces;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Marker for ITranslationProvider impls that need async preload before
    /// the synchronous Load() call can succeed. Used on Android where
    /// StreamingAssets lives inside the APK (jar://path) and requires UnityWebRequest.
    ///
    /// Sprint #15: Closes the Android gap documented in JsonTranslationProvider's
    /// comment. Sync providers (HardcodedTranslationProvider, JsonTranslationProvider)
    /// do NOT implement this — they load on demand from File.ReadAllText.
    /// </summary>
    public interface IAsyncTranslationProvider : ITranslationProvider
    {
        Task LoadAsync(CancellationToken ct = default);
    }
}
